// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace Files.App.Data.Items
{
	public class LocationItem : ObservableObject, INavigationControlItem
	{
		public BitmapImage icon;
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

		private string text = "";
		public string Text
		{
			get => text;
			set
			{
				text = value;
				// Just in case path hasn't been set
				if (ToolTip is "")
					ToolTip = value;
			}
		}

		private string path;
		public string Path
		{
			get => path;
			set
			{
				path = value;
				ToolTip = string.IsNullOrEmpty(Path) ||
					Path.Contains('?', StringComparison.Ordinal) ||
					Path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) ||
					Path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase) ||
					Path == "Home"
					? Text
					: Path;
			}
		}

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.Location;

		public bool IsDefaultLocation { get; set; }

		public object? Children => Section == SectionType.Home ? null : ChildItems;
		public BulkConcurrentObservableCollection<INavigationControlItem>? ChildItems { get; set; }
		public IconSource? IconSource
		{
			get => new ImageIconSource()
			{
				ImageSource = icon
			};
		}

		public bool SelectsOnInvoked { get; set; } = true;

		private bool isExpanded;
		public bool IsExpanded
		{
			get => isExpanded;
			set => SetProperty(ref isExpanded, value);
		}

		public bool IsInvalid { get; set; } = false;

		public bool IsPinned => App.QuickAccessManager.Model.PinnedFolders.Contains(path);

		public SectionType Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

		public bool IsHeader { get; set; }

		private object toolTip = "";
		public virtual object ToolTip
		{
			get => toolTip;
			set
			{
				SetProperty(ref toolTip, value);
			}
		}

		public FrameworkElement? ItemDecorator
		{
			get
			{
				if (Section == SectionType.Pinned)
				{
					return new ThemedIcon()
					{
						Style = Application.Current.Resources["SidebarFavouritesPinnedIcon"] as Style
					};
				}
				return null;
			}
		}

		public int CompareTo(INavigationControlItem other)
			=> Text.CompareTo(other.Text);

		public static T Create<T>() where T : LocationItem, new()
		{
			return new T();
		}
	}

	public sealed class RecycleBinLocationItem : LocationItem
	{
		private readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();

		public async void RefreshSpaceUsed(object? sender, FileSystemEventArgs e)
		{
			await RefreshSpaceUsedAsync();
		}

		private Task RefreshSpaceUsedAsync()
		{
			return MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				SpaceUsed = await Task.Run(() => StorageTrashBinService.GetSize());
			});
		}

		private ulong spaceUsed;
		public ulong SpaceUsed
		{
			get => spaceUsed;
			set
			{
				if (SetProperty(ref spaceUsed, value))
					OnPropertyChanged(nameof(ToolTip));
			}
		}

		public override object ToolTip
		{
			get => SpaceUsed.ToSizeString();
		}

		public RecycleBinLocationItem()
		{
			StorageTrashBinService.Watcher.ItemAdded += RefreshSpaceUsed;
			StorageTrashBinService.Watcher.ItemDeleted += RefreshSpaceUsed;

			_ = RefreshSpaceUsedAsync();
		}
	}
}
