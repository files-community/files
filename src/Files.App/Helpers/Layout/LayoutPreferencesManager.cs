// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents manager for layout preferences settings.
	/// </summary>
	public sealed class LayoutPreferencesManager : ObservableObject
	{
		// Dependency injections

		private static IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		// Fields

		private static readonly Lazy<LayoutPreferencesDatabaseManager> _databaseInstance =
			new(() => new LayoutPreferencesDatabaseManager());

		private readonly FolderLayoutModes? _rootLayoutMode;

		// Properties
		public bool IsLayoutModeFixed
			=> _rootLayoutMode is not null;

		public bool IsAdaptiveLayoutEnabled
		{
			get => !LayoutPreferencesItem.IsAdaptiveLayoutOverridden;
			set
			{
				if (SetProperty(item => item.IsAdaptiveLayoutOverridden, item => item.IsAdaptiveLayoutOverridden = !value, nameof(IsAdaptiveLayoutEnabled)))
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem, true));
			}
		}

		public FolderLayoutModes LayoutMode
		{
			get => _rootLayoutMode ?? LayoutPreferencesItem.LayoutMode;
			set
			{
				if (SetProperty(item => item.LayoutMode, item => item.LayoutMode = value, nameof(LayoutMode)))
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
			}
		}

		public SortOption DirectorySortOption
		{
			get => LayoutPreferencesItem.DirectorySortOption;
			set
			{
				if (SetProperty(item => item.DirectorySortOption, item => item.DirectorySortOption = value, nameof(DirectorySortOption)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					SortOptionPreferenceUpdated?.Invoke(this, DirectorySortOption);
				}
			}
		}

		public GroupOption DirectoryGroupOption
		{
			get => LayoutPreferencesItem.DirectoryGroupOption;
			set
			{
				if (SetProperty(item => item.DirectoryGroupOption, item => item.DirectoryGroupOption = value, nameof(DirectoryGroupOption)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					GroupOptionPreferenceUpdated?.Invoke(this, DirectoryGroupOption);
				}
			}
		}

		public SortDirection DirectorySortDirection
		{
			get => LayoutPreferencesItem.DirectorySortDirection;
			set
			{
				if (SetProperty(item => item.DirectorySortDirection, item => item.DirectorySortDirection = value, nameof(DirectorySortDirection)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					SortDirectionPreferenceUpdated?.Invoke(this, DirectorySortDirection);
				}
			}
		}

		public SortDirection DirectoryGroupDirection
		{
			get => LayoutPreferencesItem.DirectoryGroupDirection;
			set
			{
				if (SetProperty(item => item.DirectoryGroupDirection, item => item.DirectoryGroupDirection = value, nameof(DirectoryGroupDirection)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					GroupDirectionPreferenceUpdated?.Invoke(this, DirectoryGroupDirection);
				}
			}
		}

		public GroupByDateUnit DirectoryGroupByDateUnit
		{
			get => LayoutPreferencesItem.DirectoryGroupByDateUnit;
			set
			{
				if (SetProperty(item => item.DirectoryGroupByDateUnit, item => item.DirectoryGroupByDateUnit = value, nameof(DirectoryGroupByDateUnit)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					GroupByDateUnitPreferenceUpdated?.Invoke(this, DirectoryGroupByDateUnit);
				}
			}
		}

		public bool SortDirectoriesAlongsideFiles
		{
			get => LayoutPreferencesItem.SortDirectoriesAlongsideFiles;
			set
			{
				if (SetProperty(item => item.SortDirectoriesAlongsideFiles, item => item.SortDirectoriesAlongsideFiles = value, nameof(SortDirectoriesAlongsideFiles)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					SortDirectoriesAlongsideFilesPreferenceUpdated?.Invoke(this, SortDirectoriesAlongsideFiles);
				}
			}
		}

		public bool SortFilesFirst
		{
			get => LayoutPreferencesItem.SortFilesFirst;
			set
			{
				if (SetProperty(item => item.SortFilesFirst, item => item.SortFilesFirst = value, nameof(SortFilesFirst)))
				{
					LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
					SortFilesFirstPreferenceUpdated?.Invoke(this, SortFilesFirst);
				}
			}
		}

		public ColumnsViewModel ColumnsViewModel
		{
			get => LayoutPreferencesItem.ColumnsViewModel;
			set
			{
				SetProperty(item => item.ColumnsViewModel, item => item.ColumnsViewModel = value, nameof(ColumnsViewModel));
				LayoutPreferencesUpdateRequired?.Invoke(this, new LayoutPreferenceEventArgs(LayoutPreferencesItem));
			}
		}

		private bool _IsLayoutModeChanging;
		public bool IsLayoutModeChanging
		{
			get => _IsLayoutModeChanging;
			set => SetProperty(ref _IsLayoutModeChanging, value);
		}

		private LayoutPreferencesItem? _LayoutPreferencesItem;
		public LayoutPreferencesItem LayoutPreferencesItem
		{
			get => _LayoutPreferencesItem!;
			private set
			{
				if (SetProperty(ref _LayoutPreferencesItem, value))
				{
					OnPropertyChanged(nameof(LayoutMode));
					OnPropertyChanged(nameof(IsAdaptiveLayoutEnabled));
					OnPropertyChanged(nameof(DirectoryGroupOption));
					OnPropertyChanged(nameof(DirectorySortOption));
					OnPropertyChanged(nameof(DirectorySortDirection));
					OnPropertyChanged(nameof(DirectoryGroupDirection));
					OnPropertyChanged(nameof(DirectoryGroupByDateUnit));
					OnPropertyChanged(nameof(SortDirectoriesAlongsideFiles));
					OnPropertyChanged(nameof(SortFilesFirst));
					OnPropertyChanged(nameof(ColumnsViewModel));
				}
			}
		}

		// Events

		public event EventHandler<LayoutPreferenceEventArgs>? LayoutPreferencesUpdateRequired;
		public event EventHandler<SortOption>? SortOptionPreferenceUpdated;
		public event EventHandler<GroupOption>? GroupOptionPreferenceUpdated;
		public event EventHandler<SortDirection>? SortDirectionPreferenceUpdated;
		public event EventHandler<SortDirection>? GroupDirectionPreferenceUpdated;
		public event EventHandler<GroupByDateUnit>? GroupByDateUnitPreferenceUpdated;
		public event EventHandler<bool>? SortDirectoriesAlongsideFilesPreferenceUpdated;
		public event EventHandler<bool>? SortFilesFirstPreferenceUpdated;
		public event EventHandler<LayoutModeEventArgs>? LayoutModeChangeRequested;

		// Constructors

		public LayoutPreferencesManager()
		{
			LayoutPreferencesItem = new LayoutPreferencesItem();
		}

		public LayoutPreferencesManager(FolderLayoutModes modeOverride) : this()
		{
			_rootLayoutMode = modeOverride;
			LayoutPreferencesItem.IsAdaptiveLayoutOverridden = true;
		}

		// Methods

		public Type GetLayoutType(string path, bool changeLayoutMode = true)
		{
			var preferencesItem = GetLayoutPreferencesForPath(path);
			if (preferencesItem is null)
				return typeof(DetailsLayoutPage);

			if (changeLayoutMode)
			{
				IsLayoutModeChanging = LayoutPreferencesItem.LayoutMode != preferencesItem.LayoutMode;
				LayoutPreferencesItem = preferencesItem;
			}

			return (preferencesItem.LayoutMode) switch
			{
				FolderLayoutModes.DetailsView => typeof(DetailsLayoutPage),
				FolderLayoutModes.ListView => typeof(GridLayoutPage),
				FolderLayoutModes.CardsView => typeof(GridLayoutPage),
				FolderLayoutModes.GridView => typeof(GridLayoutPage),
				FolderLayoutModes.ColumnView => typeof(ColumnsLayoutPage),
				_ => typeof(DetailsLayoutPage)
			};
		}

		public bool IsPathUsingDefaultLayout(string? path)
		{
			return UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories ||
				string.IsNullOrEmpty(path) ||
				GetLayoutPreferencesFromDatabase(path, Win32Helper.GetFolderFRN(path)) is null;
		}

		public void ToggleLayoutModeColumnView(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Column View
			LayoutMode = FolderLayoutModes.ColumnView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.ColumnView));
		}

		public void ToggleLayoutModeGridView(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Grid View
			LayoutMode = FolderLayoutModes.GridView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.GridView));
		}

		public void ToggleLayoutModeCards(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Cards View
			LayoutMode = FolderLayoutModes.CardsView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.CardsView));
		}

		public void ToggleLayoutModeList(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// List View
			LayoutMode = FolderLayoutModes.ListView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.ListView));
		}

		public void ToggleLayoutModeDetailsView(bool manuallySet)
		{
			IsAdaptiveLayoutEnabled &= !manuallySet;

			// Details View
			LayoutMode = FolderLayoutModes.DetailsView;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.DetailsView));
		}

		public void ToggleLayoutModeAdaptive()
		{
			// Adaptive
			IsAdaptiveLayoutEnabled = true;

			LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(FolderLayoutModes.Adaptive));
		}

		public void OnDefaultPreferencesChanged(string path, string settingsName)
		{
			var preferencesItem = GetLayoutPreferencesForPath(path);
			if (preferencesItem is null)
				return;

			switch (settingsName)
			{
				case nameof(UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles):
					SortDirectoriesAlongsideFiles = preferencesItem.SortDirectoriesAlongsideFiles;
					break;
				case nameof(UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst):
					SortFilesFirst = preferencesItem.SortFilesFirst;
					break;
				case nameof(UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories):
					LayoutPreferencesItem = preferencesItem;
					LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode));
					break;
				case nameof(UserSettingsService.LayoutSettingsService.DefaultLayoutMode):
					LayoutModeChangeRequested?.Invoke(this, new LayoutModeEventArgs(LayoutMode));
					break;
			}
		}

		// Static methods

		public static LayoutPreferencesDatabaseManager GetDatabaseManagerInstance()
		{
			return _databaseInstance.Value;
		}

		public static void SetDefaultLayoutPreferences(ColumnsViewModel columns)
		{
			UserSettingsService.LayoutSettingsService.ShowDateColumn = !columns.DateModifiedColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn = !columns.DateCreatedColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowTypeColumn = !columns.ItemTypeColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowSizeColumn = !columns.SizeColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowFileTagColumn = !columns.TagColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowGitStatusColumn = !columns.GitStatusColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowGitLastCommitDateColumn = !columns.GitLastCommitDateColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowGitLastCommitMessageColumn = !columns.GitLastCommitMessageColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowGitCommitAuthorColumn = !columns.GitCommitAuthorColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowGitLastCommitShaColumn = !columns.GitLastCommitShaColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowDateDeletedColumn = !columns.DateDeletedColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowPathColumn = !columns.PathColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowOriginalPathColumn = !columns.OriginalPathColumn.UserCollapsed;
			UserSettingsService.LayoutSettingsService.ShowSyncStatusColumn = !columns.StatusColumn.UserCollapsed;

			UserSettingsService.LayoutSettingsService.NameColumnWidth = columns.NameColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.DateModifiedColumnWidth = columns.DateModifiedColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.DateCreatedColumnWidth = columns.DateCreatedColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.TypeColumnWidth = columns.ItemTypeColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.SizeColumnWidth = columns.SizeColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.TagColumnWidth = columns.TagColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.GitStatusColumnWidth = columns.GitStatusColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.GitLastCommitDateColumnWidth = columns.GitLastCommitDateColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.GitLastCommitMessageColumnWidth = columns.GitLastCommitMessageColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.GitCommitAuthorColumnWidth = columns.GitCommitAuthorColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.GitLastCommitShaColumnWidth = columns.GitLastCommitShaColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.DateDeletedColumnWidth = columns.DateDeletedColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.PathColumnWidth = columns.PathColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.OriginalPathColumnWidth = columns.OriginalPathColumn.UserLengthPixels;
			UserSettingsService.LayoutSettingsService.SyncStatusColumnWidth = columns.StatusColumn.UserLengthPixels;
		}

		public static void SetLayoutPreferencesForPath(string path, LayoutPreferencesItem preferencesItem)
		{
			if (!UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories)
			{
				var folderFRN = Win32Helper.GetFolderFRN(path);
				var trimmedFolderPath = path.TrimPath();
				if (trimmedFolderPath is not null)
					SetLayoutPreferencesToDatabase(trimmedFolderPath, folderFRN, preferencesItem);
			}
			else
			{
				UserSettingsService.LayoutSettingsService.DefaultLayoutMode = preferencesItem.LayoutMode;

				// Do not save options which only work in recycle bin or cloud folders or search results as global
				if (preferencesItem.DirectorySortOption != SortOption.Path &&
					preferencesItem.DirectorySortOption != SortOption.OriginalFolder &&
					preferencesItem.DirectorySortOption != SortOption.DateDeleted &&
					preferencesItem.DirectorySortOption != SortOption.SyncStatus)
				{
					UserSettingsService.LayoutSettingsService.DefaultSortOption = preferencesItem.DirectorySortOption;
				}

				if (preferencesItem.DirectoryGroupOption != GroupOption.OriginalFolder &&
					preferencesItem.DirectoryGroupOption != GroupOption.DateDeleted &&
					preferencesItem.DirectoryGroupOption != GroupOption.FolderPath &&
					preferencesItem.DirectoryGroupOption != GroupOption.SyncStatus)
				{
					UserSettingsService.LayoutSettingsService.DefaultGroupOption = preferencesItem.DirectoryGroupOption;
				}

				UserSettingsService.LayoutSettingsService.DefaultDirectorySortDirection = preferencesItem.DirectorySortDirection;
				UserSettingsService.LayoutSettingsService.DefaultDirectoryGroupDirection = preferencesItem.DirectoryGroupDirection;
				UserSettingsService.LayoutSettingsService.DefaultGroupByDateUnit = preferencesItem.DirectoryGroupByDateUnit;
				UserSettingsService.LayoutSettingsService.DefaultSortDirectoriesAlongsideFiles = preferencesItem.SortDirectoriesAlongsideFiles;
				UserSettingsService.LayoutSettingsService.DefaultSortFilesFirst = preferencesItem.SortFilesFirst;

				UserSettingsService.LayoutSettingsService.NameColumnWidth = preferencesItem.ColumnsViewModel.NameColumn.UserLengthPixels;

				if (!preferencesItem.ColumnsViewModel.DateModifiedColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowDateColumn = !preferencesItem.ColumnsViewModel.DateModifiedColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.DateModifiedColumnWidth = preferencesItem.ColumnsViewModel.DateModifiedColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.DateCreatedColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowDateCreatedColumn = !preferencesItem.ColumnsViewModel.DateCreatedColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.DateCreatedColumnWidth = preferencesItem.ColumnsViewModel.DateCreatedColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.ItemTypeColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowTypeColumn = !preferencesItem.ColumnsViewModel.ItemTypeColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.TypeColumnWidth = preferencesItem.ColumnsViewModel.ItemTypeColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.SizeColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowSizeColumn = !preferencesItem.ColumnsViewModel.SizeColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.SizeColumnWidth = preferencesItem.ColumnsViewModel.SizeColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.TagColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowFileTagColumn = !preferencesItem.ColumnsViewModel.TagColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.TagColumnWidth = preferencesItem.ColumnsViewModel.TagColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitStatusColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowGitStatusColumn = !preferencesItem.ColumnsViewModel.GitStatusColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.GitStatusColumnWidth = preferencesItem.ColumnsViewModel.GitStatusColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitLastCommitDateColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowGitLastCommitDateColumn = !preferencesItem.ColumnsViewModel.GitLastCommitDateColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.GitLastCommitDateColumnWidth = preferencesItem.ColumnsViewModel.GitLastCommitDateColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitLastCommitMessageColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowGitLastCommitMessageColumn = !preferencesItem.ColumnsViewModel.GitLastCommitMessageColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.GitLastCommitMessageColumnWidth = preferencesItem.ColumnsViewModel.GitLastCommitMessageColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitCommitAuthorColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowGitCommitAuthorColumn = !preferencesItem.ColumnsViewModel.GitCommitAuthorColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.GitCommitAuthorColumnWidth = preferencesItem.ColumnsViewModel.GitCommitAuthorColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.GitLastCommitShaColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowGitLastCommitShaColumn = !preferencesItem.ColumnsViewModel.GitLastCommitShaColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.GitLastCommitShaColumnWidth = preferencesItem.ColumnsViewModel.GitLastCommitShaColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.DateDeletedColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowDateDeletedColumn = !preferencesItem.ColumnsViewModel.DateDeletedColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.DateDeletedColumnWidth = preferencesItem.ColumnsViewModel.DateDeletedColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.PathColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowPathColumn = !preferencesItem.ColumnsViewModel.PathColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.PathColumnWidth = preferencesItem.ColumnsViewModel.PathColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.OriginalPathColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowOriginalPathColumn = !preferencesItem.ColumnsViewModel.OriginalPathColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.OriginalPathColumnWidth = preferencesItem.ColumnsViewModel.OriginalPathColumn.UserLengthPixels;
				}
				if (!preferencesItem.ColumnsViewModel.StatusColumn.IsHidden)
				{
					UserSettingsService.LayoutSettingsService.ShowSyncStatusColumn = !preferencesItem.ColumnsViewModel.StatusColumn.UserCollapsed;
					UserSettingsService.LayoutSettingsService.SyncStatusColumnWidth = preferencesItem.ColumnsViewModel.StatusColumn.UserLengthPixels;
				}
			}
		}

		private static LayoutPreferencesItem? GetLayoutPreferencesForPath(string path)
		{
			if (!UserSettingsService.LayoutSettingsService.SyncFolderPreferencesAcrossDirectories)
			{
				path = path.TrimPath() ?? string.Empty;

				return SafetyExtensions.IgnoreExceptions(() =>
				{
					if (path.StartsWith("tag:", StringComparison.Ordinal))
						return GetLayoutPreferencesFromDatabase("Home", null);

					var folderFRN = Win32Helper.GetFolderFRN(path);

					return GetLayoutPreferencesFromDatabase(path, folderFRN)
						?? GetLayoutPreferencesFromAds(path, folderFRN);
				}, App.Logger)
					?? GetDefaultLayoutPreferences(path);
			}

			return new LayoutPreferencesItem();
		}

		private static LayoutPreferencesItem? GetLayoutPreferencesFromAds(string path, ulong? frn)
		{
			var str = Win32Helper.ReadStringFromFile($"{path}:files_layoutmode");

			var layoutPreferences = SafetyExtensions.IgnoreExceptions(() =>
				string.IsNullOrEmpty(str) ? null : JsonSerializer.Deserialize<LayoutPreferencesItem>(str));

			if (layoutPreferences is null)
				return null;

			// Port settings to the database, delete the ADS
			if (SetLayoutPreferencesToDatabase(path, frn, layoutPreferences))
				PInvoke.DeleteFileFromApp($"{path}:files_layoutmode");

			return layoutPreferences;
		}

		private static LayoutPreferencesItem? GetLayoutPreferencesFromDatabase(string path, ulong? frn)
		{
			if (string.IsNullOrEmpty(path))
				return null;

			var databaseManager = GetDatabaseManagerInstance();

			return databaseManager.GetPreferences(path, frn);
		}

		private static LayoutPreferencesItem? GetDefaultLayoutPreferences(string path)
		{
			if (string.IsNullOrEmpty(path))
				return new();

			if (path == Constants.UserEnvironmentPaths.DownloadsPath)
			{
				// Default for downloads folder is to group by date created
				return new()
				{
					DirectoryGroupOption = GroupOption.DateCreated,
					DirectoryGroupDirection = SortDirection.Descending,
					DirectoryGroupByDateUnit = GroupByDateUnit.Year
				};
			}
			else if (LibraryManager.IsLibraryPath(path))
			{
				// Default for libraries is to group by folder path
				return new()
				{
					DirectoryGroupOption = GroupOption.FolderPath
				};
			}
			else
			{
				// Either global setting or smart guess
				return new();
			}
		}

		private static bool SetLayoutPreferencesToDatabase(string path, ulong? frn, LayoutPreferencesItem preferencesItem)
		{
			if (string.IsNullOrEmpty(path))
				return false;

			return SafetyExtensions.IgnoreExceptions(() =>
			{
				var dbInstance = GetDatabaseManagerInstance();
				if (dbInstance.GetPreferences(path, frn) is null &&
					new LayoutPreferencesItem().Equals(preferencesItem))
				{
					// Do not create setting if it's default
					return;
				}

				dbInstance.SetPreferences(path, frn, preferencesItem);
			});
		}

		private bool SetProperty<TValue>(Func<LayoutPreferencesItem, TValue> prop, Action<LayoutPreferencesItem> update, string propertyName)
		{
			var oldValue = prop(LayoutPreferencesItem);
			update(LayoutPreferencesItem);

			if (EqualityComparer<TValue>.Default.Equals(prop(LayoutPreferencesItem), oldValue))
			{
				return false;
			}

			OnPropertyChanged(propertyName);
			return true;
		}
	}
}