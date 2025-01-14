// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Files.App.UserControls.Selection
{
	public abstract class ItemSelectionStrategy
	{
		protected readonly ICollection<object> selectedItems;

		protected ItemSelectionStrategy(ICollection<object> selectedItems)
		{
			this.selectedItems = selectedItems;
		}

		public abstract void HandleIntersectionWithItem(object item);

		public abstract void HandleNoIntersectionWithItem(object item);

		public virtual void StartSelection()
		{
		}

		public virtual void HandleNoItemSelected()
		{
		}
	}
}