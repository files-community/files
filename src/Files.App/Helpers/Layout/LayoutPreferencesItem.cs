// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents item for a folder's layout preferences.
	/// </summary>
	public sealed class LayoutPreferencesItem
	{
		// Dependency injections

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Properties

		public ColumnsViewModel ColumnsViewModel { get; set; }

		public bool SortDirectoriesAlongsideFiles { get; set; }
		public bool SortFilesFirst { get; set; }
		public bool IsAdaptiveLayoutOverridden { get; set; }

		public FolderLayoutModes LayoutMode { get; set; }

		public SortOption DirectorySortOption { get; set; }
		public SortDirection DirectorySortDirection { get; set; }
		public SortDirection DirectoryGroupDirection { get; set; }

		public GroupOption DirectoryGroupOption { get; set; }
		public GroupByDateUnit DirectoryGroupByDateUnit { get; set; }

		// Constructor

		public LayoutPreferencesItem()
		{
			var defaultLayout = UserSettingsService.LayoutSettingsService.DefaultLayoutMode;

			LayoutMode = defaultLayout is FolderLayoutModes.Adaptive ? FolderLayoutModes.DetailsView : defaultLayout;
			DirectorySortOption = UserSettingsService.LayoutSettingsService.DefaultSortOption;
			DirectoryGroupOption = UserSettingsService.LayoutSettingsService.DefaultGroupOption;
			DirectorySortDirection = UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection;
			DirectoryGroupDirection = UserSettingsService.LayoutSettingsService.DefaultDirectoryGroupDirection;
			DirectoryGroupByDateUnit = UserSettingsService.LayoutSettingsService.DefaultGroupByDateUnit;
			SortDirectoriesAlongsideFiles = UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles;
			SortFilesFirst = UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst;
			IsAdaptiveLayoutOverridden = defaultLayout is not FolderLayoutModes.Adaptive;

			ColumnsViewModel = new ColumnsViewModel();
			ColumnsViewModel.DateCreatedColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn;
			ColumnsViewModel.DateModifiedColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowDateColumn;
			ColumnsViewModel.ItemTypeColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowTypeColumn;
			ColumnsViewModel.SizeColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowSizeColumn;
			ColumnsViewModel.GitStatusColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowGitStatusColumn;
			ColumnsViewModel.GitLastCommitDateColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowGitLastCommitDateColumn;
			ColumnsViewModel.GitLastCommitMessageColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowGitLastCommitMessageColumn;
			ColumnsViewModel.GitCommitAuthorColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowGitCommitAuthorColumn;
			ColumnsViewModel.GitLastCommitShaColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowGitLastCommitShaColumn;
			ColumnsViewModel.TagColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowFileTagColumn;
			ColumnsViewModel.DateDeletedColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowDateDeletedColumn;
			ColumnsViewModel.PathColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowPathColumn;
			ColumnsViewModel.OriginalPathColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowOriginalPathColumn;
			ColumnsViewModel.StatusColumn.UserCollapsed = !UserSettingsService.LayoutSettingsService.ShowSyncStatusColumn;

			ColumnsViewModel.NameColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.NameColumnWidth;
			ColumnsViewModel.DateModifiedColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.DateModifiedColumnWidth;
			ColumnsViewModel.DateCreatedColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.DateCreatedColumnWidth;
			ColumnsViewModel.ItemTypeColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.TypeColumnWidth;
			ColumnsViewModel.SizeColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.SizeColumnWidth;
			ColumnsViewModel.GitStatusColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.GitStatusColumnWidth;
			ColumnsViewModel.GitLastCommitDateColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.GitLastCommitDateColumnWidth;
			ColumnsViewModel.GitLastCommitMessageColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.GitLastCommitMessageColumnWidth;
			ColumnsViewModel.GitCommitAuthorColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.GitCommitAuthorColumnWidth;
			ColumnsViewModel.GitLastCommitShaColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.GitLastCommitShaColumnWidth;
			ColumnsViewModel.TagColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.TagColumnWidth;
			ColumnsViewModel.DateDeletedColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.DateDeletedColumnWidth;
			ColumnsViewModel.PathColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.PathColumnWidth;
			ColumnsViewModel.OriginalPathColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.OriginalPathColumnWidth;
			ColumnsViewModel.StatusColumn.UserLengthPixels = UserSettingsService.LayoutSettingsService.SyncStatusColumnWidth;
		}

		// Overridden methods

		public override bool Equals(object? obj)
		{
			if (obj is null)
				return false;

			if (obj == this)
				return true;

			if (obj is LayoutPreferencesItem item)
			{
				return (
					item.LayoutMode == LayoutMode &&
					item.DirectoryGroupOption == DirectoryGroupOption &&
					item.DirectorySortOption == DirectorySortOption &&
					item.DirectorySortDirection == DirectorySortDirection &&
					item.DirectoryGroupDirection == DirectoryGroupDirection &&
					item.DirectoryGroupByDateUnit == DirectoryGroupByDateUnit &&
					item.SortDirectoriesAlongsideFiles == SortDirectoriesAlongsideFiles &&
					item.SortFilesFirst == SortFilesFirst &&
					item.IsAdaptiveLayoutOverridden == IsAdaptiveLayoutOverridden &&
					item.ColumnsViewModel.Equals(ColumnsViewModel));
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			HashCode hash = new();

			hash.Add(LayoutMode);
			hash.Add(DirectoryGroupOption);
			hash.Add(DirectorySortOption);
			hash.Add(DirectorySortDirection);
			hash.Add(DirectoryGroupDirection);
			hash.Add(DirectoryGroupByDateUnit);
			hash.Add(SortDirectoriesAlongsideFiles);
			hash.Add(SortFilesFirst);
			hash.Add(IsAdaptiveLayoutOverridden);
			hash.Add(ColumnsViewModel);

			return hash.ToHashCode();
		}
	}
}
