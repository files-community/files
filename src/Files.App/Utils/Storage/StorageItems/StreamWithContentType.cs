// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Files.App.Utils.Storage
{
	public sealed class InputStreamWithDisposeCallback : IInputStream
	{
		private Stream stream;
		private IInputStream iStream;
		public Action DisposeCallback { get; set; }

		public InputStreamWithDisposeCallback(Stream stream)
		{
			this.stream = stream;
			iStream = stream.AsInputStream();
		}

		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			return iStream.ReadAsync(buffer, count, options);
		}

		public void Dispose()
		{
			iStream.Dispose();
			stream.Dispose();
			DisposeCallback?.Invoke();
		}
	}

	public sealed class NonSeekableRandomAccessStreamForWrite : IRandomAccessStream
	{
		private Stream stream;
		private IOutputStream oStream;
		private IRandomAccessStream imrac;
		private ulong byteSize;
		private bool isWritten;

		public Action DisposeCallback { get; set; }

		public NonSeekableRandomAccessStreamForWrite(Stream stream)
		{
			this.stream = stream;
			oStream = stream.AsOutputStream();
			imrac = new InMemoryRandomAccessStream();
		}

		public IInputStream GetInputStreamAt(ulong position)
		{
			throw new NotSupportedException();
		}

		public IOutputStream GetOutputStreamAt(ulong position)
		{
			if (position != 0)
			{
				throw new NotSupportedException();
			}
			return this;
		}

		public void Seek(ulong position)
		{
			if (position != 0)
			{
				throw new NotSupportedException();
			}
		}

		public IRandomAccessStream CloneStream() => throw new NotSupportedException();

		public bool CanRead => false;

		public bool CanWrite => true;

		public ulong Position => byteSize;

		public ulong Size
		{
			get => byteSize;
			set => throw new NotSupportedException();
		}

		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			throw new NotSupportedException();
		}

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
		{
			Func<CancellationToken, IProgress<uint>, Task<uint>> taskProvider =
				async (token, progress) =>
				{
					var res = await oStream.WriteAsync(buffer);
					byteSize += res;
					return res;
				};

			return AsyncInfo.Run(taskProvider);
		}

		public IAsyncOperation<bool> FlushAsync()
		{
			if (isWritten)
			{
				return imrac.FlushAsync();
			}

			isWritten = true;

			return AsyncInfo.Run<bool>(async (cancellationToken) =>
			{
				await stream.FlushAsync();
				return true;
			});
		}

		public void Dispose()
		{
			oStream.Dispose();
			stream.Dispose();
			imrac.Dispose();
			DisposeCallback?.Invoke();
		}
	}

	public sealed class NonSeekableRandomAccessStreamForRead : IRandomAccessStream
	{
		private Stream stream;
		private IRandomAccessStream imrac;
		private ulong virtualPosition;
		private ulong readToByte;
		private ulong byteSize;

		public Action DisposeCallback { get; set; }

		public NonSeekableRandomAccessStreamForRead(Stream baseStream, ulong size)
		{
			stream = baseStream;
			imrac = new InMemoryRandomAccessStream();
			virtualPosition = 0;
			readToByte = 0;
			byteSize = size;
		}

		public IInputStream GetInputStreamAt(ulong position)
		{
			Seek(position);
			return this;
		}

		public IOutputStream GetOutputStreamAt(ulong position)
		{
			throw new NotSupportedException();
		}

		public void Seek(ulong position)
		{
			imrac.Size = Math.Max(imrac.Size, position);
			virtualPosition = position;
		}

		public IRandomAccessStream CloneStream() => throw new NotSupportedException();

		public bool CanRead => true;

		public bool CanWrite => false;

		public ulong Position => virtualPosition;

		public ulong Size
		{
			get => byteSize;
			set => throw new NotSupportedException();
		}

		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			Func<CancellationToken, IProgress<uint>, Task<IBuffer>> taskProvider =
				async (token, progress) =>
				{
					int read;
					var tempBuffer = new byte[16384];
					imrac.Seek(readToByte);
					while (imrac.Position < virtualPosition + count)
					{
						read = await stream.ReadAsync(tempBuffer, 0, tempBuffer.Length);
						if (read == 0)
						{
							break;
						}
						await imrac.WriteAsync(tempBuffer.AsBuffer(0, read));
					}
					readToByte = imrac.Position;

					imrac.Seek(virtualPosition);
					var res = await imrac.ReadAsync(buffer, count, options);
					virtualPosition = imrac.Position;
					return res;
				};

			return AsyncInfo.Run(taskProvider);
		}

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
		{
			throw new NotSupportedException();
		}

		public IAsyncOperation<bool> FlushAsync()
		{
			return imrac.FlushAsync();
		}

		public void Dispose()
		{
			stream.Dispose();
			imrac.Dispose();
			DisposeCallback?.Invoke();
		}
	}

	public sealed class StreamWithContentType : IRandomAccessStreamWithContentType
	{
		private IRandomAccessStream baseStream;

		public StreamWithContentType(IRandomAccessStream stream)
		{
			baseStream = stream;
		}

		public IInputStream GetInputStreamAt(ulong position) => baseStream.GetInputStreamAt(position);

		public IOutputStream GetOutputStreamAt(ulong position) => baseStream.GetOutputStreamAt(position);

		public void Seek(ulong position) => baseStream.Seek(position);

		public IRandomAccessStream CloneStream() => baseStream.CloneStream();

		public bool CanRead => baseStream.CanRead;

		public bool CanWrite => baseStream.CanWrite;

		public ulong Position => baseStream.Position;

		public ulong Size { get => baseStream.Size; set => baseStream.Size = value; }

		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
		{
			return baseStream.ReadAsync(buffer, count, options);
		}

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => baseStream.WriteAsync(buffer);

		public IAsyncOperation<bool> FlushAsync() => baseStream.FlushAsync();

		public void Dispose()
		{
			baseStream.Dispose();
		}

		public string ContentType { get; set; } = "application/octet-stream";
	}

	public sealed class ComStreamWrapper : Stream
	{
		private IStream iStream;
		private STATSTG iStreamStat;

		public ComStreamWrapper(IStream stream)
		{
			iStream = stream;
			iStream.Stat(out iStreamStat, 0);
		}

		public override bool CanRead => true;

		public override bool CanSeek => true;

		public override bool CanWrite => false;

		public override long Length => iStreamStat.cbSize;

		public override long Position
		{
			get => Seek(0, SeekOrigin.Current);
			set => Seek(value, SeekOrigin.Begin);
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (offset != 0)
				throw new NotSupportedException();
			unsafe
			{
				int newPos = 0;
				iStream.Read(buffer, count, new IntPtr(&newPos));
				return (int)newPos;
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			unsafe
			{
				long newPos = 0;
				iStream.Seek(0, (int)origin, new IntPtr(&newPos));
				return newPos;
			}
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Marshal.ReleaseComObject(iStream);
		}
	}
}
