// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contexts
{
	public interface IDisplayPageContext : INotifyPropertyChanging, INotifyPropertyChanged
	{
		bool IsLayoutAdaptiveEnabled { get; }
		LayoutTypes LayoutType { get; set; }

		SortOption SortOption { get; set; }
		SortDirection SortDirection { get; set; }

		GroupOption GroupOption { get; set; }
		SortDirection GroupDirection { get; set; }
		GroupByDateUnit GroupByDateUnit { get; set; }

		bool SortDirectoriesAlongsideFiles { get; set; }
		bool SortFilesFirst { get; set; }
	}
}
