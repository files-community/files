// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using ByteSize = ByteSizeLib.ByteSize;

namespace Files.App.Data.Items
{
	public sealed class DriveItem : ObservableObject, INavigationControlItem, ILocatableFolder
	{
		private BitmapImage icon;
		public BitmapImage Icon
		{
			get => icon;
			set
			{
				SetProperty(ref icon, value, nameof(Icon));
				OnPropertyChanged(nameof(IconSource));
			}
		}

		public byte[] IconData { get; set; }

		private string path;
		public string Path
		{
			get => path;
			set => path = value;
		}

		public string DeviceID { get; set; }

		public StorageFolder Root { get; set; }

		public NavigationControlItemType ItemType { get; set; } = NavigationControlItemType.Drive;

		public Visibility ItemVisibility { get; set; } = Visibility.Visible;

		public bool IsRemovable
			=> Type == DriveType.Removable || Type == DriveType.CDRom;

		public bool IsNetwork
			=> Type == DriveType.Network;

		public bool IsPinned
			=> App.QuickAccessManager.Model.PinnedFolders.Contains(path);

		public string MaxSpaceText
			=> MaxSpace.ToSizeString();

		public string FreeSpaceText
			=> FreeSpace.ToSizeString();

		public string UsedSpaceText
			=> SpaceUsed.ToSizeString();

		private ByteSize maxSpace;
		public ByteSize MaxSpace
		{
			get => maxSpace;
			set
			{
				if (SetProperty(ref maxSpace, value))
				{
					if (Type != DriveType.CloudDrive)
					{
						ToolTip = GetSizeString();
					}

					OnPropertyChanged(nameof(MaxSpaceText));
					OnPropertyChanged(nameof(ShowDriveDetails));
				}
			}
		}

		private ByteSize freeSpace;
		public ByteSize FreeSpace
		{
			get => freeSpace;
			set
			{
				if (SetProperty(ref freeSpace, value))
				{
					if (Type != DriveType.CloudDrive)
					{
						ToolTip = GetSizeString();
					}

					OnPropertyChanged(nameof(FreeSpaceText));
				}
			}
		}

		private ByteSize spaceUsed;
		public ByteSize SpaceUsed
		{
			get => spaceUsed;
			set
			{
				if (SetProperty(ref spaceUsed, value))
				{
					OnPropertyChanged(nameof(UsedSpaceText));
				}
			}
		}

		public bool ShowDriveDetails
			=> MaxSpace.Bytes > 0d;

		private DriveType type;
		public DriveType Type
		{
			get => type;
			set
			{
				type = value;

				if (value is DriveType.Network or DriveType.CloudDrive)
					ToolTip = Text;

				OnPropertyChanged(nameof(TypeText));
			}
		}

		public string TypeText => string.Format("DriveType{0}", Type).GetLocalizedResource();

		private string filesystem = string.Empty;
		public string Filesystem
		{
			get => filesystem;
			set => SetProperty(ref filesystem, value);
		}

		private string text;
		public string Text
		{
			get => text;
			set => SetProperty(ref text, value);
		}

		private string spaceText;
		public string SpaceText
		{
			get => spaceText;
			set => SetProperty(ref spaceText, value);
		}

		public SectionType Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

		private float percentageUsed = 0.0f;
		public float PercentageUsed
		{
			get => percentageUsed;
			set
			{
				if (!SetProperty(ref percentageUsed, value))
					return;

				if (Type == DriveType.Fixed)
					ShowStorageSense = percentageUsed >= Constants.Widgets.Drives.LowStorageSpacePercentageThreshold;
			}
		}

		private bool showStorageSense = false;
		public bool ShowStorageSense
		{
			get => showStorageSense;
			set => SetProperty(ref showStorageSense, value);
		}

		public string Id => DeviceID;

		public string Name => Root.DisplayName;

		public object? Children => null;

		private object toolTip = "";
		public object ToolTip
		{
			get => toolTip;
			set
			{
				SetProperty(ref toolTip, value);
			}
		}

		public bool IsExpanded { get => false; set { } }

		public IconSource? IconSource
		{
			get => new ImageIconSource()
			{
				ImageSource = Icon
			};
		}

		public FrameworkElement? ItemDecorator
		{
			get
			{
				if (!IsRemovable)
					return null; // Non-removable items don't need the eject button

				var itemDecorator = new Button()
				{
					Style = Application.Current.Resources["SidebarEjectButtonStyle"] as Style,
					Content = new ThemedIcon()
					{
						Style = Application.Current.Resources["App.ThemedIcons.Actions.Eject.12"] as Style,
						Height = 12,
						Width = 12
					}
				};

				ToolTipService.SetToolTip(itemDecorator, "Eject".GetLocalizedResource());

				itemDecorator.Click += ItemDecorator_Click;

				return itemDecorator;
			}
		}

		private void ItemDecorator_Click(object sender, RoutedEventArgs e)
		{
			DriveHelpers.EjectDeviceAsync(Path);
		}

