// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;

namespace Files.App.Actions
{
	internal sealed class PinFolderToSidebarAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly IQuickAccessService service;

		public string Label
			=> "PinFolderToSidebar".GetLocalizedResource();

		public string Description
			=> "PinFolderToSidebarDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.FavoritePin");

		public bool IsExecutable
			=> GetIsExecutable();

		public PinFolderToSidebarAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			service = Ioc.Default.GetRequiredService<IQuickAccessService>();

			context.PropertyChanged += Context_PropertyChanged;
			App.QuickAccessManager.UpdateQuickAccessWidget += QuickAccessManager_DataChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.HasSelection)
			{
				var items = context.SelectedItems.Select(x => x.ItemPath).ToArray();

				await service.PinToSidebarAsync(items);
			}
			else if (context.Folder is not null)
			{
				await service.PinToSidebarAsync(context.Folder.ItemPath);
			}
		}

		private bool GetIsExecutable()
		{
			string[] pinnedFolders = [.. App.QuickAccessManager.Model.PinnedFolders];

			return context.HasSelection
				? context.SelectedItems.All(IsPinnable)
				: context.Folder is not null && IsPinnable(context.Folder);

			bool IsPinnable(ListedItem item)
			{
				return
					item.PrimaryItemAttribute is StorageItemTypes.Folder &&
					!pinnedFolders.Contains(item.ItemPath);
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
