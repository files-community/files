// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	[DebuggerTypeProxy(typeof(CollectionDebugView<>))]
	[DebuggerDisplay("Count = {Count}")]
	public sealed class ConcurrentCollection<T> : ICollection<T>, IList<T>, ICollection, IList
	{
		private readonly object syncRoot = new object();
		
		private readonly List<T> collection = [];

		public int Count
		{
			get
			{
				lock (syncRoot)
				{
					return collection.Count;
				}
			}
		}

		public bool IsReadOnly => false;

		public bool IsFixedSize => false;

		public bool IsSynchronized => true;

		public object SyncRoot => syncRoot;

		object? IList.this[int index]
		{
			get
			{
				return this[index];
			}
			set
			{
				if (value is not null)
					this[index] = (T)value;
			}
		}

		public T this[int index]
		{
			get
			{
				lock (syncRoot)
				{
					return collection[index];
				}
			}
			set
			{
				T item;
				lock (syncRoot)
				{
					item = collection[index];
					collection[index] = value;
				}
			}
		}

		public ConcurrentCollection() { }

		public ConcurrentCollection(IEnumerable<T> items)
		{
			AddRange(items);
		}

		public void Add(T? item)
		{
			if (item is null)
				return;

			lock (syncRoot)
			{
				collection.Add(item);
			}
		}

		public void Clear()
		{
			lock (syncRoot)
			{
				collection.Clear();
			}
		}

		public bool Contains(T? item)
		{
			if (item is null)
				return false;

			lock (syncRoot)
			{
				return collection.Contains(item);
			}
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			lock (syncRoot)
			{
				collection.CopyTo(array, arrayIndex);
			}
		}

		public bool Remove(T? item)
		{
			if (item is null)
				return false;

			lock (syncRoot)
			{
				var index = collection.IndexOf(item);

				if (index == -1)
					return false;

				collection.RemoveAt(index);
			}

			return true;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new BlockingListEnumerator<T>(collection, syncRoot);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int IndexOf(T? item)
		{
			if (item is null)
				return -1;

			lock (syncRoot)
			{
				return collection.IndexOf(item);
			}
		}

		public void Insert(int index, T? item)
		{
			if (item is null)
				return;

			lock (syncRoot)
			{
				collection.Insert(index, item);
			}
		}

		public void RemoveAt(int index)
		{
			lock (syncRoot)
			{
				collection.RemoveAt(index);
			}
		}

		public void AddRange(IEnumerable<T> items)
		{
			if (!items.Any())
				return;

			lock (syncRoot)
			{
				collection.AddRange(items);
			}
		}

		public void InsertRange(int index, IEnumerable<T> items)
		{
			if (!items.Any())
				return;

			lock (syncRoot)
			{
				collection.InsertRange(index, items);
			}
		}

		public void RemoveRange(int index, int count)
		{
			if (count <= 0)
				return;

			lock (syncRoot)
			{
				collection.RemoveRange(index, count);
			}
		}

		public void ReplaceRange(int index, IEnumerable<T> items)
		{
			var count = items.Count();

			if (count == 0)
				return;

			lock (syncRoot)
			{
				collection.RemoveRange(index, count);
				collection.InsertRange(index, items.ToList());
			}
		}

		public void Sort()
		{
			lock (syncRoot)
			{
				collection.Sort();
			}
		}

		public void Sort(Comparison<T> comparison)
		{
			lock (syncRoot)
			{
				collection.Sort(comparison);
			}
		}

		public void Order(Func<List<T>, IEnumerable<T>> func)
		{
			IEnumerable<T> result;

			lock (syncRoot)
			{
				result = func.Invoke(collection);
			}

			ReplaceRange(0, result);
		}

		public void OrderOne(Func<List<T>, IEnumerable<T>> func, T item)
		{
			List<T> result;

			lock (syncRoot)
			{
				result = func.Invoke(collection).ToList();
			}

			Remove(item);

			var index = result.IndexOf(item);
			if (index != -1)
				Insert(index, item);
		}

		public List<T> ToList()
		{
			lock (syncRoot)
			{
				return Enumerable.ToList(this);
			}
		}

		int IList.Add(object? value)
		{
			if (value is null)
				return -1;

			int index;

			lock (syncRoot)
			{
				index = ((IList)collection).Add((T)value);
			}

			return index;
		}

		bool IList.Contains(object? value)
		{
			return Contains((T?)value);
		}

		int IList.IndexOf(object? value)
		{
			return IndexOf((T?)value);
		}

		void IList.Insert(int index, object? value)
		{
			Insert(index, (T?)value);
		}

		void IList.Remove(object? value)
		{
			Remove((T?)value);
		}

		void ICollection.CopyTo(Array array, int index)
		{
			CopyTo((T[])array, index);
		}
	}
}
