using Files.Common;
using Files.Controllers;
using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;

namespace Files.ViewModels
{
    public class SettingsViewModel : ObservableObject
    {
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public CloudDrivesManager CloudDrivesManager { get; private set; }

        public TerminalController TerminalController { get; set; }

        private async Task<SettingsViewModel> Initialize()
        {
            DetectDateTimeFormat();
            DetectQuickLook();

            // Load the supported languages
            var supportedLang = ApplicationLanguages.ManifestLanguages;
            DefaultLanguages = new ObservableCollection<DefaultLanguageModel> { new DefaultLanguageModel(null) };
            foreach (var lang in supportedLang)
            {
                DefaultLanguages.Add(new DefaultLanguageModel(lang));
            }

            TerminalController = await TerminalController.CreateInstance();

            FileTagsSettings = new FileTagsSettings();

            // Send analytics to AppCenter
            StartAppCenter();
            return this;
        }

        public static Task<SettingsViewModel> CreateInstance()
        {
            var settings = new SettingsViewModel();
            return settings.Initialize();
        }

        private SettingsViewModel()
        {
        }

        private async void StartAppCenter()
        {
            JObject obj;
            try
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri(@"ms-appx:///Resources/AppCenterKey.txt"));
                var lines = await FileIO.ReadTextAsync(file);
                obj = JObject.Parse(lines);
            }
            catch
            {
                return;
            }

            AppCenter.Start((string)obj.SelectToken("key"), typeof(Analytics), typeof(Crashes));

