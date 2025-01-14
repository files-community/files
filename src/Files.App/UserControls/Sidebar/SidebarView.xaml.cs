// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.UserControls.Sidebar
{
	[ContentProperty(Name = "InnerContent")]
	public sealed partial class SidebarView : UserControl, INotifyPropertyChanged
	{
		private const double COMPACT_MAX_WIDTH = 200;

		public event EventHandler<object>? ItemInvoked;
		public event EventHandler<ItemContextInvokedArgs>? ItemContextInvoked;
		public event PropertyChangedEventHandler? PropertyChanged;

		internal SidebarItem? SelectedItemContainer = null;

		private bool draggingSidebarResizer;
		private double preManipulationSidebarWidth = 0;

		public SidebarView()
		{
			InitializeComponent();
		}

		internal void UpdateSelectedItemContainer(SidebarItem container)
		{
			SelectedItemContainer = container;
		}

		internal void RaiseItemInvoked(SidebarItem item, PointerUpdateKind pointerUpdateKind)
		{
			// Only leaves can be selected
			if (item.Item is null || item.IsGroupHeader) return;

			SelectedItem = item.Item;
			ItemInvoked?.Invoke(item, item.Item);
			ViewModel.HandleItemInvokedAsync(item.Item, pointerUpdateKind);
		}

		internal void RaiseContextRequested(SidebarItem item, Point e)
		{
			ItemContextInvoked?.Invoke(item, new ItemContextInvokedArgs(item.Item, e));
			ViewModel.HandleItemContextInvokedAsync(item, new ItemContextInvokedArgs(item.Item, e));
		}

		internal async Task RaiseItemDropped(SidebarItem sideBarItem, SidebarItemDropPosition dropPosition, DragEventArgs rawEvent)
		{
			if (sideBarItem.Item is null) return;
			await ViewModel.HandleItemDroppedAsync(new ItemDroppedEventArgs(sideBarItem.Item, rawEvent.DataView, dropPosition, rawEvent));
		}

		internal async Task RaiseItemDragOver(SidebarItem sideBarItem, SidebarItemDropPosition dropPosition, DragEventArgs rawEvent)
		{
			if (sideBarItem.Item is null) return;
			await ViewModel.HandleItemDragOverAsync(new ItemDragOverEventArgs(sideBarItem.Item, rawEvent.DataView, dropPosition, rawEvent));
		}

		private void UpdateMinimalMode()
		{
			if (DisplayMode != SidebarDisplayMode.Minimal) return;

			if (IsPaneOpen)
			{
				VisualStateManager.GoToState(this, "MinimalExpanded", true);
			}
			else
			{
				VisualStateManager.GoToState(this, "MinimalCollapsed", true);
			}
		}

		private void UpdateDisplayMode()
		{
			switch (DisplayMode)
			{
				case SidebarDisplayMode.Compact:
					VisualStateManager.GoToState(this, "Compact", true);
					return;
				case SidebarDisplayMode.Expanded:
					VisualStateManager.GoToState(this, "Expanded", true);
					return;
				case SidebarDisplayMode.Minimal:
					IsPaneOpen = false;
					UpdateMinimalMode();
					return;
			}
		}

		private void UpdateDisplayModeForPaneWidth(double newPaneWidth)
		{
			if (newPaneWidth < COMPACT_MAX_WIDTH)
			{
				DisplayMode = SidebarDisplayMode.Compact;
			}
			else if (newPaneWidth > COMPACT_MAX_WIDTH)
			{
				DisplayMode = SidebarDisplayMode.Expanded;
				OpenPaneLength = newPaneWidth;
			}
		}

		private void UpdateOpenPaneLengthColumn()
		{
			PaneColumnDefinition.Width = new GridLength(OpenPaneLength);
		}

		private void SidebarView_Loaded(object sender, RoutedEventArgs e)
		{
			UpdateDisplayMode();
			UpdateOpenPaneLengthColumn();
			PaneColumnGrid.Translation = new System.Numerics.Vector3(0, 0, 32);
		}

		private void SidebarResizer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			draggingSidebarResizer = true;
			preManipulationSidebarWidth = PaneColumnGrid.ActualWidth;
			VisualStateManager.GoToState(this, "ResizerPressed", true);
			e.Handled = true;
		}

		private void SidebarResizer_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
		{
			var newWidth = preManipulationSidebarWidth + e.Cumulative.Translation.X;
			UpdateDisplayModeForPaneWidth(newWidth);
			e.Handled = true;
		}

		private void SidebarResizerControl_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if
			(
				e.Key != VirtualKey.Space &&
				e.Key != VirtualKey.Enter &&
				e.Key != VirtualKey.Left &&
				e.Key != VirtualKey.Right &&
				e.Key != VirtualKey.Control
			)
				return;

			var primaryInvocation = e.Key == VirtualKey.Space || e.Key == VirtualKey.Enter;
			if (DisplayMode == SidebarDisplayMode.Expanded)
			{
				if (primaryInvocation)
				{
					DisplayMode = SidebarDisplayMode.Compact;
					return;
				}

				var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
				var increment = ctrl.HasFlag(CoreVirtualKeyStates.Down) ? 5 : 1;

				// Left makes the pane smaller so we invert the increment
				if (e.Key == VirtualKey.Left)
					increment = -increment;

				var newWidth = OpenPaneLength + increment;
				UpdateDisplayModeForPaneWidth(newWidth);
				e.Handled = true;
				return;
			}
			else if (DisplayMode == SidebarDisplayMode.Compact)
			{
				if (primaryInvocation || e.Key == VirtualKey.Right)
				{
					DisplayMode = SidebarDisplayMode.Expanded;
					e.Handled = true;
				}
			}
		}

		private void PaneLightDismissLayer_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			IsPaneOpen = false;
			e.Handled = true;
		}

		private void PaneLightDismissLayer_Tapped(object sender, TappedRoutedEventArgs e)
		{
			IsPaneOpen = false;
			e.Handled = true;
		}

		private void SidebarResizer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (DisplayMode == SidebarDisplayMode.Expanded)
			{
				DisplayMode = SidebarDisplayMode.Compact;
				e.Handled = true;
			}
			else
			{
				DisplayMode = SidebarDisplayMode.Expanded;
				e.Handled = true;
			}
		}

		private void SidebarResizer_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			var sidebarResizer = (FrameworkElement)sender;
			sidebarResizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
			VisualStateManager.GoToState(this, "ResizerPointerOver", true);
			e.Handled = true;
		}

		private void SidebarResizer_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (draggingSidebarResizer)
				return;

			var sidebarResizer = (FrameworkElement)sender;
			sidebarResizer.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
			VisualStateManager.GoToState(this, "ResizerNormal", true);
			e.Handled = true;
		}

		private void SidebarResizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			draggingSidebarResizer = false;
			VisualStateManager.GoToState(this, "ResizerNormal", true);
			e.Handled = true;
		}

		private void PaneColumnGrid_ContextRequested(UIElement sender, ContextRequestedEventArgs e)
		{
			var newArgs = new ItemContextInvokedArgs(null, e.TryGetPosition(this, out var point) ? point : default);
			ViewModel.HandleItemContextInvokedAsync(this, newArgs);
			e.Handled = true;
		}

		private void MenuItemsHost_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
		{
			if (args.Element is SidebarItem sidebarItem)
			{
				sidebarItem.HandleItemChange();
			}
		}
	}
}
