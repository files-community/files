// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.UserControls.Menus;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
namespace Files.App.Helpers.ContextFlyouts
{
	/// <summary>
	/// Provides static helper for conversion from ContextMenuFlyoutItemViewModels into a control.
	/// </summary>
	public static class ContextFlyoutModelToElementHelper
	{
		public static List<MenuFlyoutItemBase>? GetMenuFlyoutItemsFromModel(List<ContextMenuFlyoutItemViewModel>? items)
		{
			if (items is null)
				return null;

			var flyout = new List<MenuFlyoutItemBase>();
			items.ForEach(i =>
			{
				var menuItem = GetMenuItem(i);
				flyout.Add(menuItem);
				if (menuItem is MenuFlyoutSubItem menuFlyoutSubItem && menuFlyoutSubItem.Items.Count == 0)
				{
					// Add a placeholder
					menuItem.Visibility = Visibility.Collapsed;

					var placeolder = new MenuFlyoutItem()
					{
						Text = menuFlyoutSubItem.Text,
						Tag = menuFlyoutSubItem.Tag,
						Icon = menuFlyoutSubItem.Icon,
					};
					flyout.Add(placeolder);
				}
			});
			return flyout;
		}

		public static (List<ICommandBarElement> primaryElements, List<ICommandBarElement> secondaryElements) GetAppBarItemsFromModel(List<ContextMenuFlyoutItemViewModel> items)
		{
			var primaryModels = items.Where(i => i.IsPrimary).ToList();
			var secondaryModels = items.Except(primaryModels).ToList();

			if (!secondaryModels.IsEmpty() && secondaryModels.Last().ItemType is ContextMenuFlyoutItemType.Separator)
				secondaryModels.RemoveAt(secondaryModels.Count - 1);

			var primary = new List<ICommandBarElement>();
			primaryModels.ForEach(i => primary.Add(GetCommandBarItem(i)));
			var secondary = new List<ICommandBarElement>();
			secondaryModels.ForEach(i => secondary.Add(GetCommandBarItem(i)));

			return (primary, secondary);
		}

		/// <summary>
		/// Same as GetAppBarItemsFromModel, but ignores the IsPrimary property and returns one list
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		public static List<ICommandBarElement> GetAppBarButtonsFromModelIgnorePrimary(List<ContextMenuFlyoutItemViewModel> items)
		{
			var elements = new List<ICommandBarElement>();
			items.ForEach(i => elements.Add(GetCommandBarItem(i)));
			return elements;
		}

		public static MenuFlyoutItemBase GetMenuItem(ContextMenuFlyoutItemViewModel item)
		{
			return item.ItemType switch
			{
				ContextMenuFlyoutItemType.Separator => new MenuFlyoutSeparator(),
				_ => GetMenuFlyoutItem(item),
			};
		}

		private static MenuFlyoutItemBase GetMenuFlyoutItem(ContextMenuFlyoutItemViewModel item)
		{
			if (item.Items is not null)
			{
				var flyoutSubItem = new MenuFlyoutSubItem()
				{
					Text = item.Text,
					Tag = item.Tag,
				};

				if (item.BitmapIcon is not null)
				{
					flyoutSubItem.Style = App.Current.Resources["MenuFlyoutSubItemWithImageStyle"] as Style;
					try
					{
						MenuFlyoutSubItemCustomProperties.SetBitmapIcon(flyoutSubItem, item.BitmapIcon);
					}
					catch (Exception e)
					{
						Debug.WriteLine(e);
					}
				}

				item.Items.ForEach(i =>
				{
					flyoutSubItem.Items.Add(GetMenuItem(i));
				});

				flyoutSubItem.IsEnabled = item.IsEnabled;
				flyoutSubItem.Visibility = item.IsHidden ? Visibility.Collapsed : Visibility.Visible;

				return flyoutSubItem;
			}
			return GetItem(item);
		}

		private static MenuFlyoutItemBase GetItem(ContextMenuFlyoutItemViewModel i)
		{
			if (i.BitmapIcon is not null)
			{
				var item = new MenuFlyoutItemWithImage()
				{
					Text = i.Text,
					Tag = i.Tag,
					Command = i.Command,
					CommandParameter = i.CommandParameter,
				};
				try
				{
					item.BitmapIcon = i.BitmapIcon;
				}
				catch (Exception e)
				{
					Debug.WriteLine(e);
				}
				return item;
			}
			MenuFlyoutItem flyoutItem;

			if (i.ItemType is ContextMenuFlyoutItemType.Toggle)
			{
				flyoutItem = new ToggleMenuFlyoutItem()
				{
					Text = i.Text,
					Tag = i.Tag,
					Command = i.Command,
					CommandParameter = i.CommandParameter,
					IsChecked = i.IsChecked,
				};
				if (!string.IsNullOrEmpty(i.Glyph))
				{
					flyoutItem.Icon = new FontIcon { Glyph = i.Glyph };
				}
			}
			else
			{
				var icon = string.IsNullOrEmpty(i.Glyph) ? null : new FontIcon
				{
					Glyph = i.Glyph,
				};

				if (icon is not null && !string.IsNullOrEmpty(i.GlyphFontFamilyName))
				{
					var fontFamily = App.Current.Resources[i.GlyphFontFamilyName] as FontFamily;
					icon.FontFamily = fontFamily;
				}

				flyoutItem = new MenuFlyoutItem()
				{
					Text = i.Text,
					Tag = i.Tag,
					Command = i.Command,
					CommandParameter = i.CommandParameter,
					Icon = icon,
				};
			}

			if (i.KeyboardAccelerator is not null)
				flyoutItem.KeyboardAccelerators.Add(i.KeyboardAccelerator);

			flyoutItem.IsEnabled = i.IsEnabled;
			flyoutItem.Visibility = i.IsHidden ? Visibility.Collapsed : Visibility.Visible;

			if (i.KeyboardAcceleratorTextOverride is not null)
				flyoutItem.KeyboardAcceleratorTextOverride = i.KeyboardAcceleratorTextOverride;

			return flyoutItem;
		}