            Analytics.TrackEvent($"{nameof(DisplayedTimeStyle)} {DisplayedTimeStyle}");
            Analytics.TrackEvent($"{nameof(ThemeHelper.RootTheme)} {ThemeHelper.RootTheme}");
            Analytics.TrackEvent($"{nameof(PinRecycleBinToSideBar)} {PinRecycleBinToSideBar}");
            Analytics.TrackEvent($"{nameof(ShowFileExtensions)} {ShowFileExtensions}");
            Analytics.TrackEvent($"{nameof(ShowConfirmDeleteDialog)} {ShowConfirmDeleteDialog}");
            Analytics.TrackEvent($"{nameof(IsVerticalTabFlyoutEnabled)} {IsVerticalTabFlyoutEnabled}");
            Analytics.TrackEvent($"{nameof(IsDualPaneEnabled)} {IsDualPaneEnabled}");
            Analytics.TrackEvent($"{nameof(AlwaysOpenDualPaneInNewTab)} {AlwaysOpenDualPaneInNewTab}");
            Analytics.TrackEvent($"{nameof(AreHiddenItemsVisible)} {AreHiddenItemsVisible}");
            Analytics.TrackEvent($"{nameof(AreLayoutPreferencesPerFolder)} {AreLayoutPreferencesPerFolder}");
            Analytics.TrackEvent($"{nameof(ShowDrivesWidget)} {ShowDrivesWidget}");
            Analytics.TrackEvent($"{nameof(ShowLibrarySection)} {ShowLibrarySection}");
            Analytics.TrackEvent($"{nameof(ShowBundlesWidget)} {ShowBundlesWidget}");
            Analytics.TrackEvent($"{nameof(ListAndSortDirectoriesAlongsideFiles)} {ListAndSortDirectoriesAlongsideFiles}");
            Analytics.TrackEvent($"{nameof(AreFileTagsEnabled)} {AreFileTagsEnabled}");
        }

        public static async void OpenLogLocation()
        {
            await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder);
        }

        public static async void OpenThemesFolder() => await NavigationHelpers.OpenPathInNewTab(App.ExternalResourcesHelper.ThemeFolder.Path);

        public static async void ReportIssueOnGitHub()
        {
            await Launcher.LaunchUriAsync(new Uri(@"https://github.com/files-community/Files/issues/new/choose"));
        }

        /// <summary>
        /// Gets or sets a value indicating the width of the the sidebar pane when open.
        /// </summary>
        public GridLength SidebarWidth
        {
            get => new GridLength(Math.Min(Math.Max(Get(255d), Constants.UI.MinimumSidebarWidth), 500d), GridUnitType.Pixel);
            set => Set(value.Value);
        }

        /// <summary>
        /// Gets or sets a value indicating if the sidebar pane should be open or closed.
        /// </summary>
        public bool IsSidebarOpen
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating the height of the preview pane in a horizontal layout.
        /// </summary>
        public GridLength PreviewPaneSizeHorizontal
        {
            get => new GridLength(Math.Min(Math.Max(Get(300d), 50d), 600d), GridUnitType.Pixel);
            set => Set(value.Value);
        }

        /// <summary>
        /// Gets or sets a value indicating the width of the preview pane in a vertical layout.
        /// </summary>
        public GridLength PreviewPaneSizeVertical
        {
            get => new GridLength(Math.Min(Math.Max(Get(250d), 50d), 600d), GridUnitType.Pixel);
            set => Set(value.Value);
        }

        /// <summary>
        /// Gets or sets a value indicating if the preview pane should be open or closed.
        /// </summary>
        public bool PreviewPaneEnabled
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating if the preview pane should only show the item preview without the details section
        /// </summary>
        public bool ShowPreviewOnly
        {
            get => Get(false);
            set => Set(value);
        }

        public async void DetectQuickLook()
        {
            // Detect QuickLook
            try
            {
                var connection = await AppServiceConnectionHelper.Instance;
                if (connection != null)
                {
                    var (status, response) = await connection.SendMessageForResponseAsync(new ValueSet()
                    {
                        { "Arguments", "DetectQuickLook" }
                    });
                    if (status == AppServiceResponseStatus.Success)
                    {
                        localSettings.Values["quicklook_enabled"] = response.Get("IsAvailable", false);
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.Warn(ex, ex.Message);
            }
        }

        private void DetectDateTimeFormat()
        {
            if (localSettings.Values[Constants.LocalSettings.DateTimeFormat] != null)
            {
                if (localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString() == "Application")
                {
                    DisplayedTimeStyle = TimeStyle.Application;
                }
                else if (localSettings.Values[Constants.LocalSettings.DateTimeFormat].ToString() == "System")
                {
                    DisplayedTimeStyle = TimeStyle.System;
                }
            }
            else
            {
                localSettings.Values[Constants.LocalSettings.DateTimeFormat] = "Application";
            }
        }

        private TimeStyle displayedTimeStyle = TimeStyle.Application;

        public TimeStyle DisplayedTimeStyle
        {
            get => displayedTimeStyle;
            set
            {
                SetProperty(ref displayedTimeStyle, value);
                if (value.Equals(TimeStyle.Application))
                {
                    localSettings.Values[Constants.LocalSettings.DateTimeFormat] = "Application";
                }
                else if (value.Equals(TimeStyle.System))
                {
                    localSettings.Values[Constants.LocalSettings.DateTimeFormat] = "System";
                }
            }
        }

        #region DetailsView Column Settings

        /// <summary>
        /// Gets or sets a value indicating whether or not the date column should be visible.
        /// </summary>
        public bool ShowDateColumn
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the date created column should be visible.
        /// </summary>
        public bool ShowDateCreatedColumn
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the type column should be visible.
        /// </summary>
        public bool ShowTypeColumn
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the size column should be visible.
        /// </summary>
        public bool ShowSizeColumn
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the filetag column should be visible.
        /// </summary>
        public bool ShowFileTagColumn
        {
            get => Get(true);
            set => Set(value);
        }

        #endregion DetailsView Column Settings

        #region CommonPaths

        public string DesktopPath { get; set; } = UserDataPaths.GetDefault().Desktop;
        public string DownloadsPath { get; set; } = UserDataPaths.GetDefault().Downloads;

        private string tempPath = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Environment", "TEMP", null);

        public string TempPath
        {
            get => tempPath;
            set => SetProperty(ref tempPath, value);
        }

        private string localAppDataPath = UserDataPaths.GetDefault().LocalAppData;

        public string LocalAppDataPath
        {
            get => localAppDataPath;
            set => SetProperty(ref localAppDataPath, value);
        }

        private string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public string HomePath
        {
            get => homePath;
            set => SetProperty(ref homePath, value);
        }

        // Currently is the command to open the folder from cmd ("cmd /c start Shell:RecycleBinFolder")
        public string RecycleBinPath { get; set; } = @"Shell:RecycleBinFolder";

        public string NetworkFolderPath { get; set; } = @"Shell:NetworkPlacesFolder";

        #endregion CommonPaths

        #region FilesAndFolder

        /// <summary>
        /// Gets or sets a value indicating whether or not file extensions should be visible.
        /// </summary>
        public bool ShowFileExtensions
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not hidden items should be visible.
        /// </summary>
        public bool AreHiddenItemsVisible
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not system items should be visible.
        /// </summary>
        public bool AreSystemItemsHidden
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not files should be sorted together with folders.
        /// </summary>
        public bool ListAndSortDirectoriesAlongsideFiles
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not items should open with one click.
        /// </summary>
        public bool OpenItemsWithOneclick
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to search unindexed items.
        /// </summary>
        public bool SearchUnindexedItems
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Enables saving a unique layout mode, gridview size and sort direction per folder
        /// </summary>
        public bool AreLayoutPreferencesPerFolder
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Enables adaptive layout that adjusts layout mode based on the context of the directory
        /// </summary>
        public bool AdaptiveLayoutEnabled
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to enable file tags feature.
        /// </summary>
        public bool AreFileTagsEnabled
        {
            get => Get(false);
            set => Set(value);
        }

        #endregion FilesAndFolder

        #region Multitasking

        /// <summary>
        /// Gets or sets a value indicating whether or not to enable the vertical tab flyout.
        /// </summary>
        public bool IsVerticalTabFlyoutEnabled
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to enable dual pane feature.
        /// </summary>
        public bool IsDualPaneEnabled
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to always open a second pane when opening a new tab.
        /// </summary>
        public bool AlwaysOpenDualPaneInNewTab
        {
            get => Get(false);
            set => Set(value);
        }

        #endregion Multitasking

        #region Widgets

        /// <summary>
        /// Gets or sets a value indicating whether or not the library cards widget should be visible.
        /// </summary>
        public bool ShowFolderWidgetWidget
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the recent files widget should be visible.
        /// </summary>
        public bool ShowRecentFilesWidget
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the drives widget should be visible.
        /// </summary>
        public bool ShowDrivesWidget
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the Bundles widget should be visible.
        /// </summary>
        public bool ShowBundlesWidget
        {
            get => Get(false);
            set => Set(value);
        }

        #endregion Widgets

        #region Sidebar

        /// <summary>
        /// Gets or sets a value indicating whether or not to show the Favorites section on the sidebar.
        /// </summary>
        public bool ShowFavoritesSection
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to show the library section on the sidebar.
        /// </summary>
        public bool ShowLibrarySection
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [show drives section].
        /// </summary>
        public bool ShowDrivesSection
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [show cloud drives section].
        /// </summary>
        public bool ShowCloudDrivesSection
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [show network drives section].
        /// </summary>
        public bool ShowNetworkDrivesSection
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [show wsl section].
        /// </summary>
        public bool ShowWslSection
        {
            get => Get(true);
            set => Set(value);
        }

        //TODO: This shouldn't pin recycle bin to the sidebar, it should only hold the value whether it should or shouldn't be pinned
        /// <summary>
        /// Gets or sets a value indicating whether or not recycle bin should be pinned to the sidebar.
        /// </summary>
        public bool PinRecycleBinToSideBar
        {
            get => Get(true);
            set
            {
                if (Set(value))
                {
                    _ = App.SidebarPinnedController.Model.ShowHideRecycleBinItemAsync(value);
                }
            }
        }

        #endregion Sidebar

        #region Preferences

        /// <summary>
        /// Gets or sets a value indicating whether or not the confirm delete dialog should show when deleting items.
        /// </summary>
        public bool ShowConfirmDeleteDialog
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [open folders new tab].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [open folders new tab]; otherwise, <c>false</c>.
        /// </value>
        public bool OpenFoldersNewTab
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating the application language.
        /// </summary>
        public DefaultLanguageModel CurrentLanguage { get; set; } = new DefaultLanguageModel(ApplicationLanguages.PrimaryLanguageOverride);

        /// <summary>
        /// Gets or sets an ObservableCollection of the support languages.
        /// </summary>
        public ObservableCollection<DefaultLanguageModel> DefaultLanguages { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating the default language.
        /// </summary>
        public DefaultLanguageModel DefaultLanguage
        {
            get
            {
                return DefaultLanguages.FirstOrDefault(dl => dl.ID == ApplicationLanguages.PrimaryLanguageOverride) ??
                           DefaultLanguages.FirstOrDefault();
            }
            set
            {
                ApplicationLanguages.PrimaryLanguageOverride = value.ID;
            }
        }

        #endregion Preferences

        #region Appearance

        /// <summary>
        /// Gets or sets a value indicating whether or not to move overflow menu items into a sub menu.
        /// </summary>
        public bool MoveOverflowMenuItemsToSubMenu
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets the user's current selected skin
        /// </summary>
        public AppTheme SelectedTheme
        {
            get => Newtonsoft.Json.JsonConvert.DeserializeObject<AppTheme>(Get(System.Text.Json.JsonSerializer.Serialize(new AppTheme()
            {
                Name = "DefaultTheme".GetLocalized()
            })));
            set => Set(Newtonsoft.Json.JsonConvert.SerializeObject(value));
        }

        #endregion Appearance

        #region Startup

        /// <summary>
        /// Gets or sets a value indicating whether or not to navigate to a specific location when launching the app.
        /// </summary>
        public bool OpenASpecificPageOnStartup
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating the default startup location.
        /// </summary>
        public string OpenASpecificPageOnStartupPath
        {
            get => Get("");
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not continue the last session whenever the app is launched.
        /// </summary>
        public bool ContinueLastSessionOnStartUp
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to open a page when the app is launched.
        /// </summary>
        public bool OpenNewTabPageOnStartup
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not opening the app from the jumplist should open the directory in a new instance.
        /// </summary>
        public bool AlwaysOpenANewInstance
        {
            get => Get(false);
            set => Set(value);
        }

        public string[] PagesOnStartupList
        {
            get => Get<string[]>(null);
            set => Set(value);
        }

        public string[] LastSessionPages
        {
            get => Get<string[]>(null);
            set => Set(value);
        }

        #endregion Startup

        /// <summary>
        /// Gets or sets a value indicating whether or not to show a teaching tip informing the user about the status center.
        /// </summary>
        public bool ShowOngoingTasksTeachingTip
        {
            get => Get(true);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to restore tabs after restarting the app.
        /// </summary>
        public bool ResumeAfterRestart
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to show the confirm elevation dialog.
        /// </summary>
        public bool HideConfirmElevateDialog
        {
            get => Get(false);
            set => Set(value);
        }

        /// <summary>
        /// Gets or sets a value indicating the default volume on media preview.
        /// </summary>
        public double MediaVolume
        {
            get => Math.Min(Math.Max(Get(1.0d), 0.0d), 1.0d);
            set => Set(value);
        }

        public event EventHandler ThemeModeChanged;

        public RelayCommand UpdateThemeElements => new RelayCommand(() =>
        {
            ThemeModeChanged?.Invoke(this, EventArgs.Empty);
        });

        public AcrylicTheme AcrylicTheme { get; set; } = new AcrylicTheme();

        public FolderLayoutModes DefaultLayoutMode
        {
            get => (FolderLayoutModes)Get((byte)FolderLayoutModes.DetailsView); // Details View
            set => Set((byte)value);
        }

        public int DefaultGridViewSize
        {
            get => Get(Constants.Browser.GridViewBrowser.GridViewSizeSmall);
            set => Set(value);
        }

        public SortDirection DefaultDirectorySortDirection
        {
            get => (SortDirection)Get((byte)SortDirection.Ascending);
            set => Set((byte)value);
        }

        public SortOption DefaultDirectorySortOption
        {
            get => (SortOption)Get((byte)SortOption.Name);
            set => Set((byte)value);
        }

        public GroupOption DefaultDirectoryGroupOption
        {
            get => (GroupOption)Get((byte)GroupOption.None);
            set => Set((byte)value);
        }

        public FileTagsSettings FileTagsSettings { get; private set; }

        #region ReadAndSaveSettings

        public bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = null)
        {
            propertyName = propertyName != null && propertyName.StartsWith("set_", StringComparison.InvariantCultureIgnoreCase)
                ? propertyName.Substring(4)
                : propertyName;

            TValue originalValue = default;

            if (localSettings.Values.ContainsKey(propertyName))
            {
                originalValue = Get(originalValue, propertyName);

                localSettings.Values[propertyName] = value;
                if (!base.SetProperty(ref originalValue, value, propertyName))
                {
                    return false;
                }
            }
            else
            {
                localSettings.Values[propertyName] = value;
            }

            return true;
        }

        public TValue Get<TValue>(TValue defaultValue, [CallerMemberName] string propertyName = null)
        {
            var name = propertyName ??
                       throw new ArgumentNullException(nameof(propertyName), "Cannot store property of unnamed.");

            name = name.StartsWith("get_", StringComparison.InvariantCultureIgnoreCase)
                ? propertyName.Substring(4)
                : propertyName;

            if (localSettings.Values.ContainsKey(name))
            {
                var value = localSettings.Values[name];

                if (!(value is TValue tValue))
                {
                    if (value is IConvertible)
                    {
                        tValue = (TValue)Convert.ChangeType(value, typeof(TValue));
                    }
                    else
                    {
                        var valueType = value.GetType();
                        var tryParse = typeof(TValue).GetMethod("TryParse", BindingFlags.Instance | BindingFlags.Public);

                        if (tryParse == null)
                        {
                            return default;
                        }

                        var stringValue = value.ToString();
                        tValue = default;

                        var tryParseDelegate =
                            (TryParseDelegate<TValue>)Delegate.CreateDelegate(valueType, tryParse, false);

                        tValue = (tryParseDelegate?.Invoke(stringValue, out tValue) ?? false) ? tValue : default;
                    }

                    Set(tValue, propertyName); // Put the corrected value in settings.
                    return tValue;
                }
                return tValue;
            }

            localSettings.Values[propertyName] = defaultValue;

            return defaultValue;
        }

        internal static void CopyVersionInfo()
        {
            throw new NotImplementedException();
        }

        private delegate bool TryParseDelegate<TValue>(string inValue, out TValue parsedValue);

        #endregion ReadAndSaveSettings
    }
}