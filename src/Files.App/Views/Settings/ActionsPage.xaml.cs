// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Files.App.Views.Settings
{
	public sealed partial class ActionsPage : Page
	{
		private IGeneralSettingsService GeneralSettingsService { get; } = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

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

			if (sender is UserControl userControl &&
				userControl.FindChild(PART_EditButton) is Button editButton &&
				userControl.DataContext is ModifiableCommandHotKeyItem item &&
				!item.IsEditMode)
				editButton.Visibility = Visibility.Visible;
		}

		private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, NormalState, true);

			if (sender is UserControl userControl &&
				userControl.FindChild(PART_EditButton) is Button editButton &&
				userControl.DataContext is ModifiableCommandHotKeyItem item &&
				!item.IsEditMode)
				editButton.Visibility = Visibility.Collapsed;
		}

		private void EditButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is ModifiableCommandHotKeyItem item)
			{
				// Hide the add-new section grid
				ViewModel.ShowAddNewShortcutGrid = false;

				// Reset the selected item's info
				if (ViewModel.SelectedNewShortcutItem is not null)
				{
					ViewModel.SelectedNewShortcutItem.HotKeyText = "";
					ViewModel.SelectedNewShortcutItem = null;
				}

				// Reset edit mode for each item
				foreach (var hotkey in ViewModel.KeyboardShortcuts)
				{
					hotkey.IsEditMode = false;
					hotkey.HotKeyText = hotkey.HotKey.LocalizedLabel;
				}

				// Enter edit mode
				item.IsEditMode = true;
			}
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.DataContext is not ModifiableCommandHotKeyItem item)
				return;

			if (item.HotKeyText == item.PreviousHotKey.LocalizedLabel)
			{
				item.IsEditMode = false;
				return;
			}

			// Check if this hot key is already taken
			foreach (var hotkey in ViewModel.KeyboardShortcuts)
			{
				if (item.HotKeyText == hotkey.PreviousHotKey)
				{
					ViewModel.IsAlreadyUsedTeachingTipOpened = true;
					return;
				}
			}

			// Get clone of customized hotkeys to overwrite
			var actions =
				GeneralSettingsService.Actions is not null
					? new Dictionary<string, string>(GeneralSettingsService.Actions)
					: [];

			// Get raw string keys stored in the user setting
			var storedKeys = actions.GetValueOrDefault(item.CommandCode.ToString());

			// Initialize
			var newHotKey = HotKey.Parse(item.HotKeyText);
			var modifiedCollection = HotKeyCollection.Empty;

			if (!item.IsCustomized)
			{
				// The first time to customize
				if (string.IsNullOrEmpty(storedKeys))
				{
					// Replace with new one
					var modifiableDefaultCollection = item.DefaultHotKeyCollection.ToList();
					modifiableDefaultCollection.RemoveAll(x => x.RawLabel == item.PreviousHotKey.RawLabel);
					modifiableDefaultCollection.Add(newHotKey);
					modifiedCollection = new HotKeyCollection(modifiableDefaultCollection);
				}
				// Stored in the user setting
				else
				{
					// Replace with new one
					var modifiableCollection = HotKeyCollection.Parse(storedKeys).ToList();
					modifiableCollection.RemoveAll(x => x.RawLabel == item.PreviousHotKey.RawLabel);
					modifiableCollection.Add(newHotKey);
					modifiedCollection = new HotKeyCollection(modifiableCollection);
				}

				// Store
				actions.Add(item.CommandCode.ToString(), modifiedCollection.RawLabel);
				GeneralSettingsService.Actions = actions;

				// Update visual
				item.PreviousHotKey = newHotKey;
				item.HotKey = newHotKey;

				// Set as customized
				foreach (var action in ViewModel.KeyboardShortcuts)
				{
					if (action.CommandCode == item.CommandCode)
						action.IsCustomized = !item.DefaultHotKeyCollection.Contains(action.HotKey);
				}

				// Exit edit mode
				item.IsEditMode = false;

				return;
			}
			else
			{
				// Remove existing setting
				var modifiableCollection = HotKeyCollection.Parse(storedKeys!).ToList();
				if (modifiableCollection.Contains(newHotKey))
					return;
				modifiableCollection.Add(HotKey.Parse(item.HotKeyText));
				modifiedCollection = new(modifiableCollection);

				// Remove previous one
				actions.Remove(item.CommandCode.ToString());

				// Add new one
				if (modifiedCollection.Select(x => x.RawLabel).SequenceEqual(item.DefaultHotKeyCollection.Select(x => x.RawLabel)))
					actions.Add(item.CommandCode.ToString(), modifiedCollection.RawLabel);

				// Save
				GeneralSettingsService.Actions = actions;

				// Update visual
				item.PreviousHotKey = newHotKey;
				item.HotKey = newHotKey;

				// Set as customized
				foreach (var action in ViewModel.KeyboardShortcuts)
				{
					if (action.CommandCode == item.CommandCode)
						action.IsCustomized = !item.DefaultHotKeyCollection.Contains(action.HotKey);
				}

				// Exit edit mode
				item.IsEditMode = false;
			}
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.DataContext is not ModifiableCommandHotKeyItem item)
				return;

			item.IsEditMode = false;
			item.HotKeyText = item.HotKey.LocalizedLabel;
		}

		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.DataContext is not ModifiableCommandHotKeyItem item)
				return;

			// Get clone of customized hotkeys to overwrite
			var actions =
				GeneralSettingsService.Actions is not null
					? new Dictionary<string, string>(GeneralSettingsService.Actions)
					: [];

			// Get raw string keys stored in the user setting
			var storedKeys = actions.GetValueOrDefault(item.CommandCode.ToString());

			// Initialize
			var modifiedCollection = HotKeyCollection.Empty;

			// The first time to customize
			if (!item.IsCustomized)
			{
				// Initialize
				var newHotKey = HotKey.Parse($"!{item.PreviousHotKey.RawLabel}");

				// The first time to customize
				if (string.IsNullOrEmpty(storedKeys))
				{
					// Replace with new one
					var modifiableDefaultCollection = item.DefaultHotKeyCollection.ToList();
					modifiableDefaultCollection.RemoveAll(x => x.RawLabel == item.PreviousHotKey.RawLabel);
					modifiableDefaultCollection.Add(newHotKey);
					modifiedCollection = new HotKeyCollection(modifiableDefaultCollection);
				}
				// Stored in the user setting
				else
				{
					// Replace with new one
					var modifiableCollection = HotKeyCollection.Parse(storedKeys).ToList();
					modifiableCollection.RemoveAll(x => x.RawLabel == item.PreviousHotKey.RawLabel);
					modifiableCollection.Add(newHotKey);
					modifiedCollection = new HotKeyCollection(modifiableCollection);
				}

				// Remove previous one and add new one
				actions.Remove(item.CommandCode.ToString());
				actions.Add(item.CommandCode.ToString(), modifiedCollection.RawLabel);

				// Store
				GeneralSettingsService.Actions = actions;

				// Exit
				item.IsEditMode = false;
				ViewModel.KeyboardShortcuts.Remove(item);

				return;
			}
			else
			{
				// Remove existing setting
				var modifiableCollection = HotKeyCollection.Parse(storedKeys!).ToList();
				modifiableCollection.RemoveAll(x => x.RawLabel == item.PreviousHotKey.RawLabel);
				modifiedCollection = new(modifiableCollection);

				// Remove previous
				actions.Remove(item.CommandCode.ToString());

				if (modifiedCollection.HumanizedLabel != string.Empty)
					actions.Add(item.CommandCode.ToString(), modifiedCollection.RawLabel);

				// Save
				GeneralSettingsService.Actions = actions;

				// Exit
				item.IsEditMode = false;
				ViewModel.KeyboardShortcuts.Remove(item);

				return;
			}
		}

		private void EditorTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (sender is not TextBox textBox)
				return;

			var pressedKey = e.OriginalKey;

			List<VirtualKey> modifierKeys = 
			[
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
			];

			// If pressed key is one of modifier don't show it in the TextBox yet
			foreach (var modifier in modifierKeys)
			{
				if (pressedKey == modifier)
					return;
			}

			var pressedModifiers = HotKeyHelpers.GetCurrentKeyModifiers();
			string text = string.Empty;

			// Add the modifiers with translated
			if (pressedModifiers.HasFlag(KeyModifiers.Ctrl))
				text += $"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Ctrl)}+";
			if (pressedModifiers.HasFlag(KeyModifiers.Menu))
				text += $"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Menu)}+";
			if (pressedModifiers.HasFlag(KeyModifiers.Shift))
				text += $"{HotKey.LocalizedModifiers.GetValueOrDefault(KeyModifiers.Shift)}+";

			// Add the key with translated
			text += HotKey.LocalizedKeys.GetValueOrDefault((Keys)pressedKey);

			// Set text
			textBox.Text = text;

			// Prevent key down event in other UIElements from getting invoked
			e.Handled = true;
		}
	}
}
