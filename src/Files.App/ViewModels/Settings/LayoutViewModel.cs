// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.Settings
{
	public sealed class LayoutViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// FileTag combobox indexes (required to hide SyncStatus)
		private readonly int FileTagSortingIndex = 5;
		private readonly int FileTagGroupingIndex = 6;

		public LayoutViewModel()
		{
			// Layout mode
			SelectedDefaultLayoutModeIndex = (int)DefaultLayoutMode;

			// Sorting options
			SelectedDefaultSortingIndex = UserSettingsService.LayoutSettingsService.DefaultSortOption == SortOption.FileTag ? FileTagSortingIndex : (int)UserSettingsService.LayoutSettingsService.DefaultSortOption;
			SelectedDefaultSortPriorityIndex = UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles ? 2 : UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst ? 1 : 0;
			
			// Grouping options
			SelectedDefaultGroupingIndex = UserSettingsService.LayoutSettingsService.DefaultGroupOption == GroupOption.FileTag ? FileTagGroupingIndex : (int)UserSettingsService.LayoutSettingsService.DefaultGroupOption;
			SelectedDefaultGroupByDateUnitIndex = (int)UserSettingsService.LayoutSettingsService.DefaultGroupByDateUnit;
		}

		// Properties

		public bool SyncFolderPreferencesAcrossDirectories
		{
			get => UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories)
				{
					UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories = value;

					ResetLayoutPreferences();
					OnPropertyChanged();

					if (value && DefaultLayoutMode is FolderLayoutModes.Adaptive)
					{
						// Change the default layout to Details, as Adaptive layout is not available when preferences are synced.
						SelectedDefaultLayoutModeIndex = 0;
						ShowAdaptiveDisabledTeachingTip = true;
					}
				}
			}
		}

		// Layout mode

		public FolderLayoutModes DefaultLayoutMode
		{
			get => UserSettingsService.LayoutSettingsService.DefaultLayoutMode;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.DefaultLayoutMode)
				{
					UserSettingsService.LayoutSettingsService.DefaultLayoutMode = value;

					OnPropertyChanged();
				}
			}
		}

		private int selectedDefaultLayoutModeIndex;
		public int SelectedDefaultLayoutModeIndex
		{
			get => selectedDefaultLayoutModeIndex;
			set
			{
				if (SetProperty(ref selectedDefaultLayoutModeIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultLayoutModeIndex));
					DefaultLayoutMode = (FolderLayoutModes)value;
				}
			}
		}


		// Sorting options

		private int selectedDefaultSortingIndex;
		public int SelectedDefaultSortingIndex
		{
			get => selectedDefaultSortingIndex;
			set
			{
				if (SetProperty(ref selectedDefaultSortingIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultSortingIndex));

					UserSettingsService.LayoutSettingsService.DefaultSortOption = value == FileTagSortingIndex ? SortOption.FileTag : (SortOption)value;
				}
			}
		}

		public bool SortInDescendingOrder
		{
			get => UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection == SortDirection.Descending;
			set
			{
				if (value != (UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection == SortDirection.Descending))
				{
					UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection = value ? SortDirection.Descending : SortDirection.Ascending;
					OnPropertyChanged();
				}
			}
		}

	
		private int selectedDefaultSortPriorityIndex;
		public int SelectedDefaultSortPriorityIndex
		{
			get => selectedDefaultSortPriorityIndex;
			set
			{
				if (SetProperty(ref selectedDefaultSortPriorityIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultSortPriorityIndex));

					switch (value)
					{
						case 0:
							UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles = false;
							UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst = false;
							break;
						case 1:
							UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles = false;
							UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst = true;
							break;
						case 2:
							UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles = true;
							break;
						default:
							break;
					}
				}
			}
		}


		// Grouping options

		private int selectedDefaultGroupingIndex;
		public int SelectedDefaultGroupingIndex
		{
			get => selectedDefaultGroupingIndex;
			set
			{
				if (SetProperty(ref selectedDefaultGroupingIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultGroupingIndex));

					UserSettingsService.LayoutSettingsService.DefaultGroupOption = value == FileTagGroupingIndex ? GroupOption.FileTag : (GroupOption)value;

					// Raise an event for the grouping option toggle switches availability
					OnPropertyChanged(nameof(IsDefaultGrouped));
					OnPropertyChanged(nameof(IsGroupByDate));
				}
			}
		}

		public bool GroupInDescendingOrder
		{
			get => UserSettingsService.LayoutSettingsService.DefaultDirectoryGroupDirection == SortDirection.Descending;
			set
			{
				if (value != (UserSettingsService.LayoutSettingsService.DefaultDirectoryGroupDirection == SortDirection.Descending))
				{
					UserSettingsService.LayoutSettingsService.DefaultDirectoryGroupDirection = value ? SortDirection.Descending : SortDirection.Ascending;
					OnPropertyChanged();
				}
			}
		}

		private int defaultGroupByDateUnitIndex;

		public int SelectedDefaultGroupByDateUnitIndex
		{
			get => defaultGroupByDateUnitIndex;
			set
			{
				if (SetProperty(ref defaultGroupByDateUnitIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDefaultGroupByDateUnitIndex));
					UserSettingsService.LayoutSettingsService.DefaultGroupByDateUnit = (GroupByDateUnit)value;
				}
			}
		}

		public bool IsGroupByDate
			=> UserSettingsService.LayoutSettingsService.DefaultGroupOption.IsGroupByDate();

		public bool IsDefaultGrouped
			=> UserSettingsService.LayoutSettingsService.DefaultGroupOption != GroupOption.None;


		// Details view

		public bool ShowFileTagColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowFileTagColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowFileTagColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowFileTagColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowSizeColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowSizeColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowSizeColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowSizeColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowTypeColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowTypeColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowTypeColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowTypeColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowDateCreatedColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowDateColumn
		{
			get => UserSettingsService.LayoutSettingsService.ShowDateColumn;
			set
			{
				if (value != UserSettingsService.LayoutSettingsService.ShowDateColumn)
				{
					UserSettingsService.LayoutSettingsService.ShowDateColumn = value;

					OnPropertyChanged();
				}
			}
		}

		private bool _ShowAdaptiveDisabledTeachingTip;
		public bool ShowAdaptiveDisabledTeachingTip
		{
			get => _ShowAdaptiveDisabledTeachingTip;
			set => SetProperty(ref _ShowAdaptiveDisabledTeachingTip, value);
		}


		// Methods

		public void ResetLayoutPreferences()
		{
			// Is this proper practice?
			var dbInstance = LayoutPreferencesManager.GetDatabaseManagerInstance();

			dbInstance.ResetAll();
		}
	}
}
