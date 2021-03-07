﻿using Files.Common;
using Files.Controllers;
using Files.Enums;
using Files.Filesystem;
using Files.ViewModels;
using Files.Views;
using Microsoft.Toolkit.Uwp.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Files.DataModels
{
    public class SidebarPinnedModel
    {
        private SidebarPinnedController controller;

        private LocationItem favoriteSection, homeSection, librarySection;

        private const int numberDefaultRecentItems = 3;

        [JsonIgnore]
        public SettingsViewModel AppSettings => App.AppSettings;

        [JsonProperty("libraryitems")]
        public List<string> LibraryItems { get; set; } = new List<string>();

        [JsonProperty("favoriteitems")]
        public List<string> FavoriteItems { get; set; } = new List<string>();

        public void SetController(SidebarPinnedController controller)
        {
            this.controller = controller;
        }

        public SidebarPinnedModel()
        {
            homeSection = new LocationItem()
            {
                Text = "SidebarHome".GetLocalized(),
                Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                Glyph = "\uea80",
                IsDefaultLocation = true,
                Path = "Home",
                ChildItems = new ObservableCollection<INavigationControlItem>()
            };
            favoriteSection = new LocationItem()
            {
                Text = "SidebarFavorites".GetLocalized(),
                Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                Glyph = "\ueb83",
                ChildItems = new ObservableCollection<INavigationControlItem>()
            };
            librarySection = new LocationItem()
            {
                Text = "SidebarLibrary".GetLocalized(),
                Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                Glyph = "\uEC13",
                ChildItems = new ObservableCollection<INavigationControlItem>()
            };
        }

        /// <summary>
        /// Adds the default items to the navigation page
        /// </summary>
        public void AddDefaultItems()
        {
            LibraryItems.Add(AppSettings.DesktopPath);
            LibraryItems.Add(AppSettings.DownloadsPath);
            LibraryItems.Add(AppSettings.DocumentsPath);
            LibraryItems.Add(AppSettings.PicturesPath);
            LibraryItems.Add(AppSettings.MusicPath);
            LibraryItems.Add(AppSettings.VideosPath);
        }

        /// <summary>
        /// Adds the default favorites items.
        /// </summary>
        public async void AddDefaultFavoritesItems()
        {
            FavoriteItems.Add(AppSettings.DesktopPath);
            await AddItemToFavoritesSidebarAsync(AppSettings.DesktopPath);

            FavoriteItems.Add(AppSettings.DocumentsPath);
            await AddItemToFavoritesSidebarAsync(AppSettings.DocumentsPath);

            FavoriteItems.Add(AppSettings.DownloadsPath);
            await AddItemToFavoritesSidebarAsync(AppSettings.DownloadsPath);
        }

        /// <summary>
        /// Gets the items from the navigation page
        /// </summary>
        public List<string> GetItems()
        {
            return FavoriteItems;
        }

        /// <summary>
        /// Adds the item to the navigation page
        /// </summary>
        /// <param name="item">Item to remove</param>
        public async void AddItem(string item)
        {
            if (!FavoriteItems.Contains(item))
            {
                FavoriteItems.Add(item);
                await AddItemToFavoritesSidebarAsync(item);
                Save();
            }
        }

        public async Task ShowHideRecycleBinItemAsync(bool show)
        {
            await MainPage.SideBarItemsSemaphore.WaitAsync();
            try
            {
                if (show)
                {
                    var recycleBinItem = new LocationItem
                    {
                        Text = ApplicationData.Current.LocalSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                        Font = Application.Current.Resources["RecycleBinIcons"] as FontFamily,
                        Glyph = "\uEF87",
                        IsDefaultLocation = true,
                        Path = App.AppSettings.RecycleBinPath
                    };
                    // Add recycle bin to sidebar, title is read from LocalSettings (provided by the fulltrust process)
                    // TODO: the very first time the app is launched localized name not available
                    if (!favoriteSection.ChildItems.Any(x => x.Path == App.AppSettings.RecycleBinPath))
                    {
                        favoriteSection.ChildItems.Add(recycleBinItem);
                    }
                }
                else
                {
                    foreach (INavigationControlItem item in favoriteSection.ChildItems.ToList())
                    {
                        if (item is LocationItem && item.Path == App.AppSettings.RecycleBinPath)
                        {
                            favoriteSection.ChildItems.Remove(item);
                        }
                    }
                }
            }
            finally
            {
                MainPage.SideBarItemsSemaphore.Release();
            }
        }

        /// <summary>
        /// Removes the item from the navigation page
        /// </summary>
        /// <param name="item">Item to remove</param>
        public void RemoveItem(string item)
        {
            if (FavoriteItems.Contains(item))
            {
                FavoriteItems.Remove(item);
                RemoveFavoritesSidebarItems(item);
                Save();
            }
        }

        /// <summary>
        /// Moves the location item in the navigation sidebar from the old position to the new position
        /// </summary>
        /// <param name="locationItem">Location item to move</param>
        /// <param name="oldIndex">The old position index of the location item</param>
        /// <param name="newIndex">The new position index of the location item</param>
        /// <returns>True if the move was successful</returns>
        public bool MoveItem(INavigationControlItem locationItem, int oldIndex, int newIndex)
        {
            if (locationItem == null)
            {
                return false;
            }

            if (oldIndex >= 0 && newIndex >= 0)
            {
                favoriteSection.ChildItems.RemoveAt(oldIndex);
                favoriteSection.ChildItems.Insert(newIndex, locationItem);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Swaps two location items in the navigation sidebar
        /// </summary>
        /// <param name="firstLocationItem">The first location item</param>
        /// <param name="secondLocationItem">The second location item</param>
        public void SwapItems(INavigationControlItem firstLocationItem, INavigationControlItem secondLocationItem)
        {
            if (firstLocationItem == null || secondLocationItem == null)
            {
                return;
            }

            // A backup of the items, because the swapping of items requires removing and inserting them in the correct position
            var sidebarItemsBackup = new List<string>(this.FavoriteItems);

            try
            {
                var indexOfFirstItemInMainPage = IndexOfItem(firstLocationItem);
                var indexOfSecondItemInMainPage = IndexOfItem(secondLocationItem);

                // Moves the items in the MainPage
                var result = MoveItem(firstLocationItem, indexOfFirstItemInMainPage, indexOfSecondItemInMainPage);

                // Moves the items in this model and saves the model
                if (result == true)
                {
                    var indexOfFirstItemInModel = this.FavoriteItems.IndexOf(firstLocationItem.Path);
                    var indexOfSecondItemInModel = this.FavoriteItems.IndexOf(secondLocationItem.Path);
                    if (indexOfFirstItemInModel >= 0 && indexOfSecondItemInModel >= 0)
                    {
                        this.FavoriteItems.RemoveAt(indexOfFirstItemInModel);
                        this.FavoriteItems.Insert(indexOfSecondItemInModel, firstLocationItem.Path);
                    }

                    Save();
                }
            }
            catch (Exception ex) when (
                ex is ArgumentException // Pinned item was invalid
                || ex is FileNotFoundException // Pinned item was deleted
                || ex is System.Runtime.InteropServices.COMException // Pinned item's drive was ejected
                || (uint)ex.HResult == 0x8007000F // The system cannot find the drive specified
                || (uint)ex.HResult == 0x800700A1) // The specified path is invalid (usually an mtp device was disconnected)
            {
                Debug.WriteLine($"An error occurred while swapping pinned items in the navigation sidebar. {ex.Message}");
                this.FavoriteItems = sidebarItemsBackup;
                this.RemoveFavoritesSidebarItems();
                _ = this.AddAllItemsToSidebar();
            }
        }

        /// <summary>
        /// Returns the index of the location item in the navigation sidebar
        /// </summary>
        /// <param name="locationItem">The location item</param>
        /// <returns>Index of the item</returns>
        public int IndexOfItem(INavigationControlItem locationItem)
        {
            return favoriteSection.ChildItems.IndexOf(locationItem);
        }

        /// <summary>
        /// Returns the index of the location item in the collection containing Navigation control items
        /// </summary>
        /// <param name="locationItem">The location item</param>
        /// <param name="collection">The collection in which to find the location item</param>
        /// <returns>Index of the item</returns>
        public int IndexOfItem(INavigationControlItem locationItem, List<INavigationControlItem> collection)
        {
            return collection.IndexOf(locationItem);
        }

        /// <summary>
        /// Saves the model
        /// </summary>
        public void Save() => controller?.SaveModel();

        /// <summary>
        /// Adds the item do the navigation sidebar
        /// </summary>
        /// <param name="path">The path which to save</param>
        /// <returns>Task</returns>
        public async Task AddItemToFavoritesSidebarAsync(string path)
        {
            var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
            var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));
            if (res || (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path))
            {
                var lastItem = favoriteSection.ChildItems.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(App.AppSettings.RecycleBinPath));
                int insertIndex = lastItem != null ? favoriteSection.ChildItems.IndexOf(lastItem) + 1 : 0;
                var locationItem = new LocationItem
                {
                    Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                    Path = path,
                    Glyph = GetItemIcon(path),
                    IsDefaultLocation = false,
                    Text = res.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'))
                };

                if (!favoriteSection.ChildItems.Contains(locationItem))
                {
                    favoriteSection.ChildItems.Insert(insertIndex, locationItem);
                }
            }
            else
            {
                Debug.WriteLine($"Pinned item was invalid and will be removed from the file lines list soon: {res.ErrorCode}");
                RemoveItem(path);
            }
        }

        /// <summary>
        /// Adds the item to library sidebar asynchronous.
        /// </summary>
        /// <param name="path">The path.</param>
        public async Task AddItemToLibrarySidebarAsync(string path)
        {
            var item = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(path));
            var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));
            if (res || (FilesystemResult)FolderHelpers.CheckFolderAccessWithWin32(path))
            {
                var lastItem = librarySection.ChildItems.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(App.AppSettings.RecycleBinPath));
                int insertIndex = lastItem != null ? librarySection.ChildItems.IndexOf(lastItem) + 1 : 0;
                var locationItem = new LocationItem
                {
                    Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily,
                    Path = path,
                    Glyph = GetItemIcon(path),
                    IsDefaultLocation = false,
                    Text = res.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'))
                };

                if (!librarySection.ChildItems.Contains(locationItem))
                {
                    librarySection.ChildItems.Insert(insertIndex, locationItem);
                }
            }
            else
            {
                Debug.WriteLine($"Pinned item was invalid and will be removed from the file lines list soon: {res.ErrorCode}");
                RemoveItem(path);
            }
        }

        /// <summary>
        /// Adds the item to sidebar asynchronous.
        /// </summary>
        /// <param name="section">The section.</param>
        private void AddItemToSidebarAsync(LocationItem section)
        {
            var lastItem = favoriteSection.ChildItems.LastOrDefault(x => x.ItemType == NavigationControlItemType.Location && !x.Path.Equals(App.AppSettings.RecycleBinPath));
            int insertIndex = lastItem != null ? favoriteSection.ChildItems.IndexOf(lastItem) + 1 : 0;

            if (!favoriteSection.ChildItems.Contains(section))
            {
                favoriteSection.ChildItems.Insert(insertIndex, section);
            }
        }

        /// <summary>
        /// Adds all items to the navigation sidebar
        /// </summary>
        public async Task AddAllItemsToSidebar()
        {
            await MainPage.SideBarItemsSemaphore.WaitAsync();
            try
            {
                MainPage.SideBarItems.BeginBulkOperation();

                if (homeSection != null)
                {
                    AddItemToSidebarAsync(homeSection);
                }

                if (!MainPage.SideBarItems.Contains(favoriteSection))
                {
                    MainPage.SideBarItems.Add(favoriteSection);
                    AddDefaultFavoritesItems();
                }

                if (App.AppSettings.ShowLibrarySection)
                {
                    if (!MainPage.SideBarItems.Contains(librarySection))
                    {
                        MainPage.SideBarItems.Add(librarySection);

                        for (int i = 0; i < LibraryItems.Count(); i++)
                        {
                            string path = LibraryItems[i];
                            await AddItemToLibrarySidebarAsync(path);
                        }
                    }
                }

                MainPage.SideBarItems.EndBulkOperation();
            }
            finally
            {
                MainPage.SideBarItemsSemaphore.Release();
            }

            await ShowHideRecycleBinItemAsync(App.AppSettings.PinRecycleBinToSideBar);
        }

        /// <summary>
        /// Removes stale items in the navigation sidebar
        /// </summary>
        public void RemoveLibrarySidebarItems(string unpinFolder)
        {
            var item = favoriteSection.ChildItems.Where(x => x.Path.Equals(unpinFolder)).FirstOrDefault();
            favoriteSection.ChildItems.Remove(item);

            var mostRecentlyUsed = StorageApplicationPermissions.MostRecentlyUsedList;
            mostRecentlyUsed.Remove(mostRecentlyUsed.Entries.Where(x => x.Metadata.Equals(item.Path)).FirstOrDefault().Token);
        }

        public void RemoveFavoritesSidebarItems()
        {
            // Remove unpinned items from sidebar
            for (int i = 0; i < favoriteSection.ChildItems.Count(); i++)
            {
                if (favoriteSection.ChildItems[i] is LocationItem)
                {
                    var item = favoriteSection.ChildItems[i] as LocationItem;
                    if (!item.IsDefaultLocation && !FavoriteItems.Contains(item.Path))
                    {
                        favoriteSection.ChildItems.RemoveAt(i);
                    }
                }
            }
        }
        public async void RemoveFavoritesSidebarItems(string path)
        {            
            try
            {
                var sectionItem = favoriteSection.ChildItems.Where(x => x.Path.Equals(path)).FirstOrDefault();
                if (sectionItem != null)
                {
                    favoriteSection.ChildItems.Remove(sectionItem);
                }

                FavoriteItems = (from n in FavoriteItems select n).Distinct().ToList();
                var item = FavoriteItems.Where(x => x.Equals(path)).FirstOrDefault();
                if (item != null)
                {
                    FavoriteItems.Remove(item);
                }
            }
            catch
            { }
            finally
            {
                var mostRecentlyUsed = StorageApplicationPermissions.MostRecentlyUsedList;
                var item = mostRecentlyUsed.Entries.Where(x => x.Metadata.Equals(path)).FirstOrDefault();
                mostRecentlyUsed.Remove(item.Token);
            }          
        }

        /// <summary>
        /// Gets the icon for the items in the navigation sidebar
        /// </summary>
        /// <param name="path">The path in the sidebar</param>
        /// <returns>The icon code</returns>
        public string GetItemIcon(string path)
        {
            string iconCode;
            
            if (path.Equals(AppSettings.DesktopPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\ue9f1";
            }
            else if (path.Equals(AppSettings.DownloadsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uE91c";
            }
            else if (path.Equals(AppSettings.DocumentsPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uea11";
            }
            else if (path.Equals(AppSettings.PicturesPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uea83";
            }
            else if (path.Equals(AppSettings.MusicPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uead4";
            }
            else if (path.Equals(AppSettings.VideosPath, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\uec0d";
            }
            else if (Path.GetPathRoot(path).Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                iconCode = "\ueb8b";
            }
            else
            {
                iconCode = "\uea55";
            }

            return iconCode;
        }
    }
}