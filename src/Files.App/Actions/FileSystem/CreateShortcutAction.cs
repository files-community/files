// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class CreateShortcutAction : BaseUIAction, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "CreateShortcut".GetLocalizedResource();

		public string Description
			=> "CreateShortcutDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.URL");

		public override bool IsExecutable =>
			context.HasSelection &&
			context.CanCreateItem &&
			UIHelpers.CanShowDialog;

		public CreateShortcutAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return UIFilesystemHelpers.CreateShortcutAsync(context.ShellPage, context.SelectedItems);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.CanCreateItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
