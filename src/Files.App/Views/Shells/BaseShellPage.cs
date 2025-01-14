// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Core;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.Views.Shells
{
	public abstract class BaseShellPage : Page, IShellPage, INotifyPropertyChanged
	{
		private readonly DispatcherQueueTimer _updateDateDisplayTimer;

		private DateTimeFormats _lastDateTimeFormats;

		private Task _gitFetch = Task.CompletedTask;

		private CancellationTokenSource _gitFetchToken = new CancellationTokenSource();

		public static readonly DependencyProperty NavParamsProperty =
			DependencyProperty.Register(
				"NavParams",
				typeof(NavigationParams),
				typeof(ModernShellPage),
				new PropertyMetadata(null));

		public StorageHistoryHelpers StorageHistoryHelpers { get; }

		protected readonly CancellationTokenSource cancellationTokenSource;

		protected readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

		protected readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		protected readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		protected readonly IUpdateService updateSettingsService = Ioc.Default.GetRequiredService<IUpdateService>();

		protected readonly ICommandManager commands = Ioc.Default.GetRequiredService<ICommandManager>();

		public AddressToolbarViewModel ToolbarViewModel { get; } = new AddressToolbarViewModel();

		public IBaseLayoutPage SlimContentPage => ContentPage;

		public IFilesystemHelpers FilesystemHelpers { get; protected set; }

		public Type CurrentPageType => ItemDisplay.SourcePageType;

		public LayoutPreferencesManager FolderSettings => InstanceViewModel.FolderSettings;

		public AppModel AppModel => App.AppModel;

		protected abstract Frame ItemDisplay { get; }

		public virtual bool CanNavigateForward => ItemDisplay.CanGoForward;

		public virtual bool CanNavigateBackward => ItemDisplay.CanGoBack;

		public bool IsColumnView => SlimContentPage is ColumnsLayoutPage;

		public virtual IList<PageStackEntry> ForwardStack => ItemDisplay.ForwardStack;

		public virtual IList<PageStackEntry> BackwardStack => ItemDisplay.BackStack;

		public ShellViewModel ShellViewModel { get; protected set; }

		public CurrentInstanceViewModel InstanceViewModel { get; }

		protected BaseLayoutPage _ContentPage;
		public BaseLayoutPage ContentPage
		{
			get => _ContentPage;
			set
			{
				if (value != _ContentPage)
				{
					if (_ContentPage is not null)
						_ContentPage.StatusBarViewModel.CheckoutRequested -= GitCheckout_Required;

					_ContentPage = value;

					NotifyPropertyChanged(nameof(ContentPage));
					NotifyPropertyChanged(nameof(SlimContentPage));
					if (value is not null)
						_ContentPage.StatusBarViewModel.CheckoutRequested += GitCheckout_Required;
				}
			}
		}

		protected IShellPanesPage _PaneHolder;
		public IShellPanesPage PaneHolder
		{
			get => _PaneHolder;
			set
			{
				if (value != _PaneHolder)
				{
					_PaneHolder = value;

					NotifyPropertyChanged(nameof(PaneHolder));
				}
			}
		}

		protected TabBarItemParameter _TabItemArguments;
		public TabBarItemParameter TabBarItemParameter
		{
			get => _TabItemArguments;
			set
			{
				if (_TabItemArguments != value)
				{
					_TabItemArguments = value;

					ContentChanged?.Invoke(this, value);
				}
			}
		}

		protected TaskCompletionSource _IsCurrentInstanceTCS = new();
		protected bool _IsCurrentInstance = false;
		public bool IsCurrentInstance
		{
			get => _IsCurrentInstance;
			set
			{
				if (_IsCurrentInstance != value)
				{
					_IsCurrentInstance = value;

					if (!value && SlimContentPage is not ColumnsLayoutPage)
						ToolbarViewModel.IsEditModeEnabled = false;

					if (value)
						_IsCurrentInstanceTCS.TrySetResult();
					else
						_IsCurrentInstanceTCS = new();

					NotifyPropertyChanged(nameof(IsCurrentInstance));

					// Update background to show off the focused shell page
					if (!IsColumnView)
						VisualStateManager.GoToState(this, value ? "ShellBackgroundFocusOnState" : "ShellBackgroundFocusOffState", true);
				}
			}
		}

		public virtual bool IsCurrentPane => IsCurrentInstance;

		public virtual Task WhenIsCurrent() => _IsCurrentInstanceTCS.Task;

		public event PropertyChangedEventHandler PropertyChanged;

		public event EventHandler<TabBarItemParameter> ContentChanged;

		public BaseShellPage(CurrentInstanceViewModel instanceViewModel)
		{
			InstanceViewModel = instanceViewModel;
			InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired += FolderSettings_LayoutPreferencesUpdateRequired;
			cancellationTokenSource = new CancellationTokenSource();
			FilesystemHelpers = new FilesystemHelpers(this, cancellationTokenSource.Token);
			StorageHistoryHelpers = new StorageHistoryHelpers(new StorageHistoryOperations(this, cancellationTokenSource.Token));

			ToolbarViewModel.InstanceViewModel = InstanceViewModel;

			InitToolbarCommands();

			DisplayFilesystemConsentDialogAsync();

			if (FilePropertiesHelpers.FlowDirectionSettingIsRightToLeft)
				FlowDirection = FlowDirection.RightToLeft;

			ToolbarViewModel.ToolbarPathItemInvoked += ShellPage_NavigationRequested;
			ToolbarViewModel.ToolbarFlyoutOpening += ShellPage_ToolbarFlyoutOpening;
			ToolbarViewModel.ToolbarPathItemLoaded += ShellPage_ToolbarPathItemLoaded;
			ToolbarViewModel.AddressBarTextEntered += ShellPage_AddressBarTextEntered;
			ToolbarViewModel.PathBoxItemDropped += ShellPage_PathBoxItemDropped;

			ToolbarViewModel.RefreshRequested += ShellPage_RefreshRequested;
			ToolbarViewModel.EditModeEnabled += NavigationToolbar_EditModeEnabled;
			ToolbarViewModel.ItemDraggedOverPathItem += ShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxQuerySubmitted += NavigationToolbar_QuerySubmitted;
			ToolbarViewModel.SearchBox.TextChanged += ShellPage_TextChanged;
			ToolbarViewModel.SearchBox.QuerySubmitted += ShellPage_QuerySubmitted;

			InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated += AppSettings_SortDirectionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated += AppSettings_SortOptionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated += AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortFilesFirstPreferenceUpdated += AppSettings_SortFilesFirstPreferenceUpdated;

			PointerPressed += CoreWindow_PointerPressed;

			drivesViewModel.PropertyChanged += DrivesManager_PropertyChanged;

			PreviewKeyDown += ShellPage_PreviewKeyDown;

			GitHelpers.GitFetchCompleted += FilesystemViewModel_GitDirectoryUpdated;

			_updateDateDisplayTimer = DispatcherQueue.CreateTimer();
			_updateDateDisplayTimer.Interval = TimeSpan.FromSeconds(1);
			_updateDateDisplayTimer.Tick += UpdateDateDisplayTimer_Tick;
			_lastDateTimeFormats = userSettingsService.GeneralSettingsService.DateTimeFormat;
			_updateDateDisplayTimer.Start();
		}

		protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void FilesystemViewModel_PageTypeUpdated(object sender, PageTypeUpdatedEventArgs e)
		{
			InstanceViewModel.IsPageTypeCloudDrive = e.IsTypeCloudDrive;
		}

		protected void FilesystemViewModel_OnSelectionRequestedEvent(object sender, List<ListedItem> e)
		{
			// Set focus since selection might occur before the UI finishes updating
			ContentPage.ItemManipulationModel.FocusFileList();
			ContentPage.ItemManipulationModel.SetSelectedItems(e);
		}

		protected async void FilesystemViewModel_DirectoryInfoUpdated(object sender, EventArgs e)
		{
			if (ContentPage is null)
				return;

			var directoryItemCountLocalization = "Items".GetLocalizedFormatResource(ShellViewModel.FilesAndFolders.Count);

			BranchItem? headBranch = headBranch = InstanceViewModel.IsGitRepository
					? await GitHelpers.GetRepositoryHead(InstanceViewModel.GitRepositoryPath)
					: null;

			if (InstanceViewModel.GitRepositoryPath != ShellViewModel.GitDirectory)
			{
				InstanceViewModel.GitRepositoryPath = ShellViewModel.GitDirectory;
				InstanceViewModel.IsGitRepository = ShellViewModel.IsValidGitDirectory;

				InstanceViewModel.GitBranchName = headBranch is not null
					? headBranch.Name
					: string.Empty;

				if (!_gitFetch.IsCompleted)
				{
					_gitFetchToken.Cancel();
					await _gitFetch;
					_gitFetchToken.TryReset();
				}
				if (InstanceViewModel.IsGitRepository && !GitHelpers.IsExecutingGitAction)
				{
					_gitFetch = Task.Run(
						() => GitHelpers.FetchOrigin(InstanceViewModel.GitRepositoryPath),
						_gitFetchToken.Token);
				}
			}

			var contentPage = ContentPage;
			if (contentPage is null)
				return;

			if (!GitHelpers.IsExecutingGitAction)
			{
				contentPage.StatusBarViewModel.UpdateGitInfo(
					InstanceViewModel.IsGitRepository,
					InstanceViewModel.GitRepositoryPath,
					headBranch);
			}

			contentPage.StatusBarViewModel.DirectoryItemCount = $"{ShellViewModel.FilesAndFolders.Count} {directoryItemCountLocalization}";
			contentPage.UpdateSelectionSize();
		}

		protected async void FilesystemViewModel_GitDirectoryUpdated(object sender, EventArgs e)
		{
			if (GitHelpers.IsExecutingGitAction)
				return;

			var head = InstanceViewModel.IsGitRepository
				? await GitHelpers.GetRepositoryHead(InstanceViewModel.GitRepositoryPath)
				: null;

			InstanceViewModel.GitBranchName = head is not null
				? head.Name
				: string.Empty;

			ContentPage?.StatusBarViewModel.UpdateGitInfo(
				InstanceViewModel.IsGitRepository,
				InstanceViewModel.GitRepositoryPath,
				head);
		}

		protected async void GitCheckout_Required(object? sender, string branchName)
		{
			if (!await GitHelpers.Checkout(ShellViewModel.GitDirectory, branchName))
			{
				_ContentPage.StatusBarViewModel.ShowLocals = true;
				_ContentPage.StatusBarViewModel.SelectedBranchIndex = StatusBarViewModel.ACTIVE_BRANCH_INDEX;
			}
			else
			{
				ContentPage.StatusBarViewModel.UpdateGitInfo(
					InstanceViewModel.IsGitRepository,
					InstanceViewModel.GitRepositoryPath,
					await GitHelpers.GetRepositoryHead(InstanceViewModel.GitRepositoryPath));
			}
		}

		protected virtual void Page_Loaded(object sender, RoutedEventArgs e)
		{
			OnNavigationParamsChanged();
			this.Loaded -= Page_Loaded;
		}

		// Some keys are overridden by control built-in defaults(e.g. 'Space').
		// They must be handled here since they're not propagated to KeyboardAccelerator.
		protected void ShellPage_PreviewKeyDown(object sender, KeyRoutedEventArgs args)
		{
			var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shift = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var alt = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
			var tabInstance =
				CurrentPageType == typeof(DetailsLayoutPage) ||
				CurrentPageType == typeof(GridLayoutPage) ||
				CurrentPageType == typeof(ColumnsLayoutPage) ||
				CurrentPageType == typeof(ColumnLayoutPage);

			switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.Key)
			{
				// Ctrl + Space, toggle media playback
				case (true, false, false, true, VirtualKey.Space):
					if (Ioc.Default.GetRequiredService<InfoPaneViewModel>().PreviewPaneContent is UserControls.FilePreviews.MediaPreview mediaPreviewContent)
					{
						mediaPreviewContent.ViewModel.TogglePlayback();
						args.Handled = true;
					}
					break;
			}
		}

		protected async void ShellPage_QuerySubmitted(ISearchBoxViewModel sender, SearchBoxQuerySubmittedEventArgs e)
		{
			if (e.ChosenSuggestion is SuggestionModel item && !string.IsNullOrWhiteSpace(item.ItemPath))
				await NavigationHelpers.OpenPath(item.ItemPath, this);
			else if (e.ChosenSuggestion is null && !string.IsNullOrWhiteSpace(sender.Query))
				SubmitSearch(sender.Query);
		}

		protected async void ShellPage_TextChanged(ISearchBoxViewModel sender, SearchBoxTextChangedEventArgs e)
		{
			if (e.Reason != SearchBoxTextChangeReason.UserInput)
				return;

			ShellViewModel.FilesAndFoldersFilter = sender.Query;
			await ShellViewModel.ApplyFilesAndFoldersChangesAsync();

			if (!string.IsNullOrWhiteSpace(sender.Query))
			{
				var search = new FolderSearch
				{
					Query = sender.Query,
					Folder = ShellViewModel.WorkingDirectory,
					MaxItemCount = 10,
				};

				sender.SetSuggestions((await search.SearchAsync()).Select(suggestion => new SuggestionModel(suggestion)));
			}
			else
			{
				sender.AddRecentQueries();
			}
		}

		protected async void ShellPage_RefreshRequested(object sender, EventArgs e)
		{
			await Refresh_Click();
		}

		protected void AppSettings_SortDirectionPreferenceUpdated(object sender, SortDirection e)
		{
			ShellViewModel?.UpdateSortDirectionStatusAsync();
		}

		protected void AppSettings_SortOptionPreferenceUpdated(object sender, SortOption e)
		{
			ShellViewModel?.UpdateSortOptionStatusAsync();
		}

		protected void AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated(object sender, bool e)
		{
			ShellViewModel?.UpdateSortDirectoriesAlongsideFilesAsync();
		}

		protected void AppSettings_SortFilesFirstPreferenceUpdated(object sender, bool e)
		{
			ShellViewModel?.UpdateSortFilesFirstAsync();
		}

		protected void CoreWindow_PointerPressed(object sender, PointerRoutedEventArgs args)
		{
			if (!IsCurrentInstance)
				return;

			if (args.GetCurrentPoint(this).Properties.IsXButton1Pressed)
				Back_Click();
			else if (args.GetCurrentPoint(this).Properties.IsXButton2Pressed)
				Forward_Click();
		}

		protected async void ShellPage_PathBoxItemDropped(object sender, PathBoxItemDroppedEventArgs e)
		{
			await FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.Package, e.Path, false, true);
			e.SignalEvent?.Set();
		}

		protected async void ShellPage_AddressBarTextEntered(object sender, AddressBarTextEnteredEventArgs e)
		{
			await ToolbarViewModel.SetAddressBarSuggestionsAsync(e.AddressBarTextField, this);
		}

		protected async void ShellPage_ToolbarPathItemLoaded(object sender, ToolbarPathItemLoadedEventArgs e)
		{
			await ToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, e.Item, this);
		}

		protected async void ShellPage_ToolbarFlyoutOpening(object sender, ToolbarFlyoutOpeningEventArgs e)
		{
			var pathBoxItem = ((Button)e.OpeningFlyout.Target).DataContext as PathBoxItem;

			if (pathBoxItem is not null)
				await ToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpeningFlyout, pathBoxItem, this);
		}

		protected async void NavigationToolbar_QuerySubmitted(object sender, ToolbarQuerySubmittedEventArgs e)
		{
			await ToolbarViewModel.CheckPathInputAsync(e.QueryText, ToolbarViewModel.PathComponents.LastOrDefault()?.Path, this);
		}

		protected void NavigationToolbar_EditModeEnabled(object sender, EventArgs e)
		{
			ToolbarViewModel.ManualEntryBoxLoaded = true;
			ToolbarViewModel.ClickablePathLoaded = false;
			ToolbarViewModel.PathText = string.IsNullOrEmpty(ShellViewModel?.WorkingDirectory)
				? Constants.UserEnvironmentPaths.HomePath
				: ShellViewModel.WorkingDirectory;
		}

		protected async void DrivesManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ShowUserConsentOnInit")
				await DisplayFilesystemConsentDialogAsync();
		}

		private volatile CancellationTokenSource? cts;

		// Ensure that the path bar gets updated for user interaction
		// whenever the path changes.We will get the individual directories from
		// the updated, most-current path and add them to the UI.
		public async Task UpdatePathUIToWorkingDirectoryAsync(string newWorkingDir, string singleItemOverride = null)
		{
			if (string.IsNullOrWhiteSpace(singleItemOverride))
			{
				cts = new CancellationTokenSource();

				var components = await StorageFileExtensions.GetDirectoryPathComponentsWithDisplayNameAsync(newWorkingDir);

				// Cancel if overrided by single item
				if (cts.IsCancellationRequested)
					return;

				ToolbarViewModel.PathComponents.Clear();
				foreach (var component in components)
					ToolbarViewModel.PathComponents.Add(component);
			}
			else
			{
				cts?.Cancel();

				// Clear the path UI
				ToolbarViewModel.PathComponents.Clear();
				ToolbarViewModel.IsSingleItemOverride = true;
				ToolbarViewModel.PathComponents.Add(new PathBoxItem() { Path = null, Title = singleItemOverride });
			}
		}

		public void SubmitSearch(string query)
		{
			ShellViewModel.CancelSearch();
			InstanceViewModel.CurrentSearchQuery = query;

			var args = new NavigationArguments()
			{
				AssociatedTabInstance = this,
				IsSearchResultPage = true,
				SearchPathParam = ShellViewModel.WorkingDirectory,
				SearchQuery = query,
			};

			var layout = InstanceViewModel.FolderSettings.GetLayoutType(ShellViewModel.WorkingDirectory);

			if (layout == typeof(ColumnsLayoutPage))
				NavigateToPath(ShellViewModel.WorkingDirectory, typeof(DetailsLayoutPage), args);
			else
				NavigateToPath(ShellViewModel.WorkingDirectory, layout, args);
		}

		public void NavigateWithArguments(Type sourcePageType, NavigationArguments navArgs)
		{
			NavigateToPath(navArgs.NavPathParam, sourcePageType, navArgs);
		}

		public void NavigateToPath(string navigationPath, NavigationArguments? navArgs = null)
		{
			var layout = FolderSettings.GetLayoutType(navigationPath);

			// Don't use Columns Layout for displaying tags
			if (navigationPath.StartsWith("tag:") && layout == typeof(ColumnsLayoutPage))
				layout = typeof(DetailsLayoutPage);

			NavigateToPath(navigationPath, layout, navArgs);
		}

		public Task TabItemDragOver(object sender, DragEventArgs e)
		{
			return SlimContentPage?.CommandsViewModel.DragOverAsync(e);
		}

		public Task TabItemDrop(object sender, DragEventArgs e)
		{
			return SlimContentPage?.CommandsViewModel.DropAsync(e);
		}

		public async Task RefreshIfNoWatcherExistsAsync()
		{
			if (ShellViewModel.HasNoWatcher)
				await Refresh_Click();
		}

		public async Task Refresh_Click()
		{
			if (InstanceViewModel.IsPageTypeSearchResults)
			{
				ToolbarViewModel.CanRefresh = false;
				var searchInstance = new FolderSearch
				{
					Query = InstanceViewModel.CurrentSearchQuery ?? (string)TabBarItemParameter.NavigationParameter,
					Folder = ShellViewModel.WorkingDirectory,
				};

				await ShellViewModel.SearchAsync(searchInstance);
			}
			else if (CurrentPageType != typeof(HomePage))
			{
				ToolbarViewModel.CanRefresh = false;
				ShellViewModel?.RefreshItems(null);
			}
		}

		public virtual void Back_Click()
		{
			var previousPageContent = ItemDisplay.BackStack[ItemDisplay.BackStack.Count - 1];
			HandleBackForwardRequest(previousPageContent);

			if (ItemDisplay.CanGoBack)
				ItemDisplay.GoBack();
		}

		public virtual void Forward_Click()
		{
			var incomingPageContent = ItemDisplay.ForwardStack[ItemDisplay.ForwardStack.Count - 1];
			HandleBackForwardRequest(incomingPageContent);

			if (ItemDisplay.CanGoForward)
				ItemDisplay.GoForward();
		}

		public void ResetNavigationStackLayoutMode()
		{
			foreach (PageStackEntry entry in ItemDisplay.BackStack.ToList())
			{
				if (entry.Parameter is NavigationArguments args &&
					args.NavPathParam is not null and not "Home")
				{
					var correctPageType = FolderSettings.GetLayoutType(args.NavPathParam, false);
					if (!entry.SourcePageType.Equals(correctPageType))
					{
						int index = ItemDisplay.BackStack.IndexOf(entry);
						var newEntry = new PageStackEntry(correctPageType, entry.Parameter, entry.NavigationTransitionInfo);
						ItemDisplay.BackStack.RemoveAt(index);
						ItemDisplay.BackStack.Insert(index, newEntry);
					}
				}
			}

			foreach (PageStackEntry entry in ItemDisplay.ForwardStack.ToList())
			{
				if (entry.Parameter is NavigationArguments args &&
					args.NavPathParam is not null and not "Home")
				{
					var correctPageType = FolderSettings.GetLayoutType(args.NavPathParam, false);
					if (!entry.SourcePageType.Equals(correctPageType))
					{
						int index = ItemDisplay.ForwardStack.IndexOf(entry);
						var newEntry = new PageStackEntry(correctPageType, entry.Parameter, entry.NavigationTransitionInfo);
						ItemDisplay.ForwardStack.RemoveAt(index);
						ItemDisplay.ForwardStack.Insert(index, newEntry);
					}
				}
			}
		}

		public void RemoveLastPageFromBackStack()
		{
			ItemDisplay.BackStack.Remove(ItemDisplay.BackStack.LastOrDefault());
		}

		public void RaiseContentChanged(IShellPage instance, TabBarItemParameter args)
		{
			ContentChanged?.Invoke(instance, args);
		}

		protected void FilesystemViewModel_ItemLoadStatusChanged(object sender, ItemLoadStatusChangedEventArgs e)
		{
			switch (e.Status)
			{
				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting:
					ToolbarViewModel.CanRefresh = false;
					SetLoadingIndicatorForTabs(true);
					break;
				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress:
					var columnCanNavigateBackward = false;
					var columnCanNavigateForward = false;
					if (SlimContentPage is ColumnsLayoutPage browser)
					{
						columnCanNavigateBackward = browser.ParentShellPageInstance?.CanNavigateBackward ?? false;
						columnCanNavigateForward = browser.ParentShellPageInstance?.CanNavigateForward ?? false;
					}
					ToolbarViewModel.CanGoBack = ItemDisplay.CanGoBack || columnCanNavigateBackward;
					ToolbarViewModel.CanGoForward = ItemDisplay.CanGoForward || columnCanNavigateForward;
					SetLoadingIndicatorForTabs(true);
					break;
				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete:
					SetLoadingIndicatorForTabs(false);

					if (ContentPage is not null)
						ContentPage.ItemManipulationModel.ScrollToTop();

					ToolbarViewModel.CanRefresh = true;
					// Select previous directory
					if (!string.IsNullOrWhiteSpace(e.PreviousDirectory) &&
						e.PreviousDirectory.Contains(e.Path, StringComparison.Ordinal) &&
						!e.PreviousDirectory.Contains(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
					{
						// Remove the WorkingDir from previous dir
						e.PreviousDirectory = e.PreviousDirectory.Replace(e.Path, string.Empty, StringComparison.Ordinal);

						var isNetwork = e.Path.StartsWith("\\\\");
						var isFtp = FtpHelpers.IsFtpPath(e.Path);
						var separator = isFtp ? "/" : "\\";

						// Get previous dir name
						if (e.PreviousDirectory.StartsWith(separator))
							e.PreviousDirectory = e.PreviousDirectory.Remove(0, 1);
						if (e.PreviousDirectory.Contains(separator))
							e.PreviousDirectory = e.PreviousDirectory.Split(separator)[0];

						// Get the first folder and combine it with WorkingDir
						string folderToSelect = e.Path + separator + e.PreviousDirectory;

						// Make sure we don't get double separators in the e.Path
						folderToSelect = folderToSelect.Replace(separator + separator, separator, StringComparison.Ordinal);

						if (isNetwork)
							folderToSelect = separator + folderToSelect;
						else if (isFtp)
							folderToSelect = folderToSelect.Replace(":/", "://", StringComparison.Ordinal);

						if (folderToSelect.EndsWith(separator))
							folderToSelect = folderToSelect.Remove(folderToSelect.Length - 1, 1);

						var itemToSelect = ShellViewModel.FilesAndFolders.ToList().FirstOrDefault((item) => item.ItemPath == folderToSelect);

						if (itemToSelect is not null && ContentPage is not null && userSettingsService.FoldersSettingsService.ScrollToPreviousFolderWhenNavigatingUp)
						{
							ContentPage.ItemManipulationModel.SetSelectedItem(itemToSelect);
							ContentPage.ItemManipulationModel.ScrollIntoView(itemToSelect);
						}
					}
					break;
			}
		}

		private void FolderSettings_LayoutPreferencesUpdateRequired(object sender, LayoutPreferenceEventArgs e)
		{
			if (ShellViewModel is null)
				return;

			LayoutPreferencesManager.SetLayoutPreferencesForPath(ShellViewModel.WorkingDirectory, e.LayoutPreference);
			if (e.IsAdaptiveLayoutUpdateRequired)
				AdaptiveLayoutHelpers.ApplyAdaptativeLayout(InstanceViewModel.FolderSettings, ShellViewModel.FilesAndFolders.ToList());
		}

		protected virtual void ViewModel_WorkingDirectoryModified(object sender, WorkingDirectoryModifiedEventArgs e)
		{
		}

		protected virtual void OnNavigationParamsChanged()
		{
		}

		protected virtual void ShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
		{
		}

		protected void InitToolbarCommands()
		{
			ToolbarViewModel.OpenNewWindowCommand = new AsyncRelayCommand(NavigationHelpers.LaunchNewWindowAsync);
			ToolbarViewModel.CreateNewFileCommand = new RelayCommand<ShellNewEntry>(x => UIFilesystemHelpers.CreateFileFromDialogResultTypeAsync(AddItemDialogItemType.File, x, this));
			ToolbarViewModel.UpdateCommand = new AsyncRelayCommand(async () => await updateSettingsService.DownloadUpdatesAsync());
		}

		protected async Task<BaseLayoutPage> GetContentOrNullAsync()
		{
			// WINUI3: Make sure not to run this synchronously, do not use EnqueueAsync
			var tcs = new TaskCompletionSource<object?>();
			DispatcherQueue.TryEnqueue(() =>
			{
				tcs.SetResult(ItemDisplay.Content);
			});

			return await tcs.Task as BaseLayoutPage;
		}

		protected async Task DisplayFilesystemConsentDialogAsync()
		{
			if (drivesViewModel?.ShowUserConsentOnInit ?? false)
			{
				drivesViewModel.ShowUserConsentOnInit = false;
				await DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					var dialog = DynamicDialogFactory.GetFor_ConsentDialog();

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					await dialog.ShowAsync();
				});
			}
		}

		protected void SelectSidebarItemFromPath(Type incomingSourcePageType = null)
		{
			if (incomingSourcePageType == typeof(HomePage) && incomingSourcePageType is not null)
				ToolbarViewModel.PathControlDisplayText = "Home".GetLocalizedResource();
		}

		protected void SetLoadingIndicatorForTabs(bool isLoading)
		{
			try
			{
				var multitaskingControls = ((MainWindow.Instance.Content as Frame).Content as MainPage).ViewModel.MultitaskingControls;

				foreach (var x in multitaskingControls)
					x.SetLoadingIndicatorStatus(x.Items.FirstOrDefault(x => x.TabItemContent == PaneHolder), isLoading);
			}
			catch (COMException)
			{

			}
		}

		// WINUI3
		protected static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;
			return contentDialog;
		}

		private void HandleBackForwardRequest(PageStackEntry pageContent)
		{
			var incomingPageNavPath = pageContent.Parameter as NavigationArguments;
			if (incomingPageNavPath is not null)
				incomingPageNavPath.IsLayoutSwitch = false;

			// Update layout type
			if (pageContent.SourcePageType != typeof(HomePage))
				InstanceViewModel.FolderSettings.GetLayoutType(incomingPageNavPath.IsSearchResultPage ? incomingPageNavPath.SearchPathParam : incomingPageNavPath.NavPathParam);

			SelectSidebarItemFromPath(pageContent.SourcePageType);
		}

		public abstract void Up_Click();

		public abstract void NavigateHome();

		public abstract void NavigateToPath(string? navigationPath, Type? sourcePageType, NavigationArguments? navArgs = null);

		private void UpdateDateDisplayTimer_Tick(object sender, object e)
		{
			if (App.AppModel.IsMainWindowClosed)
				return;

			if (userSettingsService.GeneralSettingsService.DateTimeFormat != _lastDateTimeFormats)
			{
				_lastDateTimeFormats = userSettingsService.GeneralSettingsService.DateTimeFormat;
				ShellViewModel?.UpdateDateDisplay(true);
			}
			else if (userSettingsService.GeneralSettingsService.DateTimeFormat == DateTimeFormats.Application)
			{
				ShellViewModel?.UpdateDateDisplay(false);
			}
		}

		public virtual void Dispose()
		{
			PreviewKeyDown -= ShellPage_PreviewKeyDown;
			PointerPressed -= CoreWindow_PointerPressed;
			drivesViewModel.PropertyChanged -= DrivesManager_PropertyChanged;

			ToolbarViewModel.ToolbarPathItemInvoked -= ShellPage_NavigationRequested;
			ToolbarViewModel.ToolbarFlyoutOpening -= ShellPage_ToolbarFlyoutOpening;
			ToolbarViewModel.ToolbarPathItemLoaded -= ShellPage_ToolbarPathItemLoaded;
			ToolbarViewModel.AddressBarTextEntered -= ShellPage_AddressBarTextEntered;
			ToolbarViewModel.PathBoxItemDropped -= ShellPage_PathBoxItemDropped;
			ToolbarViewModel.RefreshRequested -= ShellPage_RefreshRequested;
			ToolbarViewModel.EditModeEnabled -= NavigationToolbar_EditModeEnabled;
			ToolbarViewModel.ItemDraggedOverPathItem -= ShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxQuerySubmitted -= NavigationToolbar_QuerySubmitted;
			ToolbarViewModel.SearchBox.TextChanged -= ShellPage_TextChanged;

			InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired -= FolderSettings_LayoutPreferencesUpdateRequired;
			InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= AppSettings_SortDirectionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= AppSettings_SortOptionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated -= AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortFilesFirstPreferenceUpdated -= AppSettings_SortFilesFirstPreferenceUpdated;

			// Prevent weird case of this being null when many tabs are opened/closed quickly
			if (ShellViewModel is not null)
			{
				ShellViewModel.WorkingDirectoryModified -= ViewModel_WorkingDirectoryModified;
				ShellViewModel.ItemLoadStatusChanged -= FilesystemViewModel_ItemLoadStatusChanged;
				ShellViewModel.DirectoryInfoUpdated -= FilesystemViewModel_DirectoryInfoUpdated;
				ShellViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
				ShellViewModel.OnSelectionRequestedEvent -= FilesystemViewModel_OnSelectionRequestedEvent;
				ShellViewModel.GitDirectoryUpdated -= FilesystemViewModel_GitDirectoryUpdated;
				ShellViewModel.Dispose();
			}

			if (ItemDisplay.Content is IDisposable disposableContent)
				disposableContent?.Dispose();

			GitHelpers.GitFetchCompleted -= FilesystemViewModel_GitDirectoryUpdated;

			_updateDateDisplayTimer.Stop();
		}
	}
}
