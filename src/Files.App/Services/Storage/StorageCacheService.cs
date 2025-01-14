// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;

namespace Files.App.Utils.Storage
{
	/// <inheritdoc cref="IStorageCacheService"/>
	internal sealed class StorageCacheService : IStorageCacheService
	{
		private readonly ConcurrentDictionary<string, string> cachedDictionary = new();

		/// <inheritdoc/>
		public ValueTask<string> GetDisplayName(string path, CancellationToken cancellationToken)
		{
			return
				cachedDictionary.TryGetValue(path, out var displayName)
					? ValueTask.FromResult(displayName)
					: ValueTask.FromResult(string.Empty);
		}

		/// <inheritdoc/>
		public ValueTask AddDisplayName(string path, string? displayName)
		{
			if (string.IsNullOrEmpty(displayName))
			{
				cachedDictionary.TryRemove(path, out _);
				return ValueTask.CompletedTask;
			}

			cachedDictionary[path] = displayName;

			return ValueTask.CompletedTask;
		}
	}
}
