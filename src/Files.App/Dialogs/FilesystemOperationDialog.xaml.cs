// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class FilesystemOperationDialog : ContentDialog, IDialog<FileSystemDialogViewModel>
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public FileSystemDialogViewModel ViewModel
		{
			get => (FileSystemDialogViewModel)DataContext;
			set
			{
				if (value is not null)
				{
					value.PrimaryButtonEnabled = true;
				}

				DataContext = value;
			}
		}

		public FilesystemOperationDialog()
		{
			InitializeComponent();

			MainWindow.Instance.SizeChanged += Current_SizeChanged;
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await SetContentDialogRoot(this).ShowAsync();
		}

		// WINUI3
		private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			return contentDialog;
		}

		private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			UpdateDialogLayout();
		}

		private void UpdateDialogLayout()
		{
			try
			{
				if (ViewModel.FileSystemDialogMode.ConflictsExist)
					ContainerGrid.Width = MainWindow.Instance.Bounds.Width <= 700 ? MainWindow.Instance.Bounds.Width - 50 : 650;
			}
			catch (Exception ex)
			{
				// Handle exception in case WinUI Windows is closed
				// (see https://github.com/files-community/Files/issues/15599)

				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var primaryButton = this.FindDescendant("PrimaryButton") as Button;
			if (primaryButton is not null)
				primaryButton.GotFocus += PrimaryButton_GotFocus;
		}

		private void PrimaryButton_GotFocus(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn)
				btn.GotFocus -= PrimaryButton_GotFocus;

			if (chkPermanentlyDelete is not null)
				chkPermanentlyDelete.IsEnabled = ViewModel.IsDeletePermanentlyEnabled;

			DetailsGrid.IsEnabled = true;
		}

		private void RootDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			if (args.Result == ContentDialogResult.Primary)
				ViewModel.SaveConflictResolveOption();

			MainWindow.Instance.SizeChanged -= Current_SizeChanged;
			ViewModel.CancelCts();
		}

		private void NameStackPanel_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
		{
			if (sender is FrameworkElement element &&
				element.DataContext is FileSystemDialogConflictItemViewModel conflictItem &&
				conflictItem.ConflictResolveOption == FileNameConflictResolveOptionType.GenerateNewName)
			{
				StartRename(conflictItem);
			}
		}

		private void NameEdit_LostFocus(object sender, RoutedEventArgs e)
		{
			if ((sender as FrameworkElement)?.DataContext is FileSystemDialogConflictItemViewModel conflictItem)
				EndRename(conflictItem);
		}

		private void NameEdit_Loaded(object sender, RoutedEventArgs e)
		{
			(sender as TextBox)?.Focus(FocusState.Programmatic);
		}

		private void FilesystemOperationDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
		{
			UpdateDialogLayout();
		}

		private void NameEdit_PreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (sender is not FrameworkElement element || element.DataContext is not FileSystemDialogConflictItemViewModel currentItem)
				return;

			if (e.Key is Windows.System.VirtualKey.Down)
			{
				var index = ViewModel.Items.IndexOf(currentItem);
				if (index == -1 || index == ViewModel.Items.Count - 1)
					return;

				var nextItem = ViewModel.Items.Skip(index + 1)
					.FirstOrDefault(i => i is FileSystemDialogConflictItemViewModel { ConflictResolveOption: FileNameConflictResolveOptionType.GenerateNewName });

				if (nextItem is null)
					return;

				EndRename(currentItem);
				StartRename((FileSystemDialogConflictItemViewModel)nextItem);

				e.Handled = true;
			}
			else if (e.Key is Windows.System.VirtualKey.Up)
			{
				var index = ViewModel.Items.IndexOf(currentItem);
				if (index == -1 || index == 0)
					return;

				var prevItem = ViewModel.Items.Take(index)
					.LastOrDefault(i => i is FileSystemDialogConflictItemViewModel { ConflictResolveOption: FileNameConflictResolveOptionType.GenerateNewName });
				if (prevItem is null)
					return;

				EndRename(currentItem);
				StartRename((FileSystemDialogConflictItemViewModel)prevItem);

				e.Handled = true;
			}
		}

		private void StartRename(FileSystemDialogConflictItemViewModel conflictItem)
		{
			conflictItem.IsTextBoxVisible = conflictItem.ConflictResolveOption == FileNameConflictResolveOptionType.GenerateNewName;
			conflictItem.CustomName = conflictItem.DestinationDisplayName;
		}

		private void EndRename(FileSystemDialogConflictItemViewModel conflictItem)
		{
			conflictItem.CustomName = FilesystemHelpers.FilterRestrictedCharacters(conflictItem.CustomName);

			if (ViewModel.IsNameAvailableForItem(conflictItem, conflictItem.CustomName!))
				conflictItem.IsTextBoxVisible = false;
			else
				ViewModel.PrimaryButtonEnabled = false;

			if (conflictItem.CustomName.Equals(conflictItem.DisplayName))
			{
				var savedName = conflictItem.DestinationDisplayName;
				conflictItem.DestinationDisplayName = savedName;
			}
		}
	}
}
