// Copyright(c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Microsoft.UI.Dispatching;
using System.IO;

namespace Files.App.ViewModels.Properties
{
	public sealed class FileProperties : BaseProperties, IFileProperties
	{
		public ListedItem Item { get; }

		public FileProperties(
			SelectedItemsPropertiesViewModel viewModel,
			CancellationTokenSource tokenSource,
			DispatcherQueue coreDispatcher,
			ListedItem item,
			IShellPage instance)
		{
			ViewModel = viewModel;
			TokenSource = tokenSource;
			Dispatcher = coreDispatcher;
			Item = item;
			AppInstance = instance;

			GetBaseProperties();

			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public override void GetBaseProperties()
		{
			if (Item is null)
				return;

			ViewModel.ItemName = Item.Name;
			ViewModel.OriginalItemName = Item.Name;
			ViewModel.ItemType = Item.ItemType;
			ViewModel.ItemLocation = (Item as RecycleBinItem)?.ItemOriginalFolder ??
				(Path.IsPathRooted(Item.ItemPath) ? Path.GetDirectoryName(Item.ItemPath) : Item.ItemPath);
			ViewModel.ItemModifiedTimestampReal = Item.ItemDateModifiedReal;
			ViewModel.ItemCreatedTimestampReal = Item.ItemDateCreatedReal;
			ViewModel.LoadCustomIcon = Item.LoadCustomIcon;
			ViewModel.CustomIconSource = Item.CustomIconSource;
			ViewModel.LoadFileIcon = Item.LoadFileIcon;
			ViewModel.IsDownloadedFile = Win32Helper.ReadStringFromFile($"{Item.ItemPath}:Zone.Identifier") is not null;
			ViewModel.IsEditAlbumCoverVisible =
				FileExtensionHelpers.IsVideoFile(Item.FileExtension) ||
				FileExtensionHelpers.IsAudioFile(Item.FileExtension);

			if (!Item.IsShortcut)
				return;

			var shortcutItem = (ShortcutItem)Item;

			var isApplication =
				FileExtensionHelpers.IsExecutableFile(shortcutItem.TargetPath) ||
				FileExtensionHelpers.IsMsiFile(shortcutItem.TargetPath);

			ViewModel.ShortcutItemType = isApplication ? "Application".GetLocalizedResource() :
				Item.IsLinkItem ? "PropertiesShortcutTypeLink".GetLocalizedResource() : "File".GetLocalizedResource();
			ViewModel.ShortcutItemPath = shortcutItem.TargetPath;
			ViewModel.IsShortcutItemPathReadOnly = shortcutItem.IsSymLink;
			ViewModel.ShortcutItemWorkingDir = shortcutItem.WorkingDirectory;
			ViewModel.ShortcutItemWorkingDirVisibility = Item.IsLinkItem || shortcutItem.IsSymLink ? false : true;
			ViewModel.ShortcutItemArguments = shortcutItem.Arguments;
			ViewModel.ShowWindowCommand = shortcutItem.ShowWindowCommand;
			ViewModel.ShortcutItemArgumentsVisibility = Item.IsLinkItem || shortcutItem.IsSymLink ? false : true;
			ViewModel.ShortcutItemWindowArgsVisibility = Item.IsLinkItem || shortcutItem.IsSymLink ? false : true;

			if (isApplication)
				ViewModel.RunAsAdmin = shortcutItem.RunAsAdmin;

			ViewModel.IsSelectedItemShortcut = FileExtensionHelpers.IsShortcutFile(Item.FileExtension);

			ViewModel.ShortcutItemOpenLinkCommand = new RelayCommand(async () =>
			{
				if (Item.IsLinkItem)
				{
					var tmpItem = (ShortcutItem)Item;
					await Win32Helper.InvokeWin32ComponentAsync(ViewModel.ShortcutItemPath, AppInstance, ViewModel.ShortcutItemArguments, ViewModel.RunAsAdmin, ViewModel.ShortcutItemWorkingDir);
				}
				else
				{
					await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(
						() => NavigationHelpers.OpenPathInNewTab(Path.GetDirectoryName(ViewModel.ShortcutItemPath), true));
				}
			},
			() =>
			{
				return !string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath);
			});
		}

		public override async Task GetSpecialPropertiesAsync()
		{
			ViewModel.IsReadOnly = Win32Helper.HasFileAttribute(Item.ItemPath, System.IO.FileAttributes.ReadOnly);
			ViewModel.IsHidden = Win32Helper.HasFileAttribute(Item.ItemPath, System.IO.FileAttributes.Hidden);
			ViewModel.CanCompressContent = Win32Helper.CanCompressContent(Item.ItemPath);
			ViewModel.IsContentCompressed = Win32Helper.HasFileAttribute(Item.ItemPath, System.IO.FileAttributes.Compressed);

			ViewModel.ItemSizeVisibility = true;
			ViewModel.ItemSize = Item.FileSizeBytes.ToLongSizeString();

			// Only load the size for items on the device
			if (Item.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not CloudDriveSyncStatus.FolderOnline)
				ViewModel.ItemSizeOnDisk = Win32Helper.GetFileSizeOnDisk(Item.ItemPath)?.ToLongSizeString() ??
				   string.Empty;

			var result = await FileThumbnailHelper.GetIconAsync(
				Item.ItemPath,
				Constants.ShellIconSizes.ExtraLarge,
				false,
				IconOptions.UseCurrentScale);

			if (result is not null)
			{
				ViewModel.IconData = result;
				ViewModel.LoadUnknownTypeGlyph = false;
				ViewModel.LoadFileIcon = true;
			}

			if (Item.IsShortcut)
			{
				ViewModel.ItemCreatedTimestampReal = Item.ItemDateCreatedReal;
				ViewModel.ItemAccessedTimestampReal = Item.ItemDateAccessedReal;
				if (Item.IsLinkItem || string.IsNullOrWhiteSpace(((ShortcutItem)Item).TargetPath))
				{
					// Can't show any other property
					return;
				}
			}

			string filePath = (Item as ShortcutItem)?.TargetPath ?? Item.ItemPath;
			BaseStorageFile file = await AppInstance.ShellViewModel.GetFileFromPathAsync(filePath);

			// Couldn't access the file and can't load any other properties
			if (file is null)
				return;

			// Can't load any other properties
			if (Item.IsShortcut)
				return;

			if (Item.SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not CloudDriveSyncStatus.FolderOnline)
				if (FileExtensionHelpers.IsBrowsableZipFile(Item.FileExtension, out _))
					if (await ZipStorageFolder.FromPathAsync(Item.ItemPath) is ZipStorageFolder zipFolder)
					{
						var uncompressedSize = await zipFolder.GetUncompressedSize();
						ViewModel.UncompressedItemSize = uncompressedSize.ToLongSizeString();
						ViewModel.UncompressedItemSizeBytes = uncompressedSize;
					}

			if (file.Properties is not null)
				GetOtherPropertiesAsync(file.Properties);
		}

