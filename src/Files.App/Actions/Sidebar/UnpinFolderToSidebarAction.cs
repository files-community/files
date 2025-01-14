// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class UnpinFolderFromSidebarAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly IQuickAccessService service;

		public string Label
			=> "UnpinFolderFromSidebar".GetLocalizedResource();

		public string Description
			=> "UnpinFolderFromSidebarDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.FavoritePinRemove");

		public bool IsExecutable
			=> GetIsExecutable();

		public UnpinFolderFromSidebarAction()
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
				await service.UnpinFromSidebarAsync(items);
			}
			else if (context.Folder is not null)
			{
				await service.UnpinFromSidebarAsync(context.Folder.ItemPath);
			}
		}

		private bool GetIsExecutable()
		{
			string[] pinnedFolders = [.. App.QuickAccessManager.Model.PinnedFolders];

			return context.HasSelection
				? context.SelectedItems.All(IsPinned)
				: context.Folder is not null && IsPinned(context.Folder);

			bool IsPinned(ListedItem item)
			{
				return pinnedFolders.Contains(item.ItemPath);
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
