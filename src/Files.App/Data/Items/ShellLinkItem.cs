// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Data.Items
{
	public sealed class ShellLinkItem : ShellFileItem
	{
		public string TargetPath { get; set; }

		public string Arguments { get; set; }

		public string WorkingDirectory { get; set; }

		public bool RunAsAdmin { get; set; }

		public bool InvalidTarget { get; set; }

		public SHOW_WINDOW_CMD ShowWindowCommand { get; set; }

		public ShellLinkItem()
		{
		}

		public ShellLinkItem(ShellFileItem baseItem)
		{
			RecyclePath = baseItem.RecyclePath;
			FileName = baseItem.FileName;
			FilePath = baseItem.FilePath;
			RecycleDate = baseItem.RecycleDate;
			ModifiedDate = baseItem.ModifiedDate;
			CreatedDate = baseItem.CreatedDate;
			FileSize = baseItem.FileSize;
			FileSizeBytes = baseItem.FileSizeBytes;
			FileType = baseItem.FileType;
			PIDL = baseItem.PIDL;
		}
	}
}
