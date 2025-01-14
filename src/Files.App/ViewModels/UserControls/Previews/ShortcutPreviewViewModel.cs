// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;

namespace Files.App.ViewModels.Previews
{
	internal sealed class ShortcutPreviewViewModel : BasePreviewModel
	{
		public ShortcutPreviewViewModel(ListedItem item) : base(item) { }

		public async override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			var item = Item as ShortcutItem;
			var details = new List<FileProperty>
			{
				GetFileProperty("PropertyParsingPath", item.ItemPath),
				GetFileProperty("PropertyItemName", item.Name),
				GetFileProperty("PropertyItemTypeText", item.ItemType),
				GetFileProperty("PropertyItemTarget", item.TargetPath),
				GetFileProperty("Arguments", item.Arguments),
			};

			await LoadItemThumbnailAsync();

			return details;
		}

		public override async Task LoadAsync()
		{
			var details = await LoadPreviewAndDetailsAsync();

			Item.FileDetails?.Clear();
			Item.FileDetails = new(details.OfType<FileProperty>());
		}

		private async Task LoadItemThumbnailAsync()
		{
			var result = await FileThumbnailHelper.GetIconAsync(
				Item.ItemPath,
				Constants.ShellIconSizes.Jumbo,
				false,
				IconOptions.None);

			if (result is not null)
				FileImage = await result.ToBitmapAsync();
		}
	}
}
