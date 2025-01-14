// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Files.Shared.Helpers;
using System.Windows.Input;
using TagLib;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Data.Models
{
	public sealed class SelectedItemsPropertiesViewModel : ObservableObject
	{
		private static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetRequiredService<IDateTimeFormatter>();

		private bool loadFolderGlyph;
		public bool LoadFolderGlyph
		{
			get => loadFolderGlyph;
			set => SetProperty(ref loadFolderGlyph, value);
		}

		private bool loadUnknownTypeGlyph;
		public bool LoadUnknownTypeGlyph
		{
			get => loadUnknownTypeGlyph;
			set => SetProperty(ref loadUnknownTypeGlyph, value);
		}

		private bool loadCombinedItemsGlyph;
		public bool LoadCombinedItemsGlyph
		{
			get => loadCombinedItemsGlyph;
			set => SetProperty(ref loadCombinedItemsGlyph, value);
		}

		private Uri customIconSource;
		public Uri CustomIconSource
		{
			get => customIconSource;
			set => SetProperty(ref customIconSource, value);
		}

		private bool loadCustomIcon;
		public bool LoadCustomIcon
		{
			get => loadCustomIcon;
			set => SetProperty(ref loadCustomIcon, value);
		}

		private bool loadFileIcon;
		public bool LoadFileIcon
		{
			get => loadFileIcon;
			set => SetProperty(ref loadFileIcon, value);
		}

		private byte[] iconData;
		public byte[] IconData
		{
			get => iconData;
			set => SetProperty(ref iconData, value);
		}

		private string itemName;
		public string ItemName
		{
			get => itemName;
			set
			{
				ItemNameVisibility = true;
				SetProperty(ref itemName, value);
			}
		}

		private string originalItemName;
		public string OriginalItemName
		{
			get => originalItemName;
			set
			{
				ItemNameVisibility = true;
				SetProperty(ref originalItemName, value);
			}
		}

		private bool itemNameVisibility = false;
		public bool ItemNameVisibility
		{
			get => itemNameVisibility;
			set => SetProperty(ref itemNameVisibility, value);
		}

		private string itemType;
		public string ItemType
		{
			get => itemType;
			set
			{
				ItemTypeVisibility = true;
				SetProperty(ref itemType, value);
			}
		}

		private bool itemTypeVisibility = false;
		public bool ItemTypeVisibility
		{
			get => itemTypeVisibility;
			set => SetProperty(ref itemTypeVisibility, value);
		}

		private string driveFileSystem;
		public string DriveFileSystem
		{
			get => driveFileSystem;
			set
			{
				DriveFileSystemVisibility = true;
				SetProperty(ref driveFileSystem, value);
			}
		}

		private bool driveFileSystemVisibility = false;
		public bool DriveFileSystemVisibility
		{
			get => driveFileSystemVisibility;
			set => SetProperty(ref driveFileSystemVisibility, value);
		}

		private string itemLocation;
		public string ItemLocation
		{
			get => itemLocation;
			set
			{
				ItemLocationVisibility = true;
				SetProperty(ref itemLocation, value);
			}
		}

		private bool itemLocationVisibility = false;
		public bool ItemLocationVisibility
		{
			get => itemLocationVisibility;
			set => SetProperty(ref itemLocationVisibility, value);
		}

		private string itemSize;
		public string ItemSize
		{
			get => itemSize;
			set => SetProperty(ref itemSize, value);
		}

		private string itemSizeOnDisk;
		public string ItemSizeOnDisk
		{
			get => itemSizeOnDisk;
			set => SetProperty(ref itemSizeOnDisk, value);
		}

		private string uncompresseditemSize;
		public string UncompressedItemSize
		{
			get => uncompresseditemSize;
			set
			{
				IsUncompressedItemSizeVisibile = true;
				SetProperty(ref uncompresseditemSize, value);
			}
		}

		private bool itemSizeVisibility = false;
		public bool ItemSizeVisibility
		{
			get => itemSizeVisibility;
			set => SetProperty(ref itemSizeVisibility, value);
		}

		private bool isUncompressedItemSizeVisibile = false;
		public bool IsUncompressedItemSizeVisibile
		{
			get => isUncompressedItemSizeVisibile;
			set => SetProperty(ref isUncompressedItemSizeVisibile, value);
		}

		private decimal itemSizeBytes;
		public decimal ItemSizeBytes
		{
			get => itemSizeBytes;
			set => SetProperty(ref itemSizeBytes, value);
		}

		private long itemSizeOnDiskBytes;
		public long ItemSizeOnDiskBytes
		{
			get => itemSizeOnDiskBytes;
			set => SetProperty(ref itemSizeOnDiskBytes, value);
		}

		private long uncompresseditemSizeBytes;
		public long UncompressedItemSizeBytes
		{
			get => uncompresseditemSizeBytes;
			set => SetProperty(ref uncompresseditemSizeBytes, value);
		}

		private bool itemSizeProgressVisibility = false;
		public bool ItemSizeProgressVisibility
		{
			get => itemSizeProgressVisibility;
			set => SetProperty(ref itemSizeProgressVisibility, value);
		}

		private bool itemSizeOnDiskProgressVisibility = false;
		public bool ItemSizeOnDiskProgressVisibility
		{
			get => itemSizeOnDiskProgressVisibility;
			set => SetProperty(ref itemSizeOnDiskProgressVisibility, value);
		}

		// For libraries
		public int locationsCount;
		public int LocationsCount
		{
			get => locationsCount;
			set => SetProperty(ref locationsCount, value);
		}

		public int foldersCount;
		public int FoldersCount
		{
			get => foldersCount;
			set => SetProperty(ref foldersCount, value);
		}

		public int filesCount;
		public int FilesCount
		{
			get => filesCount;
			set => SetProperty(ref filesCount, value);
		}

		public string filesAndFoldersCountString;
		public string FilesAndFoldersCountString
		{
			get => filesAndFoldersCountString;
			set
			{
				if (FilesAndFoldersCountVisibility == false)
					FilesAndFoldersCountVisibility = true;

				SetProperty(ref filesAndFoldersCountString, value);
			}
		}

		public bool filesAndFoldersCountVisibility = false;
		public bool FilesAndFoldersCountVisibility
		{
			get => filesAndFoldersCountVisibility;
			set => SetProperty(ref filesAndFoldersCountVisibility, value);
		}

		private ulong driveUsedSpaceValue;
		public ulong DriveUsedSpaceValue
		{
			get => driveUsedSpaceValue;
			set
			{
				SetProperty(ref driveUsedSpaceValue, value);
				DriveUsedSpace = DriveUsedSpaceValue.ToSizeString();
				DriveUsedSpaceLongSize = DriveUsedSpaceValue.ToLongSizeString();
				OnPropertyChanged(nameof(DrivePercentageValue));
			}
		}

		private string driveUsedSpace;
		public string DriveUsedSpace
		{
			get => driveUsedSpace;
			set
			{
				DriveUsedSpaceVisibility = true;
				SetProperty(ref driveUsedSpace, value);
			}
		}

		private string driveUsedSpaceLongSize;
		public string DriveUsedSpaceLongSize
		{
			get => driveUsedSpaceLongSize;
			set
			{
				SetProperty(ref driveUsedSpaceLongSize, value);
			}
		}

		public bool driveUsedSpaceVisibility = false;
		public bool DriveUsedSpaceVisibility
		{
			get => driveUsedSpaceVisibility;
			set => SetProperty(ref driveUsedSpaceVisibility, value);
		}

		private ulong driveFreeSpaceValue;
		public ulong DriveFreeSpaceValue
		{
			get => driveFreeSpaceValue;
			set
			{
				SetProperty(ref driveFreeSpaceValue, value);
				DriveFreeSpace = DriveFreeSpaceValue.ToSizeString();
				DriveFreeSpaceLongSize = DriveFreeSpaceValue.ToLongSizeString();

			}
		}

		private string driveFreeSpace;
		public string DriveFreeSpace
		{
			get => driveFreeSpace;
			set
			{
				DriveFreeSpaceVisibility = true;
				SetProperty(ref driveFreeSpace, value);
			}
		}

		private string driveFreeSpaceLongSize;
		public string DriveFreeSpaceLongSize
		{
			get => driveFreeSpaceLongSize;
			set
			{
				SetProperty(ref driveFreeSpaceLongSize, value);
			}
		}

		public bool driveFreeSpaceVisibility = false;
		public bool DriveFreeSpaceVisibility
		{
			get => driveFreeSpaceVisibility;
			set => SetProperty(ref driveFreeSpaceVisibility, value);
		}

		public string ItemCreatedTimestamp { get; private set; }

		private DateTimeOffset itemCreatedTimestampReal;
		public DateTimeOffset ItemCreatedTimestampReal
		{
			get => itemCreatedTimestampReal;
			set
			{
				ItemCreatedTimestampVisibility = true;
				SetProperty(ref itemCreatedTimestampReal, value);
				ItemCreatedTimestamp = dateTimeFormatter.ToShortLabel(value);
				OnPropertyChanged(nameof(ItemCreatedTimestamp));
			}
		}

		public bool itemCreatedTimestampVisibility = false;
		public bool ItemCreatedTimestampVisibility
		{
			get => itemCreatedTimestampVisibility;
			set => SetProperty(ref itemCreatedTimestampVisibility, value);
		}

		public string ItemModifiedTimestamp { get; private set; }

		private DateTimeOffset itemModifiedTimestampReal;
		public DateTimeOffset ItemModifiedTimestampReal
		{
			get => itemModifiedTimestampReal;
			set
			{
				ItemModifiedTimestampVisibility = true;
				SetProperty(ref itemModifiedTimestampReal, value);
				ItemModifiedTimestamp = dateTimeFormatter.ToShortLabel(value);
				OnPropertyChanged(nameof(ItemModifiedTimestamp));
			}
		}

		private bool itemModifiedTimestampVisibility = false;
		public bool ItemModifiedTimestampVisibility
		{
			get => itemModifiedTimestampVisibility;
			set => SetProperty(ref itemModifiedTimestampVisibility, value);
		}

		public string ItemAccessedTimestamp { get; private set; }

		public DateTimeOffset itemAccessedTimestampReal;
		public DateTimeOffset ItemAccessedTimestampReal
		{
			get => itemAccessedTimestampReal;
			set
			{
				ItemAccessedTimestampVisibility = true;
				SetProperty(ref itemAccessedTimestampReal, value);
				ItemAccessedTimestamp = dateTimeFormatter.ToShortLabel(value);
				OnPropertyChanged(nameof(ItemAccessedTimestamp));
			}
		}

		private bool itemAccessedTimestampVisibility = false;
		public bool ItemAccessedTimestampVisibility
		{
			get => itemAccessedTimestampVisibility;
			set => SetProperty(ref itemAccessedTimestampVisibility, value);
		}

		private bool lastSeparatorVisibility = true;
		public bool LastSeparatorVisibility
		{
			get => lastSeparatorVisibility;
			set => SetProperty(ref lastSeparatorVisibility, value);
		}

		private ulong driveCapacityValue;
		public ulong DriveCapacityValue
		{
			get => driveCapacityValue;
			set
			{
				SetProperty(ref driveCapacityValue, value);
				DriveCapacity = DriveCapacityValue.ToSizeString();
				DriveCapacityLongSize = DriveCapacityValue.ToLongSizeString();
				OnPropertyChanged(nameof(DrivePercentageValue));
			}
		}

		private string driveCapacity;
		public string DriveCapacity
		{
			get => driveCapacity;
			set
			{
				DriveCapacityVisibility = true;
				SetProperty(ref driveCapacity, value);
			}
		}

		private string driveCapacityLongSize;
		public string DriveCapacityLongSize
		{
			get => driveCapacityLongSize;
			set
			{
				SetProperty(ref driveCapacityLongSize, value);
			}
		}

		public bool driveCapacityVisibility = false;
		public bool DriveCapacityVisibility
		{
			get => driveCapacityVisibility;
			set => SetProperty(ref driveCapacityVisibility, value);
		}

		public double DrivePercentageValue
		{
			get => DriveCapacityValue > 0 ? DriveUsedSpaceValue / (double)DriveCapacityValue * 100 : 0;
		}

		private bool cleanupVisibility = false;
		public bool CleanupVisibility
		{
			get => cleanupVisibility;
			set => SetProperty(ref cleanupVisibility, value);
		}

		private ICommand cleanupDriveCommand;
		public ICommand CleanupDriveCommand
		{
			get => cleanupDriveCommand;
			set => SetProperty(ref cleanupDriveCommand, value);
		}

		private bool formatVisibility = false;
		public bool FormatVisibility
		{
			get => formatVisibility;
			set => SetProperty(ref formatVisibility, value);
		}

		private ICommand formatDriveCommand;
		public ICommand FormatDriveCommand
		{
			get => formatDriveCommand;
			set => SetProperty(ref formatDriveCommand, value);
		}

		private ICommand editAlbumCoverCommand;
		public ICommand EditAlbumCoverCommand
		{
			get => editAlbumCoverCommand;
			set => SetProperty(ref editAlbumCoverCommand, value);
		}

		private bool itemAttributesVisibility = true;
		public bool ItemAttributesVisibility
		{
			get => itemAttributesVisibility;
			set => SetProperty(ref itemAttributesVisibility, value);
		}

		private string selectedItemsCountString;
		public string SelectedItemsCountString
		{
			get => selectedItemsCountString;
			set => SetProperty(ref selectedItemsCountString, value);
		}

		private int selectedItemsCount;
		public int SelectedItemsCount
		{
			get => selectedItemsCount;
			set => SetProperty(ref selectedItemsCount, value);
		}

		private bool isItemSelected;
		public bool IsItemSelected
		{
			get => isItemSelected;
			set => SetProperty(ref isItemSelected, value);
		}

		public SelectedItemsPropertiesViewModel()
		{
		}

		private bool isSelectedItemImage = false;
		public bool IsCompatibleToSetAsWindowsWallpaper
		{
			get => isSelectedItemImage;
			set => SetProperty(ref isSelectedItemImage, value);
		}

		private bool isSelectedItemShortcut = false;
		public bool IsSelectedItemShortcut
		{
			get => isSelectedItemShortcut;
			set => SetProperty(ref isSelectedItemShortcut, value);
		}

		public void CheckAllFileExtensions(List<string> itemExtensions)
		{
			// Checks if all the item extensions are image extensions of some kind.
			IsCompatibleToSetAsWindowsWallpaper = itemExtensions.TrueForAll(FileExtensionHelpers.IsCompatibleToSetAsWindowsWallpaper);
			// Checks if there is only one selected item and if it's a shortcut.
			IsSelectedItemShortcut = (itemExtensions.Count == 1) && (itemExtensions.TrueForAll(itemExtension => FileExtensionHelpers.IsShortcutFile(itemExtension)));
		}

		private string shortcutItemType;
		public string ShortcutItemType
		{
			get => shortcutItemType;
			set => SetProperty(ref shortcutItemType, value);
		}

		private string shortcutItemPath;
		public string ShortcutItemPath
		{
			get => shortcutItemPath;
			set
			{
				SetProperty(ref shortcutItemPath, value);
				ShortcutItemPathEditedValue = value;
			}
		}

		private string shortcutItemPathEditedValue;
		public string ShortcutItemPathEditedValue
		{
			get => shortcutItemPathEditedValue;
			set => SetProperty(ref shortcutItemPathEditedValue, value);
		}

		private bool isShortcutItemPathReadOnly;
		public bool IsShortcutItemPathReadOnly
		{
			get => isShortcutItemPathReadOnly;
			set => SetProperty(ref isShortcutItemPathReadOnly, value);
		}

		private string shortcutItemWorkingDir;
		public string ShortcutItemWorkingDir
		{
			get => shortcutItemWorkingDir;
			set
			{
				SetProperty(ref shortcutItemWorkingDir, value);
				ShortcutItemWorkingDirEditedValue = value;
			}
		}

		private string shortcutItemWorkingDirEditedValue;
		public string ShortcutItemWorkingDirEditedValue
		{
			get => shortcutItemWorkingDirEditedValue;
			set => SetProperty(ref shortcutItemWorkingDirEditedValue, value);
		}

		private bool shortcutItemWorkingDirVisibility = false;
		public bool ShortcutItemWorkingDirVisibility
		{
			get => shortcutItemWorkingDirVisibility;
			set => SetProperty(ref shortcutItemWorkingDirVisibility, value);
		}

		private string shortcutItemArguments;
		public string ShortcutItemArguments
		{
			get => shortcutItemArguments;
			set
			{
				SetProperty(ref shortcutItemArguments, value);
				ShortcutItemArgumentsEditedValue = value;
			}
		}

		private string shortcutItemArgumentsEditedValue;
		public string ShortcutItemArgumentsEditedValue
		{
			get => shortcutItemArgumentsEditedValue;
			set
			{
				SetProperty(ref shortcutItemArgumentsEditedValue, value);
			}
		}

		private bool shortcutItemArgumentsVisibility = false;
		public bool ShortcutItemArgumentsVisibility
		{
			get => shortcutItemArgumentsVisibility;
			set => SetProperty(ref shortcutItemArgumentsVisibility, value);
		}
		
		private bool shortcutItemWindowArgsVisibility = false;
		public bool ShortcutItemWindowArgsVisibility
		{
			get => shortcutItemWindowArgsVisibility;
			set => SetProperty(ref shortcutItemWindowArgsVisibility, value);
		}

		private RelayCommand shortcutItemOpenLinkCommand;
		public RelayCommand ShortcutItemOpenLinkCommand
		{
			get => shortcutItemOpenLinkCommand;
			set
			{
				SetProperty(ref shortcutItemOpenLinkCommand, value);
			}
		}

		public bool ContainsFilesOrFolders { get; set; }

		public Uri FolderIconSource
		{
			get
			{
				return ContainsFilesOrFolders ? new Uri("ms-appx:///Assets/FolderIcon2.svg") : new Uri("ms-appx:///Assets/FolderIcon.svg");
			}
		}

		private ObservableCollection<FilePropertySection> propertySections = [];
		public ObservableCollection<FilePropertySection> PropertySections
		{
			get => propertySections;
			set => SetProperty(ref propertySections, value);
		}

		private ObservableCollection<FileProperty> fileProperties = [];
		public ObservableCollection<FileProperty> FileProperties
		{
			get => fileProperties;
			set => SetProperty(ref fileProperties, value);
		}

		private bool? isReadOnly;
		public bool? IsReadOnly
		{
			get => isReadOnly;
			set
			{
				IsReadOnlyEnabled = true;
				SetProperty(ref isReadOnly, value);
				IsReadOnlyEditedValue = value;
			}
		}

		private bool? isReadOnlyEditedValue;
		public bool? IsReadOnlyEditedValue
		{
			get => isReadOnlyEditedValue;
			set
			{
				IsReadOnlyEnabled = true;
				SetProperty(ref isReadOnlyEditedValue, value);
			}
		}

		private bool isReadOnlyEnabled;
		public bool IsReadOnlyEnabled
		{
			get => isReadOnlyEnabled;
			set => SetProperty(ref isReadOnlyEnabled, value);
		}

		private bool? isHidden;
		public bool? IsHidden
		{
			get => isHidden;
			set
			{
				SetProperty(ref isHidden, value);
				IsHiddenEditedValue = value;
			}
		}

		private bool? isHiddenEditedValue;
		public bool? IsHiddenEditedValue
		{
			get => isHiddenEditedValue;
			set => SetProperty(ref isHiddenEditedValue, value);
		}

		private bool? isContentCompressed;
		/// <remarks>
		/// Applies to NTFS item compression.
		/// </remarks>
		public bool? IsContentCompressed
		{
			get => isContentCompressed;
			set
			{
				SetProperty(ref isContentCompressed, value);
				IsContentCompressedEditedValue = value;
			}
		}

		private bool? isContentCompressedEditedValue;
		/// <remarks>
		/// Applies to NTFS item compression.
		/// </remarks>
		public bool? IsContentCompressedEditedValue
		{
			get => isContentCompressedEditedValue;
			set => SetProperty(ref isContentCompressedEditedValue, value);
		}

		private bool canCompressContent;
		/// <remarks>
		/// Applies to NTFS item compression.
		/// </remarks>
		public bool CanCompressContent
		{
			get => canCompressContent;
			set => SetProperty(ref canCompressContent, value);
		}

		private bool runAsAdmin;
		public bool RunAsAdmin
		{
			get => runAsAdmin;
			set
			{
				RunAsAdminEnabled = true;
				SetProperty(ref runAsAdmin, value);
				RunAsAdminEditedValue = value;
			}
		}

		private bool runAsAdminEditedValue;
		public bool RunAsAdminEditedValue
		{
			get => runAsAdminEditedValue;
			set
			{
				RunAsAdminEnabled = true;
				SetProperty(ref runAsAdminEditedValue, value);
			}
		}

		private bool runAsAdminEnabled;
		public bool RunAsAdminEnabled
		{
			get => runAsAdminEnabled;
			set => SetProperty(ref runAsAdminEnabled, value);
		}

		private static readonly IReadOnlyDictionary<SHOW_WINDOW_CMD, string> showWindowCommandTypes = new Dictionary<SHOW_WINDOW_CMD, string>
		{
			{ SHOW_WINDOW_CMD.SW_NORMAL, Strings.NormalWindow.GetLocalizedResource() },
			{ SHOW_WINDOW_CMD.SW_SHOWMINNOACTIVE, Strings.Minimized.GetLocalizedResource() },
			{ SHOW_WINDOW_CMD.SW_MAXIMIZE, Strings.Maximized.GetLocalizedResource() }
		}.AsReadOnly();

		/// <summary>
		/// The available show window command types.
		/// </summary>
		public IReadOnlyDictionary<SHOW_WINDOW_CMD, string> ShowWindowCommandTypes { get => showWindowCommandTypes; }

		/// <summary>
		/// The localized string of the currently selected ShowWindowCommand.
		/// This value can be used for display in the UI.
		/// </summary>
		public string SelectedShowWindowCommand
		{
			get => ShowWindowCommandTypes.GetValueOrDefault(ShowWindowCommandEditedValue)!;
			set => ShowWindowCommandEditedValue = ShowWindowCommandTypes.First(e => e.Value == value).Key;
		}

		private SHOW_WINDOW_CMD showWindowCommand;
		/// <summary>
		/// The current <see cref="SHOW_WINDOW_CMD"/> property of the item.
		/// </summary>
		public SHOW_WINDOW_CMD ShowWindowCommand
		{
			get => showWindowCommand;
			set
			{
				if (SetProperty(ref showWindowCommand, value))
					ShowWindowCommandEditedValue = value;
			}
		}

		private SHOW_WINDOW_CMD showWindowCommandEditedValue;
		/// <summary>
		/// The edited <see cref="SHOW_WINDOW_CMD"/> property of the item.
		/// </summary>
		public SHOW_WINDOW_CMD ShowWindowCommandEditedValue
		{
			get => showWindowCommandEditedValue;
			set
			{
				if (SetProperty(ref showWindowCommandEditedValue, value))
					OnPropertyChanged(nameof(SelectedShowWindowCommand));
			}
		}

		private bool isPropertiesLoaded;
		public bool IsPropertiesLoaded
		{
			get => isPropertiesLoaded;
			set => SetProperty(ref isPropertiesLoaded, value);
		}

		private bool isDownloadedFile;
		public bool IsDownloadedFile
		{
			get => isDownloadedFile;
			set => SetProperty(ref isDownloadedFile, value);
		}

		private bool isUnblockFileSelected;
		public bool IsUnblockFileSelected
		{
			get => isUnblockFileSelected;
			set => SetProperty(ref isUnblockFileSelected, value);
		}

		private bool isAblumCoverModified;
		public bool IsAblumCoverModified
		{
			get => isAblumCoverModified;
			set => SetProperty(ref isAblumCoverModified, value);
		}

		private bool isEditAlbumCoverVisible;
		public bool IsEditAlbumCoverVisible
		{
			get => isEditAlbumCoverVisible;
			set => SetProperty(ref isEditAlbumCoverVisible, value);
		}

		private Picture modifiedAlbumCover;
		public Picture ModifiedAlbumCover
		{
			get => modifiedAlbumCover;
			set => SetProperty(ref modifiedAlbumCover, value);
		}
	}
}
