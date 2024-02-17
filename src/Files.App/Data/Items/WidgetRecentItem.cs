// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;

namespace Files.App.Data.Items
{
	public class WidgetRecentItem : ObservableObject, IWidgetCardItem, IEquatable<WidgetRecentItem>
	{
		private BitmapImage _fileImg;
		public BitmapImage Thumbnail
		{
			get => _fileImg;
			set => SetProperty(ref _fileImg, value);
		}

		public string LinkPath { get; set; }    // path of shortcut item (this is unique)
		public string RecentPath { get; set; }  // path to target item
		public string Name { get; set; }
		public StorageItemTypes Type { get; set; }
		public bool FolderImg { get; set; }
		public bool EmptyImgVis { get; set; }
		public bool FileIconVis { get; set; }
		public bool IsFile { get => Type == StorageItemTypes.File; }
		public DateTime LastModified { get; set; }
		public byte[] PIDL { get; set; }
		public string Path { get => RecentPath; }

		public object Item => this;

		public WidgetRecentItem()
		{
			EmptyImgVis = true; // defer icon load to LoadRecentItemIcon()
		}

		/// <summary>
		/// Create a RecentItem instance from a link path.
		/// This is usually needed if a shortcut is deleted -- the metadata is lost (i.e. the target item).
		/// </summary>
		/// <param name="linkPath">The location that shortcut lives/lived in</param>
		public WidgetRecentItem(string linkPath) : base()
		{
			LinkPath = linkPath;
		}

		/// <summary>
		/// Create a RecentItem from a ShellLinkItem (usually from shortcuts in `Windows\Recent`)
		/// </summary>
		public WidgetRecentItem(ShellLinkItem linkItem) : base()
		{
			LinkPath = linkItem.FilePath;
			RecentPath = linkItem.TargetPath;
			Name = NameOrPathWithoutExtension(linkItem.FileName);
			Type = linkItem.IsFolder ? StorageItemTypes.Folder : ZipStorageFolder.IsZipPath(LinkPath) ? StorageItemTypes.Folder : StorageItemTypes.File;
			FolderImg = linkItem.IsFolder;
			FileIconVis = !linkItem.IsFolder;
			LastModified = linkItem.ModifiedDate;
			PIDL = linkItem.PIDL;
		}

		/// <summary>
		/// Create a RecentItem from a ShellFileItem (usually from enumerating Quick Access directly).
		/// </summary>
		/// <param name="fileItem">The shell file item</param>
		public WidgetRecentItem(ShellFileItem fileItem) : base()
		{
			LinkPath = ShellStorageFolder.IsShellPath(fileItem.FilePath) ? fileItem.RecyclePath : fileItem.FilePath; // use true path on disk for shell items
			RecentPath = LinkPath; // intentionally the same
			Name = NameOrPathWithoutExtension(fileItem.FileName);
			Type = fileItem.IsFolder ? StorageItemTypes.Folder : ZipStorageFolder.IsZipPath(LinkPath) ? StorageItemTypes.Folder : StorageItemTypes.File;
			FolderImg = fileItem.IsFolder;
			FileIconVis = !fileItem.IsFolder;
			LastModified = fileItem.ModifiedDate;
			PIDL = fileItem.PIDL;
		}

		public async Task LoadCardThumbnailAsync()
		{
			var iconData = await FileThumbnailHelper.LoadIconWithoutOverlayAsync(RecentPath, Constants.ShellIconSizes.Large, false, false, false);
			if (iconData is not null)
			{
				EmptyImgVis = false;
				Thumbnail = await iconData.ToBitmapAsync();
			}
		}

		public bool Equals(WidgetRecentItem other)
		{
			if (other is null)
			{
				return false;
			}

			// do not include LastModified or anything else here; otherwise, Remove(...) will fail since we lose metadata on deletion!
			// when constructing a RecentItem from a deleted link, the only thing we have is the LinkPath (where the link use to be)
			return LinkPath == other.LinkPath &&
				   RecentPath == other.RecentPath;
		}

		public override int GetHashCode() => (LinkPath, RecentPath).GetHashCode();
		public override bool Equals(object? o) => o is WidgetRecentItem other && Equals(other);

		private static string NameOrPathWithoutExtension(string nameOrPath)
		{
			string strippedExtension = SystemIO.Path.GetFileNameWithoutExtension(nameOrPath);
			return string.IsNullOrEmpty(strippedExtension) ? SystemIO.Path.GetFileName(nameOrPath) : strippedExtension;
		}
	}
}
