// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Files.App.Data.Items;
using Files.App.Extensions;
using Files.App.ViewModels.Dialogs;
using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Dialogs
{
	public sealed partial class ReorderSidebarItemsDialog : ContentDialog, IDialog<ReorderSidebarItemsDialogViewModel>
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public ReorderSidebarItemsDialogViewModel ViewModel
		{
			get => (ReorderSidebarItemsDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public ReorderSidebarItemsDialog()
		{
			InitializeComponent();
		}

		private async void MoveItemAsync(object sender, PointerRoutedEventArgs e)
		{
			var properties = e.GetCurrentPoint(null).Properties;
			if (!properties.IsLeftButtonPressed)
				return;

			var icon = sender as FontIcon;

			var navItem = icon?.FindAscendant<Grid>();
			if (navItem is not null)
				await navItem.StartDragAsync(e.GetCurrentPoint(navItem));
		}

		private void ListViewItem_DragStarting(object sender, DragStartingEventArgs e)
		{
			if (sender is not Grid nav || nav.DataContext is not LocationItem)
				return;

			// Adding the original Location item dragged to the DragEvents data view
			e.Data.Properties.Add("sourceLocationItem", nav);
			e.AllowedOperations = DataPackageOperation.Move;
		}

		private void ListViewItem_DragOver(object sender, DragEventArgs e)
		{
			if ((sender as Grid)?.DataContext is not LocationItem locationItem)
				return;
			var deferral = e.GetDeferral();
			
			if ((e.DataView.Properties["sourceLocationItem"] as Grid)?.DataContext is LocationItem sourceLocationItem)
			{
				DragOver_SetCaptions(sourceLocationItem, locationItem, e);
			}

			deferral.Complete();
		}

		private void DragOver_SetCaptions(LocationItem senderLocationItem, LocationItem sourceLocationItem, DragEventArgs e)
		{
			// If the location item is the same as the original dragged item
			if (sourceLocationItem.CompareTo(senderLocationItem) == 0)
			{
				e.AcceptedOperation = DataPackageOperation.None;
				e.DragUIOverride.IsCaptionVisible = false;
			}
			else
			{
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.Caption = "MoveItemsDialogPrimaryButtonText".GetLocalizedResource();
				e.AcceptedOperation = DataPackageOperation.Move;
			}
		}

		private void ListViewItem_Drop(object sender, DragEventArgs e)
		{
			if (sender is not Grid navView || navView.DataContext is not LocationItem locationItem)
				return;

			if ((e.DataView.Properties["sourceLocationItem"] as Grid)?.DataContext is LocationItem sourceLocationItem)
				ViewModel.SidebarPinnedFolderItems.Move(ViewModel.SidebarPinnedFolderItems.IndexOf(sourceLocationItem), ViewModel.SidebarPinnedFolderItems.IndexOf(locationItem));
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}
	}
}
