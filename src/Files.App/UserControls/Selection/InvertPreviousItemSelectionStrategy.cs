// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Files.App.UserControls.Selection
{
	internal sealed class InvertPreviousItemSelectionStrategy : ItemSelectionStrategy
	{
		private readonly List<object> prevSelectedItems;

		public InvertPreviousItemSelectionStrategy(ICollection<object> selectedItems, List<object> prevSelectedItems) : base(selectedItems)
		{
			this.prevSelectedItems = prevSelectedItems;
		}

		public override void HandleIntersectionWithItem(object item)
		{
			try
			{
				if (prevSelectedItems.Contains(item))
				{
					selectedItems.Remove(item);
				}
				else if (!selectedItems.Contains(item))
				{
					selectedItems.Add(item);
				}
			}
			catch (COMException) // List is being modified
			{
			}
		}

		public override void HandleNoIntersectionWithItem(object item)
		{
			try
			{
				// Restore selection on items not intersecting with the rectangle
				if (prevSelectedItems.Contains(item))
				{
					if (!selectedItems.Contains(item))
					{
						selectedItems.Add(item);
					}
				}
				else
				{
					selectedItems.Remove(item);
				}
			}
			catch (COMException) // List is being modified
			{
			}
		}
	}
}