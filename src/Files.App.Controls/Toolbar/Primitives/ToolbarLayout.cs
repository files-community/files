// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Files.App.Controls.Primitives
{
	public partial class ToolbarLayout : NonVirtualizingLayout
	{
		private Size m_availableSize;

		public ToolbarLayout()
		{

		}

		private int GetItemCount(NonVirtualizingLayoutContext context)
		{
			return context.Children.Count;
		}

		private UIElement GetElementAt(NonVirtualizingLayoutContext context , int index)
		{
			return context.Children[index];
		}

		// Measuring is performed in a single step, every element is measured, including the overflow button
		// item, but the total amount of space needed is only composed of the Toolbar Items
		protected override Size MeasureOverride(NonVirtualizingLayoutContext context , Size availableSize)
		{
			m_availableSize = availableSize;

			Size accumulatedItemsSize = new(0, 0);

			for (int i = 0; i < GetItemCount(context); ++i)
			{
				//var toolbarItem = (ToolbarItem)GetElementAt(context, i);
				//toolbarItem.Measure( availableSize );

				if ( i != 0 )
				{
					//accumulatedItemsSize.Width += toolbarItem.DesiredSize.Width;
					//accumulatedItemsSize.Height = Math.Max( accumulatedItemsSize.Height , toolbarItem.DesiredSize.Height );
				}
			}

			if ( accumulatedItemsSize.Width > availableSize.Width )
			{
				
			}
			else
			{
				
			}

			return accumulatedItemsSize;
		}

		private void ArrangeItem(UIElement breadcrumbItem , ref float accumulatedWidths , float maxElementHeight)
		{
		}

		// Arranging is performed in a single step, as many elements are tried to be drawn going from the last element
		// towards the first one, if there's not enough space, then the ellipsis button is drawn
		protected override Size ArrangeOverride(NonVirtualizingLayoutContext context , Size finalSize)
		{
			return finalSize;
		}
	}
}
