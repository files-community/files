// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.Sidebar
{
	public interface ISidebarItemModel : INotifyPropertyChanged
	{
		/// <summary>
		/// The children of this item that will be rendered as child elements of the SidebarItem
		/// </summary>
		object? Children { get; }

		/// <summary>
		/// The icon source used to generate the icon for the SidebarItem
		/// </summary>
		IconSource? IconSource { get; }

		/// <summary>
		/// Item decorator for the given item.
		/// </summary>
		FrameworkElement? ItemDecorator { get => null; }

		/// <summary>
		/// Determines whether the SidebarItem is expanded and the children are visible 
		/// or if it is collapsed and children are not visible.
		/// </summary>
		bool IsExpanded { get; set; }

		/// <summary>
		/// The text of this item that will be rendered as the label of the SidebarItem
		/// </summary>
		string Text { get; }

		/// <summary>
		/// The tooltip used when hovering over this item in the sidebar
		/// </summary>
		object ToolTip { get; }
	}
}
