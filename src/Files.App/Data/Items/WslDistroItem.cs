// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items
{
	public sealed class WslDistroItem : ObservableObject, INavigationControlItem
	{
		public string Text { get; set; }

		private string path;
		public string Path
		{
			get => path;
			set
			{
				path = value;
				ToolTip = Path.Contains('?', StringComparison.Ordinal) ? Text : Path;
			}
		}

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.LinuxDistro;

		private Uri icon;
		public Uri Icon
		{
			get => icon;
			set
			{
				SetProperty(ref icon, value, nameof(Icon));
				OnPropertyChanged(nameof(IconSource));
			}
		}

		public SectionType Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

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
			get => new BitmapIconSource()
			{
				UriSource = icon,
				ShowAsMonochrome = false,
			};
		}

		public int CompareTo(INavigationControlItem other) => Text.CompareTo(other.Text);
	}
}
