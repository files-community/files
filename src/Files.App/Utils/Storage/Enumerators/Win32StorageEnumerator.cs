// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Services.SizeProvider;
using Files.Shared.Helpers;
using System.IO;
using Windows.Storage;
using static Files.App.Helpers.Win32Helper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.App.Utils.Storage
{
	public static class Win32StorageEnumerator
	{
		private static readonly ISizeProvider folderSizeProvider = Ioc.Default.GetService<ISizeProvider>();
		private static readonly IStorageCacheService fileListCache = Ioc.Default.GetRequiredService<IStorageCacheService>();

		private static readonly string folderTypeTextLocalized = "Folder".GetLocalizedResource();

		public static async Task<List<ListedItem>> ListEntries(
			string path,
			IntPtr hFile,
			Win32PInvoke.WIN32_FIND_DATA findData,
			CancellationToken cancellationToken,
			int countLimit,
			Func<List<ListedItem>, Task> intermediateAction
		)
		{
			var sampler = new IntervalSampler(500);
			var tempList = new List<ListedItem>();
			var count = 0;

			IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			bool CalculateFolderSizes = userSettingsService.FoldersSettingsService.CalculateFolderSizes;

			var isGitRepo = GitHelpers.IsRepositoryEx(path, out var repoPath) && !string.IsNullOrEmpty((await GitHelpers.GetRepositoryHead(repoPath))?.Name);

			do
			{
				var isSystem = ((FileAttributes)findData.dwFileAttributes & FileAttributes.System) == FileAttributes.System;
				var isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
				var startWithDot = findData.cFileName.StartsWith('.');
				if ((!isHidden ||
					(userSettingsService.FoldersSettingsService.ShowHiddenItems &&
					(!isSystem || userSettingsService.FoldersSettingsService.ShowProtectedSystemFiles))) &&
					(!startWithDot || userSettingsService.FoldersSettingsService.ShowDotFiles))
				{
					if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
					{
						var file = await GetFile(findData, path, isGitRepo, cancellationToken);
						if (file is not null)
						{
							tempList.Add(file);
							++count;

							if (userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible)
							{
								tempList.AddRange(EnumAdsForPath(file.ItemPath, file));
							}
						}
					}
					else if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
					{
						if (findData.cFileName != "." && findData.cFileName != "..")
						{
							var folder = await GetFolder(findData, path, isGitRepo, cancellationToken);
							if (folder is not null)
							{
								tempList.Add(folder);
								++count;

								if (userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible)
									tempList.AddRange(EnumAdsForPath(folder.ItemPath, folder));

								if (CalculateFolderSizes)
								{
									if (folderSizeProvider.TryGetSize(folder.ItemPath, out var size))
									{
										folder.FileSizeBytes = (long)size;
										folder.FileSize = size.ToSizeString();
									}

									_ = folderSizeProvider.UpdateAsync(folder.ItemPath, cancellationToken);
								}
							}
						}
					}
				}

				if (cancellationToken.IsCancellationRequested || count == countLimit)
					break;

				if (intermediateAction is not null && (count == 32 || sampler.CheckNow()))
				{
					await intermediateAction(tempList);

					// clear the temporary list every time we do an intermediate action
					tempList.Clear();
				}
			} while (Win32PInvoke.FindNextFile(hFile, out findData));

			Win32PInvoke.FindClose(hFile);

			return tempList;
		}

		private static IEnumerable<ListedItem> EnumAdsForPath(string itemPath, ListedItem main)
		{
			foreach (var ads in Win32Helper.GetAlternateStreams(itemPath))
				yield return GetAlternateStream(ads, main);
		}

		public static ListedItem GetAlternateStream((string Name, long Size) ads, ListedItem main)
		{
			string itemType = "File".GetLocalizedResource();
			string itemFileExtension = null;

			if (ads.Name.Contains('.'))
			{
				itemFileExtension = Path.GetExtension(ads.Name);
				itemType = itemFileExtension.Trim('.') + " " + itemType;
			}

			string adsName = ads.Name.Substring(1, ads.Name.Length - 7); // Remove ":" and ":$DATA"

			return new AlternateStreamItem()
			{
				PrimaryItemAttribute = StorageItemTypes.File,
				FileExtension = itemFileExtension,
				FileImage = null,
				LoadFileIcon = false,
				ItemNameRaw = adsName,
				IsHiddenItem = false,
				Opacity = Constants.UI.DimItemOpacity,
				ItemDateModifiedReal = main.ItemDateModifiedReal,
				ItemDateAccessedReal = main.ItemDateAccessedReal,
				ItemDateCreatedReal = main.ItemDateCreatedReal,
				ItemType = itemType,
				ItemPath = $"{main.ItemPath}:{adsName}",
				FileSize = ads.Size.ToSizeString(),
				FileSizeBytes = ads.Size
			};
		}

		public static async Task<ListedItem> GetFolder(
			Win32PInvoke.WIN32_FIND_DATA findData,
			string pathRoot,
			bool isGitRepo,
			CancellationToken cancellationToken
		)
		{
			if (cancellationToken.IsCancellationRequested)
				return null;

			DateTime itemModifiedDate;
			DateTime itemCreatedDate;

			try
			{
				Win32PInvoke.FileTimeToSystemTime(ref findData.ftLastWriteTime, out Win32PInvoke.SYSTEMTIME systemModifiedTimeOutput);
				itemModifiedDate = systemModifiedTimeOutput.ToDateTime();

				Win32PInvoke.FileTimeToSystemTime(ref findData.ftCreationTime, out Win32PInvoke.SYSTEMTIME systemCreatedTimeOutput);
				itemCreatedDate = systemCreatedTimeOutput.ToDateTime();
			}
			catch (ArgumentException)
			{
				// Invalid date means invalid findData, do not add to list
				return null;
			}

			var itemPath = Path.Combine(pathRoot, findData.cFileName);

			string itemName = await fileListCache.GetDisplayName(itemPath, cancellationToken);
			if (string.IsNullOrEmpty(itemName))
				itemName = findData.cFileName;

			bool isHidden = (((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden);
			double opacity = 1;

			if (isHidden)
				opacity = Constants.UI.DimItemOpacity;

			if (isGitRepo)
			{
				return new GitItem()
				{
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemNameRaw = itemName,
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateCreatedReal = itemCreatedDate,
					ItemType = folderTypeTextLocalized,
					FileImage = null,
					IsHiddenItem = isHidden,
					Opacity = opacity,
					LoadFileIcon = false,
					ItemPath = itemPath,
					FileSize = null,
					FileSizeBytes = 0,
				};
			}
			else
			{
				return new ListedItem(null)
				{
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemNameRaw = itemName,
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateCreatedReal = itemCreatedDate,
					ItemType = folderTypeTextLocalized,
					FileImage = null,
					IsHiddenItem = isHidden,
					Opacity = opacity,
					LoadFileIcon = false,
					ItemPath = itemPath,
					FileSize = null,
					FileSizeBytes = 0,
				};
			}
		}

		public static async Task<ListedItem> GetFile(
			Win32PInvoke.WIN32_FIND_DATA findData,
			string pathRoot,
			bool isGitRepo,
			CancellationToken cancellationToken
		)
		{
			var itemPath = Path.Combine(pathRoot, findData.cFileName);
			var itemName = findData.cFileName;

			DateTime itemModifiedDate, itemCreatedDate, itemLastAccessDate;

			try
			{
				Win32PInvoke.FileTimeToSystemTime(ref findData.ftLastWriteTime, out Win32PInvoke.SYSTEMTIME systemModifiedDateOutput);
				itemModifiedDate = systemModifiedDateOutput.ToDateTime();

				Win32PInvoke.FileTimeToSystemTime(ref findData.ftCreationTime, out Win32PInvoke.SYSTEMTIME systemCreatedDateOutput);
				itemCreatedDate = systemCreatedDateOutput.ToDateTime();

				Win32PInvoke.FileTimeToSystemTime(ref findData.ftLastAccessTime, out Win32PInvoke.SYSTEMTIME systemLastAccessOutput);
				itemLastAccessDate = systemLastAccessOutput.ToDateTime();
			}
			catch (ArgumentException)
			{
				// Invalid date means invalid findData, do not add to list
				return null;
			}

			long itemSizeBytes = findData.GetSize();
			var itemSize = itemSizeBytes.ToSizeString();
			string itemType = "File".GetLocalizedResource();
			string itemFileExtension = null;

			if (findData.cFileName.Contains('.'))
			{
				itemFileExtension = Path.GetExtension(itemPath);
				itemType = itemFileExtension.Trim('.') + " " + itemType;
			}

			bool itemThumbnailImgVis = false;
			bool itemEmptyImgVis = true;

			if (cancellationToken.IsCancellationRequested)
				return null;

			bool isHidden = ((FileAttributes)findData.dwFileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden;
			double opacity = isHidden ? Constants.UI.DimItemOpacity : 1;

			// https://learn.microsoft.com/openspecs/windows_protocols/ms-fscc/c8e77b37-3909-4fe6-a4ea-2b9d423b1ee4
			bool isReparsePoint = ((FileAttributes)findData.dwFileAttributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
			bool isSymlink = isReparsePoint && findData.dwReserved0 == Win32PInvoke.IO_REPARSE_TAG_SYMLINK;

			if (isSymlink)
			{
				var targetPath = Win32Helper.ParseSymLink(itemPath);
				if (isGitRepo)
				{
					return new GitShortcutItem()
					{
						PrimaryItemAttribute = StorageItemTypes.File,
						FileExtension = itemFileExtension,
						IsHiddenItem = isHidden,
						Opacity = opacity,
						FileImage = null,
						LoadFileIcon = itemThumbnailImgVis,
						ItemNameRaw = itemName,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateAccessedReal = itemLastAccessDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = "Shortcut".GetLocalizedResource(),
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = itemSizeBytes,
						TargetPath = targetPath,
						IsSymLink = true,
					};
				}
				else
				{
					return new ShortcutItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.File,
						FileExtension = itemFileExtension,
						IsHiddenItem = isHidden,
						Opacity = opacity,
						FileImage = null,
						LoadFileIcon = itemThumbnailImgVis,
						ItemNameRaw = itemName,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateAccessedReal = itemLastAccessDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = "Shortcut".GetLocalizedResource(),
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = itemSizeBytes,
						TargetPath = targetPath,
						IsSymLink = true
					};
				}
			}
			else if (FileExtensionHelpers.IsShortcutOrUrlFile(findData.cFileName))
			{
				var isUrl = FileExtensionHelpers.IsWebLinkFile(findData.cFileName);

				var shInfo = await FileOperationsHelpers.ParseLinkAsync(itemPath);
				if (shInfo is null)
					return null;

				if (isGitRepo)
				{
					return new GitShortcutItem()
					{
						PrimaryItemAttribute = shInfo.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File,
						FileExtension = itemFileExtension,
						IsHiddenItem = isHidden,
						Opacity = opacity,
						FileImage = null,
						LoadFileIcon = !shInfo.IsFolder && itemThumbnailImgVis,
						ItemNameRaw = itemName,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateAccessedReal = itemLastAccessDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = isUrl ? "ShortcutWebLinkFileType".GetLocalizedResource() : "Shortcut".GetLocalizedResource(),
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = itemSizeBytes,
						TargetPath = shInfo.TargetPath,
						Arguments = shInfo.Arguments,
						WorkingDirectory = shInfo.WorkingDirectory,
						RunAsAdmin = shInfo.RunAsAdmin,
						ShowWindowCommand = shInfo.ShowWindowCommand,
						IsUrl = isUrl,
					};
				}
				else
				{
					return new ShortcutItem(null)
					{
						PrimaryItemAttribute = shInfo.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File,
						FileExtension = itemFileExtension,
						IsHiddenItem = isHidden,
						Opacity = opacity,
						FileImage = null,
						LoadFileIcon = !shInfo.IsFolder && itemThumbnailImgVis,
						ItemNameRaw = itemName,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateAccessedReal = itemLastAccessDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = isUrl ? "ShortcutWebLinkFileType".GetLocalizedResource() : "Shortcut".GetLocalizedResource(),
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = itemSizeBytes,
						TargetPath = shInfo.TargetPath,
						Arguments = shInfo.Arguments,
						WorkingDirectory = shInfo.WorkingDirectory,
						RunAsAdmin = shInfo.RunAsAdmin,
						ShowWindowCommand = shInfo.ShowWindowCommand,
						IsUrl = isUrl,
					};
				}
			}
			else if (App.LibraryManager.TryGetLibrary(itemPath, out LibraryLocationItem library))
			{
				return new LibraryItem(library)
				{
					Opacity = opacity,
					ItemDateModifiedReal = itemModifiedDate,
					ItemDateCreatedReal = itemCreatedDate,
				};
			}
			else
			{
				if (ZipStorageFolder.IsZipPath(itemPath) && await ZipStorageFolder.CheckDefaultZipApp(itemPath))
				{
					return new ZipItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.Folder, // Treat zip files as folders
						FileExtension = itemFileExtension,
						FileImage = null,
						LoadFileIcon = itemThumbnailImgVis,
						ItemNameRaw = itemName,
						IsHiddenItem = isHidden,
						Opacity = opacity,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateAccessedReal = itemLastAccessDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = itemType,
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = itemSizeBytes
					};
				}
				else if (isGitRepo)
				{
					return new GitItem()
					{
						PrimaryItemAttribute = StorageItemTypes.File,
						FileExtension = itemFileExtension,
						FileImage = null,
						LoadFileIcon = itemThumbnailImgVis,
						ItemNameRaw = itemName,
						IsHiddenItem = isHidden,
						Opacity = opacity,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateAccessedReal = itemLastAccessDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = itemType,
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = itemSizeBytes
					};
				}
				else
				{
					return new ListedItem(null)
					{
						PrimaryItemAttribute = StorageItemTypes.File,
						FileExtension = itemFileExtension,
						FileImage = null,
						LoadFileIcon = itemThumbnailImgVis,
						ItemNameRaw = itemName,
						IsHiddenItem = isHidden,
						Opacity = opacity,
						ItemDateModifiedReal = itemModifiedDate,
						ItemDateAccessedReal = itemLastAccessDate,
						ItemDateCreatedReal = itemCreatedDate,
						ItemType = itemType,
						ItemPath = itemPath,
						FileSize = itemSize,
						FileSizeBytes = itemSizeBytes
					};
				}
			}

			return null;
		}
	}
}
