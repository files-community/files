// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Layouts;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views.Layouts
{
	public interface IBaseLayoutPage : IDisposable
	{
		bool IsRenamingItem { get; }

		bool IsItemSelected { get; }

		bool IsMiddleClickToScrollEnabled { get; set; }

		/// <summary>
		/// If true, the preview pane is not updated when the selected item is changed.
		/// </summary>
		bool LockPreviewPaneContent { get; set; }

		List<ListedItem>? SelectedItems { get; }

		ListedItem? SelectedItem { get; }

		ItemManipulationModel ItemManipulationModel { get; }

		InfoPaneViewModel InfoPaneViewModel { get; }

		SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }

		StatusBarViewModel StatusBarViewModel { get; }

		BaseLayoutViewModel? CommandsViewModel { get; }

		CommandBarFlyout ItemContextMenuFlyout { get; set; }

		CommandBarFlyout BaseContextMenuFlyout { get; set; }
	}
}
