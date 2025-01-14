// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models
{
	public sealed class ItemManipulationModel
	{
		public event EventHandler FocusFileListInvoked;

		public event EventHandler SelectAllItemsInvoked;

		public event EventHandler ClearSelectionInvoked;

		public event EventHandler InvertSelectionInvoked;

		public event EventHandler<ListedItem> AddSelectedItemInvoked;

		public event EventHandler<ListedItem> RemoveSelectedItemInvoked;

		public event EventHandler FocusSelectedItemsInvoked;

		public event EventHandler StartRenameItemInvoked;

		public event EventHandler<ListedItem> ScrollIntoViewInvoked;

		public event EventHandler ScrollToTopInvoked;

		public event EventHandler SetDragModeForItemsInvoked;

		public event EventHandler RefreshItemsOpacityInvoked;

		public event EventHandler RefreshItemThumbnailInvoked;

		public event EventHandler RefreshItemsThumbnailInvoked;

		public void FocusFileList()
		{
			FocusFileListInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void SelectAllItems()
		{
			SelectAllItemsInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void ClearSelection()
		{
			ClearSelectionInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void InvertSelection()
		{
			InvertSelectionInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void AddSelectedItem(ListedItem item)
		{
			AddSelectedItemInvoked?.Invoke(this, item);
		}

		public void AddSelectedItems(List<ListedItem> items)
		{
			foreach (ListedItem item in items)
			{
				AddSelectedItem(item);
			}
		}

		public void RemoveSelectedItem(ListedItem item)
		{
			RemoveSelectedItemInvoked?.Invoke(this, item);
		}

		public void RemoveSelectedItems(List<ListedItem> items)
		{
			foreach (ListedItem item in items)
			{
				RemoveSelectedItem(item);
			}
		}

		public void SetSelectedItem(ListedItem item)
		{
			ClearSelection();
			AddSelectedItem(item);
		}

		public void SetSelectedItems(List<ListedItem> items)
		{
			ClearSelection();
			AddSelectedItems(items);
		}

		public void FocusSelectedItems()
		{
			FocusSelectedItemsInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void StartRenameItem()
		{
			StartRenameItemInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void ScrollIntoView(ListedItem item)
		{
			ScrollIntoViewInvoked?.Invoke(this, item);
		}

		public void ScrollToTop()
		{
			ScrollToTopInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void SetDragModeForItems()
		{
			SetDragModeForItemsInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void RefreshItemsOpacity()
		{
			RefreshItemsOpacityInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void RefreshItemThumbnail()
		{
			RefreshItemThumbnailInvoked?.Invoke(this, EventArgs.Empty);
		}

		public void RefreshItemsThumbnail()
		{
			RefreshItemsThumbnailInvoked?.Invoke(this, EventArgs.Empty);
		}
	}
}
