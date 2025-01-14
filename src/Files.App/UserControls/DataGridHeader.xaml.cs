// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;

namespace Files.App.UserControls
{
	[DependencyProperty<string>("Header")]
	[DependencyProperty<bool>("CanBeSorted", DefaultValue = "true")]
	[DependencyProperty<SortDirection>("ColumnSortOption", "OnColumnSortOptionChanged", IsNullable = true)]
	public sealed partial class DataGridHeader : UserControl
	{
		public ICommand Command { get; set; }
		public object CommandParameter { get; set; }

		public void OnColumnSortOptionChanged(SortDirection? oldValue, SortDirection? newValue)
		{
			VisualStateManager.GoToState(
				this,
				newValue switch
				{
					SortDirection.Ascending => "SortAscending",
					SortDirection.Descending => "SortDescending",
					_ => "Unsorted",
				},
				true);
		}

		public DataGridHeader()
		{
			InitializeComponent();
		}
	}
}
