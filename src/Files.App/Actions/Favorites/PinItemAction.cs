// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class PinItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		private ActionExecutableType ExecutableType { get; set; }

		public string Label
			=> "PinToFavorites".GetLocalizedResource();

		public string Description
			=> "PinItemToFavoritesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconPinToFavorites");

		public bool IsExecutable
			=> GetIsExecutable();

		public PinItemAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
			App.QuickAccessManager.UpdateQuickAccessWidget += QuickAccessManager_DataChanged;
		}

		public async Task ExecuteAsync()
		{
			switch (ExecutableType)
			{
				case ActionExecutableType.DisplayPageContext:
					{
						if (ContentPageContext.HasSelection)
						{
							var items = ContentPageContext.SelectedItems.Select(x => x.ItemPath).ToArray();
							await QuickAccessService.PinToSidebarAsync(items);
						}
						else if (ContentPageContext.Folder is not null)
						{
							await QuickAccessService.PinToSidebarAsync(ContentPageContext.Folder.ItemPath);
						}
						break;
					}
				case ActionExecutableType.HomePageContext:
					{
						await QuickAccessService.PinToSidebarAsync(HomePageContext.RightClickedItem!.Path ?? string.Empty);
						break;
					}
			}
		}

		private bool GetIsExecutable()
		{
			string[] favorites = App.QuickAccessManager.Model.FavoriteItems.ToArray();

			var executableInDisplayPage =
				ContentPageContext.HasSelection
					? ContentPageContext.SelectedItems.All(x => !favorites.Contains(x.ItemPath)) && ContentPageContext.SelectedItems.All(x => x.IsFolder)
					: ContentPageContext.Folder is not null && !favorites.Contains(ContentPageContext.Folder.ItemPath);

			if (executableInDisplayPage)
				ExecutableType = ActionExecutableType.DisplayPageContext;

			// TODO: Check if the item is folder
			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked &&
				!favorites.Contains(HomePageContext.RightClickedItem!.Path ?? string.Empty);

			if (executableInHomePage)
				ExecutableType = ActionExecutableType.HomePageContext;

			return executableInDisplayPage || executableInHomePage;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.Folder):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private void QuickAccessManager_DataChanged(object? sender, ModifyQuickAccessEventArgs e)
		{
			OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
