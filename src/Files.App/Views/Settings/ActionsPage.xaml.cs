// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Text;
using Windows.System;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Represents settings page called Actions, which provides a way to customize key bindings.
	/// </summary>
	public sealed partial class ActionsPage : Page
	{
		private readonly string PART_EditButton = "EditButton";
		private readonly string NormalState = "Normal";
		private readonly string PointerOverState = "PointerOver";

		private ActionsViewModel ViewModel { get; set; } = new();

		public ActionsPage()
		{
			InitializeComponent();
		}

		private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, PointerOverState, true);

			// Make edit button visible on pointer in
			if (sender is UserControl userControl &&
				userControl.FindChild(PART_EditButton) is Button editButton &&
				userControl.DataContext is ModifiableActionItem item &&
				!item.IsInEditMode)
				editButton.Visibility = Visibility.Visible;
		}

		private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, NormalState, true);

			// Make edit button invisible on pointer out
			if (sender is UserControl userControl &&
				userControl.FindChild(PART_EditButton) is Button editButton &&
				userControl.DataContext is ModifiableActionItem item &&
				!item.IsInEditMode)
				editButton.Visibility = Visibility.Collapsed;
		}

		private void KeyBindingEditorTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			// Ensure the sender is a TextBox
			if (sender is not TextBox textBox)
				return;

			// Cast the DataContext of the TextBox to a ModifiableActionItem if possible, or null if the cast fails
			var item = textBox.DataContext as ModifiableActionItem;

			var pressedKey = e.OriginalKey;
			var pressedKeyValue = HotKey.LocalizedKeys.GetValueOrDefault((Keys)pressedKey);
			var buffer = new StringBuilder();

			// Define invalid keys that shouldn't be processed
			var invalidKeys = new HashSet<VirtualKey>
			{
				VirtualKey.CapitalLock,
				VirtualKey.NumberKeyLock,
				VirtualKey.Scroll,
			};

			// Define modifier keys
			var modifierKeys = new HashSet<VirtualKey>
			{
				VirtualKey.Shift,
				VirtualKey.Control,
				VirtualKey.Menu,
				VirtualKey.LeftWindows,
				VirtualKey.RightWindows,
				VirtualKey.LeftShift,
				VirtualKey.LeftControl,
				VirtualKey.RightControl,
				VirtualKey.LeftMenu,
				VirtualKey.RightMenu
			};

			// Determine if the pressed key is invalid or a modifier
			var isInvalidKey = invalidKeys.Contains(pressedKey) || string.IsNullOrEmpty(pressedKeyValue);
			var isModifierKey = modifierKeys.Contains(pressedKey);

			// Handle invalid keys that are not modifiers
			if (isInvalidKey && !isModifierKey)
			{
				InvalidKeyTeachingTip.Target = textBox;
				ViewModel.IsInvalidKeyTeachingTipOpened = true;
			}

			// Check if the pressed key is invalid, a modifier, or has no value; Don't show it in the TextBox yet
			if (isInvalidKey || isModifierKey)
			{
				// Set the text of the TextBox to the empty buffer
				textBox.Text = buffer.ToString();

				// Update UI state based on the context item
				if (item is null)
					ViewModel.EnableAddNewKeyBindingButton = false;
				else
					item.IsValidKeyBinding = false;

				// Prevent key down event in other UIElements from getting invoked
				e.Handled = true;
				return;
			}

			// Get the currently pressed modifier keys
			var pressedModifiers = HotKeyHelpers.GetCurrentKeyModifiers();

			// Append modifier keys to the buffer
			if (pressedModifiers.HasFlag(KeyModifiers.Ctrl))
				buffer.Append($"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Ctrl)}+");
			if (pressedModifiers.HasFlag(KeyModifiers.Alt))
				buffer.Append($"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Alt)}+");
			if (pressedModifiers.HasFlag(KeyModifiers.Shift))
				buffer.Append($"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Shift)}+");

			// Append the pressed key to the buffer
			buffer.Append(pressedKeyValue);

			// Set the text of the TextBox to the constructed key combination
			textBox.Text = buffer.ToString();

			// Update UI state based on the context item
			if (item is null)
				ViewModel.EnableAddNewKeyBindingButton = true;
			else
				item.IsValidKeyBinding = true;

			// Prevent key down event in other UIElements from getting invoked
			e.Handled = true;
		}

		private void KeyBindingEditorTextBox_Loaded(object sender, RoutedEventArgs e)
		{
			// Focus the editor TextBox
			TextBox keyboardShortcutEditorTextBox = (TextBox)sender;
			keyboardShortcutEditorTextBox.Focus(FocusState.Programmatic);
		}

		private void NewKeyBindingItemPickerComboBox_DropDownClosed(object sender, object e)
		{
			// Check if a new action is selected
			if (ViewModel.SelectedActionItem is null)
				return;

			// Focus the editor TextBox
			KeyBindingEditorTextBox.Focus(FocusState.Programmatic);
		}
	}
}
