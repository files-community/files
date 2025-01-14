// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal sealed class CutItemAction : BaseTransferItemAction, IAction
	{
		public string Label
			=> "Cut".GetLocalizedResource();

		public string Description
			=> "CutItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Cut");

		public HotKey HotKey
			=> new(Keys.X, KeyModifiers.Ctrl);

		public CutItemAction() : base()
		{
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return ExecuteTransferAsync(DataPackageOperation.Move);
		}
	}
}
