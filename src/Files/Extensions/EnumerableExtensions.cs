﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Files.Extensions
{
    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Creates <see cref="List{T}"/> and returns <see cref="IEnumerable{T}"/> with provided <paramref name="item"/>
        /// </summary>
        /// <typeparam name="T">The item type</typeparam>
        /// <param name="item">The item</param>
        /// <returns><see cref="IEnumerable{T}"/> with <paramref name="item"/></returns>
        internal static IEnumerable<T> CreateEnumerable<T>(this T item) =>
            new[] { item };

        internal static List<T> CreateList<T>(this T item) =>
            new List<T>() { item };

        public static IList<T> AddIfNotPresent<T>(this IList<T> list, T element)
        {
            if (!list.Contains(element))
            {
                list.Add(element);
            }
            return list;
        }

        public static IDictionary<TKey, TValue> AddIfNotPresent<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
            }
            return dictionary;
        }

        /// <summary>
        /// Executes given lambda parallelly on given data set with max degree of parallelism set up
        /// </summary>
        /// <typeparam name="T">The item type</typeparam>
        /// <param name="source">Data to process</param>
        /// <param name="body">Lambda to execute on all items</param>
        /// <param name="maxDegreeOfParallelism">Max degree of parallelism (-1 for unbounded execution)</param>
        /// <returns></returns>
        /// <param name="cts">Cancellation token, stops all remaining operations</param>
        /// <param name="scheduler">Task scheduler on which to execute `body`</param>
        /// <returns></returns>
        public static async Task ParallelForEach<T>(this IEnumerable<T> source, Func<T, Task> body, int maxDegreeOfParallelism = DataflowBlockOptions.Unbounded, CancellationToken cts = default, TaskScheduler scheduler = null)
        {
            var options = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cts
            };
            if (scheduler != null)
                options.TaskScheduler = scheduler;

            var block = new ActionBlock<T>(body, options);

            foreach (var item in source)
                block.Post(item);

            block.Complete();
            await block.Completion;
        }

        public static async Task<IList<T>> ToListAsync<T>(this IEnumerable<T> source)
        {
            return await Task.Run(() => source.ToList());
        }

        public static IEnumerable<TResult> Zip<T1, T2, TResult>(
            this IEnumerable<T1> source,
            IEnumerable<T2> second,
            Func<T1, T2, int, TResult> func)
        {
            using (var e1 = source.GetEnumerator())
            using (var e2 = second.GetEnumerator())
            {
                var index = 0;
                while (e1.MoveNext() && e2.MoveNext())
                    yield return func(e1.Current, e2.Current, index++);
            }
        }

        public static IEnumerable<TResult> Zip<T1, T2, T3, TResult>(
            this IEnumerable<T1> source,
            IEnumerable<T2> second,
            IEnumerable<T3> third,
            Func<T1, T2, T3, int, TResult> func)
        {
            using (var e1 = source.GetEnumerator())
            using (var e2 = second.GetEnumerator())
            using (var e3 = third.GetEnumerator())
            {
                var index = 0;
                while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
                    yield return func(e1.Current, e2.Current, e3.Current, index++);
            }
        }
    }
}
