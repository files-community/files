// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contexts
{
	internal sealed class DisplayPageContext : ObservableObject, IDisplayPageContext
	{
		private readonly IMultiPanesContext context = Ioc.Default.GetRequiredService<IMultiPanesContext>();
		private readonly IFoldersSettingsService settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();
		private readonly ILayoutSettingsService layoutSettingsService = Ioc.Default.GetRequiredService<ILayoutSettingsService>();

		public bool IsLayoutAdaptiveEnabled => !layoutSettingsService.SyncFolderPreferencesAcrossDirectories;

		private LayoutTypes _LayoutType = LayoutTypes.None;
		public LayoutTypes LayoutType
		{
			get => _LayoutType;
			set
			{
				var viewModel = FolderSettings;
				if (viewModel is null)
					return;

				switch (value)
				{
					case LayoutTypes.Details:
						viewModel.ToggleLayoutModeDetailsView(true);
						break;
					case LayoutTypes.List:
						viewModel.ToggleLayoutModeList(true);
						break;
					case LayoutTypes.Cards:
						viewModel.ToggleLayoutModeCards(true);
						break;
					case LayoutTypes.Grid:
						viewModel.ToggleLayoutModeGridView(true);
						break;
					case LayoutTypes.Columns:
						viewModel.ToggleLayoutModeColumnView(true);
						break;
					case LayoutTypes.Adaptive:
						viewModel.ToggleLayoutModeAdaptive();
						break;
				}
			}
		}

		private SortOption _SortOption = SortOption.Name;
		public SortOption SortOption
		{
			get => _SortOption;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.DirectorySortOption = value;
			}
		}

		private SortDirection _SortDirection = SortDirection.Ascending;
		public SortDirection SortDirection
		{
			get => _SortDirection;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.DirectorySortDirection = value;
			}
		}

		private GroupOption _GroupOption = GroupOption.None;
		public GroupOption GroupOption
		{
			get => _GroupOption;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.DirectoryGroupOption = value;
			}
		}

		private SortDirection _GroupDirection = SortDirection.Ascending;
		public SortDirection GroupDirection
		{
			get => _GroupDirection;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.DirectoryGroupDirection = value;
			}
		}

		private GroupByDateUnit _GroupByDateUnit = GroupByDateUnit.Year;
		public GroupByDateUnit GroupByDateUnit
		{
			get => _GroupByDateUnit;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.DirectoryGroupByDateUnit = value;
			}
		}

		private bool _SortDirectoriesAlongsideFiles = false;
		public bool SortDirectoriesAlongsideFiles
		{
			get => _SortDirectoriesAlongsideFiles;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.SortDirectoriesAlongsideFiles = value;
			}
		}

		private bool _SortFilesFirst = false;
		public bool SortFilesFirst
		{
			get => _SortFilesFirst;
			set
			{
				if (FolderSettings is LayoutPreferencesManager viewModel)
					viewModel.SortFilesFirst = value;
			}
		}

		private LayoutPreferencesManager? FolderSettings => context.ActivePaneOrColumn?.InstanceViewModel?.FolderSettings;

		public DisplayPageContext()
		{
			context.ActivePaneChanging += Context_Changing;
			context.ActivePaneChanged += Context_Changed;
			layoutSettingsService.PropertyChanged += Settings_PropertyChanged;
		}

		private void Context_Changing(object? sender, EventArgs e)
		{
			var viewModel = FolderSettings;
			if (viewModel is not null)
				viewModel.PropertyChanged -= FolderSettings_PropertyChanged;
			Update();
		}
		private void Context_Changed(object? sender, EventArgs e)
		{
			var viewModel = FolderSettings;
			if (viewModel is not null)
				viewModel.PropertyChanged += FolderSettings_PropertyChanged;
			Update();
		}

		private void FolderSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			var viewModel = FolderSettings;
			if (viewModel is null)
				return;

			switch (e.PropertyName)
			{
				case nameof(LayoutPreferencesManager.LayoutMode):
				case nameof(LayoutPreferencesManager.IsAdaptiveLayoutEnabled):
					SetProperty(ref _LayoutType, GetLayoutType(), nameof(LayoutType));
					break;
				case nameof(LayoutPreferencesManager.DirectorySortOption):
					SetProperty(ref _SortOption, viewModel.DirectorySortOption, nameof(SortOption));
					break;
				case nameof(LayoutPreferencesManager.DirectorySortDirection):
					SetProperty(ref _SortDirection, viewModel.DirectorySortDirection, nameof(SortDirection));
					break;
				case nameof(LayoutPreferencesManager.DirectoryGroupOption):
					SetProperty(ref _GroupOption, viewModel.DirectoryGroupOption, nameof(GroupOption));
					break;
				case nameof(LayoutPreferencesManager.DirectoryGroupDirection):
					SetProperty(ref _GroupDirection, viewModel.DirectoryGroupDirection, nameof(GroupDirection));
					break;
				case nameof(LayoutPreferencesManager.DirectoryGroupByDateUnit):
					SetProperty(ref _GroupByDateUnit, viewModel.DirectoryGroupByDateUnit, nameof(GroupByDateUnit));
					break;
				case nameof(LayoutPreferencesManager.SortDirectoriesAlongsideFiles):
					SetProperty(ref _SortDirectoriesAlongsideFiles, viewModel.SortDirectoriesAlongsideFiles, nameof(SortDirectoriesAlongsideFiles));
					break;
				case nameof(LayoutPreferencesManager.SortFilesFirst):
					SetProperty(ref _SortFilesFirst, viewModel.SortFilesFirst, nameof(SortFilesFirst));
					break;
			}
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ILayoutSettingsService.SyncFolderPreferencesAcrossDirectories))
			{
				OnPropertyChanged(nameof(IsLayoutAdaptiveEnabled));
				SetProperty(ref _LayoutType, GetLayoutType(), nameof(LayoutType));
			}
		}

		private void Update()
		{
			var viewModel = FolderSettings;
			if (viewModel is null)
			{
				SetProperty(ref _LayoutType, LayoutTypes.None, nameof(LayoutType));
				SetProperty(ref _SortOption, SortOption.Name, nameof(SortOption));
				SetProperty(ref _SortDirection, SortDirection.Ascending, nameof(SortDirection));
				SetProperty(ref _GroupOption, GroupOption.None, nameof(GroupOption));
				SetProperty(ref _GroupDirection, SortDirection.Ascending, nameof(GroupDirection));
				SetProperty(ref _GroupByDateUnit, GroupByDateUnit.Year, nameof(GroupByDateUnit));
			}
			else
			{
				SetProperty(ref _LayoutType, GetLayoutType(), nameof(LayoutType));
				SetProperty(ref _SortOption, viewModel.DirectorySortOption, nameof(SortOption));
				SetProperty(ref _SortDirection, viewModel.DirectorySortDirection, nameof(SortDirection));
				SetProperty(ref _GroupOption, viewModel.DirectoryGroupOption, nameof(GroupOption));
				SetProperty(ref _GroupDirection, viewModel.DirectoryGroupDirection, nameof(GroupDirection));
				SetProperty(ref _GroupByDateUnit, viewModel.DirectoryGroupByDateUnit, nameof(GroupByDateUnit));
				SetProperty(ref _SortDirectoriesAlongsideFiles, viewModel.SortDirectoriesAlongsideFiles, nameof(SortDirectoriesAlongsideFiles));
				SetProperty(ref _SortFilesFirst, viewModel.SortFilesFirst, nameof(SortFilesFirst));
			}
		}

		private LayoutTypes GetLayoutType()
		{
			var viewModel = FolderSettings;
			if (viewModel is null)
				return LayoutTypes.None;

			bool isAdaptive = IsLayoutAdaptiveEnabled && viewModel.IsAdaptiveLayoutEnabled && !viewModel.IsLayoutModeFixed;
			if (isAdaptive)
				return LayoutTypes.Adaptive;

			return viewModel.LayoutMode switch
			{
				FolderLayoutModes.DetailsView => LayoutTypes.Details,
				FolderLayoutModes.ListView => LayoutTypes.List,
				FolderLayoutModes.CardsView => LayoutTypes.Cards,
				FolderLayoutModes.GridView => LayoutTypes.Grid,
				FolderLayoutModes.ColumnView => LayoutTypes.Columns,
				_ => throw new InvalidEnumArgumentException(),
			};
		}
	}
}