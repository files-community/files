// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Files.App.Services.SizeProvider
{
	public sealed class DrivesSizeProvider : ISizeProvider
	{
		private readonly ConcurrentDictionary<string, ISizeProvider> providers = new();

		public event EventHandler<SizeChangedEventArgs>? SizeChanged;

		public async Task CleanAsync()
		{
			var currentDrives = DriveInfo.GetDrives().Select(x => x.Name).ToArray();
			var oldDriveNames = providers.Keys.Except(currentDrives).ToArray();

			foreach (var oldDriveName in oldDriveNames)
				providers.TryRemove(oldDriveName, out _);

			await Task.WhenAll(providers.Values.Select(provider => provider.CleanAsync()));
		}

		public async Task ClearAsync()
		{
			await Task.WhenAll(providers.Values.Select(provider => provider.ClearAsync()));

			providers.Clear();
		}

		/// <summary>
		/// Delegate the update to an instance of CachedSizeProvider.
		/// This method is reentrant (thread safe) to avoid having to await each result.
		/// </summary>
		public Task UpdateAsync(string path, CancellationToken cancellationToken)
		{
			string driveName = GetDriveName(path);
			var provider = providers.GetOrAdd(driveName, (key) =>
			{
				return CreateProvider();
			});
			return provider.UpdateAsync(path, cancellationToken);
		}

		public bool TryGetSize(string path, out ulong size)
		{
			string driveName = GetDriveName(path);
			if (!providers.ContainsKey(driveName))
			{
				size = 0;
				return false;
			}
			var provider = providers[driveName];
			return provider.TryGetSize(path, out size);
		}

		private static string GetDriveName(string path) => Directory.GetDirectoryRoot(path);

		private ISizeProvider CreateProvider()
		{
			var provider = new CachedSizeProvider();
			provider.SizeChanged += Provider_SizeChanged;
			return provider;
		}

		private void Provider_SizeChanged(object? sender, SizeChangedEventArgs e)
			=> SizeChanged?.Invoke(this, e);

		public void Dispose()
		{
			foreach (var provider in providers.Values)
			{
				provider.SizeChanged -= Provider_SizeChanged;
				provider.Dispose();
			}
		}
	}
}
