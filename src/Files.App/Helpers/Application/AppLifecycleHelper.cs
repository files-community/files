// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Files.App.Helpers.Application;
using Files.App.Services.SizeProvider;
using Files.App.Utils.Logger;
using Files.App.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.Protocol;
using System.IO;
using System.Text;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper to manage app lifecycle.
	/// </summary>
	public static class AppLifecycleHelper
	{
		/// <summary>
		/// Gets the value that provides application environment or branch name.
		/// </summary>
		public static AppEnvironment AppEnvironment { get; } =
#if STORE
			AppEnvironment.Store;
#elif PREVIEW
			AppEnvironment.Preview;
#elif STABLE
			AppEnvironment.Stable;
#else
			AppEnvironment.Dev;
#endif

		/// <summary>
		/// Gets application package version.
		/// </summary>
		public static Version AppVersion { get; } =
			new(Package.Current.Id.Version.Major, Package.Current.Id.Version.Minor, Package.Current.Id.Version.Build, Package.Current.Id.Version.Revision);

		/// <summary>
		/// Gets application icon path.
		/// </summary>
		public static string AppIconPath { get; } =
			SystemIO.Path.Combine(Package.Current.InstalledLocation.Path, AppEnvironment switch
			{
				AppEnvironment.Dev => Constants.AssetPaths.DevLogo,
				AppEnvironment.Preview => Constants.AssetPaths.PreviewLogo,
				_ => Constants.AssetPaths.StableLogo
			});

		/// <summary>
		/// Initializes the app components.
		/// </summary>
		public static async Task InitializeAppComponentsAsync()
		{
			var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			var addItemService = Ioc.Default.GetRequiredService<IAddItemService>();
			var generalSettingsService = userSettingsService.GeneralSettingsService;
			var jumpListService = Ioc.Default.GetRequiredService<IWindowsJumpListService>();

			// Start off a list of tasks we need to run before we can continue startup
			await Task.WhenAll(
				OptionalTaskAsync(CloudDrivesManager.UpdateDrivesAsync(), generalSettingsService.ShowCloudDrivesSection),
				App.LibraryManager.UpdateLibrariesAsync(),
				OptionalTaskAsync(WSLDistroManager.UpdateDrivesAsync(), generalSettingsService.ShowWslSection),
				OptionalTaskAsync(App.FileTagsManager.UpdateFileTagsAsync(), generalSettingsService.ShowFileTagsSection),
				App.QuickAccessManager.InitializeAsync()
			);

			await Task.WhenAll(
				jumpListService.InitializeAsync(),
				addItemService.InitializeAsync(),
				ContextMenu.WarmUpQueryContextMenuAsync()
			);

			FileTagsHelper.UpdateTagsDb();

			await CheckAppUpdate();

			static Task OptionalTaskAsync(Task task, bool condition)
			{
				if (condition)
					return task;

				return Task.CompletedTask;
			}

			generalSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		/// <summary>
		/// Checks application updates and download if available.
		/// </summary>
		public static async Task CheckAppUpdate()
		{
			var updateService = Ioc.Default.GetRequiredService<IUpdateService>();

			await updateService.CheckForUpdatesAsync();
			await updateService.DownloadMandatoryUpdatesAsync();
			await updateService.CheckAndUpdateFilesLauncherAsync();
			await updateService.CheckLatestReleaseNotesAsync();
		}

		/// <summary>
		/// Configures Sentry service, such as Analytics and Crash Report.
		/// </summary>
		public static void ConfigureSentry()
		{
			SentrySdk.Init(options =>
			{
				options.Dsn = Constants.AutomatedWorkflowInjectionKeys.SentrySecret;
				options.AutoSessionTracking = true;
				options.Release = $"{SystemInformation.Instance.ApplicationVersion.Major}.{SystemInformation.Instance.ApplicationVersion.Minor}.{SystemInformation.Instance.ApplicationVersion.Build}";
				options.TracesSampleRate = 0.80;
				options.ProfilesSampleRate = 0.40;
				options.Environment = AppEnvironment == AppEnvironment.Preview ? "preview" : "production";
				options.ExperimentalMetrics = new ExperimentalMetricsOptions
				{
					EnableCodeLocations = true
				};

				options.DisableWinUiUnhandledExceptionIntegration();
			});
		}

		/// <summary>
		/// Configures DI (dependency injection) container.
		/// </summary>
		public static IHost ConfigureHost()
		{
			return Host.CreateDefaultBuilder()
				.UseEnvironment(AppLifecycleHelper.AppEnvironment.ToString())
				.ConfigureLogging(builder => builder
					.ClearProviders()
					.AddConsole()
					.AddDebug()
					.AddProvider(new FileLoggerProvider(Path.Combine(ApplicationData.Current.LocalFolder.Path, "debug.log")))
					.AddProvider(new SentryLoggerProvider())
					.SetMinimumLevel(LogLevel.Information))
				.ConfigureServices(services => services
					// Settings services
					.AddSingleton<IUserSettingsService, UserSettingsService>()
					.AddSingleton<IAppearanceSettingsService, AppearanceSettingsService>(sp => new AppearanceSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IGeneralSettingsService, GeneralSettingsService>(sp => new GeneralSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IFoldersSettingsService, FoldersSettingsService>(sp => new FoldersSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IDevToolsSettingsService, DevToolsSettingsService>(sp => new DevToolsSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IApplicationSettingsService, ApplicationSettingsService>(sp => new ApplicationSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IInfoPaneSettingsService, InfoPaneSettingsService>(sp => new InfoPaneSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<ILayoutSettingsService, LayoutSettingsService>(sp => new LayoutSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IAppSettingsService, AppSettingsService>(sp => new AppSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IActionsSettingsService, ActionsSettingsService>(sp => new ActionsSettingsService(((UserSettingsService)sp.GetRequiredService<IUserSettingsService>()).GetSharingContext()))
					.AddSingleton<IFileTagsSettingsService, FileTagsSettingsService>()
					// Contexts
					.AddSingleton<IMultiPanesContext, MultiPanesContext>()
					.AddSingleton<IContentPageContext, ContentPageContext>()
					.AddSingleton<IDisplayPageContext, DisplayPageContext>()
					.AddSingleton<IHomePageContext, HomePageContext>()
					.AddSingleton<IWindowContext, WindowContext>()
					.AddSingleton<IMultitaskingContext, MultitaskingContext>()
					.AddSingleton<ITagsContext, TagsContext>()
					.AddSingleton<ISidebarContext, SidebarContext>()
					// Services
					.AddSingleton<IWindowsIniService, WindowsIniService>()
					.AddSingleton<IWindowsWallpaperService, WindowsWallpaperService>()
					.AddSingleton<IWindowsSecurityService, WindowsSecurityService>()
					.AddSingleton<IAppThemeModeService, AppThemeModeService>()
					.AddSingleton<IDialogService, DialogService>()
					.AddSingleton<ICommonDialogService, CommonDialogService>()
					.AddSingleton<IImageService, ImagingService>()
					.AddSingleton<IThreadingService, ThreadingService>()
					.AddSingleton<ILocalizationService, LocalizationService>()
					.AddSingleton<ICloudDetector, CloudDetector>()
					.AddSingleton<IFileTagsService, FileTagsService>()
					.AddSingleton<ICommandManager, CommandManager>()
					.AddSingleton<IModifiableCommandManager, ModifiableCommandManager>()
					.AddSingleton<IStorageService, NativeStorageService>()
					.AddSingleton<IFtpStorageService, FtpStorageService>()
					.AddSingleton<IAddItemService, AddItemService>()
#if STABLE || PREVIEW
					.AddSingleton<IUpdateService, SideloadUpdateService>()
#elif STORE
					.AddSingleton<IUpdateService, StoreUpdateService>()
#else
					.AddSingleton<IUpdateService, DummyUpdateService>()
#endif
					.AddSingleton<IPreviewPopupService, PreviewPopupService>()
					.AddSingleton<IDateTimeFormatterFactory, DateTimeFormatterFactory>()
					.AddSingleton<IDateTimeFormatter, UserDateTimeFormatter>()
					.AddSingleton<ISizeProvider, UserSizeProvider>()
					.AddSingleton<IQuickAccessService, QuickAccessService>()
					.AddSingleton<IResourcesService, ResourcesService>()
					.AddSingleton<IWindowsJumpListService, WindowsJumpListService>()
					.AddSingleton<IStorageTrashBinService, StorageTrashBinService>()
					.AddSingleton<IRemovableDrivesService, RemovableDrivesService>()
					.AddSingleton<INetworkService, NetworkService>()
					.AddSingleton<IStartMenuService, StartMenuService>()
					.AddSingleton<IStorageCacheService, StorageCacheService>()
					.AddSingleton<IStorageArchiveService, StorageArchiveService>()
					.AddSingleton<IStorageSecurityService, StorageSecurityService>()
					.AddSingleton<IWindowsCompatibilityService, WindowsCompatibilityService>()
					// ViewModels
					.AddSingleton<MainPageViewModel>()
					.AddSingleton<InfoPaneViewModel>()
					.AddSingleton<SidebarViewModel>()
					.AddSingleton<DrivesViewModel>()
					.AddSingleton<StatusCenterViewModel>()
					.AddSingleton<AppearanceViewModel>()
					.AddTransient<HomeViewModel>()
					.AddSingleton<QuickAccessWidgetViewModel>()
					.AddSingleton<DrivesWidgetViewModel>()
					.AddSingleton<NetworkLocationsWidgetViewModel>()
					.AddSingleton<FileTagsWidgetViewModel>()
					.AddSingleton<RecentFilesWidgetViewModel>()
					// Utilities
					.AddSingleton<QuickAccessManager>()
					.AddSingleton<StorageHistoryWrapper>()
					.AddSingleton<FileTagsManager>()
					.AddSingleton<RecentItems>()
					.AddSingleton<LibraryManager>()
					.AddSingleton<AppModel>()
				).Build();
		}

		/// <summary>
		/// Saves saves all opened tabs to the app cache.
		/// </summary>
		public static void SaveSessionTabs()
		{
			var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			userSettingsService.GeneralSettingsService.LastSessionTabList = MainPageViewModel.AppInstances.DefaultIfEmpty().Select(tab =>
			{
				if (tab is not null && tab.NavigationParameter is not null)
				{
					return tab.NavigationParameter.Serialize();
				}
				else
				{
					return "";
				}
			})
			.ToList();
		}

		/// <summary>
		/// Shows exception on the Debug Output and sends Toast Notification to the Windows Notification Center.
		/// </summary>
		public static void HandleAppUnhandledException(Exception? ex, bool showToastNotification)
		{
			var generalSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

			StringBuilder formattedException = new()
			{
				Capacity = 200
			};

			formattedException.AppendLine("--------- UNHANDLED EXCEPTION ---------");

			if (ex is not null)
			{
				ex.Data[Mechanism.HandledKey] = false;
				ex.Data[Mechanism.MechanismKey] = "Application.UnhandledException";

				SentrySdk.CaptureException(ex, scope =>
				{
					scope.User.Id = generalSettingsService?.UserId;
					scope.Level = SentryLevel.Fatal;
				});

				formattedException.AppendLine($">>>> HRESULT: {ex.HResult}");

				if (ex.Message is not null)
				{
					formattedException.AppendLine("--- MESSAGE ---");
					formattedException.AppendLine(ex.Message);
				}
				if (ex.StackTrace is not null)
				{
					formattedException.AppendLine("--- STACKTRACE ---");
					formattedException.AppendLine(ex.StackTrace);
				}
				if (ex.Source is not null)
				{
					formattedException.AppendLine("--- SOURCE ---");
					formattedException.AppendLine(ex.Source);
				}
				if (ex.InnerException is not null)
				{
					formattedException.AppendLine("--- INNER ---");
					formattedException.AppendLine(ex.InnerException.ToString());
				}
			}
			else
			{
				formattedException.AppendLine("Exception data is not available.");
			}

			formattedException.AppendLine("---------------------------------------");

			Debug.WriteLine(formattedException.ToString());

			// Please check "Output Window" for exception details (View -> Output Window) (CTRL + ALT + O)
			Debugger.Break();

			// Save the current tab list in case it was overwriten by another instance
			SaveSessionTabs();
			App.Logger?.LogError(ex, ex?.Message ?? "An unhandled error occurred.");

			if (!showToastNotification)
				return;

			SafetyExtensions.IgnoreExceptions(() =>
			{
				AppToastNotificationHelper.ShowUnhandledExceptionToast();
			});

			// Restart the app
			var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			var lastSessionTabList = userSettingsService.GeneralSettingsService.LastSessionTabList;

			if (userSettingsService.GeneralSettingsService.LastCrashedTabList?.SequenceEqual(lastSessionTabList) ?? false)
			{
				// Avoid infinite restart loop
				userSettingsService.GeneralSettingsService.LastSessionTabList = null;
			}
			else
			{
				userSettingsService.AppSettingsService.RestoreTabsOnStartup = true;
				userSettingsService.GeneralSettingsService.LastCrashedTabList = lastSessionTabList;

				// Try to re-launch and start over
				MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					await Launcher.LaunchUriAsync(new Uri("files-uwp:"));
				})
				.Wait(100);
			}
			Process.GetCurrentProcess().Kill();
		}

		/// <summary>
		/// Updates the visibility of the system tray icon
		/// </summary>
		private static void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is not IGeneralSettingsService generalSettingsService)
				return;

			if (e.PropertyName == nameof(IGeneralSettingsService.ShowSystemTrayIcon))
			{
				if (generalSettingsService.ShowSystemTrayIcon)
					App.SystemTrayIcon?.Show();
				else
					App.SystemTrayIcon?.Hide();
			}
		}
	}
}
