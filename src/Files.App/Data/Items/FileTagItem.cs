// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Data.Items
{
	public sealed class FileTagItem : ObservableObject, INavigationControlItem
	{
		public string Text { get; set; }

		private string path;
		public string Path
		{
			get => path;
			set
			{
				path = value;
				OnPropertyChanged(nameof(IconSource));
				OnPropertyChanged(nameof(ToolTip));
			}
		}

		public string ToolTipText { get; private set; }

		public SectionType Section { get; set; }

		public ContextMenuOptions MenuOptions { get; set; }

		public NavigationControlItemType ItemType
			=> NavigationControlItemType.FileTag;

		public int CompareTo(INavigationControlItem other)
			=> Text.CompareTo(other.Text);

		public TagViewModel FileTag { get; set; }

		public object? Children => null;

		public IconSource? IconSource
		{
			get => new PathIconSource()
			{
				Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), (string)Application.Current.Resources["App.Theme.PathIcon.FilledTag"]),
				Foreground = new SolidColorBrush(FileTag.Color.ToColor())
			};
		}

		public object ToolTip => Text;

		public bool IsExpanded { get => false; set { } }
	}
}
