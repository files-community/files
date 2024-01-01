﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Items
{
	public class WidgetFolderCardItem : WidgetCardItem, IWidgetCardItem<LocationItem>
	{
		private BitmapImage thumbnail;

		private byte[] thumbnailData;

		public string AutomationProperties { get; set; }

		public bool HasPath => !string.IsNullOrEmpty(Path);

		public bool HasThumbnail => thumbnail is not null && thumbnailData is not null;

		public BitmapImage Thumbnail
		{
			get => thumbnail;
			set => SetProperty(ref thumbnail, value);
		}

		public LocationItem Item { get; private set; }

		public string Text { get; set; }

		public bool IsPinned { get; set; }

		public WidgetFolderCardItem(LocationItem item, string text, bool isPinned)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				Text = text;
				AutomationProperties = Text;
			}

			IsPinned = isPinned;
			Item = item;
			Path = item.Path;
		}

		public async Task LoadCardThumbnailAsync()
		{
			if (thumbnailData is null || thumbnailData.Length == 0)
			{
				thumbnailData = await FileThumbnailHelper.LoadIconFromPathAsync(Path, Convert.ToUInt32(Constants.Widgets.WidgetIconSize), Windows.Storage.FileProperties.ThumbnailMode.SingleItem, Windows.Storage.FileProperties.ThumbnailOptions.ResizeThumbnail);
			}

			if (thumbnailData is not null && thumbnailData.Length > 0)
			{
				Thumbnail = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => thumbnailData.ToBitmapAsync(Constants.Widgets.WidgetIconSize));
			}
		}
	}
}
