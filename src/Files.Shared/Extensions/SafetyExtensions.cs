// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Files.Shared.Extensions
{
	public static class SafetyExtensions
	{
		public static bool IgnoreExceptions(Action action, ILogger? logger = null, Type? exceptionToIgnore = null)
		{
			try
			{
				action();
				return true;
			}
			catch (Exception ex)
			{
				if (exceptionToIgnore is null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
				{
					logger?.LogInformation(ex, ex.Message);

					return false;
				}
				else
					throw;
			}
		}

		public static async Task<bool> IgnoreExceptions(Func<Task> action, ILogger? logger = null, Type? exceptionToIgnore = null)
		{
			try
			{
				await action();

				return true;
			}
			catch (Exception ex)
			{
				if (exceptionToIgnore is null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
				{
					logger?.LogInformation(ex, ex.Message);

					return false;
				}
				else
					throw;
			}
		}

		public static T? IgnoreExceptions<T>(Func<T> action, ILogger? logger = null, Type? exceptionToIgnore = null)
		{
			try
			{
				return action();
			}
			catch (Exception ex)
			{
				if (exceptionToIgnore is null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
				{
					logger?.LogInformation(ex, ex.Message);

					return default;
				}
				else
					throw;
			}
		}

		public static async Task<T?> IgnoreExceptions<T>(Func<Task<T>> action, ILogger? logger = null, Type? exceptionToIgnore = null)
		{
			try
			{
				return await action();
			}
			catch (Exception ex)
			{
				if (exceptionToIgnore is null || exceptionToIgnore.IsAssignableFrom(ex.GetType()))
				{
					logger?.LogInformation(ex, ex.Message);

					return default;
				}
				else
					throw;
			}
		}

		public static async Task<TOut> Wrap<TOut>(Func<Task<TOut>> inputTask, Func<Func<Task<TOut>>, Exception, Task<TOut>> onFailed)
		{
			try
			{
				return await inputTask();
			}
			catch (Exception ex)
			{
				return await onFailed(inputTask, ex);
			}
		}

		public static async Task WrapAsync(Func<Task> inputTask, Func<Func<Task>, Exception, Task> onFailed)
		{
			try
			{
				await inputTask();
			}
			catch (Exception ex)
			{
				await onFailed(inputTask, ex);
			}
		}
	}
}
