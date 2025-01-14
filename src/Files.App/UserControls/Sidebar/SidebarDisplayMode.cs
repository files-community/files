// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.UserControls.Sidebar
{
	/// <summary>
	/// The display mode of the <see cref="SidebarView"/>
	/// </summary>
	public enum SidebarDisplayMode
	{
		/// <summary>
		/// The sidebar is hidden and moves in from the side when the <see cref="SidebarView.IsPaneOpen"/> is set to <code>true</code>.
		/// </summary>
		Minimal,
		/// <summary>
		/// Only the icons of the top most sections are visible.
		/// </summary>
		Compact,
		/// <summary>
		/// The sidebar is expanded and items can also be expanded.
		/// </summary>
		Expanded
	}
}
