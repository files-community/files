// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents a control that displays a list of recent files with <see cref="WidgetFolderCardItem"/>.
	/// </summary>
	public sealed partial class RecentFilesWidget : UserControl
	{
		public RecentFilesWidgetViewModel ViewModel { get; set; } = Ioc.Default.GetRequiredService<RecentFilesWidgetViewModel>();

		public RecentFilesWidget()
		{
			InitializeComponent();
		}

		private void RecentFilesListView_ItemClick(object sender, ItemClickEventArgs e)
		{
			if (e.ClickedItem is not RecentItem item)
				return;

			ViewModel.NavigateToPath(item.Path);
		}

		private void RecentFilesListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.BuildItemContextMenu(e.OriginalSource, e);
		}

		private async void RecentFilesListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			var items = e.Items.OfType<RecentItem>().ToList();
			var storageItems = new List<IStorageItem>();

			foreach (var item in items)
			{
				try
				{
					var file = await StorageFile.GetFileFromPathAsync(item.Path);
					if (file != null)
						storageItems.Add(file);
				}
				catch
				{
					e.Cancel = true;
				}
			}

			if (storageItems.Count > 0)
			{
				// Create a new data package and set the storage items
				DataPackage dataPackage = new DataPackage();
				dataPackage.SetStorageItems(storageItems);
				e.Data.SetDataProvider(StandardDataFormats.StorageItems, request => request.SetData(storageItems));

				e.Data.RequestedOperation = DataPackageOperation.Move | DataPackageOperation.Copy | DataPackageOperation.Link;
			}
		}
	}
}
