// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items
{
	public class WslDistroItem : ObservableObject, INavigationControlItem
	{
		public string Text { get; set; }

		private string path;
		public string Path
		{
			get => path;
			set
			{
				path = value;
				ToolTipText = Path.Contains('?', StringComparison.Ordinal) ? Text : Path;
			}
		}

		public string ToolTipText { get; private set; }

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.LinuxDistro;

		private Uri icon;
		public Uri Icon
		{
			get => icon;
			set
			{
				SetProperty(ref icon, value, nameof(Icon));
			}
		}

		public SectionType Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

		public BulkConcurrentObservableCollection<INavigationControlItem>? ChildItems => null;
		public IconSource? GenerateIconSource() => new BitmapIconSource()
		{
			UriSource = icon,
			ShowAsMonochrome = false,
		};

		public int CompareTo(INavigationControlItem other) => Text.CompareTo(other.Text);
	}
}
