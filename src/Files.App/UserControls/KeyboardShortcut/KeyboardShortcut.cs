// Copyright (c) 2023 Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcut : Control
	{
		internal const string KeyboardShortcutItemsControl = "PART_KeyboardShortcutItemsControl";

		public KeyboardShortcut()
		{
			DefaultStyleKey = typeof(KeyboardShortcut);
		}

		private void OnItemTypeChanged()
		{
		}

		private void OnSizeChanged()
		{
		}

		private async void OnHotKeysChanged()
		{
			var keyboardShortcutItemsControl = GetTemplateChild(KeyboardShortcutItemsControl) as ItemsControl;
			for (int i = 0; i < 100 && keyboardShortcutItemsControl is null; i++)
			{
				// Wait for KeyboardShortcutItemsControl to be loaded
				await Task.Delay(10);
				keyboardShortcutItemsControl = GetTemplateChild(KeyboardShortcutItemsControl) as ItemsControl;
			}

			if (keyboardShortcutItemsControl is null)
				return;

			if (HotKeys.IsEmpty)
			{
				keyboardShortcutItemsControl.ItemsSource = null;
				return;
			}

			List<KeyboardShortcutItem> items = [];

			foreach (var item in HotKeys)
			{
				if (items.Any())
				{
					items.Add(new() { Text = ",", ItemType = KeyboardShortcutItemKind.TextOnly, Size = Size });
				}

				switch(item.Key, item.Modifier)
				{
					// No keys or modifiers specified
					case (Keys.None, KeyModifiers.None):
						break;

					// Key modifiers only
					case (Keys.None, _):
						GetModifierCode(item.Modifier);
						items.RemoveAt(items.Count - 1);
						break;

					// Keys only
					case (_, KeyModifiers.None):
						var key = HotKey.LocalizedKeys[item.Key];
						items.Add(new() { Text = key, ItemType = ItemType, Size = Size });
						break;

					// Others
					default:
						GetModifierCode(item.Modifier);
						key = HotKey.LocalizedKeys[item.Key];
						items.Add(new() { Text = key, ItemType = ItemType, Size = Size });
						break;
				}

				void GetModifierCode(KeyModifiers modifier)
				{
					if (modifier.HasFlag(KeyModifiers.Alt))
					{
						items.Add(new() { Text = HotKey.LocalizedModifiers[KeyModifiers.Alt], ItemType = ItemType, Size = Size });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.TextOnly, Size = Size });
					}
					if (modifier.HasFlag(KeyModifiers.Ctrl))
					{
						items.Add(new() { Text = HotKey.LocalizedModifiers[KeyModifiers.Ctrl], ItemType = ItemType, Size = Size });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.TextOnly, Size = Size });
					}
					if (modifier.HasFlag(KeyModifiers.Shift))
					{
						items.Add(new() { Text = HotKey.LocalizedModifiers[KeyModifiers.Shift], ItemType = ItemType, Size = Size });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.TextOnly, Size = Size });
					}
					if (modifier.HasFlag(KeyModifiers.Win))
					{
						items.Add(new() { Text = HotKey.LocalizedModifiers[KeyModifiers.Win], ItemType = ItemType, Size = Size });
						items.Add(new() { Text = "+", ItemType = KeyboardShortcutItemKind.TextOnly, Size = Size });
					}
				}
			}

			// Set value
			keyboardShortcutItemsControl.ItemsSource = items;
		}
	}
}
