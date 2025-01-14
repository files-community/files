// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Graphics.Imaging;

namespace Files.App.Actions
{
	internal sealed class RotateRightAction : BaseRotateAction
	{
		public override string Label
			=> "RotateRight".GetLocalizedResource();

		public override string Description
			=> "RotateRightDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.ImageRotate.CW");

		protected override BitmapRotation Rotation
			=> BitmapRotation.Clockwise90Degrees;
	}
}
