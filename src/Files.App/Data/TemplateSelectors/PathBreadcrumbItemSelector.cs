// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.TemplateSelectors
{
	/// <summary>
	/// Provides template selector for Path Breadcrumb template items.
	/// </summary>
	internal sealed class PathBreadcrumbItemSelector : DataTemplateSelector
	{
		public DataTemplate? ParentItems { get; set; }

		public DataTemplate? CurrentItem { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		{
			var itemsControl = ItemsControl.ItemsControlFromItemContainer(container);

			if (itemsControl.ItemsSource is ObservableCollection<PathBoxItem> items)
			{
				return
					itemsControl.IndexFromContainer(container) == items.Count - 1
						? CurrentItem!
						: ParentItems!;
			}
			else
			{
				throw new ArgumentException($"Type of {nameof(itemsControl.ItemsSource)} doesn't match ObservableCollection<{nameof(PathBoxItem)}>");
			}
		}
	}
}
