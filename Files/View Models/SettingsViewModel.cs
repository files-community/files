using Files.Common;
using Files.Controllers;
using Files.DataModels;
using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.Views;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.AppCenter.Analytics;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarSymbols;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.Globalization;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Files.View_Models
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ApplicationDataContainer _roamingSettings;
        private readonly ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public DrivesManager DrivesManager { get; }

        public TerminalController TerminalController { get; set; }

        public SettingsViewModel()
        {
            _roamingSettings = ApplicationData.Current.RoamingSettings;

            DetectOneDrivePreference();
            DetectAcrylicPreference();
            DetectDateTimeFormat();
            PinSidebarLocationItems();
            DetectRecycleBinPreference();
            DetectQuickLook();
            DetectGridViewSize();
            DrivesManager = new DrivesManager();

            //DetectWSLDistros();
            TerminalController = new TerminalController();

            // Send analytics
            Analytics.TrackEvent("DisplayedTimeStyle " + DisplayedTimeStyle.ToString());
            Analytics.TrackEvent("ThemeValue " + ThemeHelper.RootTheme.ToString());
            Analytics.TrackEvent("PinOneDriveToSideBar " + PinOneDriveToSideBar.ToString());
            Analytics.TrackEvent("PinRecycleBinToSideBar " + PinRecycleBinToSideBar.ToString());
            Analytics.TrackEvent("DoubleTapToRenameFiles " + DoubleTapToRenameFiles.ToString());
            Analytics.TrackEvent("ShowFileExtensions " + ShowFileExtensions.ToString());
            Analytics.TrackEvent("ShowConfirmDeleteDialog " + ShowConfirmDeleteDialog.ToString());
            Analytics.TrackEvent("AcrylicSidebar " + AcrylicEnabled.ToString());
            Analytics.TrackEvent("ShowFileOwner " + ShowFileOwner.ToString());
            // Load the supported languages

            var supportedLang = ApplicationLanguages.ManifestLanguages;
            DefaultLanguages = new ObservableCollection<DefaultLanguageModel> { new DefaultLanguageModel(null) };
            foreach (var lang in supportedLang)
            {
                DefaultLanguages.Add(new DefaultLanguageModel(lang));
            }
        }

        public DefaultLanguageModel CurrentLanguage = new DefaultLanguageModel(ApplicationLanguages.PrimaryLanguageOverride);

        public ObservableCollection<DefaultLanguageModel> DefaultLanguages { get; }

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

        public SortOption DirectorySortOption
        {
            get
            {
                try
                {
                    var path = App.CurrentInstance.FilesystemViewModel?.WorkingDirectory;
                    if (path != null)
                    {
                        ApplicationDataContainer dataContainer = localSettings.CreateContainer(LocalSettings.SortOptionContainer, ApplicationDataCreateDisposition.Always);
                        if (dataContainer.Values.ContainsKey(path))
                        {
                            _DirectorySortOption = (SortOption)(byte)dataContainer.Values[path];
                        }
                        else
                        {
                            dataContainer.Values[path] = (byte)_DirectorySortOption;
                        }
                    }

                    return _DirectorySortOption;
                }
                catch
                {
                    return _DirectorySortOption;
                }
            }
            set
            {
                try
                {
                    _DirectorySortOption = value;
                    ApplicationDataContainer dataContainer = localSettings.CreateContainer(LocalSettings.SortOptionContainer, ApplicationDataCreateDisposition.Always);
                    dataContainer.Values[App.CurrentInstance.FilesystemViewModel?.WorkingDirectory] = (byte)_DirectorySortOption;
                    App.CurrentInstance?.FilesystemViewModel?.UpdateSortOptionStatus();
                }
                catch
                {
                    return;
                }
            }
        }

        public SortDirection DirectorySortDirection
        {
            get
            {
                try
                {
                    var path = App.CurrentInstance.FilesystemViewModel?.WorkingDirectory;
                    if (path != null)
                    {
                        ApplicationDataContainer dataContainer = localSettings.CreateContainer(LocalSettings.SortDirectionContainer, ApplicationDataCreateDisposition.Always);
                        if (dataContainer.Values.ContainsKey(path))
                        {
                            _DirectorySortDirection = (SortDirection)(byte)dataContainer.Values[path];
                        }
                        else
                        {
                            dataContainer.Values[path] = (byte)_DirectorySortDirection;
                        }
                    }

                    return _DirectorySortDirection;
                }
                catch
                {
                    return _DirectorySortDirection;
                }
            }
            set
            {
                try
                {
                    _DirectorySortDirection = value;
                    ApplicationDataContainer dataContainer = localSettings.CreateContainer(LocalSettings.SortDirectionContainer, ApplicationDataCreateDisposition.Always);
                    dataContainer.Values[App.CurrentInstance.FilesystemViewModel?.WorkingDirectory] = (byte)_DirectorySortDirection;
                    App.CurrentInstance?.FilesystemViewModel?.UpdateSortDirectionStatus();
                }
                catch
                {
                    return;
                }
            }
        }

        private SortOption _DirectorySortOption = SortOption.Name;
        private SortDirection _DirectorySortDirection = SortDirection.Ascending;

        public async void DetectQuickLook()
        {
            // Detect QuickLook
            ApplicationData.Current.LocalSettings.Values["Arguments"] = "StartupTasks";
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
        }

        private void PinSidebarLocationItems()
        {
            AddDefaultLocations();
        }

        private void AddDefaultLocations()
        {
            MainPage.sideBarItems.Add(new LocationItem { Text = ResourceController.GetTranslation("SidebarHome"), Font = App.Current.Resources["FluentUIGlyphs"] as FontFamily, Glyph = "\uea80", IsDefaultLocation = true, Path = "Home" });
        }

        private async void DetectWSLDistros()
        {
            try
            {
                var distroFolder = await StorageFolder.GetFolderFromPathAsync(@"\\wsl$\");
                if ((await distroFolder.GetFoldersAsync()).Count > 0)
                {
                    AreLinuxFilesSupported = false;
                }

                foreach (StorageFolder folder in await distroFolder.GetFoldersAsync())
                {
                    Uri logoURI = null;
                    if (folder.DisplayName.Contains("ubuntu", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/ubuntupng.png");
                    }
                    else if (folder.DisplayName.Contains("kali", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/kalipng.png");
                    }
                    else if (folder.DisplayName.Contains("debian", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/debianpng.png");
                    }
                    else if (folder.DisplayName.Contains("opensuse", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/opensusepng.png");
                    }
                    else if (folder.DisplayName.Contains("alpine", StringComparison.OrdinalIgnoreCase))
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/alpinepng.png");
                    }
                    else
                    {
                        logoURI = new Uri("ms-appx:///Assets/WSL/genericpng.png");
                    }

                    MainPage.sideBarItems.Add(new WSLDistroItem() { Text = folder.DisplayName, Path = folder.Path, Logo = logoURI });
                }
            }
            catch (Exception)
            {
                // WSL Not Supported/Enabled
                AreLinuxFilesSupported = false;
            }
        }

        private void DetectDateTimeFormat()
        {
            if (localSettings.Values[LocalSettings.DateTimeFormat] != null)
            {
                if (localSettings.Values[LocalSettings.DateTimeFormat].ToString() == "Application")
                {
                    DisplayedTimeStyle = TimeStyle.Application;
                }
                else if (localSettings.Values[LocalSettings.DateTimeFormat].ToString() == "System")
                {
                    DisplayedTimeStyle = TimeStyle.System;
                }
            }
            else
            {
                localSettings.Values[LocalSettings.DateTimeFormat] = "Application";
            }
        }

        private TimeStyle _DisplayedTimeStyle = TimeStyle.Application;

        public TimeStyle DisplayedTimeStyle
        {
            get => _DisplayedTimeStyle;
            set
            {
                Set(ref _DisplayedTimeStyle, value);
                if (value.Equals(TimeStyle.Application))
                {
                    localSettings.Values[LocalSettings.DateTimeFormat] = "Application";
                }
                else if (value.Equals(TimeStyle.System))
                {
                    localSettings.Values[LocalSettings.DateTimeFormat] = "System";
                }
            }
        }

        private FormFactorMode _FormFactor = FormFactorMode.Regular;

        public FormFactorMode FormFactor
        {
            get => _FormFactor;
            set => Set(ref _FormFactor, value);
        }

        public string OneDrivePath = Environment.GetEnvironmentVariable("OneDrive");

        private async void DetectOneDrivePreference()
        {
            if (localSettings.Values["PinOneDrive"] == null) { localSettings.Values["PinOneDrive"] = true; }

            if ((bool)localSettings.Values["PinOneDrive"] == true)
            {
                PinOneDriveToSideBar = true;
            }
            else
            {
                PinOneDriveToSideBar = false;
            }

            try
            {
                await StorageFolder.GetFolderFromPathAsync(OneDrivePath);
            }
            catch (Exception)
            {
                PinOneDriveToSideBar = false;
            }
        }

        public bool ShowFileOwner
        {
            get => Get(false);
            set => Set(value);
        }

        private bool _PinOneDriveToSideBar = true;

        public bool PinOneDriveToSideBar
        {
            get => _PinOneDriveToSideBar;
            set
            {
                if (value != _PinOneDriveToSideBar)
                {
                    Set(ref _PinOneDriveToSideBar, value);
                    if (value == true)
                    {
                        localSettings.Values["PinOneDrive"] = true;
                        var oneDriveItem = new DriveItem()
                        {
                            Text = "OneDrive",
                            Path = OneDrivePath,
                            Type = Filesystem.DriveType.VirtualDrive,
                        };
                        MainPage.sideBarItems.Add(oneDriveItem);
                    }
                    else
                    {
                        localSettings.Values["PinOneDrive"] = false;
                        foreach (INavigationControlItem item in MainPage.sideBarItems.ToList())
                        {
                            if (item is DriveItem && item.ItemType == NavigationControlItemType.OneDrive)
                            {
                                MainPage.sideBarItems.Remove(item);
                            }
                        }
                    }
                }
            }
        }

        // Any distinguishable path here is fine
        // Currently is the command to open the folder from cmd ("cmd /c start Shell:RecycleBinFolder")
        public string RecycleBinPath = @"Shell:RecycleBinFolder";

        private void DetectRecycleBinPreference()
        {
            if (localSettings.Values["PinRecycleBin"] == null) { localSettings.Values["PinRecycleBin"] = false; }

            if ((bool)localSettings.Values["PinRecycleBin"] == true)
            {
                PinRecycleBinToSideBar = true;
            }
            else
            {
                PinRecycleBinToSideBar = false;
            }
        }

        private bool _PinRecycleBinToSideBar = false;

        public bool PinRecycleBinToSideBar
        {
            get => _PinRecycleBinToSideBar;
            set
            {
                if (value != _PinRecycleBinToSideBar)
                {
                    Set(ref _PinRecycleBinToSideBar, value);
                    if (value == true)
                    {
                        localSettings.Values["PinRecycleBin"] = true;
                        var recycleBinItem = new LocationItem
                        {
                            Text = localSettings.Values.Get("RecycleBin_Title", "Recycle Bin"),
                            Font = Application.Current.Resources["RecycleBinIcons"] as FontFamily,
                            Glyph = "\uEF87",
                            IsDefaultLocation = true,
                            Path = RecycleBinPath
                        };
                        // Add recycle bin to sidebar, title is read from LocalSettings (provided by the fulltrust process)
                        // TODO: the very first time the app is launched localized name not available
                        MainPage.sideBarItems.Insert(MainPage.sideBarItems.Where(item => item is LocationItem).Count(), recycleBinItem);
                    }
                    else
                    {
                        localSettings.Values["PinRecycleBin"] = false;
                        foreach (INavigationControlItem item in MainPage.sideBarItems.ToList())
                        {
                            if (item is LocationItem && item.Path == RecycleBinPath)
                            {
                                MainPage.sideBarItems.Remove(item);
                            }
                        }
                    }
                }
            }
        }

        public string DesktopPath = UserDataPaths.GetDefault().Desktop;

        public string DocumentsPath = UserDataPaths.GetDefault().Documents;

        public string DownloadsPath = UserDataPaths.GetDefault().Downloads;

        public string PicturesPath = UserDataPaths.GetDefault().Pictures;

        public string MusicPath = UserDataPaths.GetDefault().Music;

        public string VideosPath = UserDataPaths.GetDefault().Videos;

        private string _TempPath = (string)Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Environment", "TEMP", null);

        public string TempPath
        {
            get => _TempPath;
            set => Set(ref _TempPath, value);
        }

        private string _LocalAppDataPath = UserDataPaths.GetDefault().LocalAppData;

        public string LocalAppDataPath
        {
            get => _LocalAppDataPath;
            set => Set(ref _LocalAppDataPath, value);
        }

        private string _HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public string HomePath
        {
            get => _HomePath;
            set => Set(ref _HomePath, value);
        }

        private string _WinDirPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        public string WinDirPath
        {
            get => _WinDirPath;
            set => Set(ref _WinDirPath, value);
        }

        public bool DoubleTapToRenameFiles
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowFileExtensions
        {
            get => Get(true);
            set => Set(value);
        }

        public bool ShowConfirmDeleteDialog
        {
            get => Get(true);
            set => Set(value);
        }

        public bool AreLinuxFilesSupported
        {
            get => Get(false);
            set => Set(value);
        }

        public bool IsMultitaskingControlVisible
        {
            get => Get(true);
            set => Set(value);
        }

        public bool OpenNewTabPageOnStartup
        {
            get => Get(true);
            set => Set(value);
        }

        public bool OpenASpecificPageOnStartup
        {
            get => Get(false);
            set => Set(value);
        }

        public string OpenASpecificPageOnStartupPath
        {
            get => Get("");
            set => Set(value);
        }

        public bool AlwaysOpenANewInstance
        {
            get => Get(false);
            set => Set(value);
        }

        private void DetectAcrylicPreference()
        {
            if (localSettings.Values["AcrylicEnabled"] == null) { localSettings.Values["AcrylicEnabled"] = true; }
            AcrylicEnabled = (bool)localSettings.Values["AcrylicEnabled"];
        }

        private bool _AcrylicEnabled = true;

        public bool AcrylicEnabled
        {
            get => _AcrylicEnabled;
            set
            {
                if (value != _AcrylicEnabled)
                {
                    Set(ref _AcrylicEnabled, value);
                    localSettings.Values["AcrylicEnabled"] = value;
                }
            }
        }

        public event EventHandler ThemeModeChanged;

        public RelayCommand UpdateThemeElements => new RelayCommand(() =>
        {
            ThemeModeChanged?.Invoke(this, EventArgs.Empty);
        });

        public AcrylicTheme AcrylicTheme { get; set; }

        public LayoutMode LayoutMode = LayoutMode.ListView;

        public void SetLayoutModeForCurrentDirectory(LayoutMode layoutMode)
        {
            LayoutMode = layoutMode;
            try
            {
                ApplicationDataContainer dataContainer = localSettings.CreateContainer(LocalSettings.LayoutModeContainer, ApplicationDataCreateDisposition.Always);
                dataContainer.Values[App.CurrentInstance.FilesystemViewModel?.WorkingDirectory] = (byte)LayoutMode;
            }
            catch
            {
                return;
            }
        }

        public Type GetLayoutType(string path)
        {
            try
            {
                ApplicationDataContainer dataContainer = localSettings.CreateContainer(LocalSettings.LayoutModeContainer, ApplicationDataCreateDisposition.Always);
                if (dataContainer.Values.ContainsKey(path))
                {
                    LayoutMode = (LayoutMode)(byte)dataContainer.Values[path];
                }
                else
                {
                    if (path == PicturesPath)
                    {
                        // Set grid view by default for pictures folder
                        LayoutMode = LayoutMode.GridView;
                    }
                    else
                    {
                        LayoutMode = LayoutMode.ListView;
                    }
                    dataContainer.Values[path] = (byte)LayoutMode;
                }
            }
            catch
            {
                LayoutMode = LayoutMode.ListView;
            }

            Type type = null;
            switch (LayoutMode)
            {
                case LayoutMode.ListView:
                    type = typeof(GenericFileBrowser);
                    break;

                case LayoutMode.TilesView:
                    type = typeof(GridViewBrowser);
                    break;

                case LayoutMode.GridView:
                    type = typeof(GridViewBrowser);
                    break;

                default:
                    type = typeof(GenericFileBrowser);
                    break;
            }
            return type;
        }

        public event EventHandler LayoutModeChangeRequested;

        public RelayCommand ToggleLayoutModeGridViewLarge => new RelayCommand(() =>
        {
            SetLayoutModeForCurrentDirectory(LayoutMode.GridView);

            GridViewSize = 375; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeGridViewMedium => new RelayCommand(() =>
        {
            SetLayoutModeForCurrentDirectory(LayoutMode.GridView);

            GridViewSize = 250; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeGridViewSmall => new RelayCommand(() =>
        {
            SetLayoutModeForCurrentDirectory(LayoutMode.GridView);

            GridViewSize = 125; // Size

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeTiles => new RelayCommand(() =>
        {
            SetLayoutModeForCurrentDirectory(LayoutMode.TilesView);

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        public RelayCommand ToggleLayoutModeListView => new RelayCommand(() =>
        {
            SetLayoutModeForCurrentDirectory(LayoutMode.ListView);

            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
        });

        private void DetectGridViewSize()
        {
            _GridViewSize = Get(125, "GridViewSize"); // Get GridView Size
        }

        private int _GridViewSize = 125; // Default Size

        public int GridViewSize
        {
            get => _GridViewSize;
            set
            {
                if (value < _GridViewSize) // Size down
                {
                    if (LayoutMode == LayoutMode.TilesView) // Size down from tiles to list
                    {
                        SetLayoutModeForCurrentDirectory(LayoutMode.ListView);
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else if (LayoutMode == LayoutMode.GridView && value < 125) // Size down from grid to tiles
                    {
                        SetLayoutModeForCurrentDirectory(LayoutMode.TilesView);
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else if (LayoutMode != 0) // Resize grid view
                    {
                        _GridViewSize = (value >= 125) ? value : 125; // Set grid size to allow immediate UI update
                        Set(value);

                        if (LayoutMode != LayoutMode.GridView) // Only update layout mode if it isn't already in grid view
                        {
                            SetLayoutModeForCurrentDirectory(LayoutMode.GridView);
                            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }

                        GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                }
                else // Size up
                {
                    if (LayoutMode == LayoutMode.ListView) // Size up from list to tiles
                    {
                        SetLayoutModeForCurrentDirectory(LayoutMode.TilesView);
                        LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                    else // Size up from tiles to grid
                    {
                        _GridViewSize = (LayoutMode == LayoutMode.TilesView) ? 125 : (value <= 375) ? value : 375; // Set grid size to allow immediate UI update
                        Set(_GridViewSize);

                        if (LayoutMode != LayoutMode.GridView) // Only update layout mode if it isn't already in grid view
                        {
                            SetLayoutModeForCurrentDirectory(LayoutMode.GridView);
                            LayoutModeChangeRequested?.Invoke(this, EventArgs.Empty);
                        }

                        if (value < 375) // Don't request a grid resize if it is already at the max size (375)
                            GridViewSizeChangeRequested?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        public event EventHandler GridViewSizeChangeRequested;

        public void Dispose()
        {
            DrivesManager.Dispose();
        }

        public bool Set<TValue>(TValue value, [CallerMemberName] string propertyName = null)
        {
            propertyName = propertyName != null && propertyName.StartsWith("set_", StringComparison.InvariantCultureIgnoreCase)
                ? propertyName.Substring(4)
                : propertyName;

            TValue originalValue = default;

            if (_roamingSettings.Values.ContainsKey(propertyName))
            {
                originalValue = Get(originalValue, propertyName);

                _roamingSettings.Values[propertyName] = value;
                if (!base.Set(ref originalValue, value, propertyName)) return false;
            }
            else
            {
                _roamingSettings.Values[propertyName] = value;
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

            if (_roamingSettings.Values.ContainsKey(name))
            {
                var value = _roamingSettings.Values[name];

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

                        if (tryParse == null) return default;

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

            return defaultValue;
        }

        private delegate bool TryParseDelegate<TValue>(string inValue, out TValue parsedValue);

        public string[] PagesOnStartupList
        {
            get => Get<string[]>(null);
            set => Set(value);
        }
    }
}