		public async Task GetSystemFilePropertiesAsync()
		{
			BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath));
			if (file is null)
			{
				// Could not access file, can't show any other property
				return;
			}

			var list = await FileProperty.RetrieveAndInitializePropertiesAsync(file);

			list.Find(x => x.ID == "address").Value =
				await LocationHelpers.GetAddressFromCoordinatesAsync((double?)list.Find(
					x => x.Property == "System.GPS.LatitudeDecimal").Value,
					(double?)list.Find(x => x.Property == "System.GPS.LongitudeDecimal").Value);

			var query = list
				.Where(fileProp => !(fileProp.Value is null && fileProp.IsReadOnly))
				.GroupBy(fileProp => fileProp.SectionResource)
				.Select(group => new FilePropertySection(group) { Key = group.Key })
				.Where(section => !section.All(fileProp => fileProp.Value is null))
				.OrderBy(group => group.Priority);

			ViewModel.PropertySections = new ObservableCollection<FilePropertySection>(query);
			ViewModel.FileProperties = new ObservableCollection<FileProperty>(list.Where(i => i.Value is not null));
		}

		public async Task SyncPropertyChangesAsync()
		{
			BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath));

			// Couldn't access the file to save properties
			if (file is null)
				return;

			var failedProperties = "";

			foreach (var group in ViewModel.PropertySections)
			{
				foreach (FileProperty prop in group)
				{
					if (!prop.IsReadOnly && prop.Modified)
					{
						var newDict = new Dictionary<string, object>
						{
							{ prop.Property, prop.Value }
						};

						try
						{
							if (file.Properties is not null)
							{
								await file.Properties.SavePropertiesAsync(newDict);
							}
						}
						catch
						{
							failedProperties += $"{prop.Name}\n";
						}
					}
				}
			}

			if (!string.IsNullOrWhiteSpace(failedProperties))
			{
				throw new Exception($"The following properties failed to save: {failedProperties}");
			}
		}

		public async Task ClearPropertiesAsync()
		{
			var failedProperties = new List<string>();
			BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(Item.ItemPath));

			if (file is null)
				return;

			foreach (var group in ViewModel.PropertySections)
			{
				foreach (FileProperty prop in group)
				{
					if (!prop.IsReadOnly)
					{
						var newDict = new Dictionary<string, object>
						{
							{ prop.Property, null }
						};

						try
						{
							if (file.Properties is not null)
							{
								await file.Properties.SavePropertiesAsync(newDict);
							}
						}
						catch
						{
							failedProperties.Add(prop.Name);
						}
					}
				}
			}

			_ = GetSystemFilePropertiesAsync();
		}

		private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ViewModel.IsReadOnly):
					if (ViewModel.IsReadOnly is not null)
					{
						if ((bool)ViewModel.IsReadOnly)
							Win32Helper.SetFileAttribute(Item.ItemPath, System.IO.FileAttributes.ReadOnly);
						else
							Win32Helper.UnsetFileAttribute(Item.ItemPath, System.IO.FileAttributes.ReadOnly);
					}

					break;

				case nameof(ViewModel.IsHidden):
					if (ViewModel.IsHidden is not null)
					{
						if ((bool)ViewModel.IsHidden)
							Win32Helper.SetFileAttribute(Item.ItemPath, System.IO.FileAttributes.Hidden);
						else
							Win32Helper.UnsetFileAttribute(Item.ItemPath, System.IO.FileAttributes.Hidden);
					}

					break;

				case nameof(ViewModel.IsContentCompressed):
					Win32Helper.SetCompressionAttributeIoctl(Item.ItemPath, ViewModel.IsContentCompressed ?? false);
					break;

				case nameof(ViewModel.RunAsAdmin):
				case nameof(ViewModel.ShortcutItemPath):
				case nameof(ViewModel.ShortcutItemWorkingDir):
				case nameof(ViewModel.ShowWindowCommand):
				case nameof(ViewModel.ShortcutItemArguments):
					if (string.IsNullOrWhiteSpace(ViewModel.ShortcutItemPath))
						return;

					await FileOperationsHelpers.CreateOrUpdateLinkAsync(Item.ItemPath, ViewModel.ShortcutItemPath, ViewModel.ShortcutItemArguments, ViewModel.ShortcutItemWorkingDir, ViewModel.RunAsAdmin, ViewModel.ShowWindowCommand);

					break;
			}
		}
	}
}
