// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Files.Shared.Extensions
{
	public static class TaskExtensions
	{
		public static async Task WithTimeoutAsync(this Task task, TimeSpan timeout)
		{
			if (task == await Task.WhenAny(task, Task.Delay(timeout)))
			{
				await task;
			}
		}

		public static async Task<T?> WithTimeoutAsync<T>(this Task<T> task, TimeSpan timeout, T? defaultValue = default)
		{
			if (task == await Task.WhenAny(task, Task.Delay(timeout)))
				return await task;

			return defaultValue;
		}

		public static async Task<TOut> AndThen<TIn, TOut>(this Task<TIn> inputTask, Func<TIn, Task<TOut>> mapping)
		{
			var input = await inputTask;

			return (await mapping(input));
		}
	}
}
