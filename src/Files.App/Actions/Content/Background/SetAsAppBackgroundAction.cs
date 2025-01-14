// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class SetAsAppBackgroundAction : BaseSetAsAction
	{
		private readonly IAppearanceSettingsService AppearanceSettingsService = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();

		public override string Label
			=> "SetAsAppBackground".GetLocalizedResource();

		public override string Description
			=> "SetAsAppBackgroundDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE91B");

		public override bool IsExecutable =>
			base.IsExecutable &&
			ContentPageContext.SelectedItem is not null;

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (IsExecutable && ContentPageContext.SelectedItem is ListedItem selectedItem)
				AppearanceSettingsService.AppThemeBackgroundImageSource = selectedItem.ItemPath;

			return Task.CompletedTask;
		}
	}
}