		public static ICommandBarElement GetCommandBarItem(ContextMenuFlyoutItemViewModel item)
		{
			return item.ItemType switch
			{
				ContextMenuFlyoutItemType.Separator => new AppBarSeparator()
				{
					Tag = item.Tag,
					Visibility = item.IsHidden ? Visibility.Collapsed : Visibility.Visible,
				},
				_ => GetCommandBarButton(item),
			};
		}

		private static ICommandBarElement GetCommandBarButton(ContextMenuFlyoutItemViewModel item)
		{
			ICommandBarElement element;
			FontIcon? icon = null;
			if (!string.IsNullOrEmpty(item.Glyph))
			{
				icon = new FontIcon
				{
					Glyph = item.Glyph,
				};

				if (!string.IsNullOrEmpty(item.GlyphFontFamilyName))
				{
					var fontFamily = App.Current.Resources[item.GlyphFontFamilyName] as FontFamily;
					icon.FontFamily = fontFamily;
				}
			}

			MenuFlyout? ctxFlyout = null;
			if ((item.Items is not null && item.Items.Count > 0) || item.ID == "ItemOverflow")
			{
				ctxFlyout = new MenuFlyout();
				GetMenuFlyoutItemsFromModel(item.Items)?.ForEach(i => ctxFlyout.Items.Add(i));
			}

			UIElement? content = null;
			if (item.BitmapIcon is not null)
				content = new Image()
				{
					Source = item.BitmapIcon,
				};
			else if (item.ThemedIconModel.IsValid)
				content = item.ThemedIconModel.ToThemedIcon();
			else if (item.ShowLoadingIndicator)
				content = new ProgressRing()
				{
					IsIndeterminate = true,
					IsActive = true,
				};

			if (item.ItemType is ContextMenuFlyoutItemType.Toggle)
			{
				element = new AppBarToggleButton()
				{
					Label = item.Text,
					Tag = item.Tag,
					Command = item.Command,
					CommandParameter = item.CommandParameter,
					IsChecked = item.IsChecked,
					Content = content,
					LabelPosition = item.IsPrimary || item.CollapseLabel ? CommandBarLabelPosition.Collapsed : CommandBarLabelPosition.Default,
					IsEnabled = item.IsEnabled,
					Visibility = item.IsHidden ? Visibility.Collapsed : Visibility.Visible,
				};

				if (element is AppBarToggleButton toggleButton)
				{
					if (icon is not null)
						toggleButton.Icon = icon;

					if (item.IsPrimary || item.CollapseLabel)
						toggleButton.SetValue(ToolTipService.ToolTipProperty, item.Text);

					if (item.KeyboardAccelerator is not null && item.KeyboardAcceleratorTextOverride is not null)
					{
						toggleButton.KeyboardAccelerators.Add(item.KeyboardAccelerator);
						toggleButton.KeyboardAcceleratorTextOverride = item.KeyboardAcceleratorTextOverride;
					}
				}
			}
			else
			{
				element = new AppBarButton()
				{
					Label = item.Text,
					Tag = item.Tag,
					Command = item.Command,
					CommandParameter = item.CommandParameter,
					Flyout = ctxFlyout,
					LabelPosition = item.IsPrimary || item.CollapseLabel ? CommandBarLabelPosition.Collapsed : CommandBarLabelPosition.Default,
					Content = content,
					IsEnabled = item.IsEnabled,
					Visibility = item.IsHidden ? Visibility.Collapsed : Visibility.Visible,
				};

				if (element is AppBarButton button)
				{
					if (icon is not null)
						button.Icon = icon;

					if (item.IsPrimary || item.CollapseLabel)
						button.SetValue(ToolTipService.ToolTipProperty, item.Text);

					if (item.KeyboardAccelerator is not null && item.KeyboardAcceleratorTextOverride is not null)
					{
						button.KeyboardAccelerators.Add(item.KeyboardAccelerator);
						button.KeyboardAcceleratorTextOverride = item.KeyboardAcceleratorTextOverride;
						// Fixes #16193: VirtualKey does not support OEM keys (e.g "�")
						button.KeyboardAcceleratorPlacementMode = Microsoft.UI.Xaml.Input.KeyboardAcceleratorPlacementMode.Hidden;
					}
				}
			}

			return element;
		}
	}
}