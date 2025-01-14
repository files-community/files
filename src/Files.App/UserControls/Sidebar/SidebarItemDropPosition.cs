// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.UserControls.Sidebar
{
	/// <summary>
	/// The position of the item that was dropped on the sidebar item.
	/// </summary>
	public enum SidebarItemDropPosition
	{
		/// <summary>
		/// The item was dropped on the top of the sidebar item indicating it should be moved/inserted above this item.
		/// </summary>
		Top,
		/// <summary>
		/// The item was dropped on the bottom of the sidebar item indicating it should be moved/inserted below this item.
		/// </summary>
		Bottom,
		/// <summary>
		/// The item was dropped on the center of the sidebar item indicating it should be moved/inserted as a child of this item.
		/// </summary>
		Center
	}
}
