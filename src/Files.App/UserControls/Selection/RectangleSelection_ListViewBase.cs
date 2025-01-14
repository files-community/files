// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.UserControls.Selection
{
	public sealed class RectangleSelection_ListViewBase : RectangleSelection
	{
		private ListViewBase uiElement;
		private ScrollViewer scrollViewer;
		private SelectionChangedEventHandler selectionChanged;
		private DispatcherQueueTimer timer;
		private Point originDragPoint;
		private Dictionary<object, System.Drawing.Rectangle> itemsPosition;
		private List<object> prevSelectedItems;
		private List<object> prevSelectedItemsDrag;
		private ItemSelectionStrategy selectionStrategy;

		public RectangleSelection_ListViewBase(ListViewBase uiElement, Rectangle selectionRectangle, SelectionChangedEventHandler selectionChanged = null)
		{
			this.uiElement = uiElement;
			this.selectionRectangle = selectionRectangle;
			this.selectionChanged = selectionChanged;
			itemsPosition = [];
			timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
			InitEvents(null, null);
		}

		private void RectangleSelection_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (scrollViewer is null)
			{
				return;
			}

			var currentPoint = e.GetCurrentPoint(uiElement);
			var verticalOffset = scrollViewer.VerticalOffset;
			if (selectionState == SelectionState.Starting)
			{
				if (!HasMovedMinimalDelta(originDragPoint.X, originDragPoint.Y - verticalOffset, currentPoint.Position.X, currentPoint.Position.Y))
				{
					return;
				}

				// Clear selected items once if the pointer is pressed and moved
				selectionStrategy.StartSelection();
				OnSelectionStarted();
				selectionState = SelectionState.Active;
			}
			if (currentPoint.Properties.IsLeftButtonPressed)
			{
				var originDragPointShifted = new Point(originDragPoint.X, originDragPoint.Y - verticalOffset); // Initial drag point relative to the topleft corner
				base.DrawRectangle(currentPoint, originDragPointShifted, uiElement);
				// Selected area considering scrolled offset
				var rect = new System.Drawing.Rectangle((int)Canvas.GetLeft(selectionRectangle), (int)Math.Min(originDragPoint.Y, currentPoint.Position.Y + verticalOffset), (int)selectionRectangle.Width, (int)Math.Abs(originDragPoint.Y - (currentPoint.Position.Y + verticalOffset)));

				var selectedItemsBeforeChange = uiElement.SelectedItems.ToArray();

				foreach (var item in itemsPosition.ToList())
				{
					try
					{
						if (rect.IntersectsWith(item.Value))
						{
							selectionStrategy.HandleIntersectionWithItem(item.Key);
						}
						else
						{
							selectionStrategy.HandleNoIntersectionWithItem(item.Key);
						}
					}
					catch (ArgumentException)
					{
						// Item is not present in the ItemsSource
						itemsPosition.Remove(item);
					}
				}
				if (currentPoint.Position.Y > uiElement.ActualHeight - 20)
				{
					// Scroll down the list if pointer is at the bottom
					var scrollIncrement = Math.Min(currentPoint.Position.Y - (uiElement.ActualHeight - 20), 40);
					scrollViewer.ChangeView(null, verticalOffset + scrollIncrement, null, false);
				}
				else if (currentPoint.Position.Y < 20)
				{
					// Scroll up the list if pointer is at the top
					var scrollIncrement = Math.Min(20 - currentPoint.Position.Y, 40);
					scrollViewer.ChangeView(null, verticalOffset - scrollIncrement, null, false);
				}

				if (selectionChanged is not null)
				{
					var currentSelectedItemsDrag = uiElement.SelectedItems.Cast<object>().ToList();
					if (prevSelectedItemsDrag is null || !prevSelectedItemsDrag.SequenceEqual(currentSelectedItemsDrag))
					{
						// Trigger SelectionChanged event if the selection has changed
						var removedItems = selectedItemsBeforeChange.Except(currentSelectedItemsDrag).ToList();
						selectionChanged(sender, new SelectionChangedEventArgs(removedItems, currentSelectedItemsDrag));
						prevSelectedItemsDrag = currentSelectedItemsDrag;
					}
				}
			}
		}

		private void RectangleSelection_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (scrollViewer is null)
			{
				return;
			}

			itemsPosition.Clear();

			scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
			scrollViewer.ViewChanged += ScrollViewer_ViewChanged;

			originDragPoint = new Point(e.GetCurrentPoint(uiElement).Position.X, e.GetCurrentPoint(uiElement).Position.Y); // Initial drag point relative to the topleft corner
			prevSelectedItems = uiElement.SelectedItems.Cast<object>().ToList(); // Save current selected items

			var verticalOffset = scrollViewer.VerticalOffset;
			originDragPoint.Y += verticalOffset; // Initial drag point relative to the top of the list (considering scrolled offset)
			if (!e.GetCurrentPoint(uiElement).Properties.IsLeftButtonPressed || e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
			{
				// Trigger only on left click, do not trigger with touch
				return;
			}

			FetchItemsPosition();

			selectionStrategy = e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control) ?
					new InvertPreviousItemSelectionStrategy(uiElement.SelectedItems, prevSelectedItems) :
					e.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift) ?
						new ExtendPreviousItemSelectionStrategy(uiElement.SelectedItems, prevSelectedItems) :
						new IgnorePreviousItemSelectionStrategy(uiElement.SelectedItems);

			selectionStrategy.HandleNoItemSelected();

			uiElement.PointerMoved -= RectangleSelection_PointerMoved;
			uiElement.PointerMoved += RectangleSelection_PointerMoved;
			if (selectionChanged is not null)
			{
				// Unsunscribe from SelectionChanged event for performance
				uiElement.SelectionChanged -= selectionChanged;
			}
			uiElement.CapturePointer(e.Pointer);
			selectionState = SelectionState.Starting;
		}

		private void FetchItemsPosition()
		{
			var verticalOffset = scrollViewer.VerticalOffset;
			foreach (var item in uiElement.Items.ToList().Except(itemsPosition.Keys))
			{
				var listViewItem = (FrameworkElement)uiElement.ContainerFromItem(item); // Get ListViewItem
				if (listViewItem is null)
				{
					continue; // Element is not loaded (virtualized list)
				}

				var gt = listViewItem.TransformToVisual(uiElement);
				var itemStartPoint = gt.TransformPoint(new Point(0, verticalOffset)); // Get item position relative to the top of the list (considering scrolled offset)
				var itemRect = new System.Drawing.Rectangle((int)itemStartPoint.X, (int)itemStartPoint.Y, (int)listViewItem.ActualWidth, (int)listViewItem.ActualHeight);
				itemsPosition[item] = itemRect;
			}
		}

		private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			if (!timer.IsRunning)
			{
				timer.Debounce(FetchItemsPosition, TimeSpan.FromMilliseconds(1000));
			}
		}

		private void RectangleSelection_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (scrollViewer is null) return;
			Canvas.SetLeft(selectionRectangle, 0);
			Canvas.SetTop(selectionRectangle, 0);
			selectionRectangle.Width = 0;
			selectionRectangle.Height = 0;
			uiElement.PointerMoved -= RectangleSelection_PointerMoved;

			scrollViewer.ViewChanged -= ScrollViewer_ViewChanged;
			uiElement.ReleasePointerCapture(e.Pointer);
			if (selectionChanged is not null)
			{
				// Restore and trigger SelectionChanged event
				uiElement.SelectionChanged -= selectionChanged;
				uiElement.SelectionChanged += selectionChanged;
				if (prevSelectedItems is null || !uiElement.SelectedItems.SequenceEqual(prevSelectedItems))
				{
					// Trigger SelectionChanged event if the selection has changed
					selectionChanged(sender, null);
				}
			}
			if (selectionState == SelectionState.Active || e.OriginalSource is ListViewBase)
			{
				// Always trigger SelectionEnded to focus the file list when clicking on the empty space (#2977)
				OnSelectionEnded();
			}

			selectionStrategy = null;
			selectionState = SelectionState.Inactive;

			prevSelectedItemsDrag = null;

			e.Handled = true;
		}

		private void RectangleSelection_SizeChanged(object sender, object e)
		{
			scrollViewer ??= DependencyObjectHelpers.FindChild<ScrollViewer>(uiElement, sv => sv.VerticalScrollMode != ScrollMode.Disabled);

			if (scrollViewer is not null)
			{
				uiElement.SizeChanged -= RectangleSelection_SizeChanged;
			}
		}

		private void InitEvents(object sender, RoutedEventArgs e)
		{
			if (!uiElement.IsLoaded)
			{
				uiElement.Loaded += InitEvents;
			}
			else
			{
				uiElement.Loaded -= InitEvents;
				uiElement.PointerPressed += RectangleSelection_PointerPressed;
				uiElement.PointerReleased += RectangleSelection_PointerReleased;
				uiElement.PointerCaptureLost += RectangleSelection_PointerReleased;
				uiElement.PointerCanceled += RectangleSelection_PointerReleased;

				scrollViewer = DependencyObjectHelpers.FindChild<ScrollViewer>(uiElement, sv => sv.VerticalScrollMode != ScrollMode.Disabled);
				if (scrollViewer is null)
				{
					uiElement.SizeChanged += RectangleSelection_SizeChanged;
				}
			}
		}
	}
}