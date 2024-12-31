using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Files.App.Utils.Terminal
{
	/// <summary>
	/// Code modified from https://github.com/felixse/FluentTerminal
	/// </summary>
	internal sealed class BufferedReader : IDisposable
	{
		private const int MaxTotalDelayMilliseconds = 100;
		private const int WaitPeriodMilliseconds = 30;
		private const int NearReadsPeriodMilliseconds = 20;
		private const int NearReadsBufferingTrigger = 3;
		private const int BufferSize = 204800;

		private readonly object _lock = new object();

		private readonly Stream _stream;
		private readonly Action<byte[]> _callback;
		private readonly bool _enableBuffer;

		private bool _disposed;
		private bool _paused;

		private byte[] _buffer;
		private int _bufferIndex;
		private DateTime _lastRead;
		private DateTime _sendingDeadline;
		private DateTime _scheduledSend;
		private int _nearReadingsCount;
		private Task _sendingTask;

		internal BufferedReader(Stream stream, Action<byte[]> callback, bool enableBuffer)
		{
			_stream = stream;
			_callback = callback;
			_enableBuffer = enableBuffer;

			// ReSharper disable once AssignmentIsFullyDiscarded
			_ = Task.Factory.StartNew(ReadingLoop, TaskCreationOptions.LongRunning);
		}

		internal void SetPaused(bool value)
		{
			lock (_lock) _paused = value;
		}

		public void Dispose()
		{
			lock (_lock) _disposed = true;
		}

		private async Task ReadingLoop()
		{
			while (true)
			{
				// Allow CPU to jump between TerminalSessions' ReadingLoop Tasks 
				await Task.Delay(1).ConfigureAwait(false);

				bool paused;

				lock (_lock)
				{
					if (_disposed) return;

					paused = _paused;
				}

				if (paused)
				{
					await Task.Delay(WaitPeriodMilliseconds).ConfigureAwait(false);

					continue;
				}

				var currentBuffer = new byte[BufferSize];

				int read;

				try
				{
					read = await _stream.ReadAsync(currentBuffer, 0, currentBuffer.Length).ConfigureAwait(false);
				}
				catch
				{
					read = 0;
				}

				if (read < 1)
				{
					// Expected to happen only when terminal is closed.
					// Probably not recoverable, but we'll wait anyway 'till disposed.
					await Task.Delay(50).ConfigureAwait(false);

					continue;
				}

				if (!_enableBuffer)
				{
					// Buffering disabled. Just send.

					_buffer = currentBuffer;
					_bufferIndex = read;

					SendBuffer();

					continue;
				}

				var now = DateTime.UtcNow;

				lock (_lock)
				{
					if (_buffer != null)
					{
						// We're already in buffered mode

						if (_bufferIndex + read > BufferSize)
						{
							// No room in the buffer. Have to flush it.
							SendBuffer();

							_buffer = currentBuffer;
							_bufferIndex = 0;

							_scheduledSend = now.AddMilliseconds(WaitPeriodMilliseconds);
							_sendingDeadline = now.AddMilliseconds(MaxTotalDelayMilliseconds);
						}
						else
						{
							// Copy to existing buffer
							Buffer.BlockCopy(currentBuffer, 0, _buffer, _bufferIndex, read);
							_bufferIndex += read;

							_scheduledSend = now.AddMilliseconds(WaitPeriodMilliseconds);
						}

						if (now.Subtract(_lastRead).TotalMilliseconds < NearReadsPeriodMilliseconds)
						{
							// We should stop buffered mode
							SendBuffer();
						}

						_lastRead = now;

						continue;
					}

					if (now.Subtract(_lastRead).TotalMilliseconds < NearReadsPeriodMilliseconds)
					{
						_nearReadingsCount++;
					}
					else
					{
						_nearReadingsCount = 0;
					}

					_lastRead = now;

					if (_nearReadingsCount >= NearReadsBufferingTrigger)
					{
						// We should enter buffered mode
						_buffer = currentBuffer;
						_bufferIndex = 0;
						_sendingDeadline = now.AddMilliseconds(MaxTotalDelayMilliseconds);
						_scheduledSend = now.AddMilliseconds(WaitPeriodMilliseconds);

						if (_sendingTask == null) _sendingTask = SendAsync();

						_nearReadingsCount = 0;

						continue;
					}

					// Not in buffering mode. Just send.
					_buffer = currentBuffer;
					_bufferIndex = read;

					SendBuffer();
				}
			}
		}

		private async Task SendAsync()
		{
			// Just to release the calling thread asap
			await Task.Delay(5).ConfigureAwait(false);

			while (true)
			{
				TimeSpan sleep;

				lock (_lock)
				{
					if (_buffer == null)
					{
						_sendingTask = null;

						return;
					}

					if (_paused)
					{
						sleep = TimeSpan.FromMilliseconds(WaitPeriodMilliseconds);
					}
					else
					{
						sleep = _scheduledSend < _sendingDeadline
							? _scheduledSend.Subtract(DateTime.UtcNow)
							: _sendingDeadline.Subtract(DateTime.UtcNow);

						if (sleep.TotalMilliseconds < 5)
						{
							// Time to send
							SendBuffer();

							_sendingTask = null;

							return;
						}
					}
				}

				await Task.Delay(sleep).ConfigureAwait(false);
			}
		}

		// Has to be called from a locked code!
		private void SendBuffer()
		{
			if (_bufferIndex == _buffer.Length)
			{
				_callback(_buffer);
			}
			else
			{
				var newBuffer = new byte[_bufferIndex];

				Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _bufferIndex);

				_callback(newBuffer);
			}

			_buffer = null;
			_bufferIndex = 0;
		}
	}
}
