// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Files.Shared.Helpers;
using FluentFTP;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Drawing;
using System.IO;
using System.Text;
using Windows.Storage;
using Windows.Win32.UI.WindowsAndMessaging;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Files.App.Utils
{
	public class ListedItem : ObservableObject, IGroupableItem
	{
		protected static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		protected static IStartMenuService StartMenuService { get; } = Ioc.Default.GetRequiredService<IStartMenuService>();

		protected static readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

		protected static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetRequiredService<IDateTimeFormatter>();

		public bool IsHiddenItem { get; set; } = false;

		public StorageItemTypes PrimaryItemAttribute { get; set; }

		private volatile int itemPropertiesInitialized = 0;
		public bool ItemPropertiesInitialized
		{
			get => itemPropertiesInitialized == 1;
			set => Interlocked.Exchange(ref itemPropertiesInitialized, value ? 1 : 0);
		}

		public string ItemTooltipText
		{
			get
			{
				var tooltipBuilder = new StringBuilder();
				tooltipBuilder.AppendLine($"{"NameWithColon".GetLocalizedResource()} {Name}");
				tooltipBuilder.AppendLine($"{"ItemType".GetLocalizedResource()} {itemType}");
				tooltipBuilder.Append($"{"ToolTipDescriptionDate".GetLocalizedResource()} {ItemDateModified}");
				if (!string.IsNullOrWhiteSpace(FileSize))
					tooltipBuilder.Append($"{Environment.NewLine}{"SizeLabel".GetLocalizedResource()} {FileSize}");
				if (SyncStatusUI.SyncStatus is not CloudDriveSyncStatus.FileOnline and not CloudDriveSyncStatus.FolderOnline && !string.IsNullOrWhiteSpace(DimensionsDisplay))
					tooltipBuilder.Append($"{Environment.NewLine}{"PropertyDimensionsColon".GetLocalizedResource()} {DimensionsDisplay}");
				if (SyncStatusUI.LoadSyncStatus)
					tooltipBuilder.Append($"{Environment.NewLine}{"StatusWithColon".GetLocalizedResource()} {syncStatusUI.SyncStatusString}");

				return tooltipBuilder.ToString();
			}
		}

		public string FolderRelativeId { get; set; }

		public bool ContainsFilesOrFolders { get; set; } = true;

		private bool needsPlaceholderGlyph = true;
		public bool NeedsPlaceholderGlyph
		{
			get => needsPlaceholderGlyph;
			set => SetProperty(ref needsPlaceholderGlyph, value);
		}

		private bool loadFileIcon;
		public bool LoadFileIcon
		{
			get => loadFileIcon;
			set => SetProperty(ref loadFileIcon, value);
		}

		private bool loadCustomIcon;
		public bool LoadCustomIcon
		{
			get => loadCustomIcon;
			set => SetProperty(ref loadCustomIcon, value);
		}

		// Note: Never attempt to call this from a secondary window or another thread, create a new instance from CustomIconSource instead
		// TODO: eventually we should remove this b/c it's not thread safe
		private BitmapImage customIcon;
		public BitmapImage CustomIcon
		{
			get => customIcon;
			set
			{
				LoadCustomIcon = true;
				SetProperty(ref customIcon, value);
			}
		}

		public ulong? FileFRN { get; set; }

		private string[] fileTags = null!;
		public string[] FileTags
		{
			get => fileTags;
			set
			{
				// fileTags is null when the item is first created
				var fileTagsInitialized = fileTags is not null;
				if (SetProperty(ref fileTags, value))
				{
					Debug.Assert(value != null);

					// only set the tags if the file tags have been changed
					if (fileTagsInitialized)
					{
						var dbInstance = FileTagsHelper.GetDbInstance();
						dbInstance.SetTags(ItemPath, FileFRN, value);
						FileTagsHelper.WriteFileTag(ItemPath, value);
					}

					HasTags = !FileTags.IsEmpty();
					OnPropertyChanged(nameof(FileTagsUI));
				}
			}
		}

		public IList<TagViewModel>? FileTagsUI
		{
			get => fileTagsSettingsService.GetTagsByIds(FileTags);
		}

		private Uri customIconSource;
		public Uri CustomIconSource
		{
			get => customIconSource;
			set => SetProperty(ref customIconSource, value);
		}

		private double opacity;
		public double Opacity
		{
			get => opacity;
			set => SetProperty(ref opacity, value);
		}

		private bool hasTags;
		public bool HasTags
		{
			get => hasTags;
			set => SetProperty(ref hasTags, value);
		}

		private CloudDriveSyncStatusUI syncStatusUI = new();
		public CloudDriveSyncStatusUI SyncStatusUI
		{
			get => syncStatusUI;
			set
			{
				// For some reason this being null will cause a crash with bindings
				value ??= new CloudDriveSyncStatusUI();
				if (SetProperty(ref syncStatusUI, value))
				{
					OnPropertyChanged(nameof(SyncStatusString));
					OnPropertyChanged(nameof(ItemTooltipText));
				}
			}
		}

		// This is used to avoid passing a null value to AutomationProperties.Name, which causes a crash
		public string SyncStatusString
		{
			get => string.IsNullOrEmpty(SyncStatusUI?.SyncStatusString) ? "CloudDriveSyncStatus_Unknown".GetLocalizedResource() : SyncStatusUI.SyncStatusString;
		}

		private BitmapImage fileImage;
		public BitmapImage FileImage
		{
			get => fileImage;
			set
			{
				if (SetProperty(ref fileImage, value))
				{
					if (value is BitmapImage)
					{
						LoadFileIcon = true;
						NeedsPlaceholderGlyph = false;
					}
				}
			}
		}

		public bool IsItemPinnedToStart => StartMenuService.IsPinned((this as ShortcutItem)?.TargetPath ?? ItemPath);

		private BitmapImage iconOverlay;
		public BitmapImage IconOverlay
		{
			get => iconOverlay;
			set
			{
				if (value is not null)
				{
					SetProperty(ref iconOverlay, value);
				}
			}
		}

		private BitmapImage shieldIcon;
		public BitmapImage ShieldIcon
		{
			get => shieldIcon;
			set
			{
				if (value is not null)
				{
					SetProperty(ref shieldIcon, value);
				}
			}
		}

		private string itemPath;
		public string ItemPath
		{
			get => itemPath;
			set => SetProperty(ref itemPath, value);
		}

		private string itemNameRaw;
		public string ItemNameRaw
		{
			get => itemNameRaw;
			set
			{
				if (SetProperty(ref itemNameRaw, value))
				{
					OnPropertyChanged(nameof(Name));
				}
			}
		}

		public virtual string Name
		{
			get
			{
				if (PrimaryItemAttribute == StorageItemTypes.File)
				{
					var nameWithoutExtension = Path.GetFileNameWithoutExtension(itemNameRaw);
					if (!string.IsNullOrEmpty(nameWithoutExtension) && !UserSettingsService.FoldersSettingsService.ShowFileExtensions)
					{
						return nameWithoutExtension;
					}
				}
				return itemNameRaw;
			}
		}

		private string itemType;
		public string ItemType
		{
			get => itemType;
			set
			{
				if (value is not null)
				{
					SetProperty(ref itemType, value);
				}
			}
		}

		public string FileExtension { get; set; }

		private string fileSize;
		public string FileSize
		{
			get => fileSize;
			set
			{
				SetProperty(ref fileSize, value);
				OnPropertyChanged(nameof(FileSizeDisplay));
			}
		}

		public string FileSizeDisplay => string.IsNullOrEmpty(FileSize) ? "ItemSizeNotCalculated".GetLocalizedResource() : FileSize;

		public long FileSizeBytes { get; set; }

		public string ItemDateModified { get; private set; }

		public string ItemDateCreated { get; private set; }

		public string ItemDateAccessed { get; private set; }

		private DateTimeOffset itemDateModifiedReal;
		public DateTimeOffset ItemDateModifiedReal
		{
			get => itemDateModifiedReal;
			set
			{
				ItemDateModified = dateTimeFormatter.ToShortLabel(value);
				itemDateModifiedReal = value;
				OnPropertyChanged(nameof(ItemDateModified));
			}
		}

		private DateTimeOffset itemDateCreatedReal;
		public DateTimeOffset ItemDateCreatedReal
		{
			get => itemDateCreatedReal;
			set
			{
				ItemDateCreated = dateTimeFormatter.ToShortLabel(value);
				itemDateCreatedReal = value;
				OnPropertyChanged(nameof(ItemDateCreated));
			}
		}

		private DateTimeOffset itemDateAccessedReal;
		public DateTimeOffset ItemDateAccessedReal
		{
			get => itemDateAccessedReal;
			set
			{
				ItemDateAccessed = dateTimeFormatter.ToShortLabel(value);
				itemDateAccessedReal = value;
				OnPropertyChanged(nameof(ItemDateAccessed));
			}
		}

		private ObservableCollection<FileProperty> itemProperties;
		public ObservableCollection<FileProperty> ItemProperties
		{
			get => itemProperties;
			set => SetProperty(ref itemProperties, value);
		}

		public string DimensionsDisplay
		{
			get
			{
				int imageHeight = 0;
				int imageWidth = 0;

				var isImageFile = FileExtensionHelpers.IsImageFile(FileExtension);
				if (isImageFile)
				{
					try
					{
						// TODO: Consider to use 'System.Kind' instead.
						using FileStream fileStream = new(ItemPath, FileMode.Open, FileAccess.Read, FileShare.Read);
						using Image image = Image.FromStream(fileStream, false, false);

						if (image is not null)
						{
							imageHeight = image.Height;
							imageWidth = image.Width;
						}
					}
					catch { }
				}


				return
					isImageFile &&
					imageWidth > 0 &&
					imageHeight > 0
						? $"{imageWidth} \u00D7 {imageHeight}"
						: string.Empty;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ListedItem" /> class.
		/// </summary>
		/// <param name="folderRelativeId"></param>
		public ListedItem(string folderRelativeId) => FolderRelativeId = folderRelativeId;

		// Parameterless constructor for JsonConvert
		public ListedItem() { }

		private ObservableCollection<FileProperty> fileDetails;
		public ObservableCollection<FileProperty> FileDetails
		{
			get => fileDetails;
			set => SetProperty(ref fileDetails, value);
		}

		public override string ToString()
		{
			string suffix;
			if (IsRecycleBinItem)
			{
				suffix = "RecycleBinItemAutomation".GetLocalizedResource();
			}
			else if (IsShortcut)
			{
				suffix = "ShortcutItemAutomation".GetLocalizedResource();
			}
			else if (IsLibrary)
			{
				suffix = "Library".GetLocalizedResource();
			}
			else
			{
				suffix = PrimaryItemAttribute == StorageItemTypes.File ? "Folder".GetLocalizedResource() : "FolderItemAutomation".GetLocalizedResource();
			}

			return $"{Name}, {suffix}";
		}

		public bool IsFolder => PrimaryItemAttribute is StorageItemTypes.Folder;
		public bool IsRecycleBinItem => this is RecycleBinItem;
		public bool IsShortcut => this is IShortcutItem;
		public bool IsLibrary => this is LibraryItem;
		public bool IsLinkItem => IsShortcut && ((ShortcutItem)this).IsUrl;
		public bool IsFtpItem => this is FtpItem;
		public bool IsArchive => this is ZipItem;
		public bool IsAlternateStream => this is AlternateStreamItem;
		public bool IsGitItem => this is IGitItem;
		public virtual bool IsExecutable => !IsFolder && FileExtensionHelpers.IsExecutableFile(ItemPath);
		public virtual bool IsScriptFile => FileExtensionHelpers.IsScriptFile(ItemPath);
		public bool IsPinned => App.QuickAccessManager.Model.PinnedFolders.Contains(itemPath);
		public bool IsDriveRoot => ItemPath == PathNormalization.GetPathRoot(ItemPath);
		public bool IsElevationRequired { get; set; }

		private BaseStorageFile itemFile;
		public BaseStorageFile ItemFile
		{
			get => itemFile;
			set => SetProperty(ref itemFile, value);
		}

		// This is a hack used because x:Bind casting did not work properly
		public RecycleBinItem AsRecycleBinItem => this as RecycleBinItem;

		public GitItem AsGitItem => this as GitItem;

		public string Key { get; set; }

		/// <summary>
		/// Manually check if a folder path contains child items,
		/// updating the ContainsFilesOrFolders property from its default value of true
		/// </summary>
		public void UpdateContainsFilesFolders()
		{
			ContainsFilesOrFolders = FolderHelpers.CheckForFilesFolders(ItemPath);
		}
	}

	public sealed class RecycleBinItem : ListedItem
	{
		public RecycleBinItem(string folderRelativeId) : base(folderRelativeId)
		{
		}

		public string ItemDateDeleted { get; private set; }

		public DateTimeOffset ItemDateDeletedReal
		{
			get => itemDateDeletedReal;
			set
			{
				ItemDateDeleted = dateTimeFormatter.ToShortLabel(value);
				itemDateDeletedReal = value;
			}
		}

		private DateTimeOffset itemDateDeletedReal;

		// For recycle bin elements (path + name)
		public string ItemOriginalPath { get; set; }

		// For recycle bin elements (path)
		public string ItemOriginalFolder => Path.IsPathRooted(ItemOriginalPath) ? Path.GetDirectoryName(ItemOriginalPath) : ItemOriginalPath;

		public string ItemOriginalFolderName => Path.GetFileName(ItemOriginalFolder);
	}

	public sealed class FtpItem : ListedItem
	{
		public FtpItem(FtpListItem item, string folder) : base(null)
		{
			var isFile = item.Type == FtpObjectType.File;
			ItemDateCreatedReal = item.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawCreated;
			ItemDateModifiedReal = item.RawModified < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawModified;
			ItemNameRaw = item.Name;
			FileExtension = Path.GetExtension(item.Name);
			ItemPath = PathNormalization.Combine(folder, item.Name);
			PrimaryItemAttribute = isFile ? StorageItemTypes.File : StorageItemTypes.Folder;
			ItemPropertiesInitialized = false;

			var itemType = isFile ? "File".GetLocalizedResource() : "Folder".GetLocalizedResource();
			if (isFile && Name.Contains('.', StringComparison.Ordinal))
			{
				itemType = FileExtension.Trim('.') + " " + itemType;
			}

			ItemType = itemType;
			FileSizeBytes = item.Size;
			ContainsFilesOrFolders = !isFile;
			FileImage = null;
			FileSize = isFile ? FileSizeBytes.ToSizeString() : null;
			Opacity = 1;
			IsHiddenItem = false;
		}

		public async Task<IStorageItem> ToStorageItem() => PrimaryItemAttribute switch
		{
			StorageItemTypes.File => await new Utils.Storage.FtpStorageFile(ItemPath, ItemNameRaw, ItemDateCreatedReal).ToStorageFileAsync(),
			StorageItemTypes.Folder => new Utils.Storage.FtpStorageFolder(ItemPath, ItemNameRaw, ItemDateCreatedReal),
			_ => throw new InvalidDataException(),
		};
	}

	public sealed class ShortcutItem : ListedItem, IShortcutItem
	{
		public ShortcutItem(string folderRelativeId) : base(folderRelativeId)
		{
		}

		// Parameterless constructor for JsonConvert
		public ShortcutItem() : base()
		{ }

		// For shortcut elements (.lnk and .url)
		public string TargetPath { get; set; }

		public override string Name
			=> IsSymLink ? base.Name : Path.GetFileNameWithoutExtension(ItemNameRaw); // Always hide extension for shortcuts

		public string Arguments { get; set; }
		public string WorkingDirectory { get; set; }
		public bool RunAsAdmin { get; set; }
		public SHOW_WINDOW_CMD ShowWindowCommand { get; set; }
		public bool IsUrl { get; set; }
		public bool IsSymLink { get; set; }
		public override bool IsExecutable => FileExtensionHelpers.IsExecutableFile(TargetPath, true);
	}

	public sealed class ZipItem : ListedItem
	{
		public ZipItem(string folderRelativeId) : base(folderRelativeId)
		{
		}

		public override string Name
		{
			get
			{
				var nameWithoutExtension = Path.GetFileNameWithoutExtension(ItemNameRaw);
				if (!string.IsNullOrEmpty(nameWithoutExtension) && !UserSettingsService.FoldersSettingsService.ShowFileExtensions)
				{
					return nameWithoutExtension;
				}
				return ItemNameRaw;
			}
		}

		// Parameterless constructor for JsonConvert
		public ZipItem() : base()
		{ }
	}

	public sealed class LibraryItem : ListedItem
	{
		public LibraryItem(LibraryLocationItem library) : base(null)
		{
			ItemPath = library.Path;
			ItemNameRaw = library.Text;
			PrimaryItemAttribute = StorageItemTypes.Folder;
			ItemType = "Library".GetLocalizedResource();
			LoadCustomIcon = true;
			CustomIcon = library.Icon;
			//CustomIconSource = library.IconSource;
			LoadFileIcon = true;

			IsEmpty = library.IsEmpty;
			DefaultSaveFolder = library.DefaultSaveFolder;
			Folders = library.Folders;
		}

		public bool IsEmpty { get; }

		public string DefaultSaveFolder { get; }

		public override string Name => ItemNameRaw;

		public ReadOnlyCollection<string> Folders { get; }
	}

	public sealed class AlternateStreamItem : ListedItem
	{
		public string MainStreamPath => ItemPath.Substring(0, ItemPath.LastIndexOf(':'));
		public string MainStreamName => Path.GetFileName(MainStreamPath);

		public override string Name
		{
			get
			{
				var nameWithoutExtension = Path.GetFileNameWithoutExtension(ItemNameRaw);
				var mainStreamNameWithoutExtension = Path.GetFileNameWithoutExtension(MainStreamName);
				if (!UserSettingsService.FoldersSettingsService.ShowFileExtensions)
				{
					return $"{(string.IsNullOrEmpty(mainStreamNameWithoutExtension) ? MainStreamName : mainStreamNameWithoutExtension)}:{(string.IsNullOrEmpty(nameWithoutExtension) ? ItemNameRaw : nameWithoutExtension)}";
				}
				return $"{MainStreamName}:{ItemNameRaw}";
			}
		}
	}

	public class GitItem : ListedItem, IGitItem
	{
		private volatile int statusPropertiesInitialized = 0;
		public bool StatusPropertiesInitialized
		{
			get => statusPropertiesInitialized == 1;
			set => Interlocked.Exchange(ref statusPropertiesInitialized, value ? 1 : 0);
		}

		private volatile int commitPropertiesInitialized = 0;
		public bool CommitPropertiesInitialized
		{
			get => commitPropertiesInitialized == 1;
			set => Interlocked.Exchange(ref commitPropertiesInitialized, value ? 1 : 0);
		}

		private Style? _UnmergedGitStatusIcon;
		public Style? UnmergedGitStatusIcon
		{
			get => _UnmergedGitStatusIcon;
			set => SetProperty(ref _UnmergedGitStatusIcon, value);
		}

		private string? _UnmergedGitStatusName;
		public string? UnmergedGitStatusName
		{
			get => _UnmergedGitStatusName;
			set => SetProperty(ref _UnmergedGitStatusName, value);
		}

		private DateTimeOffset? _GitLastCommitDate;
		public DateTimeOffset? GitLastCommitDate
		{
			get => _GitLastCommitDate;
			set
			{
				SetProperty(ref _GitLastCommitDate, value);
				GitLastCommitDateHumanized = value is DateTimeOffset dto ? dateTimeFormatter.ToShortLabel(dto) : "";
			}
		}

		private string? _GitLastCommitDateHumanized;
		public string? GitLastCommitDateHumanized
		{
			get => _GitLastCommitDateHumanized;
			set => SetProperty(ref _GitLastCommitDateHumanized, value);
		}

		private string? _GitLastCommitMessage;
		public string? GitLastCommitMessage
		{
			get => _GitLastCommitMessage;
			set => SetProperty(ref _GitLastCommitMessage, value);
		}

		private string? _GitCommitAuthor;
		public string? GitLastCommitAuthor
		{
			get => _GitCommitAuthor;
			set => SetProperty(ref _GitCommitAuthor, value);
		}

		private string? _GitLastCommitSha;
		public string? GitLastCommitSha
		{
			get => _GitLastCommitSha;
			set => SetProperty(ref _GitLastCommitSha, value);
		}

		private string? _GitLastCommitFullSha;
		public string? GitLastCommitFullSha
		{
			get => _GitLastCommitFullSha;
			set => SetProperty(ref _GitLastCommitFullSha, value);
		}
	}
	public sealed class GitShortcutItem : GitItem, IShortcutItem
	{
		private volatile int statusPropertiesInitialized = 0;
		public bool StatusPropertiesInitialized
		{
			get => statusPropertiesInitialized == 1;
			set => Interlocked.Exchange(ref statusPropertiesInitialized, value ? 1 : 0);
		}

		private volatile int commitPropertiesInitialized = 0;
		public bool CommitPropertiesInitialized
		{
			get => commitPropertiesInitialized == 1;
			set => Interlocked.Exchange(ref commitPropertiesInitialized, value ? 1 : 0);
		}

		private Style? _UnmergedGitStatusIcon;
		public Style? UnmergedGitStatusIcon
		{
			get => _UnmergedGitStatusIcon;
			set => SetProperty(ref _UnmergedGitStatusIcon, value);
		}

		private string? _UnmergedGitStatusName;
		public string? UnmergedGitStatusName
		{
			get => _UnmergedGitStatusName;
			set => SetProperty(ref _UnmergedGitStatusName, value);
		}

		private DateTimeOffset? _GitLastCommitDate;
		public DateTimeOffset? GitLastCommitDate
		{
			get => _GitLastCommitDate;
			set
			{
				SetProperty(ref _GitLastCommitDate, value);
				GitLastCommitDateHumanized = value is DateTimeOffset dto ? dateTimeFormatter.ToShortLabel(dto) : "";
			}
		}

		private string? _GitLastCommitDateHumanized;
		public string? GitLastCommitDateHumanized
		{
			get => _GitLastCommitDateHumanized;
			set => SetProperty(ref _GitLastCommitDateHumanized, value);
		}

		private string? _GitLastCommitMessage;
		public string? GitLastCommitMessage
		{
			get => _GitLastCommitMessage;
			set => SetProperty(ref _GitLastCommitMessage, value);
		}

		private string? _GitCommitAuthor;
		public string? GitLastCommitAuthor
		{
			get => _GitCommitAuthor;
			set => SetProperty(ref _GitCommitAuthor, value);
		}

		private string? _GitLastCommitSha;
		public string? GitLastCommitSha
		{
			get => _GitLastCommitSha;
			set => SetProperty(ref _GitLastCommitSha, value);
		}

		private string? _GitLastCommitFullSha;
		public string? GitLastCommitFullSha
		{
			get => _GitLastCommitFullSha;
			set => SetProperty(ref _GitLastCommitFullSha, value);
		}

		public string TargetPath { get; set; }

		public override string Name
			=> IsSymLink ? base.Name : Path.GetFileNameWithoutExtension(ItemNameRaw); // Always hide extension for shortcuts

		public string Arguments { get; set; }
		public string WorkingDirectory { get; set; }
		public bool RunAsAdmin { get; set; }
		public SHOW_WINDOW_CMD ShowWindowCommand { get; set; }
		public bool IsUrl { get; set; }
		public bool IsSymLink { get; set; }
		public override bool IsExecutable => FileExtensionHelpers.IsExecutableFile(TargetPath, true);
	}
	public interface IGitItem
	{
		public bool StatusPropertiesInitialized { get ; set; }
		public bool CommitPropertiesInitialized { get; set; }

		public Style? UnmergedGitStatusIcon{ get; set; }

		public string? UnmergedGitStatusName{ get; set; }

		public DateTimeOffset? GitLastCommitDate{ get; set; }

		public string? GitLastCommitDateHumanized{ get; set; }

		public string? GitLastCommitMessage{ get; set; }

		public string? GitLastCommitAuthor{ get; set; }

		public string? GitLastCommitSha{ get; set; }

		public string? GitLastCommitFullSha{ get; set; }

		public string ItemPath
		{
			get;
			set;
		}
	}
	public interface IShortcutItem
	{
		public string TargetPath { get; set; }
		public string Name { get; }
		public string Arguments { get; set; }
		public string WorkingDirectory { get; set; }
		public bool RunAsAdmin { get; set; }
		public SHOW_WINDOW_CMD ShowWindowCommand { get; set; }
		public bool IsUrl { get; set; }
		public bool IsSymLink { get; set; }
		public bool IsExecutable { get; }
	}
}
