// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class DeleteItemAction : BaseDeleteAction, IAction
	{
		public string Label
			=> "Delete".GetLocalizedResource();

		public string Description
			=> "DeleteItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new RichGlyph(themedIconStyle: "App.ThemedIcons.Delete");

		public HotKey HotKey
			=> new(Keys.Delete);

		public HotKey SecondHotKey
			=> new(Keys.D, KeyModifiers.Ctrl);

		public Task ExecuteAsync(object? parameter = null)
		{
			return DeleteItemsAsync(false);
		}
	}
}
