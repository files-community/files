// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls
{
	public sealed partial class SearchBox : UserControl
	{
		public static readonly DependencyProperty SearchBoxViewModelProperty =
			DependencyProperty.Register(nameof(SearchBoxViewModel), typeof(SearchBoxViewModel), typeof(SearchBox), new PropertyMetadata(null));

		public SearchBoxViewModel SearchBoxViewModel
		{
			get => (SearchBoxViewModel)GetValue(SearchBoxViewModelProperty);
			set => SetValue(SearchBoxViewModelProperty, value);
		}

		public SearchBox() => InitializeComponent();

		private void SearchRegion_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
			=> SearchBoxViewModel.SearchRegion_TextChanged(sender, e);

		private void SearchRegion_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
			=> SearchBoxViewModel.SearchRegion_QuerySubmitted(sender, e);

		private void SearchRegion_Escaped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
			=> SearchBoxViewModel.SearchRegion_Escaped(sender, e);

		private void SearchRegion_KeyDown(object sender, KeyRoutedEventArgs e)
			=> SearchBoxViewModel.SearchRegion_KeyDown(sender, e);
	}
}
