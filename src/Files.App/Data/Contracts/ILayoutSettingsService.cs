// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface ILayoutSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Sync folder preferences across all directories
		/// </summary>
		bool SyncFolderPreferencesAcrossDirectories { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default layout mode.
		/// </summary>
		FolderLayoutModes DefaultLayoutMode { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default sorting option.
		/// </summary>
		SortOption DefaultSortOption { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default sorting direction.
		/// </summary>
		SortDirection DefaultDirectorySortDirection { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if directories should be sorted alongside files by.
		/// </summary>
		bool DefaultSortDirectoriesAlongsideFiles { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if files should be sorted first.
		/// </summary>
		bool DefaultSortFilesFirst { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default grouping option.
		/// </summary>
		GroupOption DefaultGroupOption { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the default grouping direction.
		/// </summary>
		SortDirection DefaultDirectoryGroupDirection { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the unit for grouping by date.
		/// </summary>
		GroupByDateUnit DefaultGroupByDateUnit { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the date column should be visible by default.
		/// </summary>
		bool ShowDateColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the date created column should be visible by default.
		/// </summary>
		bool ShowDateCreatedColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the type column should be visible by default.
		/// </summary>
		bool ShowTypeColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the size column should be visible by default.
		/// </summary>
		bool ShowSizeColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the git status column should be visible by default.
		/// </summary>
		bool ShowGitStatusColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the git last commit date column should be visible by default.
		/// </summary>
		bool ShowGitLastCommitDateColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the git last commit message column should be visible by default.
		/// </summary>
		bool ShowGitLastCommitMessageColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the git last commit author column should be visible by default.
		/// </summary>
		bool ShowGitCommitAuthorColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the git last commit SHA column should be visible by default.
		/// </summary>
		bool ShowGitLastCommitShaColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the filetag column should be visible by default.
		/// </summary>
		bool ShowFileTagColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the date deleted column should be visible by default.
		/// </summary>
		bool ShowDateDeletedColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the path column should be visible by default.
		/// </summary>
		bool ShowPathColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the original path column should be visible by default.
		/// </summary>
		bool ShowOriginalPathColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the sync status column should be visible by default.
		/// </summary>
		bool ShowSyncStatusColumn { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the width of the git status column
		/// </summary>
		double GitStatusColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the width of the commit date column
		/// </summary>
		double GitLastCommitDateColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the width of the commit message column
		/// </summary>
		double GitLastCommitMessageColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the width of the git author column
		/// </summary>
		double GitCommitAuthorColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the width of the git sha column
		/// </summary>
		double GitLastCommitShaColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating tags column's default width
		/// </summary>
		double TagColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating name column's default width
		/// </summary>
		double NameColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating date modified column's default width
		/// </summary>
		double DateModifiedColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating item type column's default width
		/// </summary>
		double TypeColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating date created column's default width
		/// </summary>
		double DateCreatedColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating size column's default width
		/// </summary>
		double SizeColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating date deleted column's default width
		/// </summary>
		double DateDeletedColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating sync status column's default width
		/// </summary>
		double SyncStatusColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating path column's default width
		/// </summary>
		double PathColumnWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating original path column's default width
		/// </summary>
		double OriginalPathColumnWidth { get; set; }

		/// <summary>
		/// Item size in the Details View
		/// </summary>
		DetailsViewSizeKind DetailsViewSize { get; set; }

		/// <summary>
		/// Item size in the List View
		/// </summary>
		ListViewSizeKind ListViewSize { get; set; }

		/// <summary>
		/// Item size in the Cards View
		/// </summary>
		CardsViewSizeKind CardsViewSize { get; set; }

		/// <summary>
		/// Item size in the Grid View
		/// </summary>
		GridViewSizeKind GridViewSize { get; set; }

		/// <summary>
		/// Item size in the Columns View
		/// </summary>
		ColumnsViewSizeKind ColumnsViewSize { get; set; }
	}
}
