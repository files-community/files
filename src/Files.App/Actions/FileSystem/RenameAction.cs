// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class RenameAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Rename".GetLocalizedResource();

		public string Description
			=> "RenameDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.F2);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Rename");

		public bool IsExecutable =>
			context.ShellPage is not null &&
			IsPageTypeValid() &&
			context.ShellPage.SlimContentPage is not null &&
			context.HasSelection;

		public RenameAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItems.Count > 1)
			{
				var viewModel = new BulkRenameDialogViewModel();
				var dialogService = Ioc.Default.GetRequiredService<IDialogService>();
				var result = await dialogService.ShowDialogAsync(viewModel);
			}
			else
			{
				context.ShellPage?.SlimContentPage?.ItemManipulationModel.StartRenameItem();
			}
		}

		private bool IsPageTypeValid()
		{
			return
				context.PageType != ContentPageTypes.None &&
				context.PageType != ContentPageTypes.Home &&
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.ZipFolder;
		}

		private void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
