// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class AddItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		private readonly IDialogService dialogService;

		private readonly AddItemDialogViewModel viewModel = new();

		public string Label
			=> "BaseLayoutContextFlyoutNew/Label".GetLocalizedResource();

		public string Description
			=> "AddItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.New.Item");

		public HotKey HotKey
			=> new(Keys.I, KeyModifiers.CtrlShift);

		public bool IsExecutable
			=> context.CanCreateItem;

		public AddItemAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			dialogService = Ioc.Default.GetRequiredService<IDialogService>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			await dialogService.ShowDialogAsync(viewModel);

			if (viewModel.ResultType.ItemType == AddItemDialogItemType.Shortcut)
			{
				await Ioc.Default.GetRequiredService<ICommandManager>().CreateShortcutFromDialog.ExecuteAsync();
			}
			else if (viewModel.ResultType.ItemType != AddItemDialogItemType.Cancel)
			{
				await UIFilesystemHelpers.CreateFileFromDialogResultTypeAsync(
					viewModel.ResultType.ItemType,
					viewModel.ResultType.ItemInfo,
					context.ShellPage!);
			}

			viewModel.ResultType.ItemType = AddItemDialogItemType.Cancel;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanCreateItem):
				case nameof(IContentPageContext.HasSelection):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
