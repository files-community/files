// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Files.App.UserControls
{
	public sealed partial class StatusBarControl : UserControl
	{
		public DirectoryPropertiesViewModel DirectoryPropertiesViewModel
		{
			get => (DirectoryPropertiesViewModel)GetValue(DirectoryPropertiesViewModelProperty);
			set => SetValue(DirectoryPropertiesViewModelProperty, value);
		}

		// Using a DependencyProperty as the backing store for DirectoryPropertiesViewModel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty DirectoryPropertiesViewModelProperty =
			DependencyProperty.Register(nameof(DirectoryPropertiesViewModel), typeof(DirectoryPropertiesViewModel), typeof(StatusBarControl), new PropertyMetadata(null));

		public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel
		{
			get => (SelectedItemsPropertiesViewModel)GetValue(SelectedItemsPropertiesViewModelProperty);
			set => SetValue(SelectedItemsPropertiesViewModelProperty, value);
		}

		public static readonly DependencyProperty SelectedItemsPropertiesViewModelProperty =
			DependencyProperty.Register(nameof(SelectedItemsPropertiesViewModel), typeof(SelectedItemsPropertiesViewModel), typeof(StatusBarControl), new PropertyMetadata(null));

		public bool ShowInfoText
		{
			get => (bool)GetValue(ShowInfoTextProperty);
			set => SetValue(ShowInfoTextProperty, value);
		}

		// Using a DependencyProperty as the backing store for HideInfoText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowInfoTextProperty =
			DependencyProperty.Register(nameof(ShowInfoText), typeof(bool), typeof(StatusBarControl), new PropertyMetadata(null));

		public StatusBarControl()
		{
			InitializeComponent();
		}

		private void Flyout_Opening(object sender, object e)
		{
			DirectoryPropertiesViewModel.SelectedBranchIndex = DirectoryPropertiesViewModel.ActiveBranchIndex;
		}

		private void StackPanel_LostFocus(object sender, RoutedEventArgs e)
		{
			((Popup)((FlyoutPresenter)((StackPanel)sender).Parent).Parent).IsOpen = false;
		}
	}
}
