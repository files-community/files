// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Dispatching;

namespace Files.App.Services
{
	internal sealed class ThreadingService : IThreadingService
	{
		private readonly DispatcherQueue _dispatcherQueue;

		public ThreadingService()
		{
			_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
		}

		public Task ExecuteOnUiThreadAsync(Action action)
		{
			return _dispatcherQueue.EnqueueOrInvokeAsync(action);
		}

		public Task<TResult?> ExecuteOnUiThreadAsync<TResult>(Func<TResult?> func)
		{
			return _dispatcherQueue.EnqueueOrInvokeAsync<TResult?>(func);
		}
	}
}
