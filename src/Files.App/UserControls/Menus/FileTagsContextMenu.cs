// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using static Files.App.Helpers.MenuFlyoutHelper;

namespace Files.App.UserControls.Menus
{
	public sealed class FileTagsContextMenu : MenuFlyout
	{
		private IFileTagsSettingsService FileTagsSettingsService { get; } =
			Ioc.Default.GetService<IFileTagsSettingsService>();

		public IEnumerable<ListedItem> SelectedItems { get; }

		public FileTagsContextMenu(IEnumerable<ListedItem> selectedItems)
		{
			SetValue(MenuFlyoutHelper.ItemsSourceProperty, FileTagsSettingsService.FileTagList
				.Select(tag => new MenuFlyoutFactoryItemViewModel(() =>
				{
					var tagItem = new ToggleMenuFlyoutItem()
					{
						Text = tag.Name,
						Tag = tag
					};
					tagItem.Icon = new PathIcon()
					{
						Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), (string)Application.Current.Resources["App.Theme.PathIcon.FilledTag"]),
						Foreground = new SolidColorBrush(ColorHelpers.FromHex(tag.Color))
					};
					tagItem.Click += TagItem_Click;
					return tagItem;
				})));

			SelectedItems = selectedItems;

			Opening += Item_Opening;
		}

		private void TagItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			var tagItem = (ToggleMenuFlyoutItem)sender;
			if (tagItem.IsChecked)
			{
				AddFileTag(SelectedItems, (TagViewModel)tagItem.Tag);
			}
			else
			{
				RemoveFileTag(SelectedItems, (TagViewModel)tagItem.Tag);
			}
		}

		private void Item_Opening(object? sender, object e)
		{
			Opening -= Item_Opening;

			if (SelectedItems is null)
				return;

			// go through each tag and find the common one for all files
			var commonFileTags = SelectedItems
				.Select(x => x?.FileTags ?? Enumerable.Empty<string>())
				.DefaultIfEmpty(Enumerable.Empty<string>())
				.Aggregate((x, y) => x.Intersect(y))
				.Select(x => Items.FirstOrDefault(y => x == ((TagViewModel)y.Tag)?.Uid));

			commonFileTags.OfType<ToggleMenuFlyoutItem>().ForEach(x => x.IsChecked = true);
		}

		private void RemoveFileTag(IEnumerable<ListedItem> selectedListedItems, TagViewModel removed)
		{
			foreach (var selectedItem in selectedListedItems)
			{
				var existingTags = selectedItem.FileTags ?? [];
				if (existingTags.Contains(removed.Uid))
				{
					var tagList = existingTags.Except(new[] { removed.Uid }).ToArray();
					selectedItem.FileTags = tagList;
				}
			}
		}

		private void AddFileTag(IEnumerable<ListedItem> selectedListedItems, TagViewModel added)
		{
			foreach (var selectedItem in selectedListedItems)
			{
				var existingTags = selectedItem.FileTags ?? [];
				if (!existingTags.Contains(added.Uid))
				{
					selectedItem.FileTags = [.. existingTags, added.Uid];
				}
			}
		}
	}
}
