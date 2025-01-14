// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Utils.Storage
{
	public static class StorageItemIconHelpers
	{
		/// <summary>
		/// Retrieves the default non-thumbnail icon for a provided item type
		/// </summary>
		/// <param name="requestedSize">Desired size of icon</param>
		/// <param name="persistenceOptions">Optionally choose not to persist icon-backing item in LocalCache</param>
		/// <param name="fileExtension">The file type (extension) of the generic icon to retrieve. Leave empty if a directory icon is desired</param>
		/// <returns></returns>
		public static async Task<StorageItemThumbnail> GetIconForItemType(uint requestedSize, IconPersistenceOptions persistenceOptions = IconPersistenceOptions.Persist, string fileExtension = null)
		{
			if (string.IsNullOrEmpty(fileExtension))
			{
				StorageFolder localFolder = ApplicationData.Current.RoamingFolder;
				return await localFolder.GetThumbnailAsync(ThumbnailMode.ListView, requestedSize, ThumbnailOptions.UseCurrentScale);
			}
			else
			{
				StorageFile emptyFile = await ApplicationData.Current.LocalCacheFolder.CreateFileAsync(string.Join(Constants.Filesystem.CachedEmptyItemName, fileExtension), CreationCollisionOption.OpenIfExists);
				var icon = await emptyFile.GetThumbnailAsync(ThumbnailMode.ListView, requestedSize, ThumbnailOptions.UseCurrentScale);

				if (persistenceOptions == IconPersistenceOptions.LoadOnce)
				{
					await emptyFile.DeleteAsync(StorageDeleteOption.PermanentDelete);
				}

				return icon;
			}
		}
	}
}
