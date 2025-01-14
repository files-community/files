// Copyright (c) 2023 Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.KeyboardShortcut
{
	public class KeyboardShortcutItemStyleSelector : StyleSelector
	{
		public Style OutlinedItemStyle { get; set; } = null!;

		public Style FilledItemStyle { get; set; } = null!;

		public Style TextOnlyItemStyle { get; set; } = null!;

		protected override Style SelectStyleCore(object item, DependencyObject container)
		{
			if (container is KeyboardShortcutItem keyboardShortcutItem)
			{
				return keyboardShortcutItem.ItemType switch
				{
					KeyboardShortcutItemKind.Outlined => OutlinedItemStyle,
					KeyboardShortcutItemKind.Filled => FilledItemStyle,
					KeyboardShortcutItemKind.TextOnly => TextOnlyItemStyle,
					_ => OutlinedItemStyle,
				};
			}

			return OutlinedItemStyle;
		}
	}
}
