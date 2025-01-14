// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Commands;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public sealed partial class StatusBar : UserControl
	{
		public ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		public StatusBarViewModel? StatusBarViewModel
		{
			get => (StatusBarViewModel)GetValue(StatusBarViewModelProperty);
			set => SetValue(StatusBarViewModelProperty, value);
		}

		// Using a DependencyProperty as the backing store for StatusBarViewModel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty StatusBarViewModelProperty =
			DependencyProperty.Register(nameof(StatusBarViewModel), typeof(StatusBarViewModel), typeof(StatusBar), new PropertyMetadata(null));

		public SelectedItemsPropertiesViewModel? SelectedItemsPropertiesViewModel
		{
			get => (SelectedItemsPropertiesViewModel)GetValue(SelectedItemsPropertiesViewModelProperty);
			set => SetValue(SelectedItemsPropertiesViewModelProperty, value);
		}

		public static readonly DependencyProperty SelectedItemsPropertiesViewModelProperty =
			DependencyProperty.Register(nameof(SelectedItemsPropertiesViewModel), typeof(SelectedItemsPropertiesViewModel), typeof(StatusBar), new PropertyMetadata(null));

		public bool ShowInfoText
		{
			get => (bool)GetValue(ShowInfoTextProperty);
			set => SetValue(ShowInfoTextProperty, value);
		}

		// Using a DependencyProperty as the backing store for HideInfoText.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowInfoTextProperty =
			DependencyProperty.Register(nameof(ShowInfoText), typeof(bool), typeof(StatusBar), new PropertyMetadata(null));

		public StatusBar()
		{
			InitializeComponent();
		}

		private async void BranchesFlyout_Opening(object _, object e)
		{
			if (StatusBarViewModel is null)
				return;

			StatusBarViewModel.IsBranchesFlyoutExpanded = true;
			StatusBarViewModel.ShowLocals = true;
			await StatusBarViewModel.LoadBranches();
			StatusBarViewModel.SelectedBranchIndex = StatusBarViewModel.ACTIVE_BRANCH_INDEX;
		}

		private void BranchesList_ItemClick(object sender, ItemClickEventArgs e)
		{
			BranchesFlyout.Hide();
		}

		private void BranchesFlyout_Closing(object _, object e)
		{
			if (StatusBarViewModel is null)
				return;

			StatusBarViewModel.IsBranchesFlyoutExpanded = false;
		}

		private async void DeleteBranch_Click(object sender, RoutedEventArgs e)
		{
			if (StatusBarViewModel is null)
				return;

			BranchesFlyout.Hide();
			await StatusBarViewModel.ExecuteDeleteBranch(((BranchItem)((Button)sender).DataContext).Name);
		}
	}
}
