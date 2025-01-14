// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Files.App.Views.Settings
{
	public sealed partial class TagsPage : Page
	{
		private readonly IWindowContext WindowContext = Ioc.Default.GetRequiredService<IWindowContext>();

		private string oldTagName = string.Empty;

		// Will be null unless the user has edited any tag
		private ListedTagViewModel? editingTag;

		private FlyoutBase? deleteItemFlyout;

		public bool AllowItemsDrag
			=> WindowContext.CanDragAndDrop;

		public TagsPage()
		{
			InitializeComponent();
		}

		private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			var textBox = (TextBox)sender;
			switch (e.Key)
			{
				case VirtualKey.Enter:
					if (!editingTag!.CanCommit)
						return;

					CommitChanges(textBox);
					e.Handled = true;
					break;
			}
		}

		private void EditTag_Click(object sender, RoutedEventArgs e)
		{
			if (editingTag is not null)
			{
				editingTag.IsEditing = false;
				editingTag.NewName = editingTag.Tag.Name;
				editingTag.NewColor = editingTag.Tag.Color;
			}

			editingTag = (ListedTagViewModel)((Button)sender).DataContext;
			editingTag.NewColor = editingTag.Tag.Color;
			editingTag.NewName = editingTag.Tag.Name;
			editingTag.IsEditing = true;

			var item = (ListViewItem)TagsList.ContainerFromItem(editingTag);
			var textBlock = item.FindDescendant("TagName") as TextBlock;
			var textBox = item.FindDescendant("TagNameTextBox") as TextBox;

			textBox!.TextChanged += RenameTextBox_TextChanged;

			textBox!.Text = textBlock!.Text;
			oldTagName = textBlock.Text;
		}

		private void CommitRenameTag_Click(object sender, RoutedEventArgs e)
		{
			var item = (ListViewItem)TagsList.ContainerFromItem(editingTag);

			CommitChanges(item.FindDescendant("TagNameTextBox") as TextBox);
		}

		private void CancelRenameTag_Click(object sender, RoutedEventArgs e)
		{
			CloseEdit();
		}

		private void PreRemoveTag_Click(object sender, RoutedEventArgs e)
		{
			deleteItemFlyout = ((Button)sender).Flyout;
		}

		private void CancelRemoveTag_Click(object sender, RoutedEventArgs e)
		{
			deleteItemFlyout?.Hide();
		}	

		private void RemoveTag_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.DeleteExistingTag((ListedTagViewModel)((Button)sender).DataContext);
		}

		private void RenameTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var text = ((TextBox)sender).Text;
			var isNullOrEmpty = string.IsNullOrEmpty(text);
			editingTag!.IsNameValid = isNullOrEmpty || (IsNameValid(text) && !ViewModel.Tags.Any(tag => tag.Tag.Name == text && editingTag!.Tag.Name != text));
			editingTag!.CanCommit = !isNullOrEmpty && editingTag!.IsNameValid && (
				text != editingTag!.Tag.Name ||
				editingTag!.NewColor != editingTag!.Tag.Color
			);
		}

		private void EditColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
		{
			if (editingTag is null)
				return;

			editingTag!.CanCommit = editingTag!.IsNameValid && (
				editingTag!.NewName != editingTag!.Tag.Name ||
				CommunityToolkit.WinUI.Helpers.ColorHelper.ToHex(sender.Color) != editingTag!.Tag.Color
			);
		}

		private void NewTagTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var text = ((TextBox)sender).Text;
			ViewModel.NewTag.Name = text;
			ViewModel.NewTag.IsNameValid = string.IsNullOrEmpty(text) || (IsNameValid(text) && !ViewModel.Tags.Any(tag => text == tag.Tag.Name));
		}

		private void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			if (args.KeyboardAccelerator.Key is VirtualKey.Escape && editingTag is not null)
			{
				CloseEdit();
				args.Handled = true;
			}
		}

		private void CommitChanges(TextBox textBox)
		{
			EndEditing(textBox);
			string newTagName = textBox.Text.Trim().TrimEnd('.');
			if (newTagName != oldTagName || editingTag!.NewColor != editingTag.Tag.Color)
				ViewModel.EditExistingTag(editingTag!, newTagName, editingTag!.NewColor);
		}

		private void EndEditing(TextBox textBox)
		{
			textBox.TextChanged -= RenameTextBox_TextChanged;
			editingTag!.IsEditing = false;
		}

		private void CloseEdit()
		{
			var item = (ListViewItem)TagsList.ContainerFromItem(editingTag);
			editingTag!.NewColor = editingTag.Tag.Color;
			editingTag!.IsNameValid = true;
			editingTag!.CanCommit = false;

			EndEditing(item.FindDescendant("TagNameTextBox") as TextBox);
		}

		private bool IsNameValid(string name)
		{
			return !(
				string.IsNullOrWhiteSpace(name) ||
				name.StartsWith('.') ||
				name.EndsWith('.')
			);
		}
	}
}
