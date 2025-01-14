// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Input;

namespace Files.App.ViewModels.Dialogs
{
	public sealed class ReorderSidebarItemsDialogViewModel : ObservableObject
	{
		private readonly IQuickAccessService quickAccessService = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public string HeaderText = "ReorderSidebarItemsDialogText".GetLocalizedResource();
		public ICommand PrimaryButtonCommand { get; private set; }

		public ObservableCollection<LocationItem> SidebarPinnedFolderItems = new(App.QuickAccessManager.Model._PinnedFolderItems
			.Where(x => x is LocationItem loc && loc.Section is SectionType.Pinned && !loc.IsHeader)
			.Cast<LocationItem>());

		public ReorderSidebarItemsDialogViewModel() 
		{
			//App.Logger.LogWarning(string.Join(", ", SidebarPinnedFolderItems.Select(x => x.Path)));
			PrimaryButtonCommand = new RelayCommand(SaveChanges);
		}

		public void SaveChanges()
		{
			quickAccessService.SaveAsync(SidebarPinnedFolderItems.Select(x => x.Path).ToArray());
		}
	}
}
