using Files.App.Extensions;
using Files.App.Utils;
using Files.App.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	internal class CombinedProperties : BaseProperties
	{
		public List<ListedItem> List { get; }

		public CombinedProperties(
			SelectedItemsPropertiesViewModel viewModel,
			CancellationTokenSource tokenSource,
			DispatcherQueue coreDispatcher,
			List<ListedItem> listedItems,
			IShellPage instance)
		{
			ViewModel = viewModel;
			TokenSource = tokenSource;
			Dispatcher = coreDispatcher;
			List = listedItems;
			AppInstance = instance;
			GetBaseProperties();
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public sealed override void GetBaseProperties()
		{
			if (List is not null)
			{
				ViewModel.LoadCombinedItemsGlyph = true;

				if (List.All(x => x.ItemType.Equals(List.First().ItemType)))
				{
					ViewModel.ItemType = string.Format("PropertiesDriveItemTypesEquals".GetLocalizedResource(), List.First().ItemType);
				}
				else
				{
					ViewModel.ItemType = "PropertiesDriveItemTypeDifferent".GetLocalizedResource();
				}

				var itemsPath = List.Select(Item => (Item as RecycleBinItem)?.ItemOriginalFolder ??
					(Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath));

				if (itemsPath.Distinct().Count() == 1)
					ViewModel.ItemLocation = string.Format("PropertiesCombinedItemLocation".GetLocalizedResource(), itemsPath.First());
			}
		}

		public override async Task GetSpecialPropertiesAsync()
		{
			if (List.All(x => x.PrimaryItemAttribute == StorageItemTypes.File))
			{
				var fileAttributesReadOnly = List.Select(x => Win32Helper.HasFileAttribute(x.ItemPath, System.IO.FileAttributes.ReadOnly));
				if (fileAttributesReadOnly.All(x => x))
					ViewModel.IsReadOnly = true;
				else if (!fileAttributesReadOnly.Any(x => x))
					ViewModel.IsReadOnly = false;
				else
					ViewModel.IsReadOnly = null;
			}

			var fileAttributesHidden = List.Select(x => Win32Helper.HasFileAttribute(x.ItemPath, System.IO.FileAttributes.Hidden));
			if (fileAttributesHidden.All(x => x))
				ViewModel.IsHidden = true;
			else if (!fileAttributesHidden.Any(x => x))
				ViewModel.IsHidden = false;
			else
				ViewModel.IsHidden = null;

			ViewModel.LastSeparatorVisibility = false;
			ViewModel.ItemSizeVisibility = true;

			ViewModel.FilesCount += List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File || x.IsArchive).ToList().Count;
			ViewModel.FoldersCount += List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsArchive).ToList().Count;

			long totalSize = 0;
			long filesSize = List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File).Sum(x => x.FileSizeBytes);
			long foldersSize = 0;
			long totalSizeOnDisk = 0;
			long filesSizeOnDisk = List.Where(x => x.PrimaryItemAttribute == StorageItemTypes.File &&
				x.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not CloudDriveSyncStatus.FolderOnline)
					.Sum(x => Win32Helper.GetFileSizeOnDisk(x.ItemPath) ?? 0);
			long foldersSizeOnDisk = 0;

			ViewModel.ItemSizeProgressVisibility = true;
			ViewModel.ItemSizeOnDiskProgressVisibility = true;

			foreach (var item in List)
			{
				if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
				{
					if (item.SyncStatusUI.SyncStatus is CloudDriveSyncStatus.FileOnline or
						CloudDriveSyncStatus.FolderOnline or
						CloudDriveSyncStatus.FolderOfflinePartial)
						continue;

					var fileSizeTask = Task.Run(() => CalculateFolderSizeAsync(item.ItemPath, TokenSource.Token));

					try
					{
						var folderSize = await fileSizeTask;
						foldersSize += folderSize.size;
						foldersSizeOnDisk += folderSize.sizeOnDisk;
					}
					catch (Exception ex)
					{
						App.Logger.LogWarning(ex, ex.Message);
					}
				}
			}

			ViewModel.ItemSizeProgressVisibility = false;
			ViewModel.ItemSizeOnDiskProgressVisibility = false;

			totalSize = filesSize + foldersSize;
			ViewModel.ItemSize = totalSize.ToLongSizeString();
			totalSizeOnDisk = filesSizeOnDisk + foldersSizeOnDisk;
			ViewModel.ItemSizeOnDisk = totalSizeOnDisk.ToLongSizeString();

			SetItemsCountString();
		}

		private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "IsReadOnly":
					{
						if (ViewModel.IsReadOnly is not null)
						{
							if ((bool)ViewModel.IsReadOnly)
							{
								List.ForEach(x => Win32Helper.SetFileAttribute(
									x.ItemPath, System.IO.FileAttributes.ReadOnly));
							}
							else
							{
								List.ForEach(x => Win32Helper.UnsetFileAttribute(
									x.ItemPath, System.IO.FileAttributes.ReadOnly));
							}
						}
					}
					break;

				case "IsHidden":
					{
						if (ViewModel.IsHidden is not null)
						{
							if ((bool)ViewModel.IsHidden)
							{
								List.ForEach(x => Win32Helper.SetFileAttribute(
									x.ItemPath, System.IO.FileAttributes.Hidden));
							}
							else
							{
								List.ForEach(x => Win32Helper.UnsetFileAttribute(
									x.ItemPath, System.IO.FileAttributes.Hidden));
							}
						}

					}
					break;

				case "IsContentCompressed":
					{
						List.ForEach(x =>
						{
							Win32Helper.SetCompressionAttributeIoctl(x.ItemPath, ViewModel.IsContentCompressed ?? false);
						});
					}
					break;
			}
		}
	}
}
