// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.Foundation;

namespace Files.App.UserControls.Selection
{
	/// <summary>
	/// Adds drag selection to a ListView, GridView or DataGrid
	/// </summary>
	public class RectangleSelection
	{
		private readonly double MinSelectionDelta = 5;

		protected Rectangle selectionRectangle;
		protected SelectionState selectionState;

		protected RectangleSelection()
		{
		}

		/// <summary>
		/// Adds drag selection to a ListView, GridView or DataGrid
		/// </summary>
		/// <param name="uiElement">Underlying UI element. Can derive from ListViewBase or DataGrid</param>
		/// <param name="selectionRectangle">Rectangle inside a Canvas</param>
		/// <param name="selectionChanged">SelectionChanged event associated with uiElement</param>
		/// <returns></returns>
		public static RectangleSelection Create(FrameworkElement uiElement, Rectangle selectionRectangle, SelectionChangedEventHandler selectionChanged = null)
		{
			if (uiElement is ListViewBase)
			{
				return new RectangleSelection_ListViewBase(uiElement as ListViewBase, selectionRectangle, selectionChanged);
			}
			else
			{
				throw new ArgumentException("uiElement must derive from ListViewBase or DataGrid");
			}
		}

		public delegate void SelectionStatusHandler(object sender, EventArgs e);

		public event SelectionStatusHandler SelectionStarted;

		public event SelectionStatusHandler SelectionEnded;

		protected void OnSelectionStarted()
		{
			SelectionStarted?.Invoke(this, EventArgs.Empty);
		}

		protected void OnSelectionEnded()
		{
			SelectionEnded?.Invoke(this, EventArgs.Empty);
		}

		public enum SelectionState
		{
			Inactive,
			Starting,
			Active
		}

		protected void DrawRectangle(PointerPoint currentPoint, Point originDragPointShifted, FrameworkElement uiElement)
		{
			// Redraw selection rectangle according to the new point
			if (currentPoint.Position.X >= originDragPointShifted.X)
			{
				double maxWidth = uiElement.ActualWidth - originDragPointShifted.X;
				if (currentPoint.Position.Y <= originDragPointShifted.Y)
				{
					// Pointer was moved up and right
					Canvas.SetLeft(selectionRectangle, Math.Max(0, originDragPointShifted.X));
					Canvas.SetTop(selectionRectangle, Math.Max(0, currentPoint.Position.Y));
					selectionRectangle.Width = Math.Max(0, Math.Min(currentPoint.Position.X - Math.Max(0, originDragPointShifted.X), maxWidth));
					selectionRectangle.Height = Math.Max(0, originDragPointShifted.Y - Math.Max(0, currentPoint.Position.Y));
				}
				else
				{
					// Pointer was moved down and right
					Canvas.SetLeft(selectionRectangle, Math.Max(0, originDragPointShifted.X));
					Canvas.SetTop(selectionRectangle, Math.Max(0, originDragPointShifted.Y));
					selectionRectangle.Width = Math.Max(0, Math.Min(currentPoint.Position.X - Math.Max(0, originDragPointShifted.X), maxWidth));
					selectionRectangle.Height = Math.Max(0, currentPoint.Position.Y - Math.Max(0, originDragPointShifted.Y));
				}
			}
			else
			{
				if (currentPoint.Position.Y <= originDragPointShifted.Y)
				{
					// Pointer was moved up and left
					Canvas.SetLeft(selectionRectangle, Math.Max(0, currentPoint.Position.X));
					Canvas.SetTop(selectionRectangle, Math.Max(0, currentPoint.Position.Y));
					selectionRectangle.Width = Math.Max(0, originDragPointShifted.X - Math.Max(0, currentPoint.Position.X));
					selectionRectangle.Height = Math.Max(0, originDragPointShifted.Y - Math.Max(0, currentPoint.Position.Y));
				}
				else
				{
					// Pointer was moved down and left
					Canvas.SetLeft(selectionRectangle, Math.Max(0, currentPoint.Position.X));
					Canvas.SetTop(selectionRectangle, Math.Max(0, originDragPointShifted.Y));
					selectionRectangle.Width = Math.Max(0, originDragPointShifted.X - Math.Max(0, currentPoint.Position.X));
					selectionRectangle.Height = Math.Max(0, currentPoint.Position.Y - Math.Max(0, originDragPointShifted.Y));
				}
			}
		}

		protected bool HasMovedMinimalDelta(double originalX, double originalY, double currentX, double currentY)
		{
			var deltaX = Math.Abs(originalX - currentX);
			var deltaY = Math.Abs(originalY - currentY);
			return deltaX > MinSelectionDelta || deltaY > MinSelectionDelta;
		}
	}
}