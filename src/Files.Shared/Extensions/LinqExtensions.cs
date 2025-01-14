// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Files.Shared.Extensions
{
	public static class LinqExtensions
	{
		/// <summary>
		/// Determines whether <paramref name="enumerable"/> is empty or not.
		/// <br/><br/>
		/// Remarks:
		/// <br/>
		/// This function is faster than enumerable.Count == 0 since it'll only iterate one element instead of all elements.
		/// <br/>
		/// This function is null-safe.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="enumerable"></param>
		/// <returns></returns>
		public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
		{
			return enumerable is null || !enumerable.Any();
		}

		public static TOut? Get<TOut, TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TOut? defaultValue = default) where TKey : notnull
		{
			if (dictionary is null || key is null)
				return defaultValue;

			if (dictionary is ConcurrentDictionary<TKey, TValue> cDict)
			{
				if (!cDict.ContainsKey(key))
				{
					if (defaultValue is TValue value)
						cDict.TryAdd(key, value);

					return defaultValue;
				}
			}
			else
			{
				lock (dictionary)
				{
					if (!dictionary.ContainsKey(key))
					{
						if (defaultValue is TValue value)
							dictionary.Add(key, value);

						return defaultValue;
					}
				}
			}

			if (dictionary[key] is TOut o)
				return o;

			return defaultValue;
		}

		public static Task<TValue?> GetAsync<TKey, TValue>(this IDictionary<TKey, Task<TValue?>> dictionary, TKey key, Func<Task<TValue?>> defaultValueFunc) where TKey : notnull
		{
			if (dictionary is null || key is null)
				return defaultValueFunc();

			if (dictionary is ConcurrentDictionary<TKey, Task<TValue?>> cDict)
			{
				if (!cDict.ContainsKey(key))
				{
					var defaultValue = defaultValueFunc();
					if (defaultValue is Task<TValue?> value)
						cDict.TryAdd(key, value);

					return defaultValue;
				}
			}
			else
			{
				lock (dictionary)
				{
					if (!dictionary.ContainsKey(key))
					{
						var defaultValue = defaultValueFunc();
						if (defaultValue is Task<TValue?> value)
							dictionary.Add(key, value);

						return defaultValue;
					}
				}
			}

			return dictionary[key];
		}

		public static async Task<IEnumerable<T>> WhereAsync<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
		{
			var results = await Task.WhenAll(source.Select(async x => (x, await predicate(x))));

			return results.Where(x => x.Item2).Select(x => x.x);
		}

		public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> source, IEnumerable<TSource> other, Func<TSource, TKey> keySelector)
		{
			var set = new HashSet<TKey>(other.Select(keySelector));

			foreach (var item in source)
			{
				if (set.Add(keySelector(item)))
					yield return item;
			}
		}

		public static IEnumerable<T> IntersectBy<T, TKey>(this IEnumerable<T> items, IEnumerable<T> others, Func<T, TKey> keySelector)
		{
			return items.Join(others.Select(keySelector), keySelector, id => id, (o, id) => o);
		}

		/// <summary>
		/// Enumerates through <see cref="IEnumerable{T}"/> of elements and executes <paramref name="action"/>
		/// </summary>
		/// <typeparam name="T">Element of <paramref name="collection"/></typeparam>
		/// <param name="collection">The collection to enumerate through</param>
		/// <param name="action">The action to take every element</param>
		public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
		{
			foreach (T value in collection)
				action(value);
		}

		public static int AddSorted<T>(this IList<T> list, T item) where T : IComparable<T>
		{
			if (!list.Any() || list.Last().CompareTo(item) <= 0)
			{
				list.Add(item);

				return list.Count;
			}

			if (list[0].CompareTo(item) >= 0)
			{
				list.Insert(0, item);

				return 0;
			}

			int index = list.ToList().BinarySearch(item);
			if (index < 0)
				index = ~index;

			list.Insert(index, item);

			return index;
		}

		/// <summary>
		/// Removes all elements from the specified index to the end of the list.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="index"></param>
		public static List<T> RemoveFrom<T>(this List<T> list, int index)
		{
			if (!list.Any())
				return list;

			return index <= 0
				? []
				: list.Take(index - 1).ToList();
		}
	}
}
