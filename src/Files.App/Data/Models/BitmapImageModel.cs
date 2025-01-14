// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Utils;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Files.App.Data.Models
{
	/// <inheritdoc cref="IImage"/>
	internal sealed class BitmapImageModel : IImage
	{
		public BitmapImage Image { get; }

		public BitmapImageModel(BitmapImage image)
		{
			Image = image;
		}
	}
}
