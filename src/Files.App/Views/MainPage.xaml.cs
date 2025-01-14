// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Controls;
using Files.App.UserControls.Sidebar;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using Windows.Services.Store;
using WinRT.Interop;
using VirtualKey = Windows.System.VirtualKey;

namespace Files.App.Views
{
	public sealed partial class MainPage : Page
	{
		private IGeneralSettingsService generalSettingsService { get; } = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		public IUserSettingsService UserSettingsService { get; }
		private readonly IWindowContext WindowContext = Ioc.Default.GetRequiredService<IWindowContext>();
		public ICommandManager Commands { get; }
		public SidebarViewModel SidebarAdaptiveViewModel { get; }
		public MainPageViewModel ViewModel { get; }
		public StatusCenterViewModel OngoingTasksViewModel { get; }

		private bool keyReleased = true;

		private DispatcherQueueTimer _updateDateDisplayTimer;

		public MainPage()
		{
			InitializeComponent();

			// Dependency Injection
			UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
			Commands = Ioc.Default.GetRequiredService<ICommandManager>();
			SidebarAdaptiveViewModel = Ioc.Default.GetRequiredService<SidebarViewModel>();
			SidebarAdaptiveViewModel.PaneFlyout = (MenuFlyout)Resources["SidebarContextMenu"];
			ViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();
			OngoingTasksViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

			if (FilePropertiesHelpers.FlowDirectionSettingIsRightToLeft)
				FlowDirection = FlowDirection.RightToLeft;

			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;

			_updateDateDisplayTimer = DispatcherQueue.CreateTimer();
			_updateDateDisplayTimer.Interval = TimeSpan.FromSeconds(1);
			_updateDateDisplayTimer.Tick += UpdateDateDisplayTimer_Tick;
		}

		private async Task PromptForReviewAsync()
		{
			var promptForReviewDialog = new ContentDialog
			{
				Title = "ReviewFiles".ToLocalized(),
				Content = "ReviewFilesContent".ToLocalized(),
				PrimaryButtonText = "Yes".ToLocalized(),
				SecondaryButtonText = "No".ToLocalized()
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				promptForReviewDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			var result = await promptForReviewDialog.TryShowAsync();

			if (result == ContentDialogResult.Primary)
			{
				try
				{
					var storeContext = StoreContext.GetDefault();
					InitializeWithWindow.Initialize(storeContext, MainWindow.Instance.WindowHandle);
					var storeRateAndReviewResult = await storeContext.RequestRateAndReviewAppAsync();

					App.Logger.LogInformation($"STORE: review request status: {storeRateAndReviewResult.Status}");

					UserSettingsService.ApplicationSettingsService.ClickedToReviewApp = true;
				}
				catch (Exception) { }
			}
		}

		private async Task AppRunningAsAdminPromptAsync()
		{
			var runningAsAdminPrompt = new ContentDialog
			{
				Title = "FilesRunningAsAdmin".ToLocalized(),
				Content = "FilesRunningAsAdminContent".ToLocalized(),
				PrimaryButtonText = "Ok".ToLocalized(),
				SecondaryButtonText = "DontShowAgain".ToLocalized()
			};

			var result = await SetContentDialogRoot(runningAsAdminPrompt).TryShowAsync();

			if (result == ContentDialogResult.Secondary)
				UserSettingsService.ApplicationSettingsService.ShowRunningAsAdminPrompt = false;
		}

		// WINUI3
		private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			return contentDialog;
		}

		private void UserSettingsService_OnSettingChangedEvent(object? sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(IInfoPaneSettingsService.IsInfoPaneEnabled):
					LoadPaneChanged();
					break;
			}
		}

		private void HorizontalMultitaskingControl_Loaded(object sender, RoutedEventArgs e)
		{
			TabControl.DragArea.SizeChanged += (_, _) => MainWindow.Instance.RaiseSetTitleBarDragRegion(SetTitleBarDragRegion);
			TabControl.SizeChanged += (_, _) => MainWindow.Instance.RaiseSetTitleBarDragRegion(SetTitleBarDragRegion);
			if (ViewModel.MultitaskingControl is not TabBar)
			{
				ViewModel.MultitaskingControl = TabControl;
				ViewModel.MultitaskingControls.Add(TabControl);
				ViewModel.MultitaskingControl.CurrentInstanceChanged += MultitaskingControl_CurrentInstanceChanged;
			}
		}

		private int SetTitleBarDragRegion(InputNonClientPointerSource source, SizeInt32 size, double scaleFactor, Func<UIElement, RectInt32?, RectInt32> getScaledRect)
		{
			var height = (int)TabControl.ActualHeight;
			source.SetRegionRects(NonClientRegionKind.Passthrough, [getScaledRect(this, new RectInt32(0, 0, (int)(TabControl.ActualWidth + TabControl.Margin.Left - TabControl.DragArea.ActualWidth), height))]);
			return height;
		}

