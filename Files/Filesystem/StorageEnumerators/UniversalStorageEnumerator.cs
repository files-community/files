﻿using ByteSizeLib;
using Files.Extensions;
using Files.Views.LayoutModes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Filesystem.StorageEnumerators
{
    public static class UniversalStorageEnumerator
    {
        public static async Task<List<ListedItem>> ListEntries(
            StorageFolder rootFolder,
            StorageFolderWithPath currentStorageFolder,
            string returnformat,
            bool shouldDisplayFileExtensions,
            Type sourcePageType,
            CancellationToken cancellationToken,
            Func<List<ListedItem>, Task> intermediateAction
        )
        {
            var tempList = new List<ListedItem>();
            uint count = 0;
            while (true)
            {
                IStorageItem item = null;
                try
                {
                    var results = await rootFolder.GetItemsAsync(count, 1);
                    item = results?.FirstOrDefault();
                    if (item == null)
                    {
                        break;
                    }
                }
                catch (NotImplementedException)
                {
                    break;
                }
                catch (Exception ex) when (
                    ex is UnauthorizedAccessException
                    || ex is FileNotFoundException
                    || (uint)ex.HResult == 0x80070490) // ERROR_NOT_FOUND
                {
                    ++count;
                    continue;
                }
                if (item.IsOfType(StorageItemTypes.Folder))
                {
                    var folder = await AddFolderAsync(item as StorageFolder, currentStorageFolder, returnformat, cancellationToken);
                    if (folder != null)
                    {
                        tempList.Add(folder);
                    }
                    ++count;
                }
                else
                {
                    var file = item as StorageFile;
                    var fileEntry = await AddFileAsync(file, currentStorageFolder, returnformat, shouldDisplayFileExtensions, true, sourcePageType, cancellationToken);
                    if (fileEntry != null)
                    {
                        tempList.Add(fileEntry);
                    }
                    ++count;
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                if (intermediateAction != null && (count == 32 || count % 300 == 0))
                {
                    await intermediateAction(tempList);
                }
            }
            return tempList;
        }

        private static async Task<ListedItem> AddFolderAsync(StorageFolder folder, StorageFolderWithPath currentStorageFolder, string dateReturnFormat, CancellationToken cancellationToken)
        {
            var basicProperties = await folder.GetBasicPropertiesAsync();

            if (!cancellationToken.IsCancellationRequested)
            {
                return new ListedItem(folder.FolderRelativeId, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.Folder,
                    ItemName = folder.Name,
                    ItemDateModifiedReal = basicProperties.DateModified,
                    ItemType = folder.DisplayType,
                    IsHiddenItem = false,
                    Opacity = 1,
                    LoadFolderGlyph = true,
                    FileImage = null,
                    LoadFileIcon = false,
                    ItemPath = string.IsNullOrEmpty(folder.Path) ? Path.Combine(currentStorageFolder.Path, folder.Name) : folder.Path,
                    LoadUnknownTypeGlyph = false,
                    FileSize = null,
                    FileSizeBytes = 0
                    //FolderTooltipText = tooltipString,
                };
            }
            return null;
        }

        private static async Task<ListedItem> AddFileAsync(
            StorageFile file,
            StorageFolderWithPath currentStorageFolder,
            string dateReturnFormat,
            bool shouldDisplayFileExtensions,
            bool suppressThumbnailLoading,
            Type sourcePageType,
            CancellationToken cancellationToken
        )
        {
            var basicProperties = await file.GetBasicPropertiesAsync();
            // Display name does not include extension
            var itemName = string.IsNullOrEmpty(file.DisplayName) || shouldDisplayFileExtensions ?
                file.Name : file.DisplayName;
            var itemDate = basicProperties.DateModified;
            var itemPath = string.IsNullOrEmpty(file.Path) ? Path.Combine(currentStorageFolder.Path, file.Name) : file.Path;
            var itemSize = ByteSize.FromBytes(basicProperties.Size).ToBinaryString().ConvertSizeAbbreviation();
            var itemSizeBytes = basicProperties.Size;
            var itemType = file.DisplayType;
            var itemFolderImgVis = false;
            var itemFileExtension = file.FileType;

            BitmapImage icon = new BitmapImage();
            bool itemThumbnailImgVis;
            bool itemEmptyImgVis;

            if (!(sourcePageType == typeof(GridViewBrowser)))
            {
                try
                {
                    var itemThumbnailImg = suppressThumbnailLoading ? null :
                        await file.GetThumbnailAsync(ThumbnailMode.ListView, 40, ThumbnailOptions.UseCurrentScale);
                    if (itemThumbnailImg != null)
                    {
                        itemEmptyImgVis = false;
                        itemThumbnailImgVis = true;
                        icon.DecodePixelWidth = 40;
                        icon.DecodePixelHeight = 40;
                        await icon.SetSourceAsync(itemThumbnailImg);
                    }
                    else
                    {
                        itemEmptyImgVis = true;
                        itemThumbnailImgVis = false;
                    }
                }
                catch
                {
                    itemEmptyImgVis = true;
                    itemThumbnailImgVis = false;
                    // Catch here to avoid crash
                }
            }
            else
            {
                try
                {
                    var itemThumbnailImg = suppressThumbnailLoading ? null :
                        await file.GetThumbnailAsync(ThumbnailMode.ListView, 80, ThumbnailOptions.UseCurrentScale);
                    if (itemThumbnailImg != null)
                    {
                        itemEmptyImgVis = false;
                        itemThumbnailImgVis = true;
                        icon.DecodePixelWidth = 80;
                        icon.DecodePixelHeight = 80;
                        await icon.SetSourceAsync(itemThumbnailImg);
                    }
                    else
                    {
                        itemEmptyImgVis = true;
                        itemThumbnailImgVis = false;
                    }
                }
                catch
                {
                    itemEmptyImgVis = true;
                    itemThumbnailImgVis = false;
                }
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            if (file.Name.EndsWith(".lnk") || file.Name.EndsWith(".url"))
            {
                // This shouldn't happen, StorageFile api does not support shortcuts
                Debug.WriteLine("Something strange: StorageFile api returned a shortcut");
            }
            else
            {
                return new ListedItem(file.FolderRelativeId, dateReturnFormat)
                {
                    PrimaryItemAttribute = StorageItemTypes.File,
                    FileExtension = itemFileExtension,
                    IsHiddenItem = false,
                    Opacity = 1,
                    LoadUnknownTypeGlyph = itemEmptyImgVis,
                    FileImage = icon,
                    LoadFileIcon = itemThumbnailImgVis,
                    LoadFolderGlyph = itemFolderImgVis,
                    ItemName = itemName,
                    ItemDateModifiedReal = itemDate,
                    ItemType = itemType,
                    ItemPath = itemPath,
                    FileSize = itemSize,
                    FileSizeBytes = (long)itemSizeBytes,
                };
            }
            return null;
        }
    }
}
