// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Helpers.ContextFlyouts;
using Files.App.ViewModels.Layouts;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using Windows.Storage;

namespace Files.App.Data.Factories
{
	/// <summary>
	/// Represents a factory to generate a list for layout pages.
	/// </summary>
	public static class ContentPageContextFlyoutFactory
	{
		// Dependency injections

		private static readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private static readonly IModifiableCommandManager ModifiableCommands = Ioc.Default.GetRequiredService<IModifiableCommandManager>();
		private static readonly IAddItemService AddItemService = Ioc.Default.GetRequiredService<IAddItemService>();
		private static readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private static IStorageArchiveService StorageArchiveService { get; } = Ioc.Default.GetRequiredService<IStorageArchiveService>();

		public static List<ContextMenuFlyoutItemViewModel> GetItemContextCommandsWithoutShellItems(CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems, BaseLayoutViewModel commandsViewModel, bool shiftPressed, SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel, ShellViewModel? itemViewModel = null)
		{
			var menuItemsList = GetBaseItemMenuItems(commandsViewModel: commandsViewModel, selectedItems: selectedItems, selectedItemsPropertiesViewModel: selectedItemsPropertiesViewModel, currentInstanceViewModel: currentInstanceViewModel, itemViewModel: itemViewModel);
			menuItemsList = Filter(items: menuItemsList, shiftPressed: shiftPressed, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems, removeOverflowMenu: false);
			return menuItemsList;
		}

		public static Task<List<ContextMenuFlyoutItemViewModel>> GetItemContextShellCommandsAsync(string workingDir, List<ListedItem> selectedItems, bool shiftPressed, bool showOpenMenu, CancellationToken cancellationToken)
		{
			return ShellContextFlyoutFactory.GetShellContextmenuAsync(shiftPressed: shiftPressed, showOpenMenu: showOpenMenu, workingDirectory: workingDir, selectedItems: selectedItems, cancellationToken: cancellationToken);
		}

