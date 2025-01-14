// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Core.Storage;
using Files.Core.Storage.Storables;
using Files.Shared.Utils;
using Windows.Storage.FileProperties;

namespace Files.App.Data.Contracts
{
	internal sealed class ImagingService : IImageService
	{
		/// <inheritdoc/>
		public async Task<IImage?> GetIconAsync(IStorable storable, CancellationToken cancellationToken)
		{
			var iconData = await FileThumbnailHelper.GetIconAsync(storable.Id, Constants.ShellIconSizes.Small, storable is IFolder, IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);
			if (iconData is null)
				return null;

			var bitmapImage = await iconData.ToBitmapAsync();
			return bitmapImage is null ? null : new BitmapImageModel(bitmapImage);
		}

		public async Task<IImage?> GetImageModelFromDataAsync(byte[] rawData)
		{
			return new BitmapImageModel(await BitmapHelper.ToBitmapAsync(rawData));
		}

		public async Task<IImage?> GetImageModelFromPathAsync(string filePath, uint thumbnailSize = 64)
		{
			if (await FileThumbnailHelper.LoadIconFromPathAsync(filePath, thumbnailSize, ThumbnailMode.ListView, ThumbnailOptions.ResizeThumbnail) is byte[] imageBuffer)
				return await GetImageModelFromDataAsync(imageBuffer);

			return null;
		}
	}
}
