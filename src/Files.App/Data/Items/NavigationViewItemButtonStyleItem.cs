// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Data.Items
{
	public sealed class NavigationViewItemButtonStyleItem : ObservableObject
	{
		public string? Name;

		public PropertiesNavigationViewItemType ItemType;

		private Style _ThemedIconStyle = (Style)Application.Current.Resources["App.ThemedIcons.Properties"];
		public Style ThemedIconStyle
		{
			get => _ThemedIconStyle;
			set => SetProperty(ref _ThemedIconStyle, value);
		}

		private bool _IsSelected;
		public bool IsSelected
		{
			get => _IsSelected;
			set => SetProperty(ref _IsSelected, value);
		}

		private bool _IsCompact;
		public bool IsCompact
		{
			get => _IsCompact;
			set => SetProperty(ref _IsCompact, value);
		}
	}
}
