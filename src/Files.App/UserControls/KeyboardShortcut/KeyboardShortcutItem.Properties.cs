// Copyright (c) 2023 Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcutItem
	{
		public static readonly DependencyProperty ItemTypeProperty =
			DependencyProperty.Register(
				nameof(ItemType),
				typeof(KeyboardShortcutItemKind),
				typeof(KeyboardShortcutItem),
				new PropertyMetadata(defaultValue: KeyboardShortcutItemKind.Outlined, (d, e) => ((KeyboardShortcutItem)d).OnItemTypePropertyChanged()));

		public KeyboardShortcutItemKind ItemType
		{
			get => (KeyboardShortcutItemKind)GetValue(ItemTypeProperty);
			set => SetValue(ItemTypeProperty, value);
		}

		public static readonly DependencyProperty SizeProperty =
			DependencyProperty.Register(
				nameof(Size),
				typeof(KeyboardShortcutItemSize),
				typeof(KeyboardShortcutItem),
				new PropertyMetadata(defaultValue: KeyboardShortcutItemSize.Small, (d, e) => ((KeyboardShortcutItem)d).OnSizePropertyChanged()));

		public KeyboardShortcutItemSize Size
		{
			get => (KeyboardShortcutItemSize)GetValue(SizeProperty);
			set => SetValue(SizeProperty, value);
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
				nameof(Text),
				typeof(string),
				typeof(KeyboardShortcutItem),
				new PropertyMetadata(defaultValue: string.Empty, (d, e) => ((KeyboardShortcutItem)d).OnTextPropertyChanged()));

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public void OnItemTypePropertyChanged()
		{
			OnItemTypeChanged();
		}

		public void OnSizePropertyChanged()
		{
			OnSizeChanged();
		}

		public void OnTextPropertyChanged()
		{
			OnTextChanged();
		}
	}
}
