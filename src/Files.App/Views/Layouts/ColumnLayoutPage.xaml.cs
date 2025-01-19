// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Files.App.UserControls.Selection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents the base page of Column View
	/// </summary>
	public sealed partial class ColumnLayoutPage : BaseGroupableLayoutPage
	{
		// Fields

		private readonly DispatcherQueueTimer doubleClickTimer;

		private ColumnsLayoutPage? columnsOwner;

		private ListViewItem? openedFolderPresenter;

		private bool isDraggingSelectionRectangle = false;

		public event EventHandler? ItemInvoked;
		public event EventHandler? ItemTapped;

		// Properties

		protected override ListViewBase ListViewBase => FileList;
		protected override SemanticZoom RootZoom => RootGridZoom;
		public ScrollViewer? ContentScroller { get; private set; }

		/// <summary>
		/// Row height in the Columns View
		/// </summary>
		public int RowHeight
		{
			get => LayoutSizeKindHelper.GetColumnsViewRowHeight(UserSettingsService.LayoutSettingsService.ColumnsViewSize);
		}

		/// <summary>
		/// Icon Box size layout. The value is increased by 4px to account for icon overlays.
		/// </summary>
		public int IconBoxSize
		{
			get => (int)(LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.ColumnView) + 4);
		}

		/// <summary>
		/// This reference is used to prevent unnecessary icon reloading by only reloading icons when their
		/// size changes, even if the layout size changes (since some layout sizes share the same icon size).
		/// </summary>
		private uint currentIconSize;

		// Constructor

		public ColumnLayoutPage() : base()
		{
			InitializeComponent();
			var selectionRectangle = RectangleSelection.Create(FileList, SelectionRectangle, FileList_SelectionChanged);
			selectionRectangle.SelectionStarted += SelectionRectangle_SelectionStarted;
			selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
			ItemInvoked += ColumnViewBase_ItemInvoked;
			GotFocus += ColumnViewBase_GotFocus;

			doubleClickTimer = DispatcherQueue.CreateTimer();
		}

		// Methods

		private void FileList_Loaded(object sender, RoutedEventArgs e)
		{
			ContentScroller = FileList.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer");
		}

		private void ColumnViewBase_GotFocus(object sender, RoutedEventArgs e)
		{
			if (FileList.SelectedItem == null && openedFolderPresenter != null)
			{
				openedFolderPresenter.Focus(FocusState.Programmatic);
				FileList.SelectedItem = FileList.ItemFromContainer(openedFolderPresenter);
			}
		}

		private void ColumnViewBase_ItemInvoked(object? sender, EventArgs e)
		{
			ClearOpenedFolderSelectionIndicator();
			openedFolderPresenter = FileList.ContainerFromItem(FileList.SelectedItem) as ListViewItem;
		}

		internal void ClearOpenedFolderSelectionIndicator()
		{
			if (openedFolderPresenter is null)
				return;

			openedFolderPresenter.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
			SetFolderBackground(openedFolderPresenter, new SolidColorBrush(Microsoft.UI.Colors.Transparent));
			openedFolderPresenter = null;
		}

		protected override void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			try
			{
				FileList.ScrollIntoView(e, ScrollIntoViewAlignment.Default);
			}
			catch (Exception)
			{
				// Catch error where row index could not be found
			}
		}

		protected override void ItemManipulationModel_ScrollToTopInvoked(object? sender, EventArgs e)
		{
			ContentScroller?.ChangeView(null, 0, null, true);
		}

		protected override void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems?.Any() ?? false)
			{
				FileList.ScrollIntoView(SelectedItems.Last());
				(FileList.ContainerFromItem(SelectedItems.Last()) as ListViewItem)?.Focus(FocusState.Keyboard);
			}
		}

		protected override void ItemManipulationModel_AddSelectedItemInvoked(object? sender, ListedItem e)
		{
			if (NextRenameIndex != 0 && TryStartRenameNextItem(e))
				return;

			FileList?.SelectedItems.Add(e);
		}

		protected override void ItemManipulationModel_RemoveSelectedItemInvoked(object? sender, ListedItem e)
		{
			FileList?.SelectedItems.Remove(e);
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			if (eventArgs.Parameter is NavigationArguments navArgs)
			{
				columnsOwner = (navArgs.AssociatedTabInstance as FrameworkElement)?.FindAscendant<ColumnsLayoutPage>();
				var index = (navArgs.AssociatedTabInstance as ColumnShellPage)?.ColumnParams?.Column;
				navArgs.FocusOnNavigation = index == columnsOwner?.FocusIndex;

				if (index < columnsOwner?.FocusIndex)
					FileList.ContainerContentChanging += HighlightPathDirectory;
			}

			base.OnNavigatedTo(eventArgs);

			currentIconSize = LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.ColumnView);

			UserSettingsService.LayoutSettingsService.PropertyChanged += LayoutSettingsService_PropertyChanged;

			SetItemContainerStyle();
		}

		private void HighlightPathDirectory(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.Item is ListedItem item && columnsOwner?.OwnerPath is string ownerPath
				&& (ownerPath == item.ItemPath || ownerPath.StartsWith(item.ItemPath) && ownerPath[item.ItemPath.Length] is '/' or '\\'))
			{
				SetFolderBackground(args.ItemContainer as ListViewItem, this.Resources["ListViewItemBackgroundSelected"] as SolidColorBrush);

				openedFolderPresenter = FileList.ContainerFromItem(item) as ListViewItem;
				FileList.ContainerContentChanging -= HighlightPathDirectory;
			}
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			UserSettingsService.LayoutSettingsService.PropertyChanged -= LayoutSettingsService_PropertyChanged;
		}

		private void LayoutSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ILayoutSettingsService.ColumnsViewSize))
			{
				// Get current scroll position
				var previousOffset = ContentScroller?.VerticalOffset;

				NotifyPropertyChanged(nameof(RowHeight));
				NotifyPropertyChanged(nameof(IconBoxSize));

				// Update the container style to match the item size
				SetItemContainerStyle();

				// Restore correct scroll position
				ContentScroller?.ChangeView(null, previousOffset, null);

				// Check if icons need to be reloaded
				var newIconSize = LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.ColumnView);
				if (newIconSize != currentIconSize)
				{
					currentIconSize = newIconSize;
					_ = ReloadItemIconsAsync();
				}
			}
		}

		private async Task ReloadItemIconsAsync()
		{
			if (ParentShellPageInstance is null)
				return;

			ParentShellPageInstance.ShellViewModel.CancelExtendedPropertiesLoading();
			var filesAndFolders = ParentShellPageInstance.ShellViewModel.FilesAndFolders.ToList();
			foreach (ListedItem listedItem in filesAndFolders)
			{
				listedItem.ItemPropertiesInitialized = false;
				if (FileList.ContainerFromItem(listedItem) is not null)
					await ParentShellPageInstance.ShellViewModel.LoadExtendedItemPropertiesAsync(listedItem);
			}
		}

		override public void StartRenameItem()
		{
			StartRenameItem("ListViewTextBoxItemName");
		}

		private async void ItemNameTextBox_BeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs args)
		{
			if (IsRenamingItem)
			{
				await ValidateItemNameInputTextAsync(textBox, args, (showError) =>
				{
					FileNameTeachingTip.Visibility = showError ? Visibility.Visible : Visibility.Collapsed;
					FileNameTeachingTip.IsOpen = showError;
				});
			}
		}

		protected override void EndRename(TextBox textBox)
		{
			FileNameTeachingTip.IsOpen = false;
			IsRenamingItem = false;

			// Unsubscribe from events
			if (textBox is not null)
			{
				textBox!.LostFocus -= RenameTextBox_LostFocus;
				textBox.KeyDown -= RenameTextBox_KeyDown;
			}

			if (textBox is not null && textBox.Parent is not null)
			{
				ListViewItem? listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
				if (listViewItem is null)
					return;

				// Re-focus selected list item
				listViewItem.Focus(FocusState.Programmatic);

				TextBlock? textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
				textBox!.Visibility = Visibility.Collapsed;
				textBlock!.Visibility = Visibility.Visible;
			}
		}

		public override void ResetItemOpacity()
		{
			// throw new NotImplementedException();
		}

		protected override bool CanGetItemFromElement(object element)
			=> element is ListViewItem;

		/// <summary>
		/// Sets the item size and spacing
		/// </summary>
		private void SetItemContainerStyle()
		{
			if (UserSettingsService.LayoutSettingsService.ColumnsViewSize == ColumnsViewSizeKind.Compact)
			{
				// Toggle style to force item size to update
				FileList.ItemContainerStyle = RegularItemContainerStyle;

				// Set correct style
				FileList.ItemContainerStyle = CompactItemContainerStyle;
			}
			else
			{
				// Toggle style to force item size to update
				FileList.ItemContainerStyle = CompactItemContainerStyle;

				// Set correct style
				FileList.ItemContainerStyle = RegularItemContainerStyle;
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			columnsOwner = null;
		}

		protected override void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			base.FileList_SelectionChanged(sender, e);
			if (e is null)
				return;

			if (e.AddedItems.Count > 0)
				columnsOwner?.HandleSelectionChange(this);

			if (e.RemovedItems.Count > 0 && openedFolderPresenter != null)
			{
				SetFolderBackground(openedFolderPresenter, this.Resources["ListViewItemBackgroundSelected"] as SolidColorBrush);
			}

			if (SelectedItems?.Count == 1 && SelectedItem?.PrimaryItemAttribute is StorageItemTypes.Folder)
			{
				// // Prevents the first selected folder from opening if the user is currently dragging the selection rectangle (#13418)
				if (isDraggingSelectionRectangle)
				{
					CloseFolder();
					return;
				}

				if (openedFolderPresenter == FileList.ContainerFromItem(SelectedItem))
					return;

				// Open the selected folder if selected through tap
				if (UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick && !isDraggingSelectionRectangle)
					ItemInvoked?.Invoke(new ColumnParam { Source = this, NavPathParam = (SelectedItem is IShortcutItem sht ? sht.TargetPath : SelectedItem.ItemPath), ListView = FileList }, EventArgs.Empty);
				else
					CloseFolder();
			}
			else if (SelectedItems?.Count > 1
				|| SelectedItem?.PrimaryItemAttribute is StorageItemTypes.File
				|| openedFolderPresenter != null && ParentShellPageInstance != null
				&& !ParentShellPageInstance.ShellViewModel.FilesAndFolders.ToList().Contains(FileList.ItemFromContainer(openedFolderPresenter))
				&& !isDraggingSelectionRectangle) // Skip closing if dragging since nothing should be open 
			{
				CloseFolder();
			}
		}

		private void CloseFolder()
		{
			var currentBladeIndex = (ParentShellPageInstance is ColumnShellPage associatedColumnShellPage) ? associatedColumnShellPage.ColumnParams.Column : 0;
			this.FindAscendant<ColumnsLayoutPage>()?.DismissOtherBlades(currentBladeIndex);
			ClearOpenedFolderSelectionIndicator();
		}

		private void FileList_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (!IsRenamingItem)
				HandleRightClick();
		}

		protected override async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if
			(
				ParentShellPageInstance is null ||
				IsRenamingItem ||
				SelectedItems?.Count > 1
			)
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

			if (ctrlPressed && e.Key is VirtualKey.A)
			{
				e.Handled = true;

				var commands = Ioc.Default.GetRequiredService<ICommandManager>();
				var hotKey = new HotKey(Keys.A, KeyModifiers.Ctrl);

				await commands[hotKey].ExecuteAsync();
			}
			else if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
			{
				e.Handled = true;

				if (IsItemSelected && SelectedItem?.PrimaryItemAttribute == StorageItemTypes.Folder)
					ItemInvoked?.Invoke(new ColumnParam { Source = this, NavPathParam = (SelectedItem is IShortcutItem sht ? sht.TargetPath : SelectedItem.ItemPath), ListView = FileList }, EventArgs.Empty);
			}
			else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
			{
				FilePropertiesHelpers.OpenPropertiesWindow(ParentShellPageInstance);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Space)
			{
				if (!ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
					e.Handled = true;
			}
			else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
			{
				// Unfocus the GridView so keyboard shortcut can be handled
				Focus(FocusState.Pointer);
			}
			else if (e.KeyStatus.IsMenuKeyDown && shiftPressed && e.Key == VirtualKey.Add)
			{
				// Unfocus the ListView so keyboard shortcut can be handled (alt + shift + "+")
				Focus(FocusState.Pointer);
			}
			else if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
			{
				ClearOpenedFolderSelectionIndicator();

				// If list has only one item, select it on arrow down/up (#5681)
				if (!IsItemSelected)
				{
					FileList.SelectedIndex = 0;
					e.Handled = true;
				}
			}
			else if (e.Key == VirtualKey.Left) // Left arrow: select parent folder (previous column)
			{
				if (ParentShellPageInstance is not null && ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
					return;

				var currentBladeIndex = (ParentShellPageInstance is ColumnShellPage associatedColumnShellPage) ? associatedColumnShellPage.ColumnParams.Column : 0;
				this.FindAscendant<ColumnsLayoutPage>()?.MoveFocusToPreviousBlade(currentBladeIndex);
				FileList.SelectedItem = null;
				ClearOpenedFolderSelectionIndicator();
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Right) // Right arrow: switch focus to next column
			{
				if (ParentShellPageInstance is not null && ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
					return;

				var currentBladeIndex = (ParentShellPageInstance is ColumnShellPage associatedColumnShellPage) ? associatedColumnShellPage.ColumnParams.Column : 0;
				this.FindAscendant<ColumnsLayoutPage>()?.MoveFocusToNextBlade(currentBladeIndex + 1);
				e.Handled = true;
			}
		}

		private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			doubleClickTimer.Stop();

			var clickedItem = e.OriginalSource as FrameworkElement;

			if (clickedItem?.DataContext is ListedItem item)
			{
				switch (item.PrimaryItemAttribute)
				{
					case StorageItemTypes.File:
						if (!UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
							await Commands.OpenItem.ExecuteAsync();
						break;
					case StorageItemTypes.Folder:
						if (!UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick)
							ItemInvoked?.Invoke(new ColumnParam { Source = this, NavPathParam = (item is IShortcutItem sht ? sht.TargetPath : item.ItemPath), ListView = FileList }, EventArgs.Empty);
						break;
					default:
						if (UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
							await Commands.NavigateUp.ExecuteAsync();
						break;
				}
			}
			else if (UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
			{
				await Commands.NavigateUp.ExecuteAsync();
			}

			ResetRenameDoubleClick();
		}

		private void FileList_Holding(object sender, HoldingRoutedEventArgs e)
		{
			HandleRightClick();
		}

		private void HandleRightClick()
		{
			if (ParentShellPageInstance is UIElement element &&
				(!ParentShellPageInstance.IsCurrentPane
				|| columnsOwner is not null && ParentShellPageInstance != columnsOwner.ActiveColumnShellPage))
				element.Focus(FocusState.Programmatic);
		}

		private async void FileList_ItemTapped(object sender, TappedRoutedEventArgs e)
		{
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var item = (e.OriginalSource as FrameworkElement)?.DataContext as ListedItem;

			// Allow for Ctrl+Shift selection
			if (ctrlPressed || shiftPressed)
				return;

			var isItemFile = item?.PrimaryItemAttribute is StorageItemTypes.File;
			var isItemFolder = item?.PrimaryItemAttribute is StorageItemTypes.Folder;

			// Check if the setting to open items with a single click is turned on
			if (UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick && isItemFile)
			{
				ResetRenameDoubleClick();
				await Commands.OpenItem.ExecuteAsync();
			}
			else if (item is not null)
			{
				var clickedItem = e.OriginalSource as FrameworkElement;
				if (clickedItem is TextBlock textBlock && textBlock.Name == "ItemName")
				{
					CheckRenameDoubleClick(clickedItem.DataContext);
				}
				else if (IsRenamingItem &&
					FileList.ContainerFromItem(RenamingItem) is ListViewItem listViewItem &&
					listViewItem.FindDescendant("ListViewTextBoxItemName") is TextBox textBox)
				{
					await CommitRenameAsync(textBox);
				}

				if (!IsRenamingItem && isItemFile)
				{
					CheckDoubleClick(item!);
				}
			}
			else
			{
				CloseFolder();
			}
		}

		private void CheckDoubleClick(ListedItem item)
		{
			doubleClickTimer.Debounce(() =>
			{
				ClearOpenedFolderSelectionIndicator();

				var itemPath = item!.ItemPath.EndsWith('\\')
					? item.ItemPath.Substring(0, item.ItemPath.Length - 1)
					: item.ItemPath;

				ItemTapped?.Invoke(new ColumnParam { Source = this, NavPathParam = Path.GetDirectoryName(itemPath), ListView = FileList }, EventArgs.Empty);

				doubleClickTimer.Stop();
			},
			TimeSpan.FromMilliseconds(200));
		}

		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			var itemContainer = (sender as Grid)?.FindAscendant<ListViewItem>();

			if (itemContainer is null)
				return;

			itemContainer.ContextFlyout = ItemContextMenuFlyout;
		}

		private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is ListedItem item)
				// Reassign values to update date display
				ToolTipService.SetToolTip(element, item.ItemTooltipText);
		}

		protected override void BaseFolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
			var parent = this.FindAscendant<ModernShellPage>();

			if (parent is null)
				return;

			switch (e.LayoutMode)
			{
				case FolderLayoutModes.ColumnView:
					break;
				case FolderLayoutModes.DetailsView:
					parent.FolderSettings.ToggleLayoutModeDetailsView(true);
					break;
				case FolderLayoutModes.ListView:
					parent.FolderSettings.ToggleLayoutModeList(true);
					break;
				case FolderLayoutModes.CardsView:
					parent.FolderSettings.ToggleLayoutModeCards(true);
					break;
				case FolderLayoutModes.GridView:
					parent.FolderSettings.ToggleLayoutModeGridView(true);
					break;
				case FolderLayoutModes.Adaptive:
					parent.FolderSettings.ToggleLayoutModeAdaptive();
					break;
			}
		}

		protected override void SelectionRectangle_SelectionEnded(object? sender, EventArgs e)
		{
			isDraggingSelectionRectangle = false;
			// Open selected folder (if only one folder is selected) after the user finishes dragging the selection rectangle
			if (SelectedItems?.Count is 1
				&& SelectedItem is not null
				&& SelectedItem.PrimaryItemAttribute is StorageItemTypes.Folder)
				ItemInvoked?.Invoke(new ColumnParam { Source = this, NavPathParam = (SelectedItem is IShortcutItem sht ? sht.TargetPath : SelectedItem.ItemPath), ListView = FileList }, EventArgs.Empty);

			base.SelectionRectangle_SelectionEnded(sender, e);
		}

		private void SelectionRectangle_SelectionStarted(object sender, EventArgs e)
		{
			isDraggingSelectionRectangle = true;
		}

		internal void ClearSelectionIndicator()
		{
			LockPreviewPaneContent = true;
			FileList.SelectedItem = null;
			LockPreviewPaneContent = false;
		}

		private static void SetFolderBackground(ListViewItem? lvi, SolidColorBrush? backgroundColor)
		{
			if (lvi == null || backgroundColor == null) return;


			if (lvi.FindDescendant<Grid>() is Grid presenter)
			{
				presenter.Background = backgroundColor;
			}
		}
	}
}
