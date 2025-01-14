// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class CreateFolderWithSelectionAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "CreateFolderWithSelection".GetLocalizedResource();

		public string Description
			=> "CreateFolderWithSelectionDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.New.Folder");

		public bool IsExecutable =>
			context.ShellPage is not null &&
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.ZipFolder &&
			context.HasSelection;

		public CreateFolderWithSelectionAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return UIFilesystemHelpers.CreateFolderWithSelectionAsync(context.ShellPage!);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.HasSelection):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