		public async void TabItemContent_ContentChanged(object? sender, TabBarItemParameter e)
		{
			if (SidebarAdaptiveViewModel.PaneHolder is null)
				return;

			var paneArgs = e.NavigationParameter as PaneNavigationArguments;
			SidebarAdaptiveViewModel.UpdateSidebarSelectedItemFromArgs(SidebarAdaptiveViewModel.PaneHolder.IsLeftPaneActive ?
				paneArgs?.LeftPaneNavPathParam : paneArgs?.RightPaneNavPathParam);

			UpdateStatusBarProperties();
			LoadPaneChanged();
			UpdateNavToolbarProperties();
			await NavigationHelpers.UpdateInstancePropertiesAsync(paneArgs);

			// Save the updated tab list
			AppLifecycleHelper.SaveSessionTabs();
		}

		public async void MultitaskingControl_CurrentInstanceChanged(object? sender, CurrentInstanceChangedEventArgs e)
		{
			if (SidebarAdaptiveViewModel.PaneHolder is not null)
				SidebarAdaptiveViewModel.PaneHolder.PropertyChanged -= PaneHolder_PropertyChanged;

			var navArgs = e.CurrentInstance.TabBarItemParameter?.NavigationParameter;
			if (e.CurrentInstance is IShellPanesPage currentInstance)
			{
				SidebarAdaptiveViewModel.PaneHolder = currentInstance;
				SidebarAdaptiveViewModel.PaneHolder.PropertyChanged += PaneHolder_PropertyChanged;
			}
			SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged((navArgs as PaneNavigationArguments)?.LeftPaneNavPathParam);

			if (SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.StatusBarViewModel is not null)
				SidebarAdaptiveViewModel.PaneHolder.ActivePaneOrColumn.SlimContentPage.StatusBarViewModel.ShowLocals = true;

			UpdateStatusBarProperties();
			UpdateNavToolbarProperties();
			LoadPaneChanged();

			e.CurrentInstance.ContentChanged -= TabItemContent_ContentChanged;
			e.CurrentInstance.ContentChanged += TabItemContent_ContentChanged;

			await NavigationHelpers.UpdateInstancePropertiesAsync(navArgs);
		}

