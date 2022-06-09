﻿using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Shared.Services.DateTimeFormatter;
using Files.Uwp.Filesystem;
using Files.Uwp.Filesystem.StorageItems;
using Files.Uwp.Helpers;
using Files.Uwp.ViewModels.Properties;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media.Imaging;

namespace Files.Uwp.ViewModels.Previews
{
    public class FolderPreviewViewModel : BasePreviewModel
    {
        private static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetService<IDateTimeFormatter>();

        private BaseStorageFolder Folder { get; set; }
        public BitmapImage Thumbnail { get; set; } = new BitmapImage();

        public FolderPreviewViewModel(ListedItem item): base(item) {}

        public async Task LoadAsync() => await LoadPreviewAndDetailsAsync();

        private async Task LoadPreviewAndDetailsAsync()
        {
            var rootItem = await FilesystemTasks.Wrap(() => DrivesManager.GetRootFromPathAsync(Item.ItemPath));
            Folder = await StorageFileExtensions.DangerousGetFolderFromPathAsync(Item.ItemPath, rootItem);
            var items = await Folder.GetItemsAsync();

            var iconData = await FileThumbnailHelper.LoadIconFromStorageItemAsync(Folder, 400, ThumbnailMode.SingleItem);
            iconData ??= await FileThumbnailHelper.LoadIconWithoutOverlayAsync(Item.ItemPath, 400);
            if (iconData is not null)
            {
                Thumbnail = await iconData.ToBitmapAsync();
            }

            var info = await Folder.GetBasicPropertiesAsync();
            Item.FileDetails = new()
            {
                GetFileProperty("PropertyItemCount", items.Count),
                GetFileProperty("PropertyDateModified", dateTimeFormatter.ToLongLabel(info.DateModified)),
                GetFileProperty("PropertyDateCreated", dateTimeFormatter.ToLongLabel(info.ItemDate)),
                GetFileProperty("PropertyItemPathDisplay", Folder.Path),
            };

            if (userSettingsService.PreferencesSettingsService.AreFileTagsEnabled)
            {
                Item.FileDetails.Add(new FileProperty()
                {
                    NameResource = "FileTags",
                    Value = Item.FileTagUI?.TagName
                });
            }
        }
    }
}