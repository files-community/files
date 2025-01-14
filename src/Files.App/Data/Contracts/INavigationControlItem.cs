// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.UserControls.Sidebar;

namespace Files.App.Data.Contracts
{
	public interface INavigationControlItem : IComparable<INavigationControlItem>, INotifyPropertyChanged, ISidebarItemModel
	{
		public new string Text { get; }

		public string Path { get; }

		public SectionType Section { get; }

		public NavigationControlItemType ItemType { get; }

		public ContextMenuOptions MenuOptions { get; }
	}

	public enum NavigationControlItemType
	{
		Drive,
		LinuxDistro,
		Location,
		FileTag
	}

	public enum SectionType
	{
		Home,
		Pinned,
		Library,
		Drives,
		CloudDrives,
		Network,
		WSL,
		FileTag
	}

	public sealed class ContextMenuOptions
	{
		public bool IsLibrariesHeader { get; set; }

		public bool IsTagsHeader { get; set; }

		public bool ShowHideSection { get; set; }

		public bool IsLocationItem { get; set; }

		public bool ShowUnpinItem { get; set; }

		public bool ShowProperties { get; set; }

		public bool ShowEmptyRecycleBin { get; set; }

		public bool ShowEjectDevice { get; set; }

		public bool ShowShellItems { get; set; }
	}
}
