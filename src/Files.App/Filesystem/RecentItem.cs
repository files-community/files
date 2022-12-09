using CommunityToolkit.Mvvm.ComponentModel;
using Files.App.Helpers;
using Files.Shared;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Files.App.Filesystem
{
	public class RecentItem : ObservableObject, IEquatable<RecentItem>
	{
		private BitmapImage _fileImg;
		public BitmapImage FileImg
		{
			get => _fileImg;
			set => SetProperty(ref _fileImg, value);
		}

		// path of shortcut item (this is unique)
		public string LinkPath { get; set; }

		// path to target item
		public string RecentPath { get; set; }

		public string Name { get; set; }

		public StorageItemTypes Type { get; set; }

		public bool FolderImg { get; set; }

		public bool EmptyImgVis { get; set; }

		public bool FileIconVis { get; set; }

		public bool IsFile { get => Type == StorageItemTypes.File; }

		public DateTime LastModified { get; set; }

		public RecentItem()
		{
			// Defer icon load to LoadRecentItemIcon()
			EmptyImgVis = true; 
		}

		/// <summary>
		/// Create a RecentItem instance from a link path.
		/// This is usually needed if a shortcut is deleted -- the metadata is lost (i.e. the target item).
		/// </summary>
		/// <param name="linkPath">The location that shortcut lives/lived in</param>
		public RecentItem(string linkPath) : base()
		{
			LinkPath = linkPath;
		}

		/// <summary>
		/// Create a RecentItem from a ShellLinkItem (usually from shortcuts in `Windows\Recent`)
		/// </summary>
		public RecentItem(ShellLinkItem linkItem) : base()
		{
			LinkPath = linkItem.FilePath;
			RecentPath = linkItem.TargetPath;
			Name = NameOrPathWithoutExtension(linkItem.FileName);
			Type = linkItem.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File;
			FolderImg = linkItem.IsFolder;
			FileIconVis = !linkItem.IsFolder;
			LastModified = linkItem.ModifiedDate;
		}

		/// <summary>
		/// Create a RecentItem from a ShellFileItem (usually from enumerating Quick Access directly).
		/// </summary>
		/// <param name="fileItem">The shell file item</param>
		public RecentItem(ShellFileItem fileItem) : base()
		{
			LinkPath = fileItem.FilePath;   // intentionally the same
			RecentPath = fileItem.FilePath; // intentionally the same
			Name = NameOrPathWithoutExtension(fileItem.FileName);
			Type = fileItem.IsFolder ? StorageItemTypes.Folder : StorageItemTypes.File;
			FolderImg = fileItem.IsFolder;
			FileIconVis = !fileItem.IsFolder;
			LastModified = fileItem.ModifiedDate;
		}

		public async Task LoadRecentItemIcon()
		{
			var iconData = await FileThumbnailHelper.LoadIconFromPathAsync(RecentPath, 96u, ThumbnailMode.SingleItem);
			if (iconData is null)
			{
				EmptyImgVis = true;
			}
			else
			{
				EmptyImgVis = false;
				FileImg = await iconData.ToBitmapAsync();
			}
		}

		/// <summary>
		/// Test equality for generic collection methods such as Remove(...)
		/// </summary>
		public bool Equals(RecentItem other)
		{
			if (other is null)
				return false;

			// Do not include LastModified or anything else here; otherwise, Remove(...) will fail since we lose metadata on deletion!
			// when constructing a RecentItem from a deleted link, the only thing we have is the LinkPath (where the link use to be)
			return LinkPath == other.LinkPath && RecentPath == other.RecentPath;
		}

        // Strips a name from an extension while aware of some edge cases.
        //
        //   example.min.js => example.min
        //   example.js     => example
        //   .gitignore     => .gitignore
        private static string NameOrPathWithoutExtension(string nameOrPath)
		{
			string strippedExtension = Path.GetFileNameWithoutExtension(nameOrPath);

			return string.IsNullOrEmpty(strippedExtension) ? Path.GetFileName(nameOrPath) : strippedExtension;
		}
	}
}
