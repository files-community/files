// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using System.IO;
using Windows.Storage;

namespace Files.App.Storage.Storables
{
	/// <inheritdoc cref="IStorageService"/>
	[Obsolete("Use the new WindowsStorable")]
	public sealed class NativeStorageLegacyService : IStorageService
	{
		/// <inheritdoc/>
		public Task<IFile> GetFileAsync(string id, CancellationToken cancellationToken = default)
		{
			if (!File.Exists(id))
				throw new FileNotFoundException();

			return Task.FromResult<IFile>(new NativeFileLegacy(id));
		}

		/// <inheritdoc/>
		public async Task<IFolder> GetFolderAsync(string id, CancellationToken cancellationToken = default)
		{
			if (!Directory.Exists(id))
				throw new DirectoryNotFoundException();

			// A special folder should use the localized name
			if (PathHelpers.IsSpecialFolder(id))
			{
				var storageFolder = await TryGetStorageFolderAsync(id);
				return new NativeFolderLegacy(id, storageFolder?.DisplayName);
			}

			return new NativeFolderLegacy(id);

			async Task<StorageFolder?> TryGetStorageFolderAsync(string path)
			{
				try
				{
					return await StorageFolder.GetFolderFromPathAsync(path);
				}
				catch (Exception)
				{
					return null;
				}
			}
		}
	}
}
