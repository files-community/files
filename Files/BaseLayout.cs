﻿using Files.Common;
using Files.Filesystem;
using Files.Helpers;
using Files.UserControls;
using Files.View_Models;
using Files.Views;
using Files.Views.Pages;
using Microsoft.Toolkit.Uwp.Extensions;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    /// <summary>
    /// The base class which every layout page must derive from
    /// </summary>
    public abstract class BaseLayout : Page, INotifyPropertyChanged
    {
        private AppServiceConnection Connection => ParentShellPageInstance?.ServiceConnection;

        public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

        public SettingsViewModel AppSettings => App.AppSettings;

        public CurrentInstanceViewModel InstanceViewModel => ParentShellPageInstance.InstanceViewModel;

        public DirectoryPropertiesViewModel DirectoryPropertiesViewModel { get; }

        public bool IsQuickLookEnabled { get; set; } = false;

        public MenuFlyout BaseLayoutContextFlyout { get; set; }

        public MenuFlyout BaseLayoutItemContextFlyout { get; set; }

        public IShellPage ParentShellPageInstance { get; private set; } = null;

        private bool isSearchResultPage = false;

        public bool IsRenamingItem { get; set; } = false;

        private bool isItemSelected = false;

        public bool IsItemSelected
        {
            get
            {
                return isItemSelected;
            }
            internal set
            {
                if (value != isItemSelected)
                {
                    isItemSelected = value;
                    NotifyPropertyChanged(nameof(IsItemSelected));
                }
            }
        }

        private List<ListedItem> selectedItems = new List<ListedItem>();

        public List<ListedItem> SelectedItems
        {
            get
            {
                return selectedItems;
            }
            internal set
            {
                if (value != selectedItems)
                {
                    selectedItems = value;
                    if (selectedItems.Count == 0)
                    {
                        IsItemSelected = false;
                        SelectedItem = null;
                        SelectedItemsPropertiesViewModel.IsItemSelected = false;
                    }
                    else
                    {
                        IsItemSelected = true;
                        SelectedItem = selectedItems.First();
                        SelectedItemsPropertiesViewModel.IsItemSelected = true;

                        if (SelectedItems.Count >= 1)
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCount = SelectedItems.Count;
                        }

                        if (SelectedItems.Count == 1)
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCountString = SelectedItems.Count.ToString() + " " + "ItemSelected/Text".GetLocalized();
                            SelectedItemsPropertiesViewModel.ItemSize = SelectedItem.FileSize;
                        }
                        else
                        {
                            SelectedItemsPropertiesViewModel.SelectedItemsCountString = SelectedItems.Count.ToString() + " " + "ItemsSelected/Text".GetLocalized();

                            if (SelectedItems.All(x => x.PrimaryItemAttribute == StorageItemTypes.File))
                            {
                                long size = 0;
                                foreach (var item in SelectedItems)
                                {
                                    size += item.FileSizeBytes;
                                }
                                SelectedItemsPropertiesViewModel.ItemSize = ByteSizeLib.ByteSize.FromBytes(size).ToBinaryString().ConvertSizeAbbreviation();
                            }
                            else
                            {
                                SelectedItemsPropertiesViewModel.ItemSize = string.Empty;
                            }
                        }
                    }
                    NotifyPropertyChanged(nameof(SelectedItems));
                    SetDragModeForItems();
                }
            }
        }

        public ListedItem SelectedItem { get; private set; }

        public BaseLayout()
        {
            SelectedItemsPropertiesViewModel = new SelectedItemsPropertiesViewModel(this);
            DirectoryPropertiesViewModel = new DirectoryPropertiesViewModel();

            // QuickLook Integration
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            var isQuickLookIntegrationEnabled = localSettings.Values["quicklook_enabled"];

            if (isQuickLookIntegrationEnabled != null && isQuickLookIntegrationEnabled.Equals(true))
            {
                IsQuickLookEnabled = true;
            }
        }

        public abstract void SelectAllItems();

        public abstract void InvertSelection();

        public abstract void ClearSelection();

        public abstract void SetDragModeForItems();

        public abstract void ScrollIntoView(ListedItem item);

        public abstract int GetSelectedIndex();

        public abstract void SetSelectedItemOnUi(ListedItem selectedItem);

        public abstract void SetSelectedItemsOnUi(List<ListedItem> selectedItems);

        public abstract void AddSelectedItemsOnUi(List<ListedItem> selectedItems);

        private void ClearShellContextMenus(MenuFlyout menuFlyout)
        {
            var contextMenuItems = menuFlyout.Items.Where(c => c.Tag != null && ParseContextMenuTag(c.Tag).menuHandle != null).ToList();
            for (int i = 0; i < contextMenuItems.Count; i++)
            {
                menuFlyout.Items.RemoveAt(menuFlyout.Items.IndexOf(contextMenuItems[i]));
            }
            if (menuFlyout.Items[0] is MenuFlyoutSeparator flyoutSeperator)
            {
                menuFlyout.Items.RemoveAt(menuFlyout.Items.IndexOf(flyoutSeperator));
            }
        }

        public virtual void SetShellContextmenu(MenuFlyout menuFlyout, bool shiftPressed, bool showOpenMenu)
        {
            ClearShellContextMenus(menuFlyout);
            var currentBaseLayoutItemCount = menuFlyout.Items.Count;
            var maxItems = AppSettings.ShowAllContextMenuItems ? int.MaxValue : shiftPressed ? 6 : 4;
            if (Connection != null)
            {
                var response = Connection.SendMessageAsync(new ValueSet()
                {
                        { "Arguments", "LoadContextMenu" },
                        { "FilePath", IsItemSelected ?
                            string.Join('|', selectedItems.Select(x => x.ItemPath)) :
                            ParentShellPageInstance.FilesystemViewModel.CurrentFolder.ItemPath},
                        { "ExtendedMenu", shiftPressed },
                        { "ShowOpenMenu", showOpenMenu }
                }).AsTask().Result;
                if (response.Status == AppServiceResponseStatus.Success
                    && response.Message.ContainsKey("Handle"))
                {
                    var contextMenu = JsonConvert.DeserializeObject<Win32ContextMenu>((string)response.Message["ContextMenu"]);
                    if (contextMenu != null)
                    {
                        LoadMenuFlyoutItem(menuFlyout.Items, contextMenu.Items, (string)response.Message["Handle"], true, maxItems);
                    }
                }
            }
            var totalFlyoutItems = menuFlyout.Items.Count - currentBaseLayoutItemCount;
            if (totalFlyoutItems > 0 && !(menuFlyout.Items[totalFlyoutItems] is MenuFlyoutSeparator))
            {
                menuFlyout.Items.Insert(totalFlyoutItems, new MenuFlyoutSeparator());
            }
        }

        public abstract void FocusSelectedItems();

        public abstract void StartRenameItem();

        public abstract void ResetItemOpacity();

        public abstract void SetItemOpacity(ListedItem item);

        protected abstract ListedItem GetItemFromElement(object element);

        private void AppSettings_LayoutModeChangeRequested(object sender, EventArgs e)
        {
            if (ParentShellPageInstance.ContentPage != null)
            {
                ParentShellPageInstance.FilesystemViewModel.CancelLoadAndClearFiles();
                ParentShellPageInstance.FilesystemViewModel.IsLoadingItems = true;
                ParentShellPageInstance.FilesystemViewModel.IsLoadingItems = false;

                ParentShellPageInstance.ContentFrame.Navigate(AppSettings.GetLayoutType(), new NavigationArguments()
                {
                    NavPathParam = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory,
                    AssociatedTabInstance = ParentShellPageInstance
                }, null);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            // Add item jumping handler
            AppSettings.LayoutModeChangeRequested += AppSettings_LayoutModeChangeRequested;
            Window.Current.CoreWindow.CharacterReceived += Page_CharacterReceived;
            var parameters = (NavigationArguments)eventArgs.Parameter;
            isSearchResultPage = parameters.IsSearchResultPage;
            ParentShellPageInstance = parameters.AssociatedTabInstance;
            IsItemSelected = false;
            ParentShellPageInstance.FilesystemViewModel.IsFolderEmptyTextDisplayed = false;

            if (!isSearchResultPage)
            {
                ParentShellPageInstance.NavigationToolbar.CanRefresh = true;
                ParentShellPageInstance.NavigationToolbar.CanCopyPathInPage = true;
                string previousDir = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory;
                await ParentShellPageInstance.FilesystemViewModel.SetWorkingDirectoryAsync(parameters.NavPathParam);

                // pathRoot will be empty on recycle bin path
                var workingDir = ParentShellPageInstance.FilesystemViewModel.WorkingDirectory;
                string pathRoot = Path.GetPathRoot(workingDir);
                if (string.IsNullOrEmpty(pathRoot) || workingDir == pathRoot)
                {
                    ParentShellPageInstance.NavigationToolbar.CanNavigateToParent = false;
                }
                else
                {
                    ParentShellPageInstance.NavigationToolbar.CanNavigateToParent = true;
                }
                ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(App.AppSettings.RecycleBinPath);
                ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\");

                MainPage.MultitaskingControl?.UpdateSelectedTab(new DirectoryInfo(workingDir).Name, workingDir, false);
                ParentShellPageInstance.FilesystemViewModel.RefreshItems(previousDir);
                ParentShellPageInstance.NavigationToolbar.PathControlDisplayText = parameters.NavPathParam;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = false;
            }
            else
            {
                ParentShellPageInstance.NavigationToolbar.CanRefresh = false;
                ParentShellPageInstance.NavigationToolbar.CanCopyPathInPage = false;
                ParentShellPageInstance.NavigationToolbar.CanNavigateToParent = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = false;
                ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = true;

                MainPage.MultitaskingControl?.UpdateSelectedTab(null, null, true);
                ParentShellPageInstance.FilesystemViewModel.AddSearchResultsToCollection(parameters.SearchResults, parameters.SearchPathParam);
            }

            ParentShellPageInstance.InstanceViewModel.IsPageTypeNotHome = true; // show controls that were hidden on the home page
            ParentShellPageInstance.Clipboard_ContentChanged(null, null);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            ParentShellPageInstance.FilesystemViewModel.CancelLoadAndClearFiles(isSearchResultPage);
            // Remove item jumping handler
            Window.Current.CoreWindow.CharacterReceived -= Page_CharacterReceived;
            AppSettings.LayoutModeChangeRequested -= AppSettings_LayoutModeChangeRequested;
        }

        private void UnloadMenuFlyoutItemByName(string nameToUnload)
        {
            var menuItem = this.FindName(nameToUnload) as DependencyObject;
            if (menuItem != null) // Prevent crash if the MenuFlyoutItem is missing
            {
                (menuItem as MenuFlyoutItemBase).Visibility = Visibility.Collapsed;
            }
        }

        private void LoadMenuFlyoutItem(IList<MenuFlyoutItemBase> MenuItemsList, IEnumerable<Win32ContextMenuItem> menuFlyoutItems, string menuHandle, bool showIcons = true, int itemsBeforeOverflow = int.MaxValue)
        {
            var items_count = 0; // Separators do not count for reaching the overflow threshold
            var menu_items = menuFlyoutItems.TakeWhile(x => x.Type == MenuItemType.MFT_SEPARATOR || ++items_count <= itemsBeforeOverflow).ToList();
            var overflow_items = menuFlyoutItems.Except(menu_items).ToList();

            if (overflow_items.Where(x => x.Type != MenuItemType.MFT_SEPARATOR).Any())
            {
                var menuLayoutSubItem = new MenuFlyoutSubItem()
                {
                    Text = "ContextMenuMoreItemsLabel".GetLocalized(),
                    Tag = ((Win32ContextMenuItem)null, menuHandle),
                    Icon = new FontIcon()
                    {
                        FontFamily = App.Current.Resources["FluentUIGlyphs"] as Windows.UI.Xaml.Media.FontFamily,
                        Glyph = "\xEAD0"
                    }
                };
                LoadMenuFlyoutItem(menuLayoutSubItem.Items, overflow_items, menuHandle, false);
                MenuItemsList.Insert(0, menuLayoutSubItem);
            }
            foreach (var menuFlyoutItem in menu_items
                .SkipWhile(x => x.Type == MenuItemType.MFT_SEPARATOR) // Remove leading seperators
                .Reverse()
                .SkipWhile(x => x.Type == MenuItemType.MFT_SEPARATOR)) // Remove trailing separators
            {
                if ((menuFlyoutItem.Type == MenuItemType.MFT_SEPARATOR) && (MenuItemsList.FirstOrDefault() is MenuFlyoutSeparator))
                {
                    // Avoid duplicate separators
                    continue;
                }

                BitmapImage image = null;
                if (showIcons)
                {
                    image = new BitmapImage();
                    if (!string.IsNullOrEmpty(menuFlyoutItem.IconBase64))
                    {
                        byte[] bitmapData = Convert.FromBase64String(menuFlyoutItem.IconBase64);
                        using (var ms = new MemoryStream(bitmapData))
                        {
#pragma warning disable CS4014
                            image.SetSourceAsync(ms.AsRandomAccessStream());
#pragma warning restore CS4014
                        }
                    }
                }

                if (menuFlyoutItem.Type == MenuItemType.MFT_SEPARATOR)
                {
                    var menuLayoutItem = new MenuFlyoutSeparator()
                    {
                        Tag = (menuFlyoutItem, menuHandle)
                    };
                    MenuItemsList.Insert(0, menuLayoutItem);
                }
                else if (menuFlyoutItem.SubItems.Where(x => x.Type != MenuItemType.MFT_SEPARATOR).Any()
                    && !string.IsNullOrEmpty(menuFlyoutItem.Label))
                {
                    var menuLayoutSubItem = new MenuFlyoutSubItem()
                    {
                        Text = menuFlyoutItem.Label.Replace("&", ""),
                        Tag = (menuFlyoutItem, menuHandle),
                    };
                    LoadMenuFlyoutItem(menuLayoutSubItem.Items, menuFlyoutItem.SubItems, menuHandle, false);
                    MenuItemsList.Insert(0, menuLayoutSubItem);
                }
                else if (!string.IsNullOrEmpty(menuFlyoutItem.Label))
                {
                    var menuLayoutItem = new MenuFlyoutItemWithImage()
                    {
                        Text = menuFlyoutItem.Label.Replace("&", ""),
                        Tag = (menuFlyoutItem, menuHandle),
                        BitmapIcon = image
                    };
                    menuLayoutItem.Click += MenuLayoutItem_Click;
                    MenuItemsList.Insert(0, menuLayoutItem);
                }
            }
        }

        private (Win32ContextMenuItem menuItem, string menuHandle) ParseContextMenuTag(object tag)
        {
            if (tag is ValueTuple<Win32ContextMenuItem, string>)
            {
                (Win32ContextMenuItem menuItem, string menuHandle) = (ValueTuple<Win32ContextMenuItem, string>)tag;
                return (menuItem, menuHandle);
            }

            return (null, null);
        }

        private async void MenuLayoutItem_Click(object sender, RoutedEventArgs e)
        {
            var currentMenuLayoutItem = (MenuFlyoutItem)sender;
            if (currentMenuLayoutItem != null)
            {
                var (menuItem, menuHandle) = ParseContextMenuTag(currentMenuLayoutItem.Tag);
                if (Connection != null)
                {
                    await Connection.SendMessageAsync(new ValueSet()
                    {
                        { "Arguments", "ExecAndCloseContextMenu" },
                        { "Handle", menuHandle },
                        { "ItemID", menuItem.ID },
                        { "CommandString", menuItem.CommandString }
                    });
                }
            }
        }

        public async void RightClickItemContextMenu_Closing(object sender, object e)
        {
            var shellContextMenuTag = (sender as MenuFlyout).Items.Where(x => x.Tag != null)
                .Select(x => ParseContextMenuTag(x.Tag)).FirstOrDefault(x => x.menuItem != null);
            if (shellContextMenuTag.menuItem != null && Connection != null)
            {
                await Connection.SendMessageAsync(new ValueSet()
                {
                    { "Arguments", "ExecAndCloseContextMenu" },
                    { "Handle", shellContextMenuTag.menuHandle }
                });
            }
        }

        public void RightClickContextMenu_Opening(object sender, object e)
        {
            ClearSelection();
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            SetShellContextmenu(BaseLayoutContextFlyout, shiftPressed, false);
        }

        public void RightClickItemContextMenu_Opening(object sender, object e)
        {
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            var showOpenMenu = (SelectedItems.Count == 1)
                && SelectedItem.PrimaryItemAttribute == StorageItemTypes.File
                && !string.IsNullOrEmpty(SelectedItem.FileExtension)
                && SelectedItem.FileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase);
            SetShellContextmenu(BaseLayoutItemContextFlyout, shiftPressed, showOpenMenu);

            if (!AppSettings.ShowCopyLocationOption)
            {
                UnloadMenuFlyoutItemByName("CopyLocationItem");
            }

            if (!DataTransferManager.IsSupported())
            {
                UnloadMenuFlyoutItemByName("ShareItem");
            }

            // Find selected items that are not folders
            if (SelectedItems.Any(x => x.PrimaryItemAttribute != StorageItemTypes.Folder))
            {
                UnloadMenuFlyoutItemByName("SidebarPinItem");
                UnloadMenuFlyoutItemByName("OpenInNewTab");
                UnloadMenuFlyoutItemByName("OpenInNewWindowItem");

                if (SelectedItems.Count == 1)
                {
                    if (!string.IsNullOrEmpty(SelectedItem.FileExtension))
                    {
                        if (SelectedItem.IsShortcutItem)
                        {
                            (this.FindName("OpenItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            UnloadMenuFlyoutItemByName("CreateShortcut");
                        }
                        else if (SelectedItem.FileExtension.Equals(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            UnloadMenuFlyoutItemByName("OpenItem");
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                        else if (SelectedItem.FileExtension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".bat", StringComparison.OrdinalIgnoreCase))
                        {
                            (this.FindName("OpenItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            (this.FindName("RunAsAdmin") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            (this.FindName("RunAsAnotherUser") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                        else if (SelectedItem.FileExtension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
                        {
                            UnloadMenuFlyoutItemByName("OpenItem");
                            UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            (this.FindName("RunAsAnotherUser") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                        else if (SelectedItem.FileExtension.Equals(".appx", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".msix", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".appxbundle", StringComparison.OrdinalIgnoreCase)
                            || SelectedItem.FileExtension.Equals(".msixbundle", StringComparison.OrdinalIgnoreCase))
                        {
                            (this.FindName("OpenItemWithAppPicker") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                        else
                        {
                            (this.FindName("OpenItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            (this.FindName("OpenItemWithAppPicker") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                            UnloadMenuFlyoutItemByName("RunAsAdmin");
                            UnloadMenuFlyoutItemByName("RunAsAnotherUser");
                            (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                        }
                    }
                }
                else if (SelectedItems.Count > 1)
                {
                    UnloadMenuFlyoutItemByName("OpenItem");
                    UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");
                    UnloadMenuFlyoutItemByName("CreateShortcut");
                }
            }
            else  // All are folders or shortcuts to folders
            {
                UnloadMenuFlyoutItemByName("OpenItem");
                UnloadMenuFlyoutItemByName("OpenItemWithAppPicker");

                if (SelectedItems.Any(x => x.IsShortcutItem))
                {
                    UnloadMenuFlyoutItemByName("SidebarPinItem");
                    UnloadMenuFlyoutItemByName("CreateShortcut");
                }
                else if (SelectedItems.Count == 1)
                {
                    (this.FindName("SidebarPinItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    (this.FindName("CreateShortcut") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    (this.FindName("OpenItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                }
                else
                {
                    (this.FindName("SidebarPinItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    UnloadMenuFlyoutItemByName("CreateShortcut");
                }

                if (SelectedItems.Count <= 5 && SelectedItems.Count > 0)
                {
                    (this.FindName("OpenInNewTab") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    (this.FindName("OpenInNewWindowItem") as MenuFlyoutItemBase).Visibility = Visibility.Visible;
                    //this.FindName("SidebarPinItem");
                    //this.FindName("OpenInNewTab");
                    //this.FindName("OpenInNewWindowItem");
                }
                else if (SelectedItems.Count > 5)
                {
                    UnloadMenuFlyoutItemByName("OpenInNewTab");
                    UnloadMenuFlyoutItemByName("OpenInNewWindowItem");
                }
            }

            //check the file extension of the selected item
            ParentShellPageInstance.ContentPage.SelectedItemsPropertiesViewModel.CheckFileExtension();
        }

        protected virtual void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            char letterPressed = Convert.ToChar(args.KeyCode);
            ParentShellPageInstance.InteractionOperations.PushJumpChar(letterPressed);
        }

        protected async void List_DragOver(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            ClearSelection();
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.Handled = true;
                IReadOnlyList<IStorageItem> draggedItems = await e.DataView.GetStorageItemsAsync();
                e.DragUIOverride.IsCaptionVisible = true;

                var folderName = Path.GetFileName(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory);
                // As long as one file doesn't already belong to this folder
                if (InstanceViewModel.IsPageTypeSearchResults || draggedItems.AreItemsAlreadyInFolder(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory))
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                else if (draggedItems.AreItemsInSameDrive(ParentShellPageInstance.FilesystemViewModel.WorkingDirectory))
                {
                    e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), folderName);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else
                {
                    e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), folderName);
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }

            deferral.Complete();
        }

        protected async void List_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                await ParentShellPageInstance.InteractionOperations.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, ParentShellPageInstance.FilesystemViewModel.WorkingDirectory, true);
                e.Handled = true;
            }

            deferral.Complete();
        }

        protected async void Item_DragStarting(object sender, DragStartingEventArgs e)
        {
            List<IStorageItem> selectedStorageItems = new List<IStorageItem>();

            foreach (ListedItem item in ParentShellPageInstance.ContentPage.SelectedItems)
            {
                if (item is ShortcutItem)
                {
                    // Can't drag shortcut items
                    continue;
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.File)
                {
                    await ParentShellPageInstance.FilesystemViewModel.GetFileFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                }
                else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    await ParentShellPageInstance.FilesystemViewModel.GetFolderFromPathAsync(item.ItemPath)
                        .OnSuccess(t => selectedStorageItems.Add(t));
                }
            }

            if (selectedStorageItems.Count == 0)
            {
                e.Cancel = true;
                return;
            }

            e.Data.SetStorageItems(selectedStorageItems, false);
            e.DragUI.SetContentFromDataPackage();
        }

        private ListedItem dragOverItem = null;
        private DispatcherTimer dragOverTimer = new DispatcherTimer();

        private void Item_DragLeave(object sender, DragEventArgs e)
        {
            ListedItem item = GetItemFromElement(sender);
            if (item == dragOverItem)
            {
                // Reset dragged over item
                dragOverItem = null;
            }
        }

        protected async void Item_DragOver(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            ListedItem item = GetItemFromElement(sender);
            SetSelectedItemOnUi(item);

            if (dragOverItem != item)
            {
                dragOverItem = item;
                dragOverTimer.Stop();
                dragOverTimer.Debounce(() =>
                {
                    if (dragOverItem != null && !InstanceViewModel.IsPageTypeSearchResults)
                    {
                        dragOverItem = null;
                        dragOverTimer.Stop();
                        ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);
                    }
                }, TimeSpan.FromMilliseconds(1000), false);
            }

            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.Handled = true;
                IReadOnlyList<IStorageItem> draggedItems = await e.DataView.GetStorageItemsAsync();

                if (InstanceViewModel.IsPageTypeSearchResults || draggedItems.AreItemsAlreadyInFolder(item.ItemPath) || draggedItems.Any(draggedItem => draggedItem.Path == item.ItemPath))
                {
                    e.AcceptedOperation = DataPackageOperation.None;
                }
                // Items from the same drive as this folder are dragged into this folder, so we move the items instead of copy
                else if (draggedItems.AreItemsInSameDrive(item.ItemPath))
                {
                    e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalized(), item.ItemName);
                    e.AcceptedOperation = DataPackageOperation.Move;
                }
                else
                {
                    e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalized(), item.ItemName);
                    e.AcceptedOperation = DataPackageOperation.Copy;
                }
            }

            deferral.Complete();
        }

        protected async void Item_Drop(object sender, DragEventArgs e)
        {
            var deferral = e.GetDeferral();

            e.Handled = true;
            ListedItem rowItem = GetItemFromElement(sender);
            await ParentShellPageInstance.InteractionOperations.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, (rowItem as ShortcutItem)?.TargetPath ?? rowItem.ItemPath, true);
            deferral.Complete();
        }

        protected void InitializeDrag(UIElement element)
        {
            ListedItem item = GetItemFromElement(element);
            if (item != null)
            {
                element.AllowDrop = false;
                element.DragStarting -= Item_DragStarting;
                element.DragStarting += Item_DragStarting;
                element.DragOver -= Item_DragOver;
                element.DragLeave -= Item_DragLeave;
                element.Drop -= Item_Drop;
                if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
                {
                    element.AllowDrop = true;
                    element.DragOver += Item_DragOver;
                    element.DragLeave += Item_DragLeave;
                    element.Drop += Item_Drop;
                }
            }
        }

        // VirtualKey doesn't support / accept plus and minus by default.
        public readonly VirtualKey PlusKey = (VirtualKey)187;

        public readonly VirtualKey MinusKey = (VirtualKey)189;

        public void GridViewSizeIncrease(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            AppSettings.GridViewSize = AppSettings.GridViewSize + Constants.Browser.GridViewBrowser.GridViewIncrement; // Make Larger
            if (args != null)
            {
                args.Handled = true;
            }
        }

        public void GridViewSizeDecrease(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            AppSettings.GridViewSize = AppSettings.GridViewSize - Constants.Browser.GridViewBrowser.GridViewIncrement; // Make Smaller
            if (args != null)
            {
                args.Handled = true;
            }
        }

        public void BaseLayout_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.KeyModifiers == VirtualKeyModifiers.Control)
            {
                if (e.GetCurrentPoint(null).Properties.MouseWheelDelta < 0) // Mouse wheel down
                {
                    GridViewSizeDecrease(null, null);
                }
                else // Mouse wheel up
                {
                    GridViewSizeIncrease(null, null);
                }

                e.Handled = true;
            }
        }
    }
}