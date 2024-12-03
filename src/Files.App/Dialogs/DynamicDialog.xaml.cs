// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class DynamicDialog : ContentDialog, IDisposable, IRealTimeControl
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public DynamicDialogViewModel ViewModel
		{
			get => (DynamicDialogViewModel)DataContext;
			private set => DataContext = value;
		}

		public DynamicDialogResult DynamicResult
		{
			get => ViewModel.DynamicResult;
		}

		public new Task<ContentDialogResult> ShowAsync()
		{
			return this.TryShowAsync();
		}

		public DynamicDialog(DynamicDialogViewModel dynamicDialogViewModel)
		{
			InitializeComponent();
			InitializeContentLayout();

			dynamicDialogViewModel.HideDialog = Hide;
			ViewModel = dynamicDialogViewModel;
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.PrimaryButtonCommand.Execute(args);
		}

		private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.SecondaryButtonCommand.Execute(args);
		}

		private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			ViewModel.CloseButtonCommand.Execute(args);
		}

		private void ContentDialog_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			ViewModel.KeyDownCommand.Execute(e);
		}

		public void Dispose()
		{
			ViewModel?.Dispose();
			ViewModel = null;
		}
	}
}