		public static List<ContextMenuFlyoutItemViewModel> Filter(List<ContextMenuFlyoutItemViewModel> items, List<ListedItem> selectedItems, bool shiftPressed, CurrentInstanceViewModel currentInstanceViewModel, bool removeOverflowMenu = true)
		{
			items = items.Where(x => Check(item: x, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList();
			items.ForEach(x => x.Items = x.Items?.Where(y => Check(item: y, currentInstanceViewModel: currentInstanceViewModel, selectedItems: selectedItems)).ToList());

			var overflow = items.FirstOrDefault(x => x.ID == "ItemOverflow");
			if (overflow is not null)
			{
				if (!shiftPressed && UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu) // items with ShowOnShift to overflow menu
				{
					var overflowItems = items.Where(x => x.ShowOnShift).ToList();

					// Adds a separator between items already there and the new ones
					if (overflow.Items.Count != 0 && overflowItems.Count > 0 && overflow.Items.Last().ItemType != ContextMenuFlyoutItemType.Separator)
						overflow.Items.Add(new ContextMenuFlyoutItemViewModel { ItemType = ContextMenuFlyoutItemType.Separator });

					items = items.Except(overflowItems).ToList();
					overflow.Items.AddRange(overflowItems);
				}

				// remove the overflow if it has no child items
				if (overflow.Items.Count == 0 && removeOverflowMenu)
					items.Remove(overflow);
			}

			return items;
		}

		private static bool Check(ContextMenuFlyoutItemViewModel item, CurrentInstanceViewModel currentInstanceViewModel, List<ListedItem> selectedItems)
		{
			return
				(item.ShowInRecycleBin || !currentInstanceViewModel.IsPageTypeRecycleBin) &&
				(item.ShowInSearchPage || !currentInstanceViewModel.IsPageTypeSearchResults) &&
				(item.ShowInFtpPage || !currentInstanceViewModel.IsPageTypeFtp) &&
				(item.ShowInZipPage || !currentInstanceViewModel.IsPageTypeZipFolder) &&
				(!item.SingleItemOnly || selectedItems.Count == 1) &&
				item.ShowItem;
		}

		public static List<ContextMenuFlyoutItemViewModel> GetBaseItemMenuItems(
			BaseLayoutViewModel commandsViewModel,
			SelectedItemsPropertiesViewModel? selectedItemsPropertiesViewModel,
			List<ListedItem> selectedItems,
			CurrentInstanceViewModel currentInstanceViewModel,
			ShellViewModel? itemViewModel = null)
		{
			bool itemsSelected = itemViewModel is null;
			bool canDecompress = selectedItems.Any() && selectedItems.All(x => x.IsArchive)
				|| selectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File && FileExtensionHelpers.IsZipFile(x.FileExtension));
			bool canCompress = !canDecompress || selectedItems.Count > 1;
			bool showOpenItemWith = selectedItems.All(
				i => (i.PrimaryItemAttribute == StorageItemTypes.File && !i.IsShortcut && !i.IsExecutable) || (i.PrimaryItemAttribute == StorageItemTypes.Folder && i.IsArchive));
			bool areAllItemsFolders = selectedItems.All(i => i.PrimaryItemAttribute == StorageItemTypes.Folder);
			bool isFirstFileExecutable = FileExtensionHelpers.IsExecutableFile(selectedItems.FirstOrDefault()?.FileExtension);
			string newArchiveName =
				Path.GetFileName(selectedItems.Count is 1 ? selectedItems[0].ItemPath : Path.GetDirectoryName(selectedItems[0].ItemPath))
				?? string.Empty;

			bool isDriveRoot = itemViewModel?.CurrentFolder is not null && (itemViewModel.CurrentFolder.ItemPath == Path.GetPathRoot(itemViewModel.CurrentFolder.ItemPath));

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(Commands.CloseActivePane)
				{
					IsVisible = !itemsSelected && Commands.CloseActivePane.IsExecutable,
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowItem = !itemsSelected && Commands.CloseActivePane.IsExecutable
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Layout".GetLocalizedResource(),
					Glyph = "\uE8A9",
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items =
					[
						new ContextMenuFlyoutItemViewModelBuilder(Commands.LayoutDetails)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.LayoutTiles)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.LayoutList)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.LayoutGrid)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.LayoutColumns)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.LayoutAdaptive)
						{
							IsToggle = true
						}.Build(),
					],
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SortBy".GetLocalizedResource(),
					ThemedIconModel = new ThemedIconModel()
					{
						ThemedIconStyle = "App.ThemedIcons.Sorting",
					},
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items =
					[
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortByName)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortByDateModified)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortByDateCreated)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortByType)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortBySize)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortBySyncStatus)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortByTag)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortByPath)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortByOriginalFolder)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortByDateDeleted)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModel
						{
							ItemType = ContextMenuFlyoutItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortAscending)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SortDescending)
						{
							IsToggle = true
						}.Build(),
					],
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "GroupBy".GetLocalizedResource(),
					Glyph = "\uF168",
					ShowItem = !itemsSelected,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					Items =
					[
						new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByNone)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByName)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "DateModifiedLowerCase".GetLocalizedResource(),
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Items =
							[
								new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByDateModifiedYear)
								{
									IsToggle = true
								}.Build(),
								new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByDateModifiedMonth)
								{
									IsToggle = true
								}.Build(),
								new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByDateModifiedDay)
								{
									IsToggle = true
								}.Build(),
							],
						},
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "DateCreated".GetLocalizedResource(),
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
							Items =
							[
								new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByDateCreatedYear)
								{
									IsToggle = true
								}.Build(),
								new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByDateCreatedMonth)
								{
									IsToggle = true
								}.Build(),
								new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByDateCreatedDay)
								{
									IsToggle = true
								}.Build(),
							],
						},
						new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByType)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupBySize)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupBySyncStatus)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByTag)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByOriginalFolder)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModel()
						{
							Text = "DateDeleted".GetLocalizedResource(),
							ShowInRecycleBin = true,
							IsHidden = !currentInstanceViewModel.IsPageTypeRecycleBin,
							Items =
							[
								new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByDateDeletedYear)
								{
									IsToggle = true
								}.Build(),
								new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByDateDeletedMonth)
								{
									IsToggle = true
								}.Build(),
								new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByDateDeletedDay)
								{
									IsToggle = true
								}.Build(),
							],
						},
						new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupByFolderPath)
						{
							IsToggle = true
						}.Build(),
						new ContextMenuFlyoutItemViewModel
						{
							ItemType = ContextMenuFlyoutItemType.Separator,
							ShowInRecycleBin = true,
							ShowInSearchPage = true,
							ShowInFtpPage = true,
							ShowInZipPage = true,
						},
						new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupAscending)
						{
							IsToggle = true,
							IsVisible = true
						}.Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.GroupDescending)
						{
							IsToggle = true,
							IsVisible = true
						}.Build(),
					],
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.RefreshItems)
				{
					IsVisible = !itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = !itemsSelected
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ThemedIconModel = new ThemedIconModel()
					{
						ThemedIconStyle = Commands.AddItem.Glyph.ThemedIconStyle
					},
					Text = Commands.AddItem.Label,
					Items = GetNewItemItems(commandsViewModel, currentInstanceViewModel.CanCreateFileInPage),
					ShowItem = !itemsSelected,
					ShowInFtpPage = true
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.EmptyRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.RestoreAllRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && !itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.RestoreRecycleBin)
				{
					IsVisible = currentInstanceViewModel.IsPageTypeRecycleBin && itemsSelected,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenItem).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenItemWithApplicationPicker)
				{
					Tag = "OpenWith",
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					// TODO add back text and icon when https://github.com/microsoft/microsoft-ui-xaml/issues/9409 is resolved
					//Text = "OpenWith".GetLocalizedResource(),
					//ThemedIconModel = new ThemedIconModel()
					//{
					//	ThemedIconStyle = "ColorIconOpenWith"
					//},
					Tag = "OpenWithOverflow",
					IsHidden = true,
					CollapseLabel = true,
					Items = [
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					],
					ShowInSearchPage = true,
					ShowItem = itemsSelected && showOpenItemWith
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenFileLocation).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenInNewTabAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewTab && Commands.OpenInNewTabAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenInNewWindowAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow && Commands.OpenInNewWindowAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenInNewPaneAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane && Commands.OpenInNewPaneAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "BaseLayoutItemContextFlyoutSetAs/Text".GetLocalizedResource(),
					ShowItem = itemsSelected && (selectedItemsPropertiesViewModel?.IsCompatibleToSetAsWindowsWallpaper ?? false),
					ShowInSearchPage = true,
					Items =
					[
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SetAsWallpaperBackground).Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SetAsLockscreenBackground).Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SetAsSlideshowBackground).Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.SetAsAppBackground).Build(),
					]
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.RotateLeft)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsCompatibleToSetAsWindowsWallpaper ?? false)
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.RotateRight)
				{
					IsVisible = !currentInstanceViewModel.IsPageTypeRecycleBin
								&& !currentInstanceViewModel.IsPageTypeZipFolder
								&& (selectedItemsPropertiesViewModel?.IsCompatibleToSetAsWindowsWallpaper ?? false)
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.RunAsAdmin).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.RunAsAnotherUser).Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowInSearchPage = true,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowItem = itemsSelected
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.CutItem)
				{
					IsPrimary = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.CopyItem)
				{
					IsPrimary = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.PasteItemToSelection)
				{
					IsPrimary = true,
					IsVisible = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.PasteItemAsShortcut).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.CopyItemPath)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowCopyPath
						&& itemsSelected
						&&!currentInstanceViewModel.IsPageTypeRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.CreateFolderWithSelection)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowCreateFolderWithSelection && itemsSelected
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.CreateShortcut)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowCreateShortcut
						&& itemsSelected
						&& (!selectedItems.FirstOrDefault()?.IsShortcut ?? false)
						&& !currentInstanceViewModel.IsPageTypeRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.CreateAlternateDataStream)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowCreateAlternateDataStream &&
						Commands.CreateAlternateDataStream.IsExecutable,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.Rename)
				{
					IsPrimary = true,
					IsVisible = itemsSelected
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.ShareItem)
				{
					IsPrimary = true
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(ModifiableCommands.DeleteItem)
				{
					IsVisible = itemsSelected,
					IsPrimary = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(ModifiableCommands.OpenProperties)
				{
					IsPrimary = true,
					IsVisible = ModifiableCommands.OpenProperties.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenParentFolder).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.PinFolderToSidebar)
				{
					IsVisible = Commands.PinFolderToSidebar.IsExecutable && UserSettingsService.GeneralSettingsService.ShowPinnedSection,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.UnpinFolderFromSidebar)
				{
					IsVisible = Commands.UnpinFolderFromSidebar.IsExecutable && UserSettingsService.GeneralSettingsService.ShowPinnedSection,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.PinToStart)
				{
					IsVisible = selectedItems.All(x => (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable || (x is IShortcutItem shortcutItem && FileExtensionHelpers.IsExecutableFile(shortcutItem.TargetPath))) && !x.IsArchive && !x.IsItemPinnedToStart),
					ShowOnShift = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.UnpinFromStart)
				{
					IsVisible = selectedItems.All(x => (x.PrimaryItemAttribute == StorageItemTypes.Folder || x.IsExecutable|| (x is IShortcutItem shortcutItem && FileExtensionHelpers.IsExecutableFile(shortcutItem.TargetPath))) && !x.IsArchive && x.IsItemPinnedToStart),
					ShowOnShift = true,
				}.Build(),
				new ContextMenuFlyoutItemViewModel
				{
					Text = "Compress".GetLocalizedResource(),
					ShowInSearchPage = true,
					ThemedIconModel = new ThemedIconModel()
					{
						ThemedIconStyle = "App.ThemedIcons.Zip",
					},
					Items =
					[
						new ContextMenuFlyoutItemViewModelBuilder(Commands.CompressIntoArchive).Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.CompressIntoZip).Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.CompressIntoSevenZip).Build(),
					],
					ShowItem = UserSettingsService.GeneralSettingsService.ShowCompressionOptions && itemsSelected && StorageArchiveService.CanCompress(selectedItems)
				},
				new ContextMenuFlyoutItemViewModel
				{
					Text = "Extract".GetLocalizedResource(),
					ShowInSearchPage = true,
					ThemedIconModel = new ThemedIconModel()
					{
						ThemedIconStyle = "App.ThemedIcons.Zip",
					},
					Items =
					[
						new ContextMenuFlyoutItemViewModelBuilder(Commands.DecompressArchive).Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.DecompressArchiveHereSmart).Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.DecompressArchiveHere).Build(),
						new ContextMenuFlyoutItemViewModelBuilder(Commands.DecompressArchiveToChildFolder).Build(),
					],
					ShowItem = UserSettingsService.GeneralSettingsService.ShowCompressionOptions && StorageArchiveService.CanDecompress(selectedItems)
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.FlattenFolder).Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendTo",
					CollapseLabel = true,
					ShowInSearchPage = true,
					ShowItem = itemsSelected && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToOverflow",
					IsHidden = true,
					CollapseLabel = true,
					Items = [
						new()
						{
							Text = "Placeholder",
							ShowInSearchPage = true,
						}
					],
					ShowInSearchPage = true,
					ShowItem = itemsSelected && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "TurnOnBitLocker".GetLocalizedResource(),
					Tag = "TurnOnBitLockerPlaceholder",
					CollapseLabel = true,
					IsEnabled = false,
					ShowItem = isDriveRoot
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "ManageBitLocker".GetLocalizedResource(),
					Tag = "ManageBitLockerPlaceholder",
					CollapseLabel = true,
					ShowItem = isDriveRoot,
					IsEnabled = false
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.EditInNotepad).Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowItem = (!itemsSelected && Commands.OpenTerminal.IsExecutable) ||
						(areAllItemsFolders && Commands.OpenTerminal.IsExecutable) ||
						Commands.OpenStorageSense.IsExecutable ||
						Commands.FormatDrive.IsExecutable
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenTerminal)
				{
					IsVisible = (!itemsSelected && Commands.OpenTerminal.IsExecutable) || (areAllItemsFolders && Commands.OpenTerminal.IsExecutable)
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenStorageSense).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.FormatDrive).Build(),
				// Shell extensions are not available on the FTP server or in the archive,
				// but following items are intentionally added because icons in the context menu will not appear
				// unless there is at least one menu item with an icon that is not an ThemedIconModel. (#12943)
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = [],
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					ShowInFtpPage = true,
					ShowInZipPage = true,
					ShowInRecycleBin = true,
					ShowInSearchPage = true,
					IsEnabled = false
				},
			}.Where(x => x.ShowItem).ToList();
		}

		public static List<ContextMenuFlyoutItemViewModel> GetNewItemItems(BaseLayoutViewModel commandsViewModel, bool canCreateFileInPage)
		{
			var list = new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModelBuilder(Commands.CreateFolder).Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "File".GetLocalizedResource(),
					Glyph = "\uE7C3",
					Command = commandsViewModel.CreateNewFileCommand,
					ShowInFtpPage = true,
					ShowInZipPage = true,
					IsEnabled = canCreateFileInPage
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.CreateShortcutFromDialog).Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
				}
			};

			if (canCreateFileInPage)
			{
				var cachedNewContextMenuEntries = AddItemService.GetEntries();
				cachedNewContextMenuEntries?.ForEach(i =>
				{
					if (!string.IsNullOrEmpty(i.IconBase64))
					{
						// loading the bitmaps takes a while, so this caches them
						byte[] bitmapData = Convert.FromBase64String(i.IconBase64);
						using var ms = new MemoryStream(bitmapData);
						var bitmap = new BitmapImage();
						_ = bitmap.SetSourceAsync(ms.AsRandomAccessStream());
						list.Add(new ContextMenuFlyoutItemViewModel()
						{
							Text = i.Name,
							BitmapIcon = bitmap,
							Command = commandsViewModel.CreateNewFileCommand,
							CommandParameter = i,
						});
					}
					else
					{
						list.Add(new ContextMenuFlyoutItemViewModel()
						{
							Text = i.Name,
							Glyph = "\xE7C3",
							Command = commandsViewModel.CreateNewFileCommand,
							CommandParameter = i,
						});
					}
				});
			}

			return list;
		}

		public static void SwapPlaceholderWithShellOption(CommandBarFlyout contextMenu, string placeholderName, ContextMenuFlyoutItemViewModel? replacingItem, int position)
		{
			var placeholder = contextMenu.SecondaryCommands
				.FirstOrDefault(x => Equals((x as AppBarButton)?.Tag, placeholderName)) as AppBarButton;

			if (placeholder is not null)
				placeholder.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;

			if (replacingItem is not null)
			{
				var (_, bitLockerCommands) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel([replacingItem]);
				contextMenu.SecondaryCommands.Insert(
					position,
					bitLockerCommands.FirstOrDefault()
				);
			}
		}
	}
}
