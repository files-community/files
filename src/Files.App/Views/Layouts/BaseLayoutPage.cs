// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Files.App.Controls;
using Files.App.Helpers.ContextFlyouts;
using Files.App.UserControls.Menus;
using Files.App.ViewModels.Layouts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using WinRT;
using static Files.App.Helpers.PathNormalization;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using SortDirection = Files.App.Data.Enums.SortDirection;
using VanaraWindowsShell = Vanara.Windows.Shell;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents the base class which every layout page must derive from
	/// </summary>
	public abstract class BaseLayoutPage : Page, IBaseLayoutPage, INotifyPropertyChanged
	{
		// Dependency injections

		protected IFileTagsSettingsService FileTagsSettingsService { get; } = Ioc.Default.GetService<IFileTagsSettingsService>()!;
		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>()!;
		protected ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();
		public InfoPaneViewModel InfoPaneViewModel { get; } = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
		protected readonly IWindowContext WindowContext = Ioc.Default.GetRequiredService<IWindowContext>();
		protected readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();

		// ViewModels

		public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel { get; }
		public StatusBarViewModel StatusBarViewModel { get; }
		public ItemManipulationModel ItemManipulationModel { get; private set; }
		public BaseLayoutViewModel? CommandsViewModel { get; protected set; }

		// Fields

		private readonly DispatcherQueueTimer jumpTimer;
		private readonly DispatcherQueueTimer dragOverTimer;
		private readonly DispatcherQueueTimer tapDebounceTimer;
		private readonly DispatcherQueueTimer hoverTimer;

		private readonly DragEventHandler Item_DragOverEventHandler;
		public event PropertyChangedEventHandler? PropertyChanged;

		protected NavigationArguments? navigationArguments;

		private CancellationTokenSource? shellContextMenuItemCancellationToken;
		private CancellationTokenSource? groupingCancellationToken;

		private bool shiftPressed;
		private bool itemDragging;

		private ListedItem? dragOverItem = null;
		private ListedItem? hoveredItem = null;
		private ListedItem? preRenamingItem = null;

		// Properties

		protected AddressToolbar? NavToolbar
			=> (MainWindow.Instance.Content as Frame)?.FindDescendant<AddressToolbar>();

		public LayoutPreferencesManager? FolderSettings
			=> ParentShellPageInstance?.InstanceViewModel.FolderSettings;

		public CurrentInstanceViewModel? InstanceViewModel
			=> ParentShellPageInstance?.InstanceViewModel;

		public static AppModel AppModel
			=> App.AppModel;

		public bool AllowItemDrag
			=> WindowContext.CanDragAndDrop;

		public CommandBarFlyout ItemContextMenuFlyout { get; set; } = new()
		{
			AlwaysExpanded = true,
			AreOpenCloseAnimationsEnabled = false,
			Placement = FlyoutPlacementMode.Right,
		};

		public CommandBarFlyout BaseContextMenuFlyout { get; set; } = new()
		{
			AlwaysExpanded = true,
			AreOpenCloseAnimationsEnabled = false,
			Placement = FlyoutPlacementMode.Right,
		};

		protected abstract ItemsControl ItemsControl { get; }

		public IShellPage? ParentShellPageInstance { get; private set; }

		public bool IsRenamingItem { get; set; }
		public bool LockPreviewPaneContent { get; set; }

		public ListedItem? RenamingItem { get; set; }
		public ListedItem? SelectedItem { get; private set; }

		public string? OldItemName { get; set; }

		private bool isMiddleClickToScrollEnabled = true;
		public bool IsMiddleClickToScrollEnabled
		{
			get => isMiddleClickToScrollEnabled;
			set
			{
				if (isMiddleClickToScrollEnabled != value)
				{
					isMiddleClickToScrollEnabled = value;

					NotifyPropertyChanged(nameof(IsMiddleClickToScrollEnabled));
				}
			}
		}

		private CollectionViewSource collectionViewSource = new()
		{
			IsSourceGrouped = true,
		};
		public CollectionViewSource CollectionViewSource
		{
			get => collectionViewSource;
			set
			{
				if (collectionViewSource == value)
					return;

				if (collectionViewSource.View is not null)
					collectionViewSource.View.VectorChanged -= View_VectorChanged;

				collectionViewSource = value;

				NotifyPropertyChanged(nameof(CollectionViewSource));

				if (collectionViewSource.View is not null)
					collectionViewSource.View.VectorChanged += View_VectorChanged;
			}
		}

		private bool isItemSelected = false;
		public bool IsItemSelected
		{
			get => isItemSelected;
			internal set
			{
				if (value != isItemSelected)
				{
					isItemSelected = value;

					NotifyPropertyChanged(nameof(IsItemSelected));
				}
			}
		}

		private string jumpString = string.Empty;
		public string JumpString
		{
			get => jumpString;
			set
			{
				// If current string is "a", and the next character typed is "a",
				// search for next file that starts with "a" (a.k.a. _jumpString = "a")
				if (jumpString.Length == 1 && value == jumpString + jumpString)
					value = jumpString;
				if (value != string.Empty)
				{
					ListedItem? jumpedToItem = null;
					ListedItem? previouslySelectedItem = IsItemSelected ? SelectedItem : null;

					// Select first matching item after currently selected item
					if (previouslySelectedItem is not null)
					{
						// Use FilesAndFolders because only displayed entries should be jumped to
						IEnumerable<ListedItem> candidateItems = ParentShellPageInstance!.ShellViewModel.FilesAndFolders.ToList()
							.SkipWhile(x => x != previouslySelectedItem)
							.Skip(value.Length == 1 ? 1 : 0) // User is trying to cycle through items starting with the same letter
							.Where(f => f.Name.Length >= value.Length && string.Equals(f.Name.Substring(0, value.Length), value, StringComparison.OrdinalIgnoreCase));
						jumpedToItem = candidateItems.FirstOrDefault();
					}

					if (jumpedToItem is null)
					{
						// Use FilesAndFolders because only displayed entries should be jumped to
						IEnumerable<ListedItem> candidateItems = ParentShellPageInstance!.ShellViewModel.FilesAndFolders.ToList()
							.Where(f => f.Name.Length >= value.Length && string.Equals(f.Name.Substring(0, value.Length), value, StringComparison.OrdinalIgnoreCase));
						jumpedToItem = candidateItems.FirstOrDefault();
					}

					if (jumpedToItem is not null)
					{
						ItemManipulationModel.SetSelectedItem(jumpedToItem);
						ItemManipulationModel.ScrollIntoView(jumpedToItem);
						ItemManipulationModel.FocusSelectedItems();
					}

					// Restart the timer
					jumpTimer.Start();
				}

				jumpString = value;
			}
		}

		private bool isSelectedItemsSorted = false;
		private List<ListedItem>? selectedItems = [];
		public List<ListedItem>? SelectedItems
		{
			get
			{
				if (!isSelectedItemsSorted)
				{
					var orderedItems = SortingHelper.OrderFileList(selectedItems, FolderSettings.DirectorySortOption, FolderSettings.DirectorySortDirection, FolderSettings.SortDirectoriesAlongsideFiles, FolderSettings.SortFilesFirst).ToList();
					selectedItems = orderedItems;
					isSelectedItemsSorted = true;
				}

				return SelectedItem is null || !selectedItems!.Contains(SelectedItem)
					? selectedItems
					: selectedItems
						.SkipWhile(x => x != SelectedItem)
						.Concat(selectedItems.TakeWhile(x => x != SelectedItem))
						.ToList();
			}
			internal set
			{
				if (value != selectedItems)
				{
					isSelectedItemsSorted = false;
					selectedItems = value;

					if (selectedItems?.Count == 0 || selectedItems?[0] is null)
					{
						IsItemSelected = false;
						SelectedItem = null;
						SelectedItemsPropertiesViewModel.IsItemSelected = false;

						ResetRenameDoubleClick();
						UpdateSelectionSize();
					}
					else if (selectedItems is not null)
					{
						IsItemSelected = true;
						SelectedItem = selectedItems.First();
						SelectedItemsPropertiesViewModel.IsItemSelected = true;

						UpdateSelectionSize();

						SelectedItemsPropertiesViewModel.SelectedItemsCount = selectedItems.Count;
						SelectedItemsPropertiesViewModel.SelectedItemsCountString = "SelectedItems".GetLocalizedFormatResource(selectedItems!.Count);

						if (selectedItems.Count == 1)
						{
							DispatcherQueue.EnqueueOrInvokeAsync(async () =>
							{
								// Tapped event must be executed first
								await Task.Delay(50);
								preRenamingItem = SelectedItem;
							});
						}
						else
							ResetRenameDoubleClick();
					}

					NotifyPropertyChanged(nameof(SelectedItems));
				}
				ParentShellPageInstance!.ToolbarViewModel.SelectedItems = value;
			}
		}

		// Constructor

		public BaseLayoutPage()
		{
			ItemManipulationModel = new ItemManipulationModel();

			HookBaseEvents();
			HookEvents();

			jumpTimer = DispatcherQueue.CreateTimer();
			jumpTimer.Interval = TimeSpan.FromSeconds(0.8);
			jumpTimer.Tick += JumpTimer_Tick;

			Item_DragOverEventHandler = new DragEventHandler(Item_DragOver);

			SelectedItemsPropertiesViewModel = new SelectedItemsPropertiesViewModel();
			StatusBarViewModel = new StatusBarViewModel();

			dragOverTimer = DispatcherQueue.CreateTimer();
			tapDebounceTimer = DispatcherQueue.CreateTimer();
			hoverTimer = DispatcherQueue.CreateTimer();
		}

		// Abstract methods

		protected abstract void HookEvents();
		protected abstract void UnhookEvents();
		protected abstract void InitializeCommandsViewModel();
		protected abstract bool CanGetItemFromElement(object element);

		// Methods

		private void HookBaseEvents()
		{
			ItemManipulationModel.RefreshItemsOpacityInvoked += ItemManipulationModel_RefreshItemsOpacityInvoked;
		}

		private void UnhookBaseEvents()
		{
			ItemManipulationModel.RefreshItemsOpacityInvoked -= ItemManipulationModel_RefreshItemsOpacityInvoked;
		}

		private void JumpTimer_Tick(object sender, object e)
		{
			jumpString = string.Empty;
			jumpTimer.Stop();
		}

		protected IEnumerable<ListedItem>? GetAllItems()
		{
			var items = CollectionViewSource.IsSourceGrouped
				? (CollectionViewSource.Source as BulkConcurrentObservableCollection<GroupedCollection<ListedItem>>)?.SelectMany(g => g) // add all items from each group to the new list
				: CollectionViewSource.Source as IEnumerable<ListedItem>;

			return items ?? new List<ListedItem>();
		}

		public virtual void ResetItemOpacity()
		{
			var items = GetAllItems();
			if (items is null)
				return;

			foreach (var item in items)
			{
				if (item is not null)
					item.Opacity = item.IsHiddenItem ? Constants.UI.DimItemOpacity : 1.0d;
			}
		}

		protected ListedItem? GetItemFromElement(object element)
		{
			if (element is not ContentControl item || !CanGetItemFromElement(element))
				return null;

			return (item.DataContext as ListedItem) ?? (item.Content as ListedItem) ?? (ItemsControl.ItemFromContainer(item) as ListedItem);
		}

		protected virtual void BaseFolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
			if (ParentShellPageInstance?.SlimContentPage is not null)
			{
				var layoutType = FolderSettings!.GetLayoutType(ParentShellPageInstance.ShellViewModel.WorkingDirectory);

				if (layoutType != ParentShellPageInstance.CurrentPageType)
				{
					ParentShellPageInstance.NavigateWithArguments(layoutType, new NavigationArguments()
					{
						NavPathParam = navigationArguments!.NavPathParam,
						IsSearchResultPage = navigationArguments.IsSearchResultPage,
						SearchPathParam = navigationArguments.SearchPathParam,
						SearchQuery = navigationArguments.SearchQuery,
						IsLayoutSwitch = true,
						AssociatedTabInstance = ParentShellPageInstance
					});

					// Remove old layout from back stack
					ParentShellPageInstance.RemoveLastPageFromBackStack();
					ParentShellPageInstance.ResetNavigationStackLayoutMode();
				}

				ParentShellPageInstance.ShellViewModel.UpdateEmptyTextType();
			}
		}

		protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected override async void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);

			// Add item jumping handler
			CharacterReceived += Page_CharacterReceived;

			navigationArguments = (NavigationArguments)e.Parameter;
			ParentShellPageInstance = navigationArguments.AssociatedTabInstance;

			// Git properties are not loaded by default
			ParentShellPageInstance.ShellViewModel.EnabledGitProperties = GitProperties.None;

			InitializeCommandsViewModel();

			IsItemSelected = false;

			FolderSettings!.LayoutModeChangeRequested += BaseFolderSettings_LayoutModeChangeRequested;
			FolderSettings.GroupOptionPreferenceUpdated += FolderSettings_GroupOptionPreferenceUpdated;
			FolderSettings.GroupDirectionPreferenceUpdated += FolderSettings_GroupDirectionPreferenceUpdated;
			FolderSettings.GroupByDateUnitPreferenceUpdated += FolderSettings_GroupByDateUnitPreferenceUpdated;

			ParentShellPageInstance.ShellViewModel.EmptyTextType = EmptyTextType.None;
			ParentShellPageInstance.ToolbarViewModel.CanRefresh = true;

			if (!navigationArguments.IsSearchResultPage)
			{
				var previousDir = ParentShellPageInstance.ShellViewModel.WorkingDirectory;
				await ParentShellPageInstance.ShellViewModel.SetWorkingDirectoryAsync(navigationArguments.NavPathParam);

				// pathRoot will be empty on recycle bin path
				var workingDir = ParentShellPageInstance.ShellViewModel.WorkingDirectory ?? string.Empty;
				var pathRoot = GetPathRoot(workingDir);

				var isRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = isRecycleBin;

				// Can't go up from recycle bin
				ParentShellPageInstance.ToolbarViewModel.CanNavigateToParent = !(string.IsNullOrEmpty(pathRoot) || isRecycleBin);

				ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary = LibraryManager.IsLibraryPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = false;
				ParentShellPageInstance.ToolbarViewModel.PathControlDisplayText = navigationArguments.NavPathParam;

				if (ParentShellPageInstance.InstanceViewModel.FolderSettings.DirectorySortOption == SortOption.Path)
					ParentShellPageInstance.InstanceViewModel.FolderSettings.DirectorySortOption = SortOption.Name;

				if (ParentShellPageInstance.InstanceViewModel.FolderSettings.DirectoryGroupOption == GroupOption.FolderPath &&
					!ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary)
					ParentShellPageInstance.InstanceViewModel.FolderSettings.DirectoryGroupOption = GroupOption.None;

				if (!navigationArguments.IsLayoutSwitch || previousDir != workingDir)
					ParentShellPageInstance.ShellViewModel.RefreshItems(previousDir, SetSelectedItemsOnNavigation);
				else
					ParentShellPageInstance.ToolbarViewModel.CanGoForward = false;
			}
			else
			{
				await ParentShellPageInstance.ShellViewModel.SetWorkingDirectoryAsync(navigationArguments.SearchPathParam);

				ParentShellPageInstance.ToolbarViewModel.CanGoForward = false;

				// Impose no artificial restrictions on back navigation. Even in a search results page.
				ParentShellPageInstance.ToolbarViewModel.CanGoBack = true;

				ParentShellPageInstance.ToolbarViewModel.CanNavigateToParent = false;

				var workingDir = ParentShellPageInstance.ShellViewModel.WorkingDirectory ?? string.Empty;

				ParentShellPageInstance.InstanceViewModel.IsPageTypeRecycleBin = workingDir.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeMtpDevice = workingDir.StartsWith("\\\\?\\", StringComparison.Ordinal);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeFtp = FtpHelpers.IsFtpPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeZipFolder = ZipStorageFolder.IsZipPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeLibrary = LibraryManager.IsLibraryPath(workingDir);
				ParentShellPageInstance.InstanceViewModel.IsPageTypeSearchResults = true;

				if (!navigationArguments.IsLayoutSwitch)
				{
					var displayName = App.LibraryManager.TryGetLibrary(navigationArguments.SearchPathParam, out var lib) ? lib.Text : navigationArguments.SearchPathParam;
					await ParentShellPageInstance.UpdatePathUIToWorkingDirectoryAsync(null, string.Format("SearchPagePathBoxOverrideText".GetLocalizedResource(), navigationArguments.SearchQuery, displayName));
					var searchInstance = new Utils.Storage.FolderSearch
					{
						Query = navigationArguments.SearchQuery,
						Folder = navigationArguments.SearchPathParam,
					};

					_ = ParentShellPageInstance.ShellViewModel.SearchAsync(searchInstance);
				}
			}

			// Show controls that were hidden on the home page
			ParentShellPageInstance.InstanceViewModel.IsPageTypeNotHome = true;
			ParentShellPageInstance.ShellViewModel.UpdateGroupOptions();

			UpdateCollectionViewSource();
			FolderSettings.IsLayoutModeChanging = false;

			SetSelectedItemsOnNavigation();

			ItemContextMenuFlyout.Opening += ItemContextFlyout_Opening;
			BaseContextMenuFlyout.Opening += BaseContextFlyout_Opening;
		}

		public void SetSelectedItemsOnNavigation()
		{
			try
			{
				if (navigationArguments is not null &&
					navigationArguments.SelectItems is not null &&
					navigationArguments.SelectItems.Any())
				{
					List<ListedItem> listedItemsToSelect =
					[
						.. ParentShellPageInstance!.ShellViewModel.FilesAndFolders.ToList().Where((li) => navigationArguments.SelectItems.Contains(li.ItemNameRaw)),
					];

					ItemManipulationModel.SetSelectedItems(listedItemsToSelect);
					ItemManipulationModel.FocusSelectedItems();
				}
				else if (navigationArguments is not null && navigationArguments.FocusOnNavigation)
				{
					// Set focus on layout specific file list control
					ItemManipulationModel.FocusFileList();
				}
			}
			catch (Exception) { }
		}

		private async void FolderSettings_GroupOptionPreferenceUpdated(object? sender, GroupOption e)
		{
			await GroupPreferenceUpdatedAsync();
		}

		private async void FolderSettings_GroupDirectionPreferenceUpdated(object? sender, SortDirection e)
		{
			await GroupPreferenceUpdatedAsync();
		}

		private async void FolderSettings_GroupByDateUnitPreferenceUpdated(object? sender, GroupByDateUnit e)
		{
			await GroupPreferenceUpdatedAsync();
		}

		private async Task GroupPreferenceUpdatedAsync()
		{
			// Two or more of these running at the same time will cause a crash, so cancel the previous one before beginning
			groupingCancellationToken?.Cancel();
			groupingCancellationToken = new CancellationTokenSource();
			var token = groupingCancellationToken.Token;

			await ParentShellPageInstance!.ShellViewModel.GroupOptionsUpdatedAsync(token);

			UpdateCollectionViewSource();

			await ParentShellPageInstance.ShellViewModel.ReloadItemGroupHeaderImagesAsync();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			// Remove item jumping handler
			CharacterReceived -= Page_CharacterReceived;
			FolderSettings!.LayoutModeChangeRequested -= BaseFolderSettings_LayoutModeChangeRequested;
			FolderSettings.GroupOptionPreferenceUpdated -= FolderSettings_GroupOptionPreferenceUpdated;
			FolderSettings.GroupDirectionPreferenceUpdated -= FolderSettings_GroupDirectionPreferenceUpdated;
			FolderSettings.GroupByDateUnitPreferenceUpdated -= FolderSettings_GroupByDateUnitPreferenceUpdated;
			ItemContextMenuFlyout.Opening -= ItemContextFlyout_Opening;
			BaseContextMenuFlyout.Opening -= BaseContextFlyout_Opening;

			var parameter = e.Parameter as NavigationArguments;
			if (parameter is not null && !parameter.IsLayoutSwitch)
				ParentShellPageInstance!.ShellViewModel.CancelLoadAndClearFiles();
		}

		private async void ItemContextFlyout_Opening(object? sender, object e)
		{
			App.LastOpenedFlyout = sender as CommandBarFlyout;

			try
			{
				if (!ParentShellPageInstance!.IsCurrentInstance || !ParentShellPageInstance.IsCurrentPane)
				{
					// Wait until the pane and column become current
					await Task.WhenAny(ParentShellPageInstance.WhenIsCurrent(), Task.Delay(500));
					// Wait a little longer to ensure the page context is updated
					await Task.Delay(10);
				}

				// Workaround for item sometimes not getting selected
				if (!IsItemSelected && (sender as CommandBarFlyout)?.Target is ListViewItem { Content: ListedItem li })
					ItemManipulationModel.SetSelectedItem(li);

				if (IsItemSelected)
				{
					// Reset menu max height
					if (ItemContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
						itc.MaxHeight = Constants.UI.ContextMenuMaxHeight;

					shellContextMenuItemCancellationToken?.Cancel();
					shellContextMenuItemCancellationToken = new CancellationTokenSource();
					SelectedItemsPropertiesViewModel.CheckAllFileExtensions(SelectedItems!.Select(selectedItem => selectedItem?.FileExtension).ToList()!);

					shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
					var items = ContentPageContextFlyoutFactory.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: InstanceViewModel!, selectedItems: SelectedItems!, selectedItemsPropertiesViewModel: SelectedItemsPropertiesViewModel, commandsViewModel: CommandsViewModel!, shiftPressed: shiftPressed, itemViewModel: null);

					ItemContextMenuFlyout.PrimaryCommands.Clear();
					ItemContextMenuFlyout.SecondaryCommands.Clear();

					var (primaryElements, secondaryElements) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel(items);
					AddCloseHandler(ItemContextMenuFlyout, primaryElements, secondaryElements);
					primaryElements.ForEach(ItemContextMenuFlyout.PrimaryCommands.Add);
					secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth); // Set menu min width
					secondaryElements.ForEach(ItemContextMenuFlyout.SecondaryCommands.Add);

					if (InstanceViewModel!.CanTagFilesInPage)
						AddNewFileTagsToMenu(ItemContextMenuFlyout);

					if (!InstanceViewModel.IsPageTypeZipFolder && !InstanceViewModel.IsPageTypeFtp)
					{
						var shellMenuItems = await ContentPageContextFlyoutFactory.GetItemContextShellCommandsAsync(workingDir: ParentShellPageInstance.ShellViewModel.WorkingDirectory, selectedItems: SelectedItems!, shiftPressed: shiftPressed, showOpenMenu: false, shellContextMenuItemCancellationToken.Token);
						if (shellMenuItems.Any())
							await AddShellMenuItemsAsync(shellMenuItems, ItemContextMenuFlyout, shiftPressed);
						else
							RemoveOverflow(ItemContextMenuFlyout);
					}
					else
					{
						RemoveOverflow(ItemContextMenuFlyout);
					}
				}
			}
			catch (Exception error)
			{
				Debug.WriteLine(error);
			}
		}

		private async void BaseContextFlyout_Opening(object? sender, object e)
		{
			App.LastOpenedFlyout = sender as CommandBarFlyout;

			try
			{
				if (!ParentShellPageInstance!.IsCurrentInstance || !ParentShellPageInstance.IsCurrentPane)
				{
					// Wait until the pane and column become current
					await Task.WhenAny(ParentShellPageInstance.WhenIsCurrent(), Task.Delay(500));
					// Wait a little longer to ensure the page context is updated
					await Task.Delay(10);
				}

				ItemManipulationModel.ClearSelection();

				// Reset menu max height
				if (BaseContextMenuFlyout.GetValue(ContextMenuExtensions.ItemsControlProperty) is ItemsControl itc)
					itc.MaxHeight = Constants.UI.ContextMenuMaxHeight;

				shellContextMenuItemCancellationToken?.Cancel();
				shellContextMenuItemCancellationToken = new CancellationTokenSource();

				shiftPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
				var items = ContentPageContextFlyoutFactory.GetItemContextCommandsWithoutShellItems(currentInstanceViewModel: InstanceViewModel!, selectedItems: [ParentShellPageInstance!.ShellViewModel.CurrentFolder], commandsViewModel: CommandsViewModel!, shiftPressed: shiftPressed, itemViewModel: ParentShellPageInstance!.ShellViewModel, selectedItemsPropertiesViewModel: null);

				BaseContextMenuFlyout.PrimaryCommands.Clear();
				BaseContextMenuFlyout.SecondaryCommands.Clear();

				var (primaryElements, secondaryElements) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel(items);

				AddCloseHandler(BaseContextMenuFlyout, primaryElements, secondaryElements);

				primaryElements.ForEach(i => BaseContextMenuFlyout.PrimaryCommands.Add(i));

				// Set menu min width
				secondaryElements.OfType<FrameworkElement>().ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);
				secondaryElements.ForEach(i => BaseContextMenuFlyout.SecondaryCommands.Add(i));

				if (!InstanceViewModel!.IsPageTypeSearchResults && !InstanceViewModel.IsPageTypeZipFolder && !InstanceViewModel.IsPageTypeFtp)
				{
					var shellMenuItems = await ContentPageContextFlyoutFactory.GetItemContextShellCommandsAsync(workingDir: ParentShellPageInstance.ShellViewModel.WorkingDirectory, selectedItems: [], shiftPressed: shiftPressed, showOpenMenu: false, shellContextMenuItemCancellationToken.Token);
					if (shellMenuItems.Any())
						await AddShellMenuItemsAsync(shellMenuItems, BaseContextMenuFlyout, shiftPressed);
					else
						RemoveOverflow(BaseContextMenuFlyout);
				}
				else
				{
					RemoveOverflow(BaseContextMenuFlyout);
				}
			}
			catch (Exception error)
			{
				Debug.WriteLine(error);
			}
		}

		public void UpdateSelectionSize()
		{
			var items = (selectedItems?.Any() ?? false) ? selectedItems : SafetyExtensions.IgnoreExceptions(GetAllItems, App.Logger);
			if (items is null)
				return;

			var isSizeKnown = !items.Any(item => string.IsNullOrEmpty(item.FileSize));
			if (isSizeKnown)
			{
				decimal size = items.Sum(item => item.FileSizeBytes);
				SelectedItemsPropertiesViewModel.ItemSizeBytes = size;
				SelectedItemsPropertiesViewModel.ItemSize = size.ToSizeString();
			}
			else
			{
				SelectedItemsPropertiesViewModel.ItemSizeBytes = 0;
				SelectedItemsPropertiesViewModel.ItemSize = string.Empty;
			}

			SelectedItemsPropertiesViewModel.ItemSizeVisibility = isSizeKnown;
		}

		private void AddCloseHandler(CommandBarFlyout flyout, IList<ICommandBarElement> primaryElements, IList<ICommandBarElement> secondaryElements)
		{
			// Workaround for WinUI (#5508)
			var closeHandler = new RoutedEventHandler((s, e) => flyout.Hide());

			primaryElements
				.OfType<AppBarButton>()
				.ForEach(button => button.Click += closeHandler);

			var menuFlyoutItems = secondaryElements
				.OfType<AppBarButton>()
				.Select(item => item.Flyout)
				.OfType<MenuFlyout>()
				.SelectMany(menu => menu.Items);

			addCloseHandler(menuFlyoutItems);

			void addCloseHandler(IEnumerable<MenuFlyoutItemBase> menuFlyoutItems)
			{
				menuFlyoutItems.OfType<MenuFlyoutItem>()
					.ForEach(button => button.Click += closeHandler);
				menuFlyoutItems.OfType<MenuFlyoutSubItem>()
					.ForEach(menu => addCloseHandler(menu.Items));
			}
		}

		private void AddNewFileTagsToMenu(CommandBarFlyout contextMenu)
		{
			var fileTagsContextMenu = new FileTagsContextMenu(SelectedItems!);
			var overflowSeparator = contextMenu.SecondaryCommands.FirstOrDefault(x => x is FrameworkElement fe && fe.Tag as string == "OverflowSeparator") as AppBarSeparator;
			var index = contextMenu.SecondaryCommands.IndexOf(overflowSeparator);
			index = index >= 0 ? index : contextMenu.SecondaryCommands.Count;

			// Only show the edit tags flyout if settings is enabled
			if (!UserSettingsService.GeneralSettingsService.ShowEditTagsMenu)
				return;

			contextMenu.SecondaryCommands.Insert(index, new AppBarSeparator());
			contextMenu.SecondaryCommands.Insert(index + 1, new AppBarButton()
			{
				Label = "EditTags".GetLocalizedResource(),
				Content = new ThemedIcon()
				{
					Style = (Style)Application.Current.Resources["App.ThemedIcons.TagEdit"],
				},
				Flyout = fileTagsContextMenu
			});
		}

		private async Task AddShellMenuItemsAsync(List<ContextMenuFlyoutItemViewModel> shellMenuItems, CommandBarFlyout contextMenuFlyout, bool shiftPressed)
		{
			var openWithMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "openas" });
			var sendToMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "sendto" });
			var turnOnBitLockerMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem menuItem && menuItem.CommandString is not null && menuItem.CommandString.StartsWith("encrypt-bde"));
			var manageBitLockerMenuItem = shellMenuItems.FirstOrDefault(x => x.Tag is Win32ContextMenuItem { CommandString: "manage-bde" });
			var shellMenuItemsFiltered = shellMenuItems.Where(x => x != openWithMenuItem && x != sendToMenuItem && x != turnOnBitLockerMenuItem && x != manageBitLockerMenuItem).ToList();
			var mainShellMenuItems = shellMenuItemsFiltered.RemoveFrom(!UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu ? int.MaxValue : shiftPressed ? 6 : 0);
			var overflowShellMenuItemsUnfiltered = shellMenuItemsFiltered.Except(mainShellMenuItems).ToList();
			var overflowShellMenuItems = overflowShellMenuItemsUnfiltered.Where(
				(x, i) => (x.ItemType == ContextMenuFlyoutItemType.Separator &&
				overflowShellMenuItemsUnfiltered[i + 1 < overflowShellMenuItemsUnfiltered.Count ? i + 1 : i].ItemType != ContextMenuFlyoutItemType.Separator)
				|| x.ItemType != ContextMenuFlyoutItemType.Separator).ToList();

			var overflowItems = ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(overflowShellMenuItems);
			var mainItems = ContextFlyoutModelToElementHelper.GetAppBarButtonsFromModelIgnorePrimary(mainShellMenuItems);

			var openedPopups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopups(MainWindow.Instance);
			var secondaryMenu = openedPopups.FirstOrDefault(popup => popup.Name == "OverflowPopup");

			var itemsControl = secondaryMenu?.Child.FindDescendant<ItemsControl>();
			if (itemsControl is not null && secondaryMenu is not null)
			{
				contextMenuFlyout.SetValue(ContextMenuExtensions.ItemsControlProperty, itemsControl);

				var ttv = secondaryMenu.TransformToVisual(MainWindow.Instance.Content);
				var cMenuPos = ttv.TransformPoint(new Point(0, 0));

				var requiredHeight = contextMenuFlyout.SecondaryCommands.Concat(mainItems).Count(x => x is not AppBarSeparator) * Constants.UI.ContextMenuSecondaryItemsHeight;
				var availableHeight = MainWindow.Instance.Bounds.Height - cMenuPos.Y - Constants.UI.ContextMenuPrimaryItemsHeight;

				// Set menu max height to current height (Avoid menu repositioning)
				if (requiredHeight > availableHeight)
					itemsControl.MaxHeight = Math.Min(Constants.UI.ContextMenuMaxHeight, Math.Max(itemsControl.ActualHeight, Math.Min(availableHeight, requiredHeight)));

				// Set items max width to current menu width (#5555)
				mainItems.OfType<FrameworkElement>().ForEach(x => x.MaxWidth = itemsControl.ActualWidth - Constants.UI.ContextMenuLabelMargin);
			}

			ContentPageContextFlyoutFactory.SwapPlaceholderWithShellOption(
				contextMenuFlyout,
				"TurnOnBitLockerPlaceholder",
				turnOnBitLockerMenuItem,
				contextMenuFlyout.SecondaryCommands.Count - 2
			);
			ContentPageContextFlyoutFactory.SwapPlaceholderWithShellOption(
				contextMenuFlyout,
				"ManageBitLockerPlaceholder",
				manageBitLockerMenuItem,
				contextMenuFlyout.SecondaryCommands.Count - 2
			);

			var overflowItem = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") as AppBarButton;
			if (overflowItem is not null)
			{
				var overflowItemFlyout = overflowItem.Flyout as MenuFlyout;
				if (overflowItemFlyout is not null)
				{
					if (overflowItemFlyout.Items.Count > 0)
						overflowItemFlyout.Items.Insert(0, new MenuFlyoutSeparator());

					var index = contextMenuFlyout.SecondaryCommands.Count - 2;
					foreach (var i in mainItems)
					{
						index++;
						contextMenuFlyout.SecondaryCommands.Insert(index, i);
					}

					index = 0;
					foreach (var i in overflowItems)
					{
						overflowItemFlyout.Items.Insert(index, i);
						index++;
					}

					if (overflowItemFlyout.Items.Count > 0 && UserSettingsService.GeneralSettingsService.MoveShellExtensionsToSubMenu)
					{
						overflowItem.Label = "ShowMoreOptions".GetLocalizedResource();
						overflowItem.IsEnabled = true;
					}
					else
					{
						overflowItem.Visibility = Visibility.Collapsed;

						// Hide separators at the end of the menu
						while (contextMenuFlyout.SecondaryCommands.LastOrDefault(x => x is UIElement element && element.Visibility is Visibility.Visible) is AppBarSeparator separator)
							separator.Visibility = Visibility.Collapsed;
					}
				}
			}
			else
			{
				mainItems.ForEach(x => contextMenuFlyout.SecondaryCommands.Add(x));
			}

			// Add items to openwith dropdown
			var openWithOverflow = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "OpenWithOverflow") as AppBarButton;

			var openWith = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "OpenWith") as AppBarButton;
			if (openWithMenuItem?.LoadSubMenuAction is not null && openWithOverflow is not null && openWith is not null)
			{
				await openWithMenuItem.LoadSubMenuAction();
				var openWithSubItems = ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(ShellContextFlyoutFactory.GetOpenWithItems(shellMenuItems));

				if (openWithSubItems is not null)
				{
					var flyout = (MenuFlyout)openWithOverflow.Flyout;

					flyout.Items.Clear();

					foreach (var item in openWithSubItems)
						flyout.Items.Add(item);

					openWithOverflow.Flyout = flyout;
					openWith.Visibility = Visibility.Collapsed;
					openWithOverflow.Visibility = Visibility.Visible;

					// TODO delete this when https://github.com/microsoft/microsoft-ui-xaml/issues/9409 is resolved
					openWithOverflow.Content = new ThemedIconModel()
					{
						ThemedIconStyle = "App.ThemedIcons.OpenWith"
					}.ToThemedIcon();
					openWithOverflow.Label = "OpenWith".GetLocalizedResource();
				}
			}

			// Add items to sendto dropdown
			if (UserSettingsService.GeneralSettingsService.ShowSendToMenu)
			{
				var sendToOverflow = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "SendToOverflow") as AppBarButton;

				var sendTo = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton abb && (abb.Tag as string) == "SendTo") as AppBarButton;
				if (sendToMenuItem?.LoadSubMenuAction is not null && sendToOverflow is not null && sendTo is not null)
				{
					await sendToMenuItem.LoadSubMenuAction();
					var sendToSubItems = ContextFlyoutModelToElementHelper.GetMenuFlyoutItemsFromModel(ShellContextFlyoutFactory.GetSendToItems(shellMenuItems));

					if (sendToSubItems is not null)
					{
						var flyout = (MenuFlyout)sendToOverflow.Flyout;

						flyout.Items.Clear();

						foreach (var item in sendToSubItems)
							flyout.Items.Add(item);

						sendToOverflow.Flyout = flyout;
						sendTo.Visibility = Visibility.Collapsed;
						sendToOverflow.Visibility = Visibility.Visible;
					}
				}
			}

			// Filter mainShellMenuItems that have a non-null LoadSubMenuAction
			var mainItemsWithSubMenu = mainShellMenuItems.Where(x => x.LoadSubMenuAction is not null);
			
			var mainSubMenuTasks = mainItemsWithSubMenu.Select(async item =>
			{
				await item.LoadSubMenuAction();
				ShellContextFlyoutFactory.AddItemsToMainMenu(mainItems, item);
			});

			// Filter overflowShellMenuItems that have a non-null LoadSubMenuAction
			var overflowItemsWithSubMenu = overflowShellMenuItems.Where(x => x.LoadSubMenuAction is not null);

			var overflowSubMenuTasks = overflowItemsWithSubMenu.Select(async item =>
			{
				await item.LoadSubMenuAction();
				ShellContextFlyoutFactory.AddItemsToOverflowMenu(overflowItem, item);
			});
			
			itemsControl?.Items.OfType<FrameworkElement>().ForEach(item =>
			{
				// Enable CharacterEllipsis text trimming for menu items
				if (item.FindDescendant("OverflowTextLabel") is TextBlock label)
					label.TextTrimming = TextTrimming.CharacterEllipsis;

				// Close main menu when clicking on subitems (#5508)
				if ((item as AppBarButton)?.Flyout as MenuFlyout is MenuFlyout flyout)
				{
					Action<IList<MenuFlyoutItemBase>> clickAction = null!;
					clickAction = (items) =>
					{
						items.OfType<MenuFlyoutItem>().ForEach(i =>
						{
							i.Click += new RoutedEventHandler((s, e) => contextMenuFlyout.Hide());
						});
						items.OfType<MenuFlyoutSubItem>().ForEach(i =>
						{
							clickAction(i.Items);
						});
					};

					clickAction(flyout.Items);
				}
			});
			
			await Task.WhenAll(mainSubMenuTasks.Concat(overflowSubMenuTasks));
		}

		private void RemoveOverflow(CommandBarFlyout contextMenuFlyout)
		{
			var overflowItem = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarButton appBarButton && (appBarButton.Tag as string) == "ItemOverflow") as AppBarButton;
			var overflowSeparator = contextMenuFlyout.SecondaryCommands.FirstOrDefault(x => x is AppBarSeparator appBarSeparator && (appBarSeparator.Tag as string) == "OverflowSeparator") as AppBarSeparator;

			if (overflowItem is not null)
				overflowItem.Visibility = Visibility.Collapsed;
			if (overflowSeparator is not null)
				overflowSeparator.Visibility = Visibility.Collapsed;
		}

		protected virtual void Page_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
		{
			if (ParentShellPageInstance!.IsCurrentInstance)
			{
				char letter = args.Character;
				JumpString += letter.ToString().ToLowerInvariant();
			}
		}

		protected virtual void FileList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			try
			{
				var itemList = e.Items.OfType<ListedItem>().ToList();
				var firstItem = itemList.FirstOrDefault();
				var sortedItems = SortingHelper.OrderFileList(itemList, FolderSettings.DirectorySortOption, FolderSettings.DirectorySortDirection, FolderSettings.SortDirectoriesAlongsideFiles, FolderSettings.SortFilesFirst).ToList();
				var orderedItems = sortedItems.SkipWhile(x => x != firstItem).Concat(sortedItems.TakeWhile(x => x != firstItem)).ToList();

				var shellItemList = SafetyExtensions.IgnoreExceptions(() => orderedItems.Select(x => new VanaraWindowsShell.ShellItem(x.ItemPath)).ToArray());
				if (shellItemList?[0].FileSystemPath is not null && !InstanceViewModel.IsPageTypeSearchResults)
				{
					var iddo = shellItemList[0].Parent.GetChildrenUIObjects<IDataObject>(HWND.NULL, shellItemList);
					shellItemList.ForEach(x => x.Dispose());
					var dataObjectProvider = e.Data.As<Shell32.IDataObjectProvider>();
					dataObjectProvider.SetDataObject(iddo);
				}
				else
				{
					// Only support IStorageItem capable paths
					var storageItemList = orderedItems.Where(x => !(x.IsHiddenItem && x.IsLinkItem && x.IsRecycleBinItem && x.IsShortcut)).Select(x => VirtualStorageItem.FromListedItem(x));
					e.Data.SetStorageItems(storageItemList, false);
				}

				// Set can window to front (#13255)
				MainWindow.Instance.SetCanWindowToFront(false);
				itemDragging = true;
			}
			catch (Exception)
			{
				e.Cancel = true;
			}
		}

		protected virtual void FileList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
		{
			// Set can window to front (#13255)
			itemDragging = false;
			MainWindow.Instance.SetCanWindowToFront(true);
		}

		private void Item_DragLeave(object sender, DragEventArgs e)
		{
			var item = GetItemFromElement(sender);

			// Reset dragged over item
			if (item == dragOverItem)
				dragOverItem = null;
		}

		private async void Item_DragOver(object sender, DragEventArgs e)
		{
			var item = GetItemFromElement(sender);
			if (item is null)
				return;

			DragOperationDeferral? deferral = null;

			try
			{
				deferral = e.GetDeferral();

				if (FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				{
					e.Handled = true;

					var draggedItems = await FilesystemHelpers.GetDraggedStorageItems(e.DataView);

					if (draggedItems.Any(draggedItem => draggedItem.Path == item.ItemPath))
					{
						e.AcceptedOperation = DataPackageOperation.None;
					}
					else if (!draggedItems.Any())
					{
						e.AcceptedOperation = DataPackageOperation.None;
					}
					else
					{
						e.DragUIOverride.IsCaptionVisible = true;

						if (item.IsExecutable || item.IsScriptFile)
						{
							e.DragUIOverride.Caption = $"{"OpenWith".GetLocalizedResource()} {item.Name}";
							e.AcceptedOperation = DataPackageOperation.Link;
						}
						// Items from the same drive as this folder are dragged into this folder, so we move the items instead of copy
						else if (e.Modifiers.HasFlag(DragDropModifiers.Alt) || e.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
						{
							e.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Link;
						}
						else if (e.Modifiers.HasFlag(DragDropModifiers.Control))
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
						else if (e.Modifiers.HasFlag(DragDropModifiers.Shift))
						{
							e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), item.Name);
							// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
							e.AcceptedOperation = DataPackageOperation.Move | DataPackageOperation.Copy;
						}
						else if (draggedItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
							|| ZipStorageFolder.IsZipPath(item.ItemPath))
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
						else if (draggedItems.AreItemsInSameDrive(item.ItemPath))
						{
							e.DragUIOverride.Caption = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), item.Name);
							// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
							e.AcceptedOperation = DataPackageOperation.Move | DataPackageOperation.Copy;
						}
						else
						{
							e.DragUIOverride.Caption = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), item.Name);
							e.AcceptedOperation = DataPackageOperation.Copy;
						}
					}
				}

				if (dragOverItem != item)
				{
					dragOverItem = item;
					dragOverTimer.Stop();

					if (e.AcceptedOperation != DataPackageOperation.None)
					{
						dragOverTimer.Debounce(() =>
						{
							if (dragOverItem is not null && !dragOverItem.IsExecutable)
							{
								dragOverTimer.Stop();
								ItemManipulationModel.SetSelectedItem(dragOverItem);
								dragOverItem = null;
								Commands.OpenItem.ExecuteAsync();
							}
						},
						TimeSpan.FromMilliseconds(Constants.DragAndDrop.HoverToOpenTimespan), false);
					}
				}
			}
			finally
			{
				deferral?.Complete();
			}
		}

		protected virtual async void Item_Drop(object sender, DragEventArgs e)
		{
			var deferral = e.GetDeferral();

			e.Handled = true;

			// Reset dragged over item
			dragOverItem = null;

			var item = GetItemFromElement(sender);
			if (item is not null)
				await ParentShellPageInstance!.FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.DataView, (item as ShortcutItem)?.TargetPath ?? item.ItemPath, false, true, item.IsExecutable, item.IsScriptFile);

			deferral.Complete();
		}

		protected void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			RefreshContainer(args.ItemContainer, args.InRecycleQueue);
			RefreshItem(args.ItemContainer, args.Item, args.InRecycleQueue, args);

			// Set can window to front (#13255)
			itemDragging = false;
			MainWindow.Instance.SetCanWindowToFront(true);
		}

		private void RefreshContainer(SelectorItem container, bool inRecycleQueue)
		{
			container.PointerPressed -= FileListItem_PointerPressed;
			container.PointerEntered -= FileListItem_PointerEntered;
			container.PointerExited -= FileListItem_PointerExited;
			container.Tapped -= FileListItem_Tapped;
			container.DoubleTapped -= FileListItem_DoubleTapped;
			container.RightTapped -= FileListItem_RightTapped;

			if (inRecycleQueue)
			{
				UninitializeDrag(container);
			}
			else
			{
				container.PointerPressed += FileListItem_PointerPressed;
				container.PointerEntered += FileListItem_PointerEntered;
				container.PointerExited += FileListItem_PointerExited;
				container.Tapped += FileListItem_Tapped;
				container.DoubleTapped += FileListItem_DoubleTapped;
				container.RightTapped += FileListItem_RightTapped;
			}
		}

		private void RefreshItem(SelectorItem container, object item, bool inRecycleQueue, ContainerContentChangingEventArgs args)
		{
			if (item is not ListedItem listedItem)
				return;

			if (inRecycleQueue)
			{
				ParentShellPageInstance!.ShellViewModel.CancelExtendedPropertiesLoadingForItem(listedItem);
			}
			else
			{
				InitializeDrag(container, listedItem);

				if (!listedItem.ItemPropertiesInitialized)
				{
					uint callbackPhase = 3;
					args.RegisterUpdateCallback(callbackPhase, async (s, c) =>
					{
						await ParentShellPageInstance!.ShellViewModel.LoadExtendedItemPropertiesAsync(listedItem);
						if (ParentShellPageInstance.ShellViewModel.EnabledGitProperties is not GitProperties.None && listedItem is IGitItem gitItem)
							await ParentShellPageInstance.ShellViewModel.LoadGitPropertiesAsync(gitItem);
					});
				}
			}
		}

		protected internal void FileListItem_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			// Set can window to front and bring the window to the front if necessary (#13255)
			if ((!itemDragging) && MainWindow.Instance.SetCanWindowToFront(true))
				Win32Helper.BringToForegroundEx(new(MainWindow.Instance.WindowHandle));

			if (sender is not SelectorItem selectorItem)
				return;

			if (selectorItem.IsSelected)
			{
				if (e.KeyModifiers == VirtualKeyModifiers.Control)
				{
					selectorItem.IsSelected = false;

					// Prevent issues arising caused by the default handlers attempting to select the item that has just been deselected by ctrl + click
					e.Handled = true;
				}
				else
				{
					SelectedItem = GetItemFromElement(sender);
				}
			}
			else if (e.GetCurrentPoint(selectorItem).Properties.IsLeftButtonPressed)
			{
				selectorItem.IsSelected = true;
			}
		}

		protected internal void FileListItem_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			// Set can window to front (#13255)
			if (sender is SelectorItem selectorItem && selectorItem.IsSelected)
				MainWindow.Instance.SetCanWindowToFront(false);

			if (!UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				return;

			hoveredItem = GetItemFromElement(sender);

			hoverTimer.Stop();
			hoverTimer.Debounce(() =>
			{
				if (hoveredItem is null)
					return;

				hoverTimer.Stop();

				// Selection of multiple individual items with control
				if (e.KeyModifiers == VirtualKeyModifiers.Control &&
					selectedItems is not null)
				{
					ItemManipulationModel.AddSelectedItem(hoveredItem);
				}
				// Selection of a range of items with shift
				else if (e.KeyModifiers == VirtualKeyModifiers.Shift &&
					selectedItems is not null &&
					selectedItems.Any())
				{
					var last = selectedItems.Last();
					byte found = 0;
					for (int i = 0; i < ItemsControl.Items.Count && found != 2; i++)
					{
						if (ItemsControl.Items[i] == last || ItemsControl.Items[i] == hoveredItem)
							found++;

						if (found != 0 && !selectedItems.Contains(ItemsControl.Items[i]))
							ItemManipulationModel.AddSelectedItem((ListedItem)ItemsControl.Items[i]);
					}
				}
				// Avoid resetting the selection if multiple items are selected
				else if (SelectedItems is null || SelectedItems.Count <= 1)
				{
					ItemManipulationModel.SetSelectedItem(hoveredItem);
				}
			},
			TimeSpan.FromMilliseconds(1000), false);
		}

		protected internal void FileListItem_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			// Set can window to front (#13255)
			if (!itemDragging)
				MainWindow.Instance.SetCanWindowToFront(true);

			if (!UserSettingsService.FoldersSettingsService.SelectFilesOnHover)
				return;

			hoverTimer.Stop();
			hoveredItem = null;
		}

		protected void FileListItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			// Set can window to front and bring the window to the front if necessary (#13255)
			if ((!itemDragging) && MainWindow.Instance.SetCanWindowToFront(true))
				Win32Helper.BringToForegroundEx(new(MainWindow.Instance.WindowHandle));
		}

		protected void FileListItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Set can window to front and bring the window to the front if necessary (#13255)
			if ((!itemDragging) && MainWindow.Instance.SetCanWindowToFront(true))
				Win32Helper.BringToForegroundEx(new(MainWindow.Instance.WindowHandle));
		}

		protected void FileListItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			// Set can window to front and bring the window to the front if necessary (#13255)
			if ((!itemDragging) && MainWindow.Instance.SetCanWindowToFront(true))
				Win32Helper.BringToForegroundEx(new(MainWindow.Instance.WindowHandle));

			var rightClickedItem = GetItemFromElement(sender);

			if (rightClickedItem is not null && !((SelectorItem)sender).IsSelected)
				ItemManipulationModel.SetSelectedItem(rightClickedItem);
		}

		protected void InitializeDrag(UIElement container, ListedItem item)
		{
			if (item is null)
				return;

			UninitializeDrag(container);
			if ((item.PrimaryItemAttribute == StorageItemTypes.Folder && !StorageTrashBinService.IsUnderTrashBin(item.ItemPath))
				|| item.IsExecutable
				|| item.IsScriptFile)
			{
				container.AllowDrop = true;
				container.AddHandler(UIElement.DragOverEvent, Item_DragOverEventHandler, true);
				container.DragLeave += Item_DragLeave;
				container.Drop += Item_Drop;
			}
		}

		protected void UninitializeDrag(UIElement element)
		{
			element.AllowDrop = false;
			element.RemoveHandler(UIElement.DragOverEvent, Item_DragOverEventHandler);
			element.DragLeave -= Item_DragLeave;
			element.Drop -= Item_Drop;
		}

		public virtual void Dispose()
		{
			InfoPaneViewModel?.Dispose();
			UnhookBaseEvents();
		}

		protected void ItemsLayout_DragOver(object sender, DragEventArgs e)
		{
			CommandsViewModel?.DragOverCommand?.Execute(e);
		}

		protected virtual void ItemsLayout_Drop(object sender, DragEventArgs e)
		{
			CommandsViewModel?.DropCommand?.Execute(e);
		}

		private void UpdateCollectionViewSource()
		{
			if (ParentShellPageInstance is null)
				return;

			if (ParentShellPageInstance.ShellViewModel.FilesAndFolders.IsGrouped)
			{
				var newSource = new CollectionViewSource()
				{
					IsSourceGrouped = true,
					Source = ParentShellPageInstance.ShellViewModel.FilesAndFolders.GroupedCollection
				};
				CollectionViewSource = newSource;
			}
			else
			{
				ZoomIn();

				var newSource = new CollectionViewSource()
				{
					IsSourceGrouped = false,
					Source = ParentShellPageInstance.ShellViewModel.FilesAndFolders
				};
				CollectionViewSource = newSource;
			}
		}

		protected virtual void ZoomIn()
		{
		}

		protected void SemanticZoom_ViewChangeStarted(object sender, SemanticZoomViewChangedEventArgs e)
		{
			if (e.IsSourceZoomedInView)
				return;

			// According to the docs this isn't necessary, but it would crash otherwise
			var destination = e.DestinationItem.Item as GroupedCollection<ListedItem>;

			e.DestinationItem.Item = destination?.FirstOrDefault();
		}

		protected void StackPanel_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
			if (element is not null)
				VisualStateManager.GoToState(element, "PointerOver", true);
		}

		protected void StackPanel_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
			if (element is not null)
				VisualStateManager.GoToState(element, "Normal", true);
		}

		protected void RootPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			var element = (sender as UIElement)?.FindAscendant<ListViewBaseHeaderItem>();
			if (element is not null)
				VisualStateManager.GoToState(element, "Pressed", true);
		}

		private void ItemManipulationModel_RefreshItemsOpacityInvoked(object? sender, EventArgs e)
		{
			var items = GetAllItems();
			if (items is null)
				return;

			foreach (ListedItem listedItem in items)
			{
				if (listedItem.IsHiddenItem)
					listedItem.Opacity = Constants.UI.DimItemOpacity;
				else
					listedItem.Opacity = 1;
			}
		}

		private void View_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
		{
			if (ParentShellPageInstance is not null)
				ParentShellPageInstance.ToolbarViewModel.HasItem = CollectionViewSource.View.Any();
		}

		virtual public void StartRenameItem()
		{
		}

		public void CheckRenameDoubleClick(object clickedItem)
		{
			if (clickedItem is ListedItem item)
			{
				if (item == preRenamingItem)
				{
					tapDebounceTimer.Debounce(() =>
					{
						if (item == preRenamingItem)
						{
							StartRenameItem();
							tapDebounceTimer.Stop();
						}
					},
					TimeSpan.FromMilliseconds(1500));
				}
				else
				{
					tapDebounceTimer.Stop();
					preRenamingItem = item;
				}
			}
			else
			{
				ResetRenameDoubleClick();
			}
		}

		public void ResetRenameDoubleClick()
		{
			preRenamingItem = null;
			tapDebounceTimer.Stop();
		}

		protected async Task ValidateItemNameInputTextAsync(TextBox textBox, TextBoxBeforeTextChangingEventArgs args, Action<bool> showError)
		{
			if (FilesystemHelpers.ContainsRestrictedCharacters(args.NewText))
			{
				args.Cancel = true;

				await DispatcherQueue.EnqueueOrInvokeAsync(() =>
				{
					var oldSelection = textBox.SelectionStart + textBox.SelectionLength;
					var oldText = textBox.Text;
					textBox.Text = FilesystemHelpers.FilterRestrictedCharacters(args.NewText);
					textBox.SelectionStart = oldSelection + textBox.Text.Length - oldText.Length;
					showError?.Invoke(true);
				});
			}
			else
			{
				showError?.Invoke(false);
			}
		}

		public sealed class ContextMenuExtensions : DependencyObject
		{
			public static ItemsControl GetItemsControl(DependencyObject obj)
			{
				return (ItemsControl)obj.GetValue(ItemsControlProperty);
			}

			public static void SetItemsControl(DependencyObject obj, ItemsControl value)
			{
				obj.SetValue(ItemsControlProperty, value);
			}

			public static readonly DependencyProperty ItemsControlProperty =
				DependencyProperty.RegisterAttached("ItemsControl", typeof(ItemsControl), typeof(ContextMenuExtensions), new PropertyMetadata(null));
		}
	}
}
