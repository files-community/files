// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Input;
using Vanara.PInvoke;
using Windows.ApplicationModel.DataTransfer;
using WinRT;

namespace Files.App.UserControls
{
	public sealed partial class ShelfPane : UserControl
	{
		public ShelfPane()
		{
			// TODO: [Shelf] Remove once view model is connected
			ItemsSource = new ObservableCollection<ShelfItem>();

			InitializeComponent();
		}

		private void Shelf_DragOver(object sender, DragEventArgs e)
		{
			if (!FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				return;

			e.Handled = true;
			e.DragUIOverride.Caption = Strings.AddToShelf.GetLocalizedResource();
			e.AcceptedOperation = DataPackageOperation.Link;
		}

		private async void Shelf_Drop(object sender, DragEventArgs e)
		{
			if (ItemsSource is null)
				return;

			// Get items
			var storageService = Ioc.Default.GetRequiredService<IStorageService>();
			var storageItems = (await FilesystemHelpers.GetDraggedStorageItems(e.DataView)).ToArray();

			// Add to list
			foreach (var item in storageItems)
			{
				var storable = item switch
				{
					StorageFileWithPath => (IStorable?)await storageService.TryGetFileAsync(item.Path),
					StorageFolderWithPath => (IStorable?)await storageService.TryGetFolderAsync(item.Path),
					_ => null
				};

				if (storable is null)
					continue;

				var shelfItem = new ShelfItem(storable, ItemsSource);
				_ = shelfItem.InitAsync();

				ItemsSource.Add(shelfItem);
			}
		}

		private void ListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			if (ItemsSource is null)
				return;

			var shellItemList = SafetyExtensions.IgnoreExceptions(() => ItemsSource.Select(x => new Vanara.Windows.Shell.ShellItem(x.Inner.Id)).ToArray());
			if (shellItemList?[0].FileSystemPath is not null)
			{
				var iddo = shellItemList[0].Parent?.GetChildrenUIObjects<IDataObject>(HWND.NULL, shellItemList);
				if (iddo is null)
					return;

				shellItemList.ForEach(x => x.Dispose());
				var dataObjectProvider = e.Data.As<Shell32.IDataObjectProvider>();
				dataObjectProvider.SetDataObject(iddo);
			}
			else
			{
				// Only support IStorageItem capable paths
				var storageItems = ItemsSource.Select(x => VirtualStorageItem.FromPath(x.Inner.Id));
				e.Data.SetStorageItems(storageItems, false);
			}
		}

		public IList<ShelfItem>? ItemsSource
		{
			get => (IList<ShelfItem>?)GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}
		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register(nameof(ItemsSource), typeof(IList<ShelfItem>), typeof(ShelfPane), new PropertyMetadata(null));

		public ICommand? ClearCommand
		{
			get => (ICommand?)GetValue(ClearCommandProperty);
			set => SetValue(ClearCommandProperty, value);
		}
		public static readonly DependencyProperty ClearCommandProperty =
			DependencyProperty.Register(nameof(ClearCommand), typeof(ICommand), typeof(ShelfPane), new PropertyMetadata(null));
	}
}
