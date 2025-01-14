// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared;
using Files.Shared.Utils;

namespace Files.App.ViewModels.Dialogs.AddItemDialog
{
	public sealed class AddItemDialogViewModel : ObservableObject
	{
		private readonly IImageService _imagingService;

		public ObservableCollection<AddItemDialogListItemViewModel> AddItemsList { get; }

		public AddItemDialogResultModel ResultType { get; set; }

		public AddItemDialogViewModel()
		{
			// Dependency injection
			_imagingService = Ioc.Default.GetRequiredService<IImageService>();

			// Initialize
			AddItemsList = [];
			ResultType = new()
			{
				ItemType = AddItemDialogItemType.Cancel
			};
		}

		public async Task AddItemsToListAsync(IEnumerable<ShellNewEntry> itemTypes)
		{
			AddItemsList.Clear();

			AddItemsList.Add(new()
			{
				Header = "Folder".ToLocalized(),
				SubHeader = "AddDialogListFolderSubHeader".ToLocalized(),
				Glyph = "\xE838",
				IsItemEnabled = true,
				ItemResult = new()
				{
					ItemType = AddItemDialogItemType.Folder
				}
			});

			AddItemsList.Add(new()
			{
				Header = "File".ToLocalized(),
				SubHeader = "AddDialogListFileSubHeader".ToLocalized(),
				Glyph = "\xE8A5",
				IsItemEnabled = true,
				ItemResult = new()
				{
					ItemType = AddItemDialogItemType.File,
					ItemInfo = null
				}
			});

			AddItemsList.Add(new()
			{
				Header = "Shortcut".ToLocalized(),
				SubHeader = "AddDialogListShortcutSubHeader".ToLocalized(),
				Glyph = "\uE71B",
				IsItemEnabled = true,
				ItemResult = new()
				{
					ItemType = AddItemDialogItemType.Shortcut,
					ItemInfo = null
				}
			});

			if (itemTypes is null)
				return;

			foreach (var itemType in itemTypes)
			{
				IImage? imageModel = null;

				if (!string.IsNullOrEmpty(itemType.IconBase64))
				{
					byte[] bitmapData = Convert.FromBase64String(itemType.IconBase64);
					imageModel = await _imagingService.GetImageModelFromDataAsync(bitmapData);
				}

				AddItemsList.Add(new()
				{
					Header = itemType.Name,
					SubHeader = itemType.Extension,
					Glyph = imageModel is not null ? null : "\xE8A5",
					Icon = imageModel,
					IsItemEnabled = true,
					ItemResult = new()
					{
						ItemType = AddItemDialogItemType.File,
						ItemInfo = itemType
					}
				});
			}
		}
	}
}
