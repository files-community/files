// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Models
{
	public sealed class SuggestionModel : ObservableObject
	{
		public bool IsRecentSearch { get; set; } = false;

		public bool LoadFileIcon { get; set; } = false;

		public bool NeedsPlaceholderGlyph { get; set; } = true;

		public string? ItemPath { get; set; }

		public string Name { get; set; }

		private BitmapImage? fileImage;
		public BitmapImage? FileImage
		{
			get => fileImage;
			set
			{
				if (fileImage is BitmapImage imgOld)
				{
					imgOld.ImageOpened -= Img_ImageOpened;
				}

				if (SetProperty(ref fileImage, value))
				{
					if (value is BitmapImage img)
					{
						if (img.PixelWidth > 0)
						{
							Img_ImageOpened(img, null);
						}
						else
						{
							img.ImageOpened += Img_ImageOpened;
						}
					}
				}
			}
		}

		private void Img_ImageOpened(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			if (sender is BitmapImage image)
			{
				image.ImageOpened -= Img_ImageOpened;

				if (image.PixelWidth > 0)
				{
					SafetyExtensions.IgnoreExceptions(() =>
					{
						LoadFileIcon = true;
						NeedsPlaceholderGlyph = false;
					}, App.Logger); // 2009482836u
				}
			}
		}

		public SuggestionModel(ListedItem item)
		{
			LoadFileIcon = item.LoadFileIcon;
			NeedsPlaceholderGlyph = item.NeedsPlaceholderGlyph;
			ItemPath = item.ItemPath;
			Name = item.Name;
			FileImage = item.FileImage;
		}

		public SuggestionModel(string itemName, bool isRecentSearch)
		{
			IsRecentSearch = isRecentSearch;
			Name = itemName;
		}
	}
}
