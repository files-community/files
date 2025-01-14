// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Dispatching;
using System.IO;
using Windows.Storage.FileProperties;
using static Files.App.Helpers.Win32Helper;
using FileAttributes = System.IO.FileAttributes;

namespace Files.App.ViewModels.Properties
{
	public abstract class BaseProperties
	{
		public IShellPage AppInstance { get; set; } = null;

		public SelectedItemsPropertiesViewModel ViewModel { get; set; }

		public CancellationTokenSource TokenSource { get; set; }

		public DispatcherQueue Dispatcher { get; set; }

		public abstract void GetBaseProperties();

		public abstract Task GetSpecialPropertiesAsync();

		public async Task GetOtherPropertiesAsync(IStorageItemExtraProperties properties)
		{
			string dateAccessedProperty = "System.DateAccessed";
			string dateModifiedProperty = "System.DateModified";

			List<string> propertiesName =
			[
				dateAccessedProperty,
				dateModifiedProperty
			];

			IDictionary<string, object> extraProperties = await properties.RetrievePropertiesAsync(propertiesName);

			// Cannot get date and owner in MTP devices
			ViewModel.ItemAccessedTimestampReal = (DateTimeOffset)(extraProperties[dateAccessedProperty] ?? DateTimeOffset.Now);
			ViewModel.ItemModifiedTimestampReal = (DateTimeOffset)(extraProperties[dateModifiedProperty] ?? DateTimeOffset.Now);
		}

		public async Task<(long size, long sizeOnDisk)> CalculateFolderSizeAsync(string path, CancellationToken token)
		{
			if (string.IsNullOrEmpty(path))
			{
				// In MTP devices calculating folder size would be too slow
				// Also should use StorageFolder methods instead of FindFirstFileExFromApp
				return (0, 0);
			}

			long size = 0;
			long sizeOnDisk = 0;
			Win32PInvoke.FINDEX_INFO_LEVELS findInfoLevel = Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic;
			int additionalFlags = Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH;

			IntPtr hFile = Win32PInvoke.FindFirstFileExFromApp(
				path + "\\*.*",
				findInfoLevel,
				out Win32PInvoke.WIN32_FIND_DATA findData,
				Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
				IntPtr.Zero,
				additionalFlags);

			var count = 0;
			if (hFile.ToInt64() != -1)
			{
				do
				{
					if (((FileAttributes)findData.dwFileAttributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
						// Skip symbolic links and junctions
						continue;

					if (((FileAttributes)findData.dwFileAttributes & FileAttributes.Directory) != FileAttributes.Directory)
					{
						size += findData.GetSize();
						var fileSizeOnDisk = Win32Helper.GetFileSizeOnDisk(Path.Combine(path, findData.cFileName));
						sizeOnDisk += fileSizeOnDisk ?? 0;
						++count;
						ViewModel.FilesCount++;
					}
					else if (findData.cFileName != "." && findData.cFileName != "..")
					{
						var itemPath = Path.Combine(path, findData.cFileName);

						var folderSize = await CalculateFolderSizeAsync(itemPath, token);
						size += folderSize.size;
						sizeOnDisk += folderSize.sizeOnDisk;
						++count;
						ViewModel.FoldersCount++;
					}

					if (size > ViewModel.ItemSizeBytes || sizeOnDisk > ViewModel.ItemSizeOnDiskBytes)
					{
						await Dispatcher.EnqueueOrInvokeAsync(() =>
						{
							ViewModel.ItemSizeBytes = size;
							ViewModel.ItemSize = size.ToSizeString();
							ViewModel.ItemSizeOnDiskBytes = sizeOnDisk;
							ViewModel.ItemSizeOnDisk = sizeOnDisk.ToSizeString();
							SetItemsCountString();
						},
						DispatcherQueuePriority.Low);
					}

					if (token.IsCancellationRequested)
						break;
				}
				while (Win32PInvoke.FindNextFile(hFile, out findData));

				Win32PInvoke.FindClose(hFile);

				return (size, sizeOnDisk);
			}
			else
			{
				return (0, 0);
			}
		}

		public void SetItemsCountString()
		{
			ViewModel.FilesAndFoldersCountString = ViewModel.LocationsCount > 0
				? "PropertiesFilesAndFoldersAndLocationsCount".GetLocalizedFormatResource(ViewModel.FilesCount, ViewModel.FoldersCount, ViewModel.LocationsCount)
				: "PropertiesFilesAndFoldersCountString".GetLocalizedFormatResource(ViewModel.FilesCount, ViewModel.FoldersCount);
		}
	}
}
