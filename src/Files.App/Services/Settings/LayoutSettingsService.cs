// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services.Settings
{
	internal sealed class LayoutSettingsService : BaseObservableJsonSettings, ILayoutSettingsService
	{
		public LayoutSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
		}

		public bool SyncFolderPreferencesAcrossDirectories
		{
			get => Get(false);
			set => Set(value);
		}

		public FolderLayoutModes DefaultLayoutMode
		{
			get => (FolderLayoutModes)Get((long)FolderLayoutModes.Adaptive);
			set => Set((long)value);
		}

		public SortOption DefaultSortOption
		{
			get => (SortOption)Get((long)SortOption.Name);
			set => Set((long)value);
		}

		public SortDirection DefaultDirectorySortDirection
		{
			get => (SortDirection)Get((long)SortDirection.Ascending);
			set => Set((long)value);
		}

		public bool DefaultSortDirectoriesAlongsideFiles
		{
			get => Get(false);
			set => Set(value);
		}

		public bool DefaultSortFilesFirst
		{
			get => Get(false);
			set => Set(value);
		}

		public GroupOption DefaultGroupOption
		{
			get => (GroupOption)Get((long)GroupOption.None);
			set => Set((long)value);
		}

		public SortDirection DefaultDirectoryGroupDirection
		{
			get => (SortDirection)Get((long)SortDirection.Ascending);
			set => Set((long)value);
		}

		public GroupByDateUnit DefaultGroupByDateUnit
		{
			get => (GroupByDateUnit)Get((long)GroupByDateUnit.Year);
			set => Set((long)value);
		}

		public double GitStatusColumnWidth
		{
			get => Get(80d);
			set
			{
				if (ShowGitStatusColumn)
					Set(value);
			}
		}

		public double GitLastCommitDateColumnWidth
		{
			get => Get(140d);
			set
			{
				if (ShowGitLastCommitDateColumn)
					Set(value);
			}
		}

		public double GitLastCommitMessageColumnWidth
		{
			get => Get(140d);
			set
			{
				if (ShowGitLastCommitMessageColumn)
					Set(value);
			}
		}

		public double GitCommitAuthorColumnWidth
		{
			get => Get(140d);
			set
			{
				if (ShowGitCommitAuthorColumn)
					Set(value);
			}
		}

		public double GitLastCommitShaColumnWidth
		{
			get => Get(80d);
			set
			{
				if (ShowGitLastCommitShaColumn)
					Set(value);
			}
		}

		public double TagColumnWidth
		{
			get => Get(140d);
			set
			{
				if (ShowFileTagColumn)
					Set(value);
			}
		}

		public double NameColumnWidth
		{
			get => Get(240d);
			set => Set(value);
		}

		public double DateModifiedColumnWidth
		{
			get => Get(200d);
			set
			{
				if (ShowDateColumn)
					Set(value);
			}
		}

		public double TypeColumnWidth
		{
			get => Get(140d);
			set
			{
				if (ShowTypeColumn)
					Set(value);
			}
		}

		public double DateCreatedColumnWidth
		{
			get => Get(200d);
			set
			{
				if (ShowDateCreatedColumn)
					Set(value);
			}
		}

		public double SizeColumnWidth
		{
			get => Get(100d);
			set
			{
				if (ShowSizeColumn)
					Set(value);
			}
		}

		public double DateDeletedColumnWidth
		{
			get => Get(200d);
			set
			{
				if (ShowDateDeletedColumn)
					Set(value);
			}
		}

		public double PathColumnWidth
		{
			get => Get(200d);
			set
			{
				if (ShowPathColumn)
					Set(value);
			}
		}

		public double OriginalPathColumnWidth
		{
			get => Get(200d);
			set
			{
				if (ShowOriginalPathColumn)
					Set(value);
			}
		}

		public double SyncStatusColumnWidth
		{
			get => Get(50d);
			set
			{
				if (ShowSyncStatusColumn)
					Set(value);
			}
		}

		public bool ShowDateColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowDateCreatedColumn
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowTypeColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowSizeColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowGitStatusColumn
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowGitLastCommitDateColumn
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowGitLastCommitMessageColumn
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowGitCommitAuthorColumn
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowGitLastCommitShaColumn
		{
			get => Get(false);
			set => Set(value);
		}

		public bool ShowFileTagColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowDateDeletedColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowPathColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowOriginalPathColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowSyncStatusColumn
		{
			get => Get(true);
			set => Set(value);
		}

		public DetailsViewSizeKind DetailsViewSize
		{
			get => Get(DetailsViewSizeKind.Small);
			set => Set(value);
		}

		public ListViewSizeKind ListViewSize
		{
			get => Get(ListViewSizeKind.Small);
			set => Set(value);
		}

		public CardsViewSizeKind CardsViewSize
		{
			get => Get(CardsViewSizeKind.Small);
			set => Set(value);
		}

		public GridViewSizeKind GridViewSize
		{
			get => Get(GridViewSizeKind.Large);
			set => Set(value);
		}

		public ColumnsViewSizeKind ColumnsViewSize
		{
			get => Get(ColumnsViewSizeKind.Small);
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
