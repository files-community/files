using ByteSizeLib;
using Files.Extensions;
using Files.ViewModels.Properties;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;

namespace Files.ViewModels
{
    public class SelectedItemsPropertiesViewModel : ObservableObject
    {
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
                ItemNameVisibility = Visibility.Visible;
                SetProperty(ref itemName, value);
            }
        }

        private string originalItemName;

        public string OriginalItemName
        {
            get => originalItemName;
            set
            {
                ItemNameVisibility = Visibility.Visible;
                SetProperty(ref originalItemName, value);
            }
        }

        private Visibility itemNameVisibility = Visibility.Collapsed;

        public Visibility ItemNameVisibility
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
                ItemTypeVisibility = Visibility.Visible;
                SetProperty(ref itemType, value);
            }
        }

        private Visibility itemTypeVisibility = Visibility.Collapsed;

        public Visibility ItemTypeVisibility
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
                DriveFileSystemVisibility = Visibility.Visible;
                SetProperty(ref driveFileSystem, value);
            }
        }

        private Visibility driveFileSystemVisibility = Visibility.Collapsed;

        public Visibility DriveFileSystemVisibility
        {
            get => driveFileSystemVisibility;
            set => SetProperty(ref driveFileSystemVisibility, value);
        }

        private string itemPath;

        public string ItemPath
        {
            get => itemPath;
            set
            {
                ItemPathVisibility = Visibility.Visible;
                SetProperty(ref itemPath, value);
            }
        }

        private Visibility itemPathVisibility = Visibility.Collapsed;

        public Visibility ItemPathVisibility
        {
            get => itemPathVisibility;
            set => SetProperty(ref itemPathVisibility, value);
        }

        private string itemSize;

        public string ItemSize
        {
            get => itemSize;
            set => SetProperty(ref itemSize, value);
        }

        private Visibility itemSizeVisibility = Visibility.Collapsed;

        public Visibility ItemSizeVisibility
        {
            get => itemSizeVisibility;
            set => SetProperty(ref itemSizeVisibility, value);
        }

        private long itemSizeBytes;

        public long ItemSizeBytes
        {
            get => itemSizeBytes;
            set => SetProperty(ref itemSizeBytes, value);
        }

        private Visibility itemSizeProgressVisibility = Visibility.Collapsed;

        public Visibility ItemSizeProgressVisibility
        {
            get => itemSizeProgressVisibility;
            set => SetProperty(ref itemSizeProgressVisibility, value);
        }

        public string itemMD5Hash;

        public string ItemMD5Hash
        {
            get => itemMD5Hash;
            set
            {
                if (!string.IsNullOrEmpty(value) && value != itemMD5Hash)
                {
                    SetProperty(ref itemMD5Hash, value);
                    ItemMD5HashProgressVisibility = Visibility.Collapsed;
                }
            }
        }

        private bool itemMD5HashCalcError;

        public bool ItemMD5HashCalcError
        {
            get => itemMD5HashCalcError;
            set => SetProperty(ref itemMD5HashCalcError, value);
        }

        public Visibility itemMD5HashVisibility = Visibility.Collapsed;

        public Visibility ItemMD5HashVisibility
        {
            get => itemMD5HashVisibility;
            set => SetProperty(ref itemMD5HashVisibility, value);
        }

        public Visibility itemMD5HashProgressVisibiity = Visibility.Collapsed;

        public Visibility ItemMD5HashProgressVisibility
        {
            get => itemMD5HashProgressVisibiity;
            set => SetProperty(ref itemMD5HashProgressVisibiity, value);
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
                if (FilesAndFoldersCountVisibility == Visibility.Collapsed)
                {
                    FilesAndFoldersCountVisibility = Visibility.Visible;
                }
                SetProperty(ref filesAndFoldersCountString, value);
            }
        }

        public Visibility filesAndFoldersCountVisibility = Visibility.Collapsed;

        public Visibility FilesAndFoldersCountVisibility
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
                DriveUsedSpace = $"{ByteSize.FromBytes(DriveUsedSpaceValue).ToBinaryString().ConvertSizeAbbreviation()} ({ByteSize.FromBytes(DriveUsedSpaceValue).Bytes:#,##0} {"ItemSizeBytes".GetLocalized()})";
                var angle = ((DriveUsedSpaceValue * (ulong)DriveMaxCapacityDoubleValue) / DriveCapacityValue);
                DriveUsedSpaceDoubleValue = Convert.ToDouble(((angle * 100) / (ulong)DriveMaxCapacityDoubleValue));
            }
        }

        private string driveUsedSpace;

        public string DriveUsedSpace
        {
            get => driveUsedSpace;
            set
            {
                DriveUsedSpaceVisibiity = Visibility.Visible;
                SetProperty(ref driveUsedSpace, value);
            }
        }

        public Visibility driveUsedSpaceVisibiity = Visibility.Collapsed;

        public Visibility DriveUsedSpaceVisibiity
        {
            get => driveUsedSpaceVisibiity;
            set => SetProperty(ref driveUsedSpaceVisibiity, value);
        }

        private ulong driveFreeSpaceValue;

        public ulong DriveFreeSpaceValue
        {
            get => driveFreeSpaceValue;
            set
            {
                SetProperty(ref driveFreeSpaceValue, value);
                DriveFreeSpace = $"{ByteSize.FromBytes(DriveFreeSpaceValue).ToBinaryString().ConvertSizeAbbreviation()} ({ByteSize.FromBytes(DriveFreeSpaceValue).Bytes:#,##0} {"ItemSizeBytes".GetLocalized()})";
            }
        }

        private string driveFreeSpace;

        public string DriveFreeSpace
        {
            get => driveFreeSpace;
            set
            {
                DriveFreeSpaceVisibiity = Visibility.Visible;
                SetProperty(ref driveFreeSpace, value);
            }
        }

        public Visibility driveFreeSpaceVisibiity = Visibility.Collapsed;

        public Visibility DriveFreeSpaceVisibiity
        {
            get => driveFreeSpaceVisibiity;
            set => SetProperty(ref driveFreeSpaceVisibiity, value);
        }

        private string itemCreatedTimestamp;

        public string ItemCreatedTimestamp
        {
            get => itemCreatedTimestamp;
            set
            {
                ItemCreatedTimestampVisibiity = Visibility.Visible;
                SetProperty(ref itemCreatedTimestamp, value);
            }
        }

        public Visibility itemCreatedTimestampVisibiity = Visibility.Collapsed;

        public Visibility ItemCreatedTimestampVisibiity
        {
            get => itemCreatedTimestampVisibiity;
            set => SetProperty(ref itemCreatedTimestampVisibiity, value);
        }

        private string itemModifiedTimestamp;

        public string ItemModifiedTimestamp
        {
            get => itemModifiedTimestamp;
            set
            {
                ItemModifiedTimestampVisibility = Visibility.Visible;
                SetProperty(ref itemModifiedTimestamp, value);
            }
        }

        private Visibility itemModifiedTimestampVisibility = Visibility.Collapsed;

        public Visibility ItemModifiedTimestampVisibility
        {
            get => itemModifiedTimestampVisibility;
            set => SetProperty(ref itemModifiedTimestampVisibility, value);
        }

        public string itemAccessedTimestamp;

        public string ItemAccessedTimestamp
        {
            get => itemAccessedTimestamp;
            set
            {
                ItemAccessedTimestampVisibility = Visibility.Visible;
                SetProperty(ref itemAccessedTimestamp, value);
            }
        }

        private Visibility itemAccessedTimestampVisibility = Visibility.Collapsed;

        public Visibility ItemAccessedTimestampVisibility
        {
            get => itemAccessedTimestampVisibility;
            set => SetProperty(ref itemAccessedTimestampVisibility, value);
        }

        private Visibility lastSeparatorVisibility = Visibility.Visible;

        public Visibility LastSeparatorVisibility
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
                DriveCapacity = $"{ByteSize.FromBytes(DriveCapacityValue).ToBinaryString().ConvertSizeAbbreviation()} ({ByteSize.FromBytes(DriveCapacityValue).Bytes:#,##0} {"ItemSizeBytes".GetLocalized()})";
            }
        }

        private string driveCapacity;

        public string DriveCapacity
        {
            get => driveCapacity;
            set
            {
                DriveCapacityVisibiity = Visibility.Visible;
                SetProperty(ref driveCapacity, value);
            }
        }

        public Visibility driveCapacityVisibiity = Visibility.Collapsed;

        public Visibility DriveCapacityVisibiity
        {
            get => driveCapacityVisibiity;
            set => SetProperty(ref driveCapacityVisibiity, value);
        }

        private const int driveMaxCapacityDoubleValue = 360;

        public int DriveMaxCapacityDoubleValue
        {
            get => driveMaxCapacityDoubleValue;
        }

        private const int driveMinCapacityDoubleValue = 0;
        public int DriveMinCapacityDoubleValue
        {
            get => driveMinCapacityDoubleValue;
        }

        private double driveUsedSpaceDoubleValue;

        public double DriveUsedSpaceDoubleValue
        {
            get => driveUsedSpaceDoubleValue;
            set => SetProperty(ref driveUsedSpaceDoubleValue, value);
        }

        private Visibility itemAttributesVisibility = Visibility.Visible;

        public Visibility ItemAttributesVisibility
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

        public bool IsSelectedItemImage
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

        public void CheckFileExtension(string itemExtension)
        {
            // Set properties to false
            IsSelectedItemImage = false;
            IsSelectedItemShortcut = false;

            //check if the selected item is an image file
            if (!string.IsNullOrEmpty(itemExtension) && SelectedItemsCount == 1)
            {
                if (itemExtension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || itemExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || itemExtension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
                || itemExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    // Since item is an image, set the IsSelectedItemImage property to true
                    IsSelectedItemImage = true;
                }
                else if (itemExtension.Equals(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    // The selected item is a shortcut, so set the IsSelectedItemShortcut property to true
                    IsSelectedItemShortcut = true;
                }
            }
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
            set => SetProperty(ref shortcutItemPath, value);
        }

        private string shortcutItemWorkingDir;

        public string ShortcutItemWorkingDir
        {
            get => shortcutItemWorkingDir;
            set => SetProperty(ref shortcutItemWorkingDir, value);
        }

        private Visibility shortcutItemWorkingDirVisibility = Visibility.Collapsed;

        public Visibility ShortcutItemWorkingDirVisibility
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
            }
        }

        private Visibility shortcutItemArgumentsVisibility = Visibility.Collapsed;

        public Visibility ShortcutItemArgumentsVisibility
        {
            get => shortcutItemArgumentsVisibility;
            set => SetProperty(ref shortcutItemArgumentsVisibility, value);
        }

        private bool loadLinkIcon;

        public bool LoadLinkIcon
        {
            get => loadLinkIcon;
            set => SetProperty(ref loadLinkIcon, value);
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

        private ObservableCollection<FilePropertySection> propertySections = new ObservableCollection<FilePropertySection>();

        public ObservableCollection<FilePropertySection> PropertySections
        {
            get => propertySections;
            set => SetProperty(ref propertySections, value);
        }

        private ObservableCollection<FileProperty> fileProperties = new ObservableCollection<FileProperty>();

        public ObservableCollection<FileProperty> FileProperties
        {
            get => fileProperties;
            set => SetProperty(ref fileProperties, value);
        }

        private bool isReadOnly;

        public bool IsReadOnly
        {
            get => isReadOnly;
            set
            {
                IsReadOnlyEnabled = true;
                SetProperty(ref isReadOnly, value);
            }
        }

        private bool isReadOnlyEnabled;

        public bool IsReadOnlyEnabled
        {
            get => isReadOnlyEnabled;
            set => SetProperty(ref isReadOnlyEnabled, value);
        }

        private bool isHidden;

        public bool IsHidden
        {
            get => isHidden;
            set => SetProperty(ref isHidden, value);
        }
    }
}