using System;
using System.Collections.Concurrent;

namespace FilesFullTrust.Helpers
{
    public class DisposableDictionary : IDisposable
    {
        private ConcurrentDictionary<string, object> dict;

        public DisposableDictionary()
        {
            dict = new ConcurrentDictionary<string, object>();
        }

        public string AddValue(object obj)
        {
            string key = Guid.NewGuid().ToString();
            if (!dict.TryAdd(key, obj))
            {
                throw new ArgumentException("Could not create handle: key exists");
            }

            return key;
        }

        public void SetValue(string key, object obj)
        {
            RemoveValue(key);
            if (!dict.TryAdd(key, obj))
            {
                throw new ArgumentException("Could not create handle: key exists");
            }
        }

        public object GetValue(string key)
        {
            if (dict.TryGetValue(key, out var elem))
            {
                return elem;
            }
            return null;
        }

        public T GetValue<T>(string key)
        {
            if (dict.TryGetValue(key, out var elem))
            {
                return (T)elem;
            }
            return default;
        }

        public void RemoveValue(string key)
        {
            dict.TryRemove(key, out var elem);
            (elem as IDisposable)?.Dispose();
        }

        public void Dispose()
        {
            foreach (var elem in dict)
            {
                dict.TryRemove(elem.Key, out _);
                (elem.Value as IDisposable)?.Dispose();
            }
        }
    }
}