		public static async Task<DriveItem> CreateFromPropertiesAsync(StorageFolder root, string deviceId, string label, DriveType type, IRandomAccessStream imageStream = null)
		{
			var item = new DriveItem();

			if (imageStream is not null)
				item.IconData = await imageStream.ToByteArrayAsync();

			item.Text = type switch
			{
				DriveType.CDRom when !string.IsNullOrEmpty(label) => root.DisplayName.Replace(label.Left(32), label),
				_ => root.DisplayName
			};
			item.Type = type;
			item.MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowEjectDevice = item.IsRemovable,
				ShowShellItems = true,
				ShowProperties = true
			};
			item.Path = string.IsNullOrEmpty(root.Path) ? $"\\\\?\\{root.Name}\\" : root.Path;
			item.DeviceID = deviceId;
			item.Root = root;

			_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(item.UpdatePropertiesAsync);

			return item;
		}

		public async Task UpdateLabelAsync()
		{
			try
			{
				var properties = await Root.Properties.RetrievePropertiesAsync(["System.ItemNameDisplay"])
					.AsTask().WithTimeoutAsync(TimeSpan.FromSeconds(5));
				Text = (string)properties["System.ItemNameDisplay"];
			}
			catch (NullReferenceException)
			{
			}
		}

		public async Task UpdatePropertiesAsync()
		{
			try
			{
				var properties = await Root.Properties.RetrievePropertiesAsync(["System.FreeSpace", "System.Capacity", "System.Volume.FileSystem"])
					.AsTask().WithTimeoutAsync(TimeSpan.FromSeconds(5));

				if (properties is not null && properties["System.Capacity"] is not null && properties["System.FreeSpace"] is not null)
				{
					MaxSpace = ByteSize.FromBytes((ulong)properties["System.Capacity"]);
					FreeSpace = ByteSize.FromBytes((ulong)properties["System.FreeSpace"]);
					SpaceUsed = MaxSpace - FreeSpace;

					SpaceText = GetSizeString();

					if (MaxSpace.Bytes > 0 && FreeSpace.Bytes > 0) // Make sure we don't divide by 0
						PercentageUsed = 100.0f - (float)(FreeSpace.Bytes / MaxSpace.Bytes) * 100.0f;
				}
				else
				{
					SpaceText = "Unknown".GetLocalizedResource();
					MaxSpace = SpaceUsed = FreeSpace = ByteSize.FromBytes(0);
				}

				if (properties is not null && properties["System.Volume.FileSystem"] is not null)
					Filesystem = (string)properties["System.Volume.FileSystem"];
				else
					Filesystem = string.Empty;

				OnPropertyChanged(nameof(ShowDriveDetails));
			}
			catch (Exception)
			{
				SpaceText = "Unknown".GetLocalizedResource();
				MaxSpace = SpaceUsed = FreeSpace = ByteSize.FromBytes(0);
				Filesystem = string.Empty;

				OnPropertyChanged(nameof(ShowDriveDetails));
			}
		}

		public int CompareTo(INavigationControlItem other)
		{
			var result = Type.CompareTo((other as DriveItem)?.Type ?? Type);
			return result == 0 ? Text.CompareTo(other.Text) : result;
		}

		public async Task LoadThumbnailAsync()
		{
			if (!string.IsNullOrEmpty(DeviceID) && !string.Equals(DeviceID, "network-folder"))
			{
				var result = await FileThumbnailHelper.GetIconAsync(
					DeviceID,
					Constants.ShellIconSizes.Small,
					false,
					IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);

				IconData ??= result;
			}

			if (Root is not null)
			{
				using var thumbnail = await DriveHelpers.GetThumbnailAsync(Root);
				IconData ??= thumbnail is not null ? await thumbnail.ToByteArrayAsync() : null;
			}

			if (string.Equals(DeviceID, "network-folder"))
				IconData ??= UIHelpers.GetSidebarIconResourceInfo(Constants.ImageRes.Network)?.IconData;

			IconData ??= UIHelpers.GetSidebarIconResourceInfo(Constants.ImageRes.Folder)?.IconData;

			Icon ??= IconData is not null ? await IconData.ToBitmapAsync() : null;
		}

		private string GetSizeString()
		{
			return string.Format(
				"DriveFreeSpaceAndCapacity".GetLocalizedResource(),
				FreeSpace.ToSizeString(),
				MaxSpace.ToSizeString());
		}

		public Task<INestedFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
		{
			var folder = new WindowsStorageFolderLegacy(Root);
			return folder.GetFileAsync(fileName, cancellationToken);
		}

		public Task<INestedFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default)
		{
			var folder = new WindowsStorageFolderLegacy(Root);
			return folder.GetFolderAsync(folderName, cancellationToken);
		}

		public IAsyncEnumerable<INestedStorable> GetItemsAsync(StorableKind kind = StorableKind.All, CancellationToken cancellationToken = default)
		{
			var folder = new WindowsStorageFolderLegacy(Root);
			return folder.GetItemsAsync(kind, cancellationToken);
		}

		public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			var folder = new WindowsStorageFolderLegacy(Root);
			return folder.GetParentAsync(cancellationToken);
		}
	}

	public enum DriveType
	{
		Fixed,
		Removable,
		Network,
		Ram,
		CDRom,
		FloppyDisk,
		Unknown,
		NoRootDirectory,
		VirtualDrive,
		CloudDrive,
	}
}
