// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;
using Windows.Storage;
using Windows.System;
using static Files.App.Helpers.MenuFlyoutHelper;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;

namespace Files.App.ViewModels.Settings
{
	public sealed class GeneralViewModel : ObservableObject, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();

		private bool disposed;

		private ReadOnlyCollection<IMenuFlyoutItemViewModel> addFlyoutItemsSource;

		public RelayCommand ChangePageCommand { get; }
		public RelayCommand<PageOnStartupViewModel> RemovePageCommand { get; }
		public RelayCommand<string> AddPageCommand { get; }
		public RelayCommand RestartCommand { get; }
		public RelayCommand CancelRestartCommand { get; }

		private bool showRestartControl;
		public bool ShowRestartControl
		{
			get => showRestartControl;
			set => SetProperty(ref showRestartControl, value);
		}

		private int selectedPageIndex = -1;
		public int SelectedPageIndex
		{
			get => selectedPageIndex;
			set
			{
				if (SetProperty(ref selectedPageIndex, value))
					IsPageListEditEnabled = value >= 0;
			}
		}

		private bool isPageListEditEnabled;
		public bool IsPageListEditEnabled
		{
			get => isPageListEditEnabled;
			set => SetProperty(ref isPageListEditEnabled, value);
		}

		private int selectedDateTimeFormatIndex;
		public int SelectedDateTimeFormatIndex
		{
			get => selectedDateTimeFormatIndex;
			set
			{
				if (SetProperty(ref selectedDateTimeFormatIndex, value))
				{
					OnPropertyChanged(nameof(SelectedDateTimeFormatIndex));
					DateTimeFormat = (DateTimeFormats)value;
				}
			}
		}

		private int selectedAppLanguageIndex;
		public int SelectedAppLanguageIndex
		{
			get => selectedAppLanguageIndex;
			set
			{
				if (AppLanguageHelper.TryChange(value))
				{
					selectedAppLanguageIndex = value;
					OnPropertyChanged(nameof(SelectedAppLanguageIndex));
					ShowRestartControl = true;
				}
			}
		}

		public List<DateTimeFormatItem> DateFormats { get; set; }

		public ObservableCollection<AppLanguageItem> AppLanguages => AppLanguageHelper.SupportedLanguages;

		public Dictionary<ShellPaneArrangement, string> ShellPaneArrangementTypes { get; private set; } = [];

		public GeneralViewModel()
		{
			ChangePageCommand = new RelayCommand(ChangePageAsync);
			RemovePageCommand = new RelayCommand<PageOnStartupViewModel>(RemovePage);
			AddPageCommand = new RelayCommand<string>(async (path) => await AddPageAsync(path));
			RestartCommand = new RelayCommand(DoRestartAsync);
			CancelRestartCommand = new RelayCommand(DoCancelRestart);

			selectedAppLanguageIndex = AppLanguageHelper.SupportedLanguages.IndexOf(AppLanguageHelper.PreferredLanguage);

			AddDateTimeOptions();
			SelectedDateTimeFormatIndex = (int)Enum.Parse(typeof(DateTimeFormats), DateTimeFormat.ToString());

			dispatcherQueue = DispatcherQueue.GetForCurrentThread();

			if (UserSettingsService.GeneralSettingsService.TabsOnStartupList is not null)
				PagesOnStartupList = new ObservableCollection<PageOnStartupViewModel>(UserSettingsService.GeneralSettingsService.TabsOnStartupList.Select((p) => new PageOnStartupViewModel(p)));
			else
				PagesOnStartupList = [];

			PagesOnStartupList.CollectionChanged += PagesOnStartupList_CollectionChanged;

			// ShellPaneArrangement
			ShellPaneArrangementTypes.Add(ShellPaneArrangement.Horizontal, "Horizontal".GetLocalizedResource());
			ShellPaneArrangementTypes.Add(ShellPaneArrangement.Vertical, "Vertical".GetLocalizedResource());
			SelectedShellPaneArrangementType = ShellPaneArrangementTypes[UserSettingsService.GeneralSettingsService.ShellPaneArrangementOption];

			InitStartupSettingsRecentFoldersFlyout();
		}

		private async void DoRestartAsync()
		{
			// Tells the app to restore tabs when it's next launched
			UserSettingsService.AppSettingsService.RestoreTabsOnStartup = true;

			// Save the updated tab list before restarting
			AppLifecycleHelper.SaveSessionTabs();

			// Launches a new instance of Files
			await Launcher.LaunchUriAsync(new Uri("files-dev:"));

			// Closes the current instance
			Process.GetCurrentProcess().Kill();
		}

		private void DoCancelRestart()
		{
			ShowRestartControl = false;
		}

		private void AddDateTimeOptions()
		{
			DateTimeOffset sampleDate1 = DateTime.Now.AddSeconds(-5);
			DateTimeOffset sampleDate2 = new DateTime(sampleDate1.Year - 5, 12, 31, 14, 30, 0);

			var styles = new DateTimeFormats[] { DateTimeFormats.Application, DateTimeFormats.System, DateTimeFormats.Universal };

			DateFormats = styles.Select(style => new DateTimeFormatItem(style, sampleDate1, sampleDate2)).ToList();
		}

