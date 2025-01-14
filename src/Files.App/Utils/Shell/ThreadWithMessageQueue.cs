// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace Files.App.Utils.Shell
{
	public sealed class ThreadWithMessageQueue : Disposable
	{
		private readonly BlockingCollection<Internal> messageQueue;

		private readonly Thread thread;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				messageQueue.CompleteAdding();
				thread.Join();
				messageQueue.Dispose();
			}
		}

		public async Task<V> PostMethod<V>(Func<object> payload)
		{
			var message = new Internal(payload);
			messageQueue.TryAdd(message);

			return (V)await message.tcs.Task;
		}

		public Task PostMethod(Action payload)
		{
			var message = new Internal(payload);
			messageQueue.TryAdd(message);

			return message.tcs.Task;
		}

		public ThreadWithMessageQueue()
		{
			messageQueue = new BlockingCollection<Internal>(new ConcurrentQueue<Internal>());

			thread = new Thread(new ThreadStart(() =>
			{
				foreach (var message in messageQueue.GetConsumingEnumerable())
				{
					var res = message.payload();
					message.tcs.SetResult(res);
				}
			}));

			thread.SetApartmentState(ApartmentState.STA);

			// Do not prevent app from closing
			thread.IsBackground = true;

			thread.Start();
		}

		private sealed class Internal
		{
			public Func<object?> payload;

			public TaskCompletionSource<object> tcs;

			public Internal(Action payload)
			{
				this.payload = () => { payload(); return default; };
				tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
			}

			public Internal(Func<object?> payload)
			{
				this.payload = payload;
				tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
			}
		}
	}
}
