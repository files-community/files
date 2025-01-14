// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Files.App.ViewModels.Layouts;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Runtime.InteropServices;
using Windows.System;
using Windows.UI.Core;
using Windows.Win32;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents layout page that can be grouped by.
	/// </summary>
	public abstract class BaseGroupableLayoutPage : BaseLayoutPage
	{
		// Constants

		private const int KEY_DOWN_MASK = 0x8000;

		// Fields

		protected int NextRenameIndex = 0;

		// Properties

		protected abstract ListViewBase ListViewBase { get; }
		protected abstract SemanticZoom RootZoom { get; }

		protected override ItemsControl ItemsControl => ListViewBase;

		// Constructor

		public BaseGroupableLayoutPage() : base()
		{
		}

		// Abstract methods

		protected abstract void ItemManipulationModel_AddSelectedItemInvoked(object? sender, ListedItem e);
		protected abstract void ItemManipulationModel_RemoveSelectedItemInvoked(object? sender, ListedItem e);
		protected abstract void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e);
		protected abstract void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e);
		protected abstract void ItemManipulationModel_ScrollToTopInvoked(object? sender, EventArgs e);
		protected abstract void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e);
		protected abstract void EndRename(TextBox textBox);

		// Overridden methods

		protected override void InitializeCommandsViewModel()
		{
			CommandsViewModel = new BaseLayoutViewModel(ParentShellPageInstance, ItemManipulationModel);
		}

		protected override void HookEvents()
		{
			UnhookEvents();

			ItemManipulationModel.FocusFileListInvoked += ItemManipulationModel_FocusFileListInvoked;
			ItemManipulationModel.SelectAllItemsInvoked += ItemManipulationModel_SelectAllItemsInvoked;
			ItemManipulationModel.ClearSelectionInvoked += ItemManipulationModel_ClearSelectionInvoked;
			ItemManipulationModel.InvertSelectionInvoked += ItemManipulationModel_InvertSelectionInvoked;
			ItemManipulationModel.AddSelectedItemInvoked += ItemManipulationModel_AddSelectedItemInvoked;
			ItemManipulationModel.RemoveSelectedItemInvoked += ItemManipulationModel_RemoveSelectedItemInvoked;
			ItemManipulationModel.FocusSelectedItemsInvoked += ItemManipulationModel_FocusSelectedItemsInvoked;
			ItemManipulationModel.StartRenameItemInvoked += ItemManipulationModel_StartRenameItemInvoked;
			ItemManipulationModel.ScrollIntoViewInvoked += ItemManipulationModel_ScrollIntoViewInvoked;
			ItemManipulationModel.ScrollToTopInvoked += ItemManipulationModel_ScrollToTopInvoked;
			ItemManipulationModel.RefreshItemThumbnailInvoked += ItemManipulationModel_RefreshItemThumbnail;
			ItemManipulationModel.RefreshItemsThumbnailInvoked += ItemManipulationModel_RefreshItemsThumbnail;
		}

		protected override void UnhookEvents()
		{
			if (ItemManipulationModel is null)
				return;

			ItemManipulationModel.FocusFileListInvoked -= ItemManipulationModel_FocusFileListInvoked;
			ItemManipulationModel.SelectAllItemsInvoked -= ItemManipulationModel_SelectAllItemsInvoked;
			ItemManipulationModel.ClearSelectionInvoked -= ItemManipulationModel_ClearSelectionInvoked;
			ItemManipulationModel.InvertSelectionInvoked -= ItemManipulationModel_InvertSelectionInvoked;
			ItemManipulationModel.AddSelectedItemInvoked -= ItemManipulationModel_AddSelectedItemInvoked;
			ItemManipulationModel.RemoveSelectedItemInvoked -= ItemManipulationModel_RemoveSelectedItemInvoked;
			ItemManipulationModel.FocusSelectedItemsInvoked -= ItemManipulationModel_FocusSelectedItemsInvoked;
			ItemManipulationModel.StartRenameItemInvoked -= ItemManipulationModel_StartRenameItemInvoked;
			ItemManipulationModel.ScrollIntoViewInvoked -= ItemManipulationModel_ScrollIntoViewInvoked;
			ItemManipulationModel.ScrollToTopInvoked -= ItemManipulationModel_ScrollToTopInvoked;
			ItemManipulationModel.RefreshItemThumbnailInvoked -= ItemManipulationModel_RefreshItemThumbnail;
			ItemManipulationModel.RefreshItemsThumbnailInvoked -= ItemManipulationModel_RefreshItemsThumbnail;
		}

		protected override void Page_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
		{
			if (ParentShellPageInstance is null ||
				ParentShellPageInstance.CurrentPageType != this.GetType() ||
				IsRenamingItem)
				return;

			// Don't block the various uses of enter key (key 13)
			var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(XamlRoot);
			var isHeaderFocused = DependencyObjectHelpers.FindParent<DataGridHeader>(focusedElement) is not null;
			if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Enter) == CoreVirtualKeyStates.Down ||
				(focusedElement is Button && !isHeaderFocused) || // Allow jumpstring when header is focused
				focusedElement is TextBox ||
				focusedElement is PasswordBox ||
				DependencyObjectHelpers.FindParent<ContentDialog>(focusedElement) is not null)
				return;

			base.Page_CharacterReceived(sender, args);
		}

		// Virtual methods

		protected virtual async void ItemManipulationModel_RefreshItemsThumbnail(object? sender, EventArgs e)
		{
			await ReloadSelectedItemsIconAsync();
		}

		protected virtual async void ItemManipulationModel_RefreshItemThumbnail(object? sender, EventArgs args)
		{
			await ReloadSelectedItemIconAsync();
		}

		protected virtual async Task ReloadSelectedItemIconAsync()
		{
			if (ParentShellPageInstance?.SlimContentPage?.SelectedItem is null)
				return;

			ParentShellPageInstance.ShellViewModel.CancelExtendedPropertiesLoading();
			ParentShellPageInstance.SlimContentPage.SelectedItem.ItemPropertiesInitialized = false;

			await ParentShellPageInstance.ShellViewModel.LoadExtendedItemPropertiesAsync(ParentShellPageInstance.SlimContentPage.SelectedItem);

			if (ParentShellPageInstance.ShellViewModel.EnabledGitProperties is not GitProperties.None &&
				ParentShellPageInstance.SlimContentPage.SelectedItem is IGitItem gitItem)
			{
				await ParentShellPageInstance.ShellViewModel.LoadGitPropertiesAsync(gitItem);
			}
		}

		protected virtual async Task ReloadSelectedItemsIconAsync()
		{
			if (ParentShellPageInstance?.SlimContentPage?.SelectedItems is null)
				return;

			ParentShellPageInstance.ShellViewModel.CancelExtendedPropertiesLoading();

			foreach (var selectedItem in ParentShellPageInstance.SlimContentPage.SelectedItems)
			{
				selectedItem.ItemPropertiesInitialized = false;
				await ParentShellPageInstance.ShellViewModel.LoadExtendedItemPropertiesAsync(selectedItem);
			}

			if (ParentShellPageInstance.ShellViewModel.EnabledGitProperties is not GitProperties.None)
			{
				await Task.WhenAll(ParentShellPageInstance.SlimContentPage.SelectedItems.Select(item =>
				{
					if (item is IGitItem gitItem)
						return ParentShellPageInstance.ShellViewModel.LoadGitPropertiesAsync(gitItem);

					return Task.CompletedTask;
				}));
			}
		}

		protected virtual void ItemManipulationModel_FocusFileListInvoked(object? sender, EventArgs e)
		{
			var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(MainWindow.Instance.Content.XamlRoot);
			var isFileListFocused = DependencyObjectHelpers.FindParent<ListViewBase>(focusedElement) == ItemsControl;
			if (!isFileListFocused)
				ListViewBase.Focus(FocusState.Programmatic);
		}

		protected virtual void ItemManipulationModel_SelectAllItemsInvoked(object? sender, EventArgs e)
		{
			ListViewBase.SelectAll();
		}

		protected virtual void ItemManipulationModel_ClearSelectionInvoked(object? sender, EventArgs e)
		{
			ListViewBase.SelectedItems.Clear();
		}

		protected virtual void ItemManipulationModel_InvertSelectionInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems.Count < GetAllItems().Count() / 2)
			{
				var oldSelectedItems = SelectedItems.ToList();
				ItemManipulationModel.SelectAllItems();
				ItemManipulationModel.RemoveSelectedItems(oldSelectedItems);
				return;
			}

			List<ListedItem> newSelectedItems = GetAllItems()
				.Cast<ListedItem>()
				.Except(SelectedItems)
				.ToList();

			ItemManipulationModel.SetSelectedItems(newSelectedItems);
		}

		protected virtual void ItemManipulationModel_StartRenameItemInvoked(object? sender, EventArgs e)
		{
			StartRenameItem();
		}

		protected override void ZoomIn()
		{
			RootZoom.IsZoomedInViewActive = true;
		}

		protected virtual void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectedItems = ListViewBase.SelectedItems.Cast<ListedItem>().Where(x => x is not null).ToList();
		}

		protected virtual void SelectionRectangle_SelectionEnded(object? sender, EventArgs e)
		{
			ListViewBase.Focus(FocusState.Programmatic);
		}

		protected virtual void StartRenameItem(string itemNameTextBox)
		{
			RenamingItem = SelectedItem;
			if (RenamingItem is null)
				return;

			int extensionLength = RenamingItem.FileExtension?.Length ?? 0;

			ListViewItem? listViewItem = ListViewBase.ContainerFromItem(RenamingItem) as ListViewItem;
			if (listViewItem is null)
				return;

			TextBox? textBox = null;
			TextBlock? textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
			textBox = listViewItem.FindDescendant(itemNameTextBox) as TextBox;
			textBox!.Text = textBlock!.Text;
			OldItemName = textBlock.Text;
			textBlock.Visibility = Visibility.Collapsed;
			textBox.Visibility = Visibility.Visible;

			if (textBox.FindParent<Grid>() is null)
			{
				textBlock.Visibility = Visibility.Visible;
				textBox.Visibility = Visibility.Collapsed;
				return;
			}

			Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);

			textBox.Focus(FocusState.Pointer);
			textBox.LostFocus += RenameTextBox_LostFocus;
			textBox.KeyDown += RenameTextBox_KeyDown;

			int selectedTextLength = SelectedItem.Name.Length;

			if (!SelectedItem.IsShortcut && UserSettingsService.FoldersSettingsService.ShowFileExtensions)
				selectedTextLength -= extensionLength;

			textBox.Select(0, selectedTextLength);
			IsRenamingItem = true;
		}

		protected virtual async Task CommitRenameAsync(TextBox textBox)
		{
			EndRename(textBox);
			string newItemName = textBox.Text.Trim().TrimEnd('.');

			await UIFilesystemHelpers.RenameFileItemAsync(RenamingItem, newItemName, ParentShellPageInstance);
		}

		protected virtual async void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			try
			{
				// This check allows the user to use the text box context menu without ending the rename
				if (!(FocusManager.GetFocusedElement(MainWindow.Instance.Content.XamlRoot) is AppBarButton or Popup))
				{
					TextBox textBox = (TextBox)e.OriginalSource;
					await CommitRenameAsync(textBox);
				}
			}
			catch (COMException)
			{

			}			
		}

		// Methods

		protected async void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			var textBox = (TextBox)sender;
			var isShiftPressed = (PInvoke.GetKeyState((int)VirtualKey.Shift) & KEY_DOWN_MASK) != 0;

			switch (e.Key)
			{
				case VirtualKey.Escape:
					textBox.LostFocus -= RenameTextBox_LostFocus;
					textBox.Text = OldItemName;
					EndRename(textBox);
					e.Handled = true;
					break;
				case VirtualKey.Enter:
					textBox.LostFocus -= RenameTextBox_LostFocus;
					await CommitRenameAsync(textBox);
					e.Handled = true;
					break;
				case VirtualKey.Up:
					if (!isShiftPressed)
						textBox.SelectionStart = 0;
					e.Handled = true;
					break;
				case VirtualKey.Down:
					if (!isShiftPressed)
						textBox.SelectionStart = textBox.Text.Length;
					e.Handled = true;
					break;
				case VirtualKey.Left:
					e.Handled = textBox.SelectionStart == 0;
					break;
				case VirtualKey.Right:
					e.Handled = (textBox.SelectionStart + textBox.SelectionLength) == textBox.Text.Length;
					break;
				case VirtualKey.Tab:
					textBox.LostFocus -= RenameTextBox_LostFocus;

					NextRenameIndex = isShiftPressed ? -1 : 1;

					if (textBox.Text != OldItemName)
					{
						await CommitRenameAsync(textBox);
					}
					else
					{
						var newIndex = ListViewBase.SelectedIndex + NextRenameIndex;
						NextRenameIndex = 0;
						EndRename(textBox);

						if (newIndex >= 0 &&
							newIndex < ListViewBase.Items.Count)
						{
							ListViewBase.SelectedIndex = newIndex;
							StartRenameItem();
						}
					}

					e.Handled = true;
					break;
			}
		}

		protected bool TryStartRenameNextItem(ListedItem item)
		{
			var nextItemIndex = ListViewBase.Items.IndexOf(item) + NextRenameIndex;
			NextRenameIndex = 0;

			if (nextItemIndex >= 0 &&
				nextItemIndex < ListViewBase.Items.Count)
			{
				ListViewBase.SelectedIndex = nextItemIndex;
				StartRenameItem();

				return true;
			}

			return false;
		}

		protected void SelectionCheckbox_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			e.Handled = true;
		}

		// Disposer

		public override void Dispose()
		{
			base.Dispose();
			UnhookEvents();
			CommandsViewModel?.Dispose();
		}
	}
}