		private void PaneHolder_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			SidebarAdaptiveViewModel.NotifyInstanceRelatedPropertiesChanged(SidebarAdaptiveViewModel.PaneHolder.ActivePane?.TabBarItemParameter?.NavigationParameter?.ToString());
			UpdateStatusBarProperties();
			UpdateNavToolbarProperties();
			LoadPaneChanged();
		}

		private void UpdateStatusBarProperties()
		{
			if (StatusBar is not null)
			{
				StatusBar.StatusBarViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.StatusBarViewModel;
				StatusBar.SelectedItemsPropertiesViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.SlimContentPage?.SelectedItemsPropertiesViewModel;
			}
		}

		private void UpdateNavToolbarProperties()
		{
			if (NavToolbar is not null)
				NavToolbar.ViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.ToolbarViewModel;

			if (InnerNavigationToolbar is not null)
				InnerNavigationToolbar.ViewModel = SidebarAdaptiveViewModel.PaneHolder?.ActivePaneOrColumn.ToolbarViewModel;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			ViewModel.OnNavigatedToAsync(e);
		}

		protected override async void OnPreviewKeyDown(KeyRoutedEventArgs e) => await OnPreviewKeyDownAsync(e);

		private async Task OnPreviewKeyDownAsync(KeyRoutedEventArgs e)
		{
			base.OnPreviewKeyDown(e);

			switch (e.Key)
			{
				case VirtualKey.Menu:
				case VirtualKey.Control:
				case VirtualKey.Shift:
				case VirtualKey.LeftWindows:
				case VirtualKey.RightWindows:
					break;
				default:
					var currentModifiers = HotKeyHelpers.GetCurrentKeyModifiers();
					HotKey hotKey = new((Keys)e.Key, currentModifiers);
					var source = e.OriginalSource as DependencyObject;

					// A textbox takes precedence over certain hotkeys.
					if (source?.FindAscendantOrSelf<TextBox>() is not null)
						break;

					// Execute command for hotkey
					var command = Commands[hotKey];

					if (command.Code is CommandCodes.OpenItem && source?.FindAscendantOrSelf<PathBreadcrumb>() is not null)
						break;
					

					if (command.Code is not CommandCodes.None && keyReleased)
					{
						keyReleased = false;
						e.Handled = command.IsExecutable;
						await command.ExecuteAsync();
					}
					break;
			}
		}

		protected override void OnPreviewKeyUp(KeyRoutedEventArgs e)
		{
			base.OnPreviewKeyUp(e);

			switch (e.Key)
			{
				case VirtualKey.Menu:
				case VirtualKey.Control:
				case VirtualKey.Shift:
				case VirtualKey.LeftWindows:
				case VirtualKey.RightWindows:
					break;
				default:
					keyReleased = true;
					break;
			}
		}

		// A workaround for issue with OnPreviewKeyUp not being called when the hotkey displays a dialog
		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);

			keyReleased = true;
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			MainWindow.Instance.AppWindow.Changed += (_, _) => MainWindow.Instance.RaiseSetTitleBarDragRegion(SetTitleBarDragRegion);

			// Defers the status bar loading until after the page has loaded to improve startup perf
			FindName(nameof(StatusBar));
			FindName(nameof(InnerNavigationToolbar));
			FindName(nameof(TabControl));
			FindName(nameof(NavToolbar));

			// Notify user that drag and drop is disabled
			// Prompt is disabled in the dev environment to prevent issues with the automation testing 
			// ToDo put this in a StartupPromptService
			if
			(
				AppLifecycleHelper.AppEnvironment is not AppEnvironment.Dev &&
				WindowContext.IsRunningAsAdmin &&
				UserSettingsService.ApplicationSettingsService.ShowRunningAsAdminPrompt
			)
			{
				DispatcherQueue.TryEnqueue(async () => await AppRunningAsAdminPromptAsync());
			}

			// ToDo put this in a StartupPromptService
			if (Package.Current.Id.Name != "49306atecsolution.FilesUWP" || UserSettingsService.ApplicationSettingsService.ClickedToReviewApp)
				return;

			var totalLaunchCount = SystemInformation.Instance.TotalLaunchCount;
			if (totalLaunchCount is 50 or 200)
			{
				// Prompt user to review app in the Store
				DispatcherQueue.TryEnqueue(async () => await PromptForReviewAsync());
			}
		}

		private void PreviewPane_Loaded(object sender, RoutedEventArgs e)
		{
			_updateDateDisplayTimer.Start();
		}

		private void PreviewPane_Unloaded(object sender, RoutedEventArgs e)
		{
			_updateDateDisplayTimer.Stop();
		}

		private void UpdateDateDisplayTimer_Tick(object sender, object e)
		{
			if (!App.AppModel.IsMainWindowClosed)
				InfoPane?.ViewModel.UpdateDateDisplay();
		}

		private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			switch (InfoPane?.Position)
			{
				case PreviewPanePositions.Right when ContentColumn.ActualWidth == ContentColumn.MinWidth:
					UserSettingsService.InfoPaneSettingsService.VerticalSizePx += e.NewSize.Width - e.PreviousSize.Width;
					UpdatePositioning();
					break;
				case PreviewPanePositions.Bottom when ContentRow.ActualHeight == ContentRow.MinHeight:
					UserSettingsService.InfoPaneSettingsService.HorizontalSizePx += e.NewSize.Height - e.PreviousSize.Height;
					UpdatePositioning();
					break;
			}
		}

		private void SidebarControl_Loaded(object sender, RoutedEventArgs e)
		{
			// Set the correct tab margin on startup
			SidebarAdaptiveViewModel.UpdateTabControlMargin();
		}

		private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e) => LoadPaneChanged();

		/// <summary>
		/// Call this function to update the positioning of the preview pane.
		/// This is a workaround as the VisualStateManager causes problems.
		/// </summary>
		private void UpdatePositioning()
		{
			if (InfoPane is null || !ViewModel.ShouldPreviewPaneBeActive)
			{
				PaneRow.MinHeight = 0;
				PaneRow.MaxHeight = double.MaxValue;
				PaneRow.Height = new GridLength(0);
				PaneColumn.MinWidth = 0;
				PaneColumn.MaxWidth = double.MaxValue;
				PaneColumn.Width = new GridLength(0);
			}
			else
			{
				InfoPane.UpdatePosition(RootGrid.ActualWidth, RootGrid.ActualHeight);
				switch (InfoPane.Position)
				{
					case PreviewPanePositions.None:
						PaneRow.MinHeight = 0;
						PaneRow.Height = new GridLength(0);
						PaneColumn.MinWidth = 0;
						PaneColumn.Width = new GridLength(0);
						break;
					case PreviewPanePositions.Right:
						InfoPane.SetValue(Grid.RowProperty, 1);
						InfoPane.SetValue(Grid.ColumnProperty, 2);
						PaneSplitter.SetValue(Grid.RowProperty, 1);
						PaneSplitter.SetValue(Grid.ColumnProperty, 1);
						PaneSplitter.Width = 2;
						PaneSplitter.Height = RootGrid.ActualHeight;
						PaneSplitter.GripperCursor = GridSplitter.GripperCursorType.SizeWestEast;
						PaneSplitter.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast));
						PaneColumn.MinWidth = InfoPane.MinWidth;
						PaneColumn.MaxWidth = InfoPane.MaxWidth;
						PaneColumn.Width = new GridLength(UserSettingsService.InfoPaneSettingsService.VerticalSizePx, GridUnitType.Pixel);
						PaneRow.MinHeight = 0;
						PaneRow.MaxHeight = double.MaxValue;
						PaneRow.Height = new GridLength(0);
						break;
					case PreviewPanePositions.Bottom:
						InfoPane.SetValue(Grid.RowProperty, 3);
						InfoPane.SetValue(Grid.ColumnProperty, 0);
						PaneSplitter.SetValue(Grid.RowProperty, 2);
						PaneSplitter.SetValue(Grid.ColumnProperty, 0);
						PaneSplitter.Height = 2;
						PaneSplitter.Width = RootGrid.ActualWidth;
						PaneSplitter.GripperCursor = GridSplitter.GripperCursorType.SizeNorthSouth;
						PaneSplitter.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth));
						PaneColumn.MinWidth = 0;
						PaneColumn.MaxWidth = double.MaxValue;
						PaneColumn.Width = new GridLength(0);
						PaneRow.MinHeight = InfoPane.MinHeight;
						PaneRow.MaxHeight = InfoPane.MaxHeight;
						PaneRow.Height = new GridLength(UserSettingsService.InfoPaneSettingsService.HorizontalSizePx, GridUnitType.Pixel);
						break;
				}
			}
		}

		private void PaneSplitter_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			switch (InfoPane?.Position)
			{
				case PreviewPanePositions.Right:
					UserSettingsService.InfoPaneSettingsService.VerticalSizePx = InfoPane.ActualWidth;
					break;
				case PreviewPanePositions.Bottom:
					UserSettingsService.InfoPaneSettingsService.HorizontalSizePx = InfoPane.ActualHeight;
					break;
			}

			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}

		private void LoadPaneChanged()
		{
			try
			{
				var isHomePage = !(SidebarAdaptiveViewModel.PaneHolder?.ActivePane?.InstanceViewModel?.IsPageTypeNotHome ?? false);
				var isMultiPane = SidebarAdaptiveViewModel.PaneHolder?.IsMultiPaneActive ?? false;
				var isBigEnough = !App.AppModel.IsMainWindowClosed &&
					(MainWindow.Instance.Bounds.Width > 450 && MainWindow.Instance.Bounds.Height > 450 || RootGrid.ActualWidth > 700 && MainWindow.Instance.Bounds.Height > 360);

				ViewModel.ShouldPreviewPaneBeDisplayed = (!isHomePage || isMultiPane) && isBigEnough;
				ViewModel.ShouldPreviewPaneBeActive = UserSettingsService.InfoPaneSettingsService.IsInfoPaneEnabled && ViewModel.ShouldPreviewPaneBeDisplayed;
				ViewModel.ShouldViewControlBeDisplayed = SidebarAdaptiveViewModel.PaneHolder?.ActivePane?.InstanceViewModel?.IsPageTypeNotHome ?? false;

				UpdatePositioning();
			}
			catch (Exception ex)
			{
				// Handle exception in case WinUI Windows is closed
				// (see https://github.com/files-community/Files/issues/15599)

				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		private async void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ViewModel.ShouldPreviewPaneBeActive) && ViewModel.ShouldPreviewPaneBeActive)
				await Ioc.Default.GetRequiredService<InfoPaneViewModel>().UpdateSelectedItemPreviewAsync();
		}

		private void RootGrid_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			switch (e.Key)
			{
				case VirtualKey.Menu:
				case VirtualKey.Control:
				case VirtualKey.Shift:
				case VirtualKey.LeftWindows:
				case VirtualKey.RightWindows:
					break;
				default:
					var currentModifiers = HotKeyHelpers.GetCurrentKeyModifiers();
					HotKey hotKey = new((Keys)e.Key, currentModifiers);

					// Prevents the arrow key events from navigating the list instead of switching compact overlay
					if (Commands[hotKey].Code is CommandCodes.EnterCompactOverlay or CommandCodes.ExitCompactOverlay)
						Focus(FocusState.Keyboard);
					break;
			}
		}

		private void NavToolbar_Loaded(object sender, RoutedEventArgs e) => UpdateNavToolbarProperties();

		private void PaneSplitter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			this.ChangeCursor(InputSystemCursor.Create(PaneSplitter.GripperCursor == GridSplitter.GripperCursorType.SizeWestEast ?
				InputSystemCursorShape.SizeWestEast : InputSystemCursorShape.SizeNorthSouth));
		}
	}
}