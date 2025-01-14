// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal sealed class RunAsAdminAction : BaseRunAsAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public override string Label
			=> "RunAsAdministrator".GetLocalizedResource();

		public override string Description
			=> "RunAsAdminDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE7EF");

		public override bool IsExecutable =>
			ContentPageContext.SelectedItem is not null &&
			ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
			ContentPageContext.PageType != ContentPageTypes.ZipFolder &&
			(FileExtensionHelpers.IsExecutableFile(ContentPageContext.SelectedItem.FileExtension) ||
			(ContentPageContext.SelectedItem is IShortcutItem shortcut &&
			shortcut.IsExecutable));

		public RunAsAdminAction() : base("runas")
		{
		}
	}
}
