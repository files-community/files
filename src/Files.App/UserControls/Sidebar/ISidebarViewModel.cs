// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Files.App.UserControls.Sidebar
{
	public record ItemDroppedEventArgs(object DropTarget, DataPackageView DroppedItem, SidebarItemDropPosition dropPosition, DragEventArgs RawEvent) { }
	public record ItemDragOverEventArgs(object DropTarget, DataPackageView DroppedItem, SidebarItemDropPosition dropPosition, DragEventArgs RawEvent) { }
	public record ItemContextInvokedArgs(object? Item, Point Position) { }

	public interface ISidebarViewModel
	{
		/// <summary>
		/// The source/list of items that will be rendered in the sidebar
		/// </summary>
		object SidebarItems { get; }

		/// <summary>
		/// Gets invoked when the context was requested for an item in the sidebar.
		/// Also applies when context was requested for the pane itsself.
		/// </summary>
		/// <param name="sender">The sender of this event</param>
		/// <param name="args">The <see cref="ItemContextInvokedArgs"/> for this event.</param>
		void HandleItemContextInvokedAsync(object sender, ItemContextInvokedArgs args);

		/// <summary>
		/// Gets invoked when an item drags over any item of the sidebar.
		/// </summary>
		/// <param name="args">The <see cref="ItemDragOverEventArgs"/> for this event.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task HandleItemDragOverAsync(ItemDragOverEventArgs args);

		/// <summary>
		/// Gets invoked when an item is dropped on any item of the sidebar.
		/// </summary>
		/// <param name="args">The <see cref="ItemDroppedEventArgs"/> for this event.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task HandleItemDroppedAsync(ItemDroppedEventArgs args);

		/// <summary>
		/// Gets invoked when an item is invoked (double clicked) on any item of the sidebar.
		/// </summary>
		/// <param name="item">The item that was invoked.</param>
		void HandleItemInvokedAsync(object item, PointerUpdateKind pointerUpdateKind);
	}
}