		private void InitStartupSettingsRecentFoldersFlyout()
		{
			var recentsItem = new MenuFlyoutSubItemViewModel("JumpListRecentGroupHeader".GetLocalizedResource());
			recentsItem.Items.Add(new MenuFlyoutItemViewModel("Home".GetLocalizedResource())
			{
				Command = AddPageCommand,
				CommandParameter = "Home",
				Tooltip = "Home".GetLocalizedResource()
			});
			recentsItem.Items.Add(new MenuFlyoutItemViewModel("Browse".GetLocalizedResource()) { Command = AddPageCommand });
		}

		private void PagesOnStartupList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (PagesOnStartupList.Count > 0)
				UserSettingsService.GeneralSettingsService.TabsOnStartupList = PagesOnStartupList.Select((p) => p.Path).ToList();
			else
				UserSettingsService.GeneralSettingsService.TabsOnStartupList = null;
		}

		public int SelectedStartupSettingIndex => ContinueLastSessionOnStartUp ? 1 : OpenASpecificPageOnStartup ? 2 : 0;

		public bool OpenNewTabPageOnStartup
		{
			get => UserSettingsService.GeneralSettingsService.OpenNewTabOnStartup;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.OpenNewTabOnStartup)
				{
					UserSettingsService.GeneralSettingsService.OpenNewTabOnStartup = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ContinueLastSessionOnStartUp
		{
			get => UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp)
				{
					UserSettingsService.GeneralSettingsService.ContinueLastSessionOnStartUp = value;

					OnPropertyChanged();
				}
			}
		}

		public bool OpenASpecificPageOnStartup
		{
			get => UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup)
				{
					UserSettingsService.GeneralSettingsService.OpenSpecificPageOnStartup = value;

					OnPropertyChanged();
				}
			}
		}

		public ObservableCollection<PageOnStartupViewModel> PagesOnStartupList { get; set; }

		public ReadOnlyCollection<IMenuFlyoutItemViewModel> AddFlyoutItemsSource
		{
			get => addFlyoutItemsSource;
			set => SetProperty(ref addFlyoutItemsSource, value);
		}

		public bool OpenTabInExistingInstance
		{
			get => UserSettingsService.GeneralSettingsService.OpenTabInExistingInstance;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.OpenTabInExistingInstance)
				{
					UserSettingsService.GeneralSettingsService.OpenTabInExistingInstance = value;

					// Needed in Program.cs
					ApplicationData.Current.LocalSettings.Values["OpenTabInExistingInstance"] = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowOpenInNewPane
		{
			get => UserSettingsService.GeneralSettingsService.ShowOpenInNewPane;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowOpenInNewPane)
				{
					UserSettingsService.GeneralSettingsService.ShowOpenInNewPane = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowCreateFolderWithSelection
		{
			get => UserSettingsService.GeneralSettingsService.ShowCreateFolderWithSelection;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowCreateFolderWithSelection)
				{
					UserSettingsService.GeneralSettingsService.ShowCreateFolderWithSelection = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowCopyPath
		{
			get => UserSettingsService.GeneralSettingsService.ShowCopyPath;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowCopyPath)
				{
					UserSettingsService.GeneralSettingsService.ShowCopyPath = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowCreateAlternateDataStream
		{
			get => UserSettingsService.GeneralSettingsService.ShowCreateAlternateDataStream;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowCreateAlternateDataStream)
				{
					UserSettingsService.GeneralSettingsService.ShowCreateAlternateDataStream = value;

					OnPropertyChanged();
				}
			}
		}

		public bool ShowCreateShortcut
		{
			get => UserSettingsService.GeneralSettingsService.ShowCreateShortcut;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowCreateShortcut)
				{
					UserSettingsService.GeneralSettingsService.ShowCreateShortcut = value;

					OnPropertyChanged();
				}
			}
		}

		public bool AlwaysOpenDualPaneInNewTab
		{
			get => UserSettingsService.GeneralSettingsService.AlwaysOpenDualPaneInNewTab;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.AlwaysOpenDualPaneInNewTab)
				{
					UserSettingsService.GeneralSettingsService.AlwaysOpenDualPaneInNewTab = value;

					OnPropertyChanged();
				}
			}
		}

		public bool AlwaysSwitchToNewlyOpenedTab
		{
			get => UserSettingsService.GeneralSettingsService.AlwaysSwitchToNewlyOpenedTab;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.AlwaysSwitchToNewlyOpenedTab)
				{
					UserSettingsService.GeneralSettingsService.AlwaysSwitchToNewlyOpenedTab = value;

					OnPropertyChanged();
				}
			}
		}

		private void ChangePageAsync()
		{
			var result = CommonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, true, [], Environment.SpecialFolder.Desktop, out var filePath);
			if (result && SelectedPageIndex >= 0)
				PagesOnStartupList[SelectedPageIndex] = new PageOnStartupViewModel(filePath);
		}

		private void RemovePage(PageOnStartupViewModel page)
		{
			PagesOnStartupList.Remove(page);
		}

		private async Task AddPageAsync(string path = null)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				bool result = CommonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, true, [], Environment.SpecialFolder.Desktop, out var filePath);
				if (!result)
					return;

				path = filePath;
			}

			if (!string.IsNullOrEmpty(path) && PagesOnStartupList is not null)
				PagesOnStartupList.Add(new PageOnStartupViewModel(path));
		}

		public string DateFormatSample
			=> string.Format("DateFormatSample".GetLocalizedResource(), DateFormats[SelectedDateTimeFormatIndex].Sample1, DateFormats[SelectedDateTimeFormatIndex].Sample2);

		private DispatcherQueue dispatcherQueue;

		public DateTimeFormats DateTimeFormat
		{
			get => UserSettingsService.GeneralSettingsService.DateTimeFormat;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.DateTimeFormat)
				{
					UserSettingsService.GeneralSettingsService.DateTimeFormat = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowQuickAccessWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget)
					UserSettingsService.GeneralSettingsService.ShowQuickAccessWidget = value;
			}
		}

		public bool ShowDrivesWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowDrivesWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowDrivesWidget)
					UserSettingsService.GeneralSettingsService.ShowDrivesWidget = value;
			}
		}

		public bool ShowNetworkLocationsWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowNetworkLocationsWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowNetworkLocationsWidget)
					UserSettingsService.GeneralSettingsService.ShowNetworkLocationsWidget = value;
			}
		}

		public bool ShowFileTagsWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowFileTagsWidget)
				{
					UserSettingsService.GeneralSettingsService.ShowFileTagsWidget = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowRecentFilesWidget
		{
			get => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget)
					UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget = value;
			}
		}

		public bool MoveShellExtensionsToSubMenu
		{
			get => UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
				{
					UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowEditTagsMenu
		{
			get => UserSettingsService.GeneralSettingsService.ShowEditTagsMenu;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowEditTagsMenu)
				{
					UserSettingsService.GeneralSettingsService.ShowEditTagsMenu = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowOpenInNewTab
		{
			get => UserSettingsService.GeneralSettingsService.ShowOpenInNewTab;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowOpenInNewTab)
				{
					UserSettingsService.GeneralSettingsService.ShowOpenInNewTab = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowCompressionOptions
		{
			get => UserSettingsService.GeneralSettingsService.ShowCompressionOptions;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowCompressionOptions)
					return;

				UserSettingsService.GeneralSettingsService.ShowCompressionOptions = value;
				OnPropertyChanged();
			}
		}

		// TODO uncomment code when feature is marked as stable
		//public bool ShowFlattenOptions
		//{
		//	get => UserSettingsService.GeneralSettingsService.ShowFlattenOptions;
		//	set
		//	{
		//		if (value == UserSettingsService.GeneralSettingsService.ShowFlattenOptions)
		//			return;

		//		UserSettingsService.GeneralSettingsService.ShowFlattenOptions = value;
		//		OnPropertyChanged();
		//	}
		//}

		public bool ShowSendToMenu
		{
			get => UserSettingsService.GeneralSettingsService.ShowSendToMenu;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowSendToMenu)
				{
					UserSettingsService.GeneralSettingsService.ShowSendToMenu = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ShowOpenInNewWindow
		{
			get => UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow)
				{
					UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow = value;
					OnPropertyChanged();
				}
			}
		}

		private string selectedShellPaneArrangementType;
		public string SelectedShellPaneArrangementType
		{
			get => selectedShellPaneArrangementType;
			set
			{
				if (SetProperty(ref selectedShellPaneArrangementType, value))
				{
					UserSettingsService.GeneralSettingsService.ShellPaneArrangementOption = ShellPaneArrangementTypes.First(e => e.Value == value).Key;
				}
			}
		}

		public void Dispose()
		{
			if (!disposed)
			{
				disposed = true;

				GC.SuppressFinalize(this);
			}
		}

		~GeneralViewModel()
		{
			Dispose();
		}
	}

	public sealed class PageOnStartupViewModel
	{
		public string Text
		{
			get => ShellHelpers.GetShellNameFromPath(Path);
		}

		public string Path { get; }

		internal PageOnStartupViewModel(string path)
			=> Path = path;
	}

	public sealed class DateTimeFormatItem
	{
		public string Label { get; }

		public string Sample1 { get; }

		public string Sample2 { get; }

		public DateTimeFormatItem(DateTimeFormats style, DateTimeOffset sampleDate1, DateTimeOffset sampleDate2)
		{
			var factory = Ioc.Default.GetRequiredService<IDateTimeFormatterFactory>();
			var formatter = factory.GetDateTimeFormatter(style);

			Label = formatter.Name;
			Sample1 = formatter.ToShortLabel(sampleDate1);
			Sample2 = formatter.ToShortLabel(sampleDate2);
		}
	}
}
