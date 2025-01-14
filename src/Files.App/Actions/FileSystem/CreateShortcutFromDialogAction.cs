// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class CreateShortcutFromDialogAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Shortcut".GetLocalizedResource();

		public string Description
			=> "CreateShortcutFromDialogDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE71B");

		public override bool IsExecutable =>
			context.CanCreateItem &&
			UIHelpers.CanShowDialog;

		public CreateShortcutFromDialogAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return UIFilesystemHelpers.CreateShortcutFromDialogAsync(context.ShellPage!);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.CanCreateItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
