using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace FilesFullTrust.Helpers
{
    public class ThreadWithMessageQueue<T> : IDisposable
    {
        private BlockingCollection<Internal> messageQueue;
        private Thread thread;
        private DisposableDictionary state;

        public void Dispose()
        {
            messageQueue.CompleteAdding();
            thread.Join();
            state.Dispose();
        }

        public async Task<V> PostMessageAsync<V>(T payload)
        {
            var message = new Internal(payload);
            messageQueue.TryAdd(message);
            return (V)await message.tcs.Task;
        }

        public Task PostMessage(T payload)
        {
            var message = new Internal(payload);
            messageQueue.TryAdd(message);
            return message.tcs.Task;
        }

        public ThreadWithMessageQueue(Func<T, DisposableDictionary, object> handleMessage)
        {
            messageQueue = new BlockingCollection<Internal>(new ConcurrentQueue<Internal>());
            state = new DisposableDictionary();
            thread = new Thread(new ThreadStart(() =>
            {
                foreach (var message in messageQueue.GetConsumingEnumerable())
                {
                    var res = handleMessage(message.payload, state);
                    message.tcs.SetResult(res);
                }
            }));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private class Internal
        {
            public T payload;
            public TaskCompletionSource<object> tcs;

            public Internal(T payload)
            {
                this.payload = payload;
                this.tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }
}
