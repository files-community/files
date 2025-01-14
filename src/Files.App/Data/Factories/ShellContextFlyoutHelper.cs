// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Files.App.Helpers.ContextFlyouts;
using Files.Shared.Helpers;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.System;
using Windows.UI.Core;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Helpers
{
	public static class ShellContextFlyoutFactory
	{
		public static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public static async Task<List<ContextMenuFlyoutItemViewModel>> GetShellContextmenuAsync(bool showOpenMenu, bool shiftPressed, string workingDirectory, List<ListedItem>? selectedItems, CancellationToken cancellationToken)
		{
			bool IsItemSelected = selectedItems?.Count > 0;

			var menuItemsList = new List<ContextMenuFlyoutItemViewModel>();

			var filePaths = IsItemSelected
				? selectedItems!.Select(x => x.ItemPath).ToArray()
				: new[] { workingDirectory };

			Func<string, bool> FilterMenuItems(bool showOpenMenu)
			{
				var knownItems = new HashSet<string>()
				{
					"opennew", "opencontaining", "opennewprocess",
					"runas", "runasuser", "pintohome", "PinToStartScreen",
					"cut", "copy", "paste", "delete", "properties", "link",
					"Windows.ModernShare", "Windows.Share", "setdesktopwallpaper",
					"eject", "rename", "explore", "openinfiles", "extract",
					"copyaspath", "undelete", "empty", "format", "rotate90", "rotate270",
					Win32Helper.ExtractStringFromDLL("shell32.dll", 34593), // Add to collection
					Win32Helper.ExtractStringFromDLL("shell32.dll", 5384), // Pin to Start
					Win32Helper.ExtractStringFromDLL("shell32.dll", 5385), // Unpin from Start
					Win32Helper.ExtractStringFromDLL("shell32.dll", 5386), // Pin to taskbar
					Win32Helper.ExtractStringFromDLL("shell32.dll", 5387), // Unpin from taskbar
					"{9F156763-7844-4DC4-B2B1-901F640F5155}", // Open in Terminal
				};

				bool filterMenuItemsImpl(string menuItem) => !string.IsNullOrEmpty(menuItem)
					&& (knownItems.Contains(menuItem) || (!showOpenMenu && menuItem.Equals("open", StringComparison.OrdinalIgnoreCase)));

				return filterMenuItemsImpl;
			}

			var contextMenu = await ContextMenu.GetContextMenuForFiles(filePaths,
				shiftPressed ? PInvoke.CMF_EXTENDEDVERBS : PInvoke.CMF_NORMAL, FilterMenuItems(showOpenMenu));

			if (contextMenu is not null)
				LoadMenuFlyoutItem(menuItemsList, contextMenu, contextMenu.Items, cancellationToken, true);

			if (cancellationToken.IsCancellationRequested)
				menuItemsList.Clear();

			return menuItemsList;
		}

		private static void LoadMenuFlyoutItem(
			List<ContextMenuFlyoutItemViewModel> menuItemsListLocal,
			ContextMenu contextMenu,
			IEnumerable<Win32ContextMenuItem> menuFlyoutItems,
			CancellationToken cancellationToken,
			bool showIcons = true,
			int itemsBeforeOverflow = int.MaxValue)
		{
			if (cancellationToken.IsCancellationRequested)
				return;

			var itemsCount = 0; // Separators do not count for reaching the overflow threshold
			var menuItems = menuFlyoutItems.TakeWhile(x => x.Type == MENU_ITEM_TYPE.MFT_SEPARATOR || ++itemsCount <= itemsBeforeOverflow).ToList();
			var overflowItems = menuFlyoutItems.Except(menuItems).ToList();

			if (overflowItems.Any(x => x.Type != MENU_ITEM_TYPE.MFT_SEPARATOR))
			{
				var moreItem = menuItemsListLocal.FirstOrDefault(x => x.ID == "ItemOverflow");
				if (moreItem is null)
				{
					var menuLayoutSubItem = new ContextMenuFlyoutItemViewModel()
					{
						Text = "ShowMoreOptions".GetLocalizedResource(),
						Glyph = "\xE712",
					};
					LoadMenuFlyoutItem(menuLayoutSubItem.Items, contextMenu, overflowItems, cancellationToken, showIcons);
					menuItemsListLocal.Insert(0, menuLayoutSubItem);
				}
				else
				{
					LoadMenuFlyoutItem(moreItem.Items, contextMenu, overflowItems, cancellationToken, showIcons);
				}
			}

			foreach (var menuFlyoutItem in menuItems
				.SkipWhile(x => x.Type == MENU_ITEM_TYPE.MFT_SEPARATOR) // Remove leading separators
				.Reverse()
				.SkipWhile(x => x.Type == MENU_ITEM_TYPE.MFT_SEPARATOR)) // Remove trailing separators
			{
				if (cancellationToken.IsCancellationRequested)
					break;

				// Avoid duplicate separators
				if ((menuFlyoutItem.Type == MENU_ITEM_TYPE.MFT_SEPARATOR) && (menuItemsListLocal.FirstOrDefault()?.ItemType == ContextMenuFlyoutItemType.Separator))
					continue;

				BitmapImage? image = null;
				if (showIcons && menuFlyoutItem.Icon is { Length: > 0 })
				{
					image = new BitmapImage();
					using var ms = new MemoryStream(menuFlyoutItem.Icon);
					image.SetSourceAsync(ms.AsRandomAccessStream()).AsTask().Wait(10);
				}

				if (menuFlyoutItem.Type is MENU_ITEM_TYPE.MFT_SEPARATOR)
				{
					var menuLayoutItem = new ContextMenuFlyoutItemViewModel()
					{
						ItemType = ContextMenuFlyoutItemType.Separator,
						Tag = menuFlyoutItem
					};
					menuItemsListLocal.Insert(0, menuLayoutItem);
				}
				else if (!string.IsNullOrEmpty(menuFlyoutItem.Label) && menuFlyoutItem.SubItems is not null)
				{
					if (string.Equals(menuFlyoutItem.Label, Win32Helper.ExtractStringFromDLL("shell32.dll", 30312)))
						menuFlyoutItem.CommandString = "sendto";

					var menuLayoutSubItem = new ContextMenuFlyoutItemViewModel()
					{
						Text = menuFlyoutItem.Label.Replace("&", "", StringComparison.Ordinal),
						Tag = menuFlyoutItem,
						BitmapIcon = image,
						Items = [],
					};

					if (menuFlyoutItem.SubItems.Any())
					{
						LoadMenuFlyoutItem(menuLayoutSubItem.Items, contextMenu, menuFlyoutItem.SubItems, cancellationToken, showIcons);
					}
					else
					{
						menuLayoutSubItem.LoadSubMenuAction = async () =>
						{
							if (await contextMenu.LoadSubMenu(menuFlyoutItem.SubItems))
								LoadMenuFlyoutItem(menuLayoutSubItem.Items, contextMenu, menuFlyoutItem.SubItems, cancellationToken, showIcons);
						};
					}

					menuItemsListLocal.Insert(0, menuLayoutSubItem);
				}
				else if (!string.IsNullOrEmpty(menuFlyoutItem.Label))
				{
					var menuLayoutItem = new ContextMenuFlyoutItemViewModel
					{
						Text = menuFlyoutItem.Label.Replace("&", "", StringComparison.Ordinal),
						Tag = menuFlyoutItem,
						BitmapIcon = image,
						Command = new AsyncRelayCommand<object>(x => InvokeShellMenuItemAsync(contextMenu, x)),
						CommandParameter = menuFlyoutItem
					};
					menuItemsListLocal.Insert(0, menuLayoutItem);
				}
			}

			async Task InvokeShellMenuItemAsync(ContextMenu contextMenu, object? tag)
			{
				if (tag is not Win32ContextMenuItem menuItem)
					return;

				var menuId = menuItem.ID;
				var isFont = FileExtensionHelpers.IsFontFile(contextMenu.ItemsPath[0]);
				var verb = menuItem.CommandString;
				switch (verb)
				{
					case "install" when isFont:
						await Win32Helper.InstallFontsAsync([.. contextMenu.ItemsPath], false);
						break;

					case "installAllUsers" when isFont:
						await Win32Helper.InstallFontsAsync([.. contextMenu.ItemsPath], true);
						break;

					case "mount":
						var vhdPath = contextMenu.ItemsPath[0];
						await Win32Helper.MountVhdDisk(vhdPath);
						break;

					case "format":
						var drivePath = contextMenu.ItemsPath[0];
						await Win32Helper.OpenFormatDriveDialog(drivePath);
						break;

					default:
						await contextMenu.InvokeItem(menuId);
						break;
				}

				//contextMenu.Dispose(); // Prevents some menu items from working (TBC)
			}
		}

		public static List<ContextMenuFlyoutItemViewModel>? GetOpenWithItems(List<ContextMenuFlyoutItemViewModel> flyout)
		{
			var item = flyout.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "openas" });
			if (item is not null)
				flyout.Remove(item);

			return item?.Items;
		}

		public static List<ContextMenuFlyoutItemViewModel>? GetSendToItems(List<ContextMenuFlyoutItemViewModel> flyout)
		{
			var item = flyout.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "sendto" });
			if (item is not null)
				flyout.Remove(item);

			return item?.Items;
		}

		public static async Task LoadShellMenuItemsAsync(
			string path,
			CommandBarFlyout itemContextMenuFlyout,
			ContextMenuOptions? options = null,
			bool showOpenWithMenu = false,
			bool showSendToMenu = false)
		{
			try
			{
				if (options is not null && !options.IsLocationItem)
					return;

				var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
				var shellMenuItems = await ContentPageContextFlyoutFactory.GetItemContextShellCommandsAsync(
					workingDir: null,
					[new ListedItem(null) { ItemPath = path }],
					shiftPressed: shiftPressed,
					showOpenMenu: false,
					default);

				var openWithItem = showOpenWithMenu ? shellMenuItems.Where(x => (x.Tag as Win32ContextMenuItem)?.CommandString == "openas").ToList().FirstOrDefault() : null;
				if (openWithItem is not null)
					shellMenuItems.Remove(openWithItem);

				var sendToItem = shellMenuItems.Where(x => (x.Tag as Win32ContextMenuItem)?.CommandString == "sendto").ToList().FirstOrDefault();
				if (sendToItem is not null &&
					(showSendToMenu || !UserSettingsService.GeneralSettingsService.ShowSendToMenu))
					shellMenuItems.Remove(sendToItem);

				var turnOnBitLocker = shellMenuItems.FirstOrDefault(x => 
					x.Tag is Win32ContextMenuItem menuItem && 
					(menuItem.CommandString?.StartsWith("encrypt-bde") ?? false));

				if (turnOnBitLocker is not null)
					shellMenuItems.Remove(turnOnBitLocker);

				ContentPageContextFlyoutFactory.SwapPlaceholderWithShellOption(
					itemContextMenuFlyout,
					"TurnOnBitLockerPlaceholder",
					turnOnBitLocker,
					itemContextMenuFlyout.SecondaryCommands.Count - 2
				);

				var manageBitLocker = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "manage-bde" });
				if (manageBitLocker is not null)
					shellMenuItems.Remove(manageBitLocker);

				var lastItem = shellMenuItems.LastOrDefault();
				while (lastItem?.ItemType is ContextMenuFlyoutItemType.Separator)
				{
					shellMenuItems.Remove(lastItem);
					lastItem = shellMenuItems.LastOrDefault();
				}

				ContentPageContextFlyoutFactory.SwapPlaceholderWithShellOption(
					itemContextMenuFlyout,
					"ManageBitLockerPlaceholder",
					manageBitLocker,
					itemContextMenuFlyout.SecondaryCommands.Count - 2
				);

				sendToItem = showSendToMenu && UserSettingsService.GeneralSettingsService.ShowSendToMenu ? sendToItem : null;

				if (!UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
				{
					var (_, secondaryElements) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel(shellMenuItems);
					if (secondaryElements.Any())
					{
						var openedPopups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(MainWindow.Instance);
						var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");

						var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
						if (itemsControl is not null)
						{
							var maxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin;
							secondaryElements.OfType<FrameworkElement>()
											 .ForEach(x => x.MaxWidth = maxWidth); // Set items max width to current menu width (#5555)
						}

						secondaryElements.ForEach(i => itemContextMenuFlyout.SecondaryCommands.Add(i));
					}
					else if (itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarSeparator appBarSeparator && (appBarSeparator.Tag as string) == "OverflowSeparator") is AppBarSeparator overflowSeparator)
					{
						overflowSeparator.Visibility = Visibility.Collapsed;
					}

					if (itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") is AppBarButton overflowItem)
						overflowItem.Visibility = Visibility.Collapsed;
				}
				else
				{
					var overflowItems = ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(shellMenuItems);
					if (itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") is not AppBarButton overflowItem
						|| itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarSeparator appBarSeparator && (appBarSeparator.Tag as string) == "OverflowSeparator") is not AppBarSeparator overflowSeparator)
						return;

					var flyoutItems = (overflowItem.Flyout as MenuFlyout)?.Items;
					if (flyoutItems is not null)
						overflowItems?.ForEach(i => flyoutItems.Add(i));
					overflowItem.Visibility = overflowItems?.Any() ?? false ? Visibility.Visible : Visibility.Collapsed;
					overflowSeparator.Visibility = overflowItems?.Any() ?? false ? Visibility.Visible : Visibility.Collapsed;

					overflowItem.Label = "ShowMoreOptions".GetLocalizedResource();
					overflowItem.IsEnabled = true;
				}

				// Add items to openwith dropdown
				if (openWithItem?.LoadSubMenuAction is not null)
				{
					await openWithItem.LoadSubMenuAction();

					openWithItem.ThemedIconModel = new ThemedIconModel()
					{
						ThemedIconStyle = "App.ThemedIcons.OpenWith",
					};
					var (_, openWithItems) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel([openWithItem]);
					var index = 0;
					var placeholder = itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => Equals((x as AppBarButton)?.Tag, "OpenWithPlaceholder")) as AppBarButton;
					if (placeholder is not null)
					{
						placeholder.Visibility = Visibility.Collapsed;
						index = itemContextMenuFlyout.SecondaryCommands.IndexOf(placeholder);
					}
					itemContextMenuFlyout.SecondaryCommands.Insert(index, openWithItems.FirstOrDefault());
				}

				// Add items to sendto dropdown
				if (sendToItem?.LoadSubMenuAction is not null)
				{
					await sendToItem.LoadSubMenuAction();

					var (_, sendToItems) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel([sendToItem]);
					var index = 1;
					var placeholder = itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => Equals((x as AppBarButton)?.Tag, "SendToPlaceholder")) as AppBarButton;
					if (placeholder is not null)
					{
						placeholder.Visibility = Visibility.Collapsed;
						index = itemContextMenuFlyout.SecondaryCommands.IndexOf(placeholder);
					}
					itemContextMenuFlyout.SecondaryCommands.Insert(index, sendToItems.FirstOrDefault());
				}

				// Add items to shell submenu
				var itemsWithSubMenu = shellMenuItems.Where(x => x.LoadSubMenuAction is not null);
				var subMenuTasks = itemsWithSubMenu.Select(async item =>
				{
					await item.LoadSubMenuAction();
					if (!UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
					{
						AddItemsToMainMenu(itemContextMenuFlyout.SecondaryCommands, item);
					}
					else if (itemContextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton 
						         && appBarButton.Tag as string == "ItemOverflow") is AppBarButton overflowItem)
					{
						AddItemsToOverflowMenu(overflowItem, item);
					}
				});

				await Task.WhenAll(subMenuTasks);
			}
			catch { }
		}

		public static void AddItemsToMainMenu(IEnumerable<ICommandBarElement> mainMenu, ContextMenuFlyoutItemViewModel viewModel)
		{
			var appBarButton = mainMenu.FirstOrDefault(x => (x as AppBarButton)?.Tag == viewModel.Tag) as AppBarButton;

			if (appBarButton is not null)
			{
				var ctxFlyout = new MenuFlyout();
				ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(viewModel.Items)?.ForEach(i => ctxFlyout.Items.Add(i));
				appBarButton.Flyout = ctxFlyout;
				appBarButton.Visibility = Visibility.Collapsed;
				appBarButton.Visibility = Visibility.Visible;
			}
		}

		public static void AddItemsToOverflowMenu(AppBarButton? overflowItem, ContextMenuFlyoutItemViewModel viewModel)
		{
			if (overflowItem?.Flyout is MenuFlyout flyout)
			{
				var flyoutSubItem = flyout.Items.FirstOrDefault(x => x.Tag == viewModel.Tag) as MenuFlyoutSubItem;
				if (flyoutSubItem is not null)
				{
					viewModel.Items.ForEach(i => flyoutSubItem.Items.Add(ContextFlyoutModelToElementHelper.GetMenuItem(i)));
					flyout.Items[flyout.Items.IndexOf(flyoutSubItem) + 1].Visibility = Visibility.Collapsed;
					flyoutSubItem.Visibility = Visibility.Visible;
				}
			}
		}
	}
}
