// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Files.App.UserControls.Selection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.Views.Layouts
{
	/// <summary>
	/// Represents the browser page of Grid View
	/// </summary>
	public sealed partial class GridLayoutPage : BaseGroupableLayoutPage
	{
		// Fields

		/// <summary>
		/// This reference is used to prevent unnecessary icon reloading by only reloading icons when their
		/// size changes, even if the layout size changes (since some layout sizes share the same icon size).
		/// </summary>
		private uint currentIconSize;

		private volatile bool shouldSetVerticalScrollMode;

		// Properties

		public ScrollViewer? ContentScroller { get; private set; }

		protected override ListViewBase ListViewBase => FileList;
		protected override SemanticZoom RootZoom => RootGridZoom;


		/// <summary>
		/// Row height in the List View layout
		/// </summary>
		public int RowHeightListView
		{
			get => LayoutSizeKindHelper.GetListViewRowHeight(UserSettingsService.LayoutSettingsService.ListViewSize);
		}

		/// <summary>
		/// Icon Box size in the List View layout. The value is increased by 4px to account for icon overlays.
		/// </summary>
		public int IconBoxSizeListView
		{
			get => (int)(LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.ListView) + 4);
		}

		/// <summary>
		/// Item width in the Grid View layout
		/// </summary>
		public int ItemWidthGridView
		{
			get => LayoutSizeKindHelper.GetGridViewItemWidth(UserSettingsService.LayoutSettingsService.GridViewSize);
		}


		/// <summary>
		/// Item width in the Cards View layout
		/// </summary>
		public int CardsViewItemWidth
		{
			get => UserSettingsService.LayoutSettingsService.CardsViewSize switch
			{
				CardsViewSizeKind.Small => 300,
				CardsViewSizeKind.Medium => 200,
				CardsViewSizeKind.Large => 240,
				CardsViewSizeKind.ExtraLarge => 260,
				_ => 300
			};
		}

		/// <summary>
		/// Item height in the Cards View layout
		/// </summary>
		public int CardsViewItemHeight
		{
			get => UserSettingsService.LayoutSettingsService.CardsViewSize switch
			{
				CardsViewSizeKind.Small => 128, 
				CardsViewSizeKind.Medium => 200, 
				CardsViewSizeKind.Large => 240, 
				CardsViewSizeKind.ExtraLarge => 260,
				_ => 128 
			};
		}

		/// <summary>
		/// Icon Box height in the Cards View layout.
		/// </summary>
		public int CardsViewIconBoxHeight
		{
			get => UserSettingsService.LayoutSettingsService.CardsViewSize switch
			{
				CardsViewSizeKind.Small => 128,
				CardsViewSizeKind.Medium => 111,
				CardsViewSizeKind.Large => 128,
				CardsViewSizeKind.ExtraLarge => 160,
				_ => 128
			};
		}

		/// <summary>
		/// Icon Box width in the Cards View layout.
		/// </summary>
		public int CardsViewIconBoxWidth
		{
			get => UserSettingsService.LayoutSettingsService.CardsViewSize switch
			{
				CardsViewSizeKind.Small => 128,
				CardsViewSizeKind.Medium => 200,
				CardsViewSizeKind.Large => 240,
				CardsViewSizeKind.ExtraLarge => 260,
				_ => 128
			};
		}

		/// <summary>
		/// Icon height in the Cards View layout.
		/// </summary>
		public int CardsViewIconSize
		{
			get => (int)(LayoutSizeKindHelper.GetIconSize(FolderLayoutModes.CardsView));
		}

		public bool IsPointerOver
		{
			get => (bool)GetValue(IsPointerOverProperty);
			set => SetValue(IsPointerOverProperty, value);
		}

		public static readonly DependencyProperty IsPointerOverProperty =
			DependencyProperty.Register(
				nameof(IsPointerOver),
				typeof(bool),
				typeof(GridLayoutPage),
				new PropertyMetadata(false));

		// Constructor

		public GridLayoutPage() : base()
		{
			InitializeComponent();
			DataContext = this;

			var selectionRectangle = RectangleSelection.Create(ListViewBase, SelectionRectangle, FileList_SelectionChanged);
			selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
		}

		// Methods

		protected override void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			FileList.ScrollIntoView(e);
		}

		protected override void ItemManipulationModel_ScrollToTopInvoked(object? sender, EventArgs e)
		{
			if (FolderSettings?.LayoutMode is FolderLayoutModes.ListView)
				ContentScroller?.ChangeView(0, null, null, true);
			else
				ContentScroller?.ChangeView(null, 0, null, true);
		}

		protected override void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems.Any())
			{
				FileList.ScrollIntoView(SelectedItems.Last());
				(FileList.ContainerFromItem(SelectedItems.Last()) as GridViewItem)?.Focus(FocusState.Keyboard);
			}
		}

		protected override void ItemManipulationModel_AddSelectedItemInvoked(object? sender, ListedItem e)
		{
			if ((NextRenameIndex != 0 && TryStartRenameNextItem(e)) || (!FileList?.Items.Contains(e) ?? true))
				return;

			FileList!.SelectedItems.Add(e);
		}

		protected override void ItemManipulationModel_RemoveSelectedItemInvoked(object? sender, ListedItem e)
		{
			if (FileList?.Items.Contains(e) ?? false)
				FileList.SelectedItems.Remove(e);
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			if (eventArgs.Parameter is NavigationArguments navArgs)
				navArgs.FocusOnNavigation = true;

			base.OnNavigatedTo(eventArgs);

			currentIconSize = LayoutSizeKindHelper.GetIconSize(FolderSettings.LayoutMode);

			FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;
			FolderSettings.LayoutModeChangeRequested += FolderSettings_LayoutModeChangeRequested;
			UserSettingsService.LayoutSettingsService.PropertyChanged += LayoutSettingsService_PropertyChanged;

			// Set ItemTemplate
			SetItemTemplate();
			SetItemContainerStyle();
			FileList.ItemsSource ??= ParentShellPageInstance.ShellViewModel.FilesAndFolders;

			var parameters = (NavigationArguments)eventArgs.Parameter;
			if (parameters.IsLayoutSwitch)
				ReloadItemIconsAsync();
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);

			if (FolderSettings != null)
				FolderSettings.LayoutModeChangeRequested -= FolderSettings_LayoutModeChangeRequested;

			UserSettingsService.LayoutSettingsService.PropertyChanged -= LayoutSettingsService_PropertyChanged;
		}

		private void LayoutSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Get current scroll position
			var previousHorizontalOffset = ContentScroller?.HorizontalOffset;
			var previousVerticalOffset = ContentScroller?.VerticalOffset;

			if (e.PropertyName == nameof(ILayoutSettingsService.ListViewSize))
			{
				NotifyPropertyChanged(nameof(RowHeightListView));
				NotifyPropertyChanged(nameof(IconBoxSizeListView));

				// Update the container style to match the item size
				SetItemContainerStyle();
				FolderSettings_IconSizeChanged();
			}
			if (e.PropertyName == nameof(ILayoutSettingsService.CardsViewSize))
			{
				NotifyPropertyChanged(nameof(CardsViewItemWidth));
				NotifyPropertyChanged(nameof(CardsViewItemHeight));
				NotifyPropertyChanged(nameof(CardsViewIconBoxHeight));
				NotifyPropertyChanged(nameof(CardsViewIconBoxWidth));
				NotifyPropertyChanged(nameof(CardsViewIconSize));

				// Update the container style to match the item size
				SetItemContainerStyle();
				FolderSettings_IconSizeChanged();
			}
			if (e.PropertyName == nameof(ILayoutSettingsService.GridViewSize))
			{
				NotifyPropertyChanged(nameof(ItemWidthGridView));

				// Update the container style to match the item size
				SetItemContainerStyle();
				FolderSettings_IconSizeChanged();
			}

			// Restore correct scroll position
			ContentScroller?.ChangeView(previousHorizontalOffset, previousVerticalOffset, null);
		}

		private void FolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
			if (FolderSettings.LayoutMode == FolderLayoutModes.ListView
				|| FolderSettings.LayoutMode == FolderLayoutModes.CardsView
				|| FolderSettings.LayoutMode == FolderLayoutModes.GridView)
			{
				// Set ItemTemplate
				SetItemTemplate();
				SetItemContainerStyle();
				FolderSettings_IconSizeChanged();
			}
		}

		private void SetItemTemplate()
		{
			var newFileListStyle = FolderSettings.LayoutMode switch
			{
				FolderLayoutModes.ListView => (Style)Resources["VerticalLayoutGridView"],
				FolderLayoutModes.CardsView => (Style)Resources["HorizontalLayoutGridView"],
				_ => (Style)Resources["HorizontalLayoutGridView"]
			};

			if (FileList.Style != newFileListStyle)
			{
				var oldSource = FileList.ItemsSource;
				FileList.ItemsSource = null;
				FileList.Style = newFileListStyle;
				FileList.ItemsSource = oldSource;
			}

			shouldSetVerticalScrollMode = true;

			switch (FolderSettings.LayoutMode)
			{
				case FolderLayoutModes.ListView:
					FileList.ItemTemplate = ListViewBrowserTemplate;
					break;
				case FolderLayoutModes.CardsView:
					FileList.ItemTemplate = CardsBrowserTemplate;
					break;
				default:
					FileList.ItemTemplate = GridViewBrowserTemplate;
					break;
			}
		}

		private void SetItemContainerStyle()
		{
			if (FolderSettings?.LayoutMode == FolderLayoutModes.ListView && UserSettingsService.LayoutSettingsService.ListViewSize == ListViewSizeKind.Compact)
			{
				// Toggle style to force item size to update
				FileList.ItemContainerStyle = DefaultItemContainerStyle;

				// Set correct style
				FileList.ItemContainerStyle = CompactListItemContainerStyle;
			}
			else
			{
				// Toggle style to force item size to update
				FileList.ItemContainerStyle = CompactListItemContainerStyle;

				// Set correct style
				FileList.ItemContainerStyle = DefaultItemContainerStyle;
			}
		}

		private void FileList_Loaded(object sender, RoutedEventArgs e)
		{
			ContentScroller = FileList.FindDescendant<ScrollViewer>(x => x.Name == "ScrollViewer");
		}

		protected override void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			base.FileList_SelectionChanged(sender, e);

			if (e != null)
			{
				foreach (var item in e.AddedItems)
					SetCheckboxSelectionState(item);

				foreach (var item in e.RemovedItems)
					SetCheckboxSelectionState(item);
			}
		}

		override public void StartRenameItem()
		{
			RenamingItem = SelectedItem;
			if (RenamingItem is null || FolderSettings is null)
				return;

			int extensionLength = RenamingItem.FileExtension?.Length ?? 0;

			if (FileList.ContainerFromItem(RenamingItem) is not GridViewItem gridViewItem)
				return;

			if (gridViewItem.FindDescendant("ItemName") is not TextBlock textBlock)
				return;

			TextBox? textBox = null;

			// Handle layout differences between Cards View and Grid View
			if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
			{
				if (gridViewItem.FindDescendant("EditPopup") is not Popup popup)
					return;

				textBox = popup.Child as TextBox;
				if (textBox is null)
					return;

				textBox.Text = textBlock.Text;
				textBlock.Opacity = 0;
				popup.IsOpen = true;
				OldItemName = textBlock.Text;
			}
			else if (FolderSettings.LayoutMode == FolderLayoutModes.ListView)
			{
				textBox = gridViewItem.FindDescendant("ListViewTextBoxItemName") as TextBox;
				if (textBox is null)
					return;

				textBox.Text = textBlock.Text;
				OldItemName = textBlock.Text;
				textBlock.Visibility = Visibility.Collapsed;
				textBox.Visibility = Visibility.Visible;

				if (textBox.FindParent<Grid>() is null)
				{
					textBlock.Visibility = Visibility.Visible;
					textBox.Visibility = Visibility.Collapsed;
					return;
				}
			}
			else
			{
				textBox = gridViewItem.FindDescendant("TileViewTextBoxItemName") as TextBox;
				if (textBox is null)
					return;

				textBox.Text = textBlock.Text;
				OldItemName = textBlock.Text;
				textBlock.Visibility = Visibility.Collapsed;
				textBox.Visibility = Visibility.Visible;

				if (textBox.FindParent<Grid>() is null)
				{
					textBlock.Visibility = Visibility.Visible;
					textBox.Visibility = Visibility.Collapsed;
					return;
				}
			}

			textBox.Focus(FocusState.Pointer);
			textBox.LostFocus += RenameTextBox_LostFocus;
			textBox.KeyDown += RenameTextBox_KeyDown;

			int selectedTextLength = RenamingItem.Name.Length;
			if (!RenamingItem.IsShortcut && UserSettingsService.FoldersSettingsService.ShowFileExtensions)
				selectedTextLength -= extensionLength;

			textBox.Select(0, selectedTextLength);
			IsRenamingItem = true;
		}

		private void ItemNameTextBox_BeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs args)
		{
			if (!IsRenamingItem)
				return;

			ValidateItemNameInputTextAsync(textBox, args, (showError) =>
			{
				FileNameTeachingTip.Visibility = showError ? Visibility.Visible : Visibility.Collapsed;
				FileNameTeachingTip.IsOpen = showError;
			});
		}

		protected override void EndRename(TextBox textBox)
		{
			GridViewItem? gridViewItem = FileList.ContainerFromItem(RenamingItem) as GridViewItem;

			if (textBox is null || gridViewItem is null)
			{
				// NOTE: Navigating away, do nothing
			}
			else if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
			{
				Popup? popup = gridViewItem.FindDescendant("EditPopup") as Popup;
				TextBlock? textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;

				if (popup is not null)
					popup.IsOpen = false;

				if (textBlock is not null)
					textBlock.Opacity = (textBlock.DataContext as ListedItem)!.Opacity;
			}
			else if (FolderSettings.LayoutMode == FolderLayoutModes.CardsView || FolderSettings.LayoutMode == FolderLayoutModes.ListView)
			{
				TextBlock? textBlock = gridViewItem.FindDescendant("ItemName") as TextBlock;

				textBox.Visibility = Visibility.Collapsed;

				if (textBlock is not null)
					textBlock.Visibility = Visibility.Visible;
			}

			// Unsubscribe from events
			if (textBox is not null)
			{
				textBox.LostFocus -= RenameTextBox_LostFocus;
				textBox.KeyDown -= RenameTextBox_KeyDown;
			}

			FileNameTeachingTip.IsOpen = false;
			IsRenamingItem = false;

			// Re-focus selected list item
			gridViewItem?.Focus(FocusState.Programmatic);
		}

		protected override async void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (ParentShellPageInstance is null || IsRenamingItem)
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var focusedElement = FocusManager.GetFocusedElement(MainWindow.Instance.Content.XamlRoot) as FrameworkElement;
			var isFooterFocused = focusedElement is HyperlinkButton;

			if (ctrlPressed && e.Key is VirtualKey.A)
			{
				e.Handled = true;

				var commands = Ioc.Default.GetRequiredService<ICommandManager>();
				var hotKey = new HotKey(Keys.A, KeyModifiers.Ctrl);

				await commands[hotKey].ExecuteAsync();
			}
			else if (e.Key == VirtualKey.Enter && !isFooterFocused && !e.KeyStatus.IsMenuKeyDown)
			{
				e.Handled = true;

				if (ctrlPressed && !shiftPressed)
				{
					var folders = ParentShellPageInstance?.SlimContentPage.SelectedItems?.Where(file => file.PrimaryItemAttribute == StorageItemTypes.Folder);
					foreach (ListedItem? folder in folders)
					{
						if (folder is not null)
							await NavigationHelpers.OpenPathInNewTab(folder.ItemPath);
					}
				}
				else if (ctrlPressed && shiftPressed)
				{
					NavigationHelpers.OpenInSecondaryPane(ParentShellPageInstance, SelectedItems.FirstOrDefault(item => item.PrimaryItemAttribute == StorageItemTypes.Folder));
				}
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
				// If list has only one item, select it on arrow down/up (#5681)
				if (IsItemSelected)
					return;

				FileList.SelectedIndex = 0;
				e.Handled = true;
			}
		}

		protected override bool CanGetItemFromElement(object element)
			=> element is GridViewItem;

		private void FolderSettings_IconSizeChanged()
		{
			// Check if icons need to be reloaded
			var newIconSize = LayoutSizeKindHelper.GetIconSize(FolderSettings.LayoutMode);
			if (newIconSize != currentIconSize)
			{
				currentIconSize = newIconSize;
				_ = ReloadItemIconsAsync();
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

			if (ParentShellPageInstance.ShellViewModel.EnabledGitProperties is not GitProperties.None)
			{
				await Task.WhenAll(filesAndFolders.Select(item =>
				{
					if (item is IGitItem gitItem)
						return ParentShellPageInstance.ShellViewModel.LoadGitPropertiesAsync(gitItem);

					return Task.CompletedTask;
				}));
			}
		}

		private async void FileList_ItemTapped(object sender, TappedRoutedEventArgs e)
		{
			var clickedItem = e.OriginalSource as FrameworkElement;
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

			var item = (e.OriginalSource as FrameworkElement)?.DataContext as ListedItem;
			if (item is null)
				return;

			// Skip code if the control or shift key is pressed or if the user is using multiselect
			if (ctrlPressed ||
				shiftPressed ||
				clickedItem is Microsoft.UI.Xaml.Shapes.Rectangle)
			{
				e.Handled = true;
				return;
			}

			// Check if the setting to open items with a single click is turned on
			if (UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
			{
				ResetRenameDoubleClick();
				await Commands.OpenItem.ExecuteAsync();
			}
			else
			{
				if (clickedItem is TextBlock textBlock && textBlock.Name == "ItemName")
				{
					CheckRenameDoubleClick(clickedItem?.DataContext);
				}
				else if (IsRenamingItem)
				{
					if (FileList.ContainerFromItem(RenamingItem) is GridViewItem gridViewItem)
					{
						if (FolderSettings.LayoutMode == FolderLayoutModes.GridView)
						{
							Popup popup = gridViewItem.FindDescendant("EditPopup") as Popup;
							var textBox = popup?.Child as TextBox;

							if (textBox is not null)
								await CommitRenameAsync(textBox);
						}
						else
						{
							var textBox = gridViewItem.FindDescendant("TileViewTextBoxItemName") as TextBox;

							if (textBox is not null)
								await CommitRenameAsync(textBox);
						}
					}
				}
			}
		}

		private async void FileList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			// Skip opening selected items if the double tap doesn't capture an item
			if ((e.OriginalSource as FrameworkElement)?.DataContext is ListedItem item && !UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
				await Commands.OpenItem.ExecuteAsync();
			else if ((e.OriginalSource as FrameworkElement)?.DataContext is not ListedItem && UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
				await Commands.NavigateUp.ExecuteAsync();

			ResetRenameDoubleClick();
		}

		private void ItemSelected_Checked(object sender, RoutedEventArgs e)
		{
			if (sender is CheckBox checkBox &&
				checkBox.DataContext is ListedItem item &&
				!FileList.SelectedItems.Contains(item))
				FileList.SelectedItems.Add(item);
		}

		private void ItemSelected_Unchecked(object sender, RoutedEventArgs e)
		{
			if (sender is CheckBox checkBox &&
				checkBox.DataContext is ListedItem item &&
				FileList.SelectedItems.Contains(item))
				FileList.SelectedItems.Remove(item);
		}

		private new void FileList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			var selectionCheckbox = args.ItemContainer.FindDescendant("SelectionCheckbox")!;

			selectionCheckbox.PointerEntered -= SelectionCheckbox_PointerEntered;
			selectionCheckbox.PointerExited -= SelectionCheckbox_PointerExited;
			selectionCheckbox.PointerCanceled -= SelectionCheckbox_PointerCanceled;

			base.FileList_ContainerContentChanging(sender, args);
			SetCheckboxSelectionState(args.Item, args.ItemContainer as GridViewItem);

			selectionCheckbox.PointerEntered += SelectionCheckbox_PointerEntered;
			selectionCheckbox.PointerExited += SelectionCheckbox_PointerExited;
			selectionCheckbox.PointerCanceled += SelectionCheckbox_PointerCanceled;
		}

		private void SetCheckboxSelectionState(object item, GridViewItem? lviContainer = null)
		{
			var container = lviContainer ?? FileList.ContainerFromItem(item) as GridViewItem;
			if (container is not null)
			{
				var checkbox = container.FindDescendant("SelectionCheckbox") as CheckBox;
				if (checkbox is not null)
				{
					// Temporarily disable events to avoid selecting wrong items
					checkbox.Checked -= ItemSelected_Checked;
					checkbox.Unchecked -= ItemSelected_Unchecked;

					checkbox.IsChecked = FileList.SelectedItems.Contains(item);

					checkbox.Checked += ItemSelected_Checked;
					checkbox.Unchecked += ItemSelected_Unchecked;
				}

				UpdateCheckboxVisibility(container, checkbox?.IsPointerOver ?? false);
			}
		}

		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			// This is the best way I could find to set the context flyout, as doing it in the styles isn't possible
			// because you can't use bindings in the setters
			DependencyObject item = VisualTreeHelper.GetParent(sender as Grid);

			while (item is not GridViewItem)
				item = VisualTreeHelper.GetParent(item);

			if (item is GridViewItem itemContainer)
				itemContainer.ContextFlyout = ItemContextMenuFlyout;

			// Set VerticalScrollMode after an item has been loaded (#14785)
			if (shouldSetVerticalScrollMode)
			{
				shouldSetVerticalScrollMode = false;

				if (FolderSettings?.LayoutMode is FolderLayoutModes.ListView)
					ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Disabled);
				else
					ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Enabled);
			}
		}

		private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is ListedItem item)
				// Reassign values to update date display
				ToolTipService.SetToolTip(element, item.ItemTooltipText);
		}

		private void SelectionCheckbox_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<GridViewItem>()!, true);
		}

		private void SelectionCheckbox_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<GridViewItem>()!, false);
		}

		private void SelectionCheckbox_PointerCanceled(object sender, PointerRoutedEventArgs e)
		{
			UpdateCheckboxVisibility((sender as FrameworkElement)!.FindAscendant<GridViewItem>()!, false);
		}

		// To avoid crashes, disable scrolling when drag-and-drop if grouped. (#14484)
		private bool ShouldDisableScrollingWhenDragAndDrop =>
			FolderSettings?.LayoutMode is FolderLayoutModes.GridView or FolderLayoutModes.CardsView &&
			(ParentShellPageInstance?.ShellViewModel.FilesAndFolders.IsGrouped ?? false);

		protected override void FileList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
		{
			if (ShouldDisableScrollingWhenDragAndDrop)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Disabled);

			base.FileList_DragItemsStarting(sender, e);

			if (ShouldDisableScrollingWhenDragAndDrop && e.Cancel)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Enabled);
		}

		private void ItemsLayout_DragEnter(object sender, DragEventArgs e)
		{
			if (ShouldDisableScrollingWhenDragAndDrop)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Disabled);
		}

		private void ItemsLayout_DragLeave(object sender, DragEventArgs e)
		{
			if (ShouldDisableScrollingWhenDragAndDrop)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Enabled);
		}

		protected override void ItemsLayout_Drop(object sender, DragEventArgs e)
		{
			if (ShouldDisableScrollingWhenDragAndDrop)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Enabled);

			base.ItemsLayout_Drop(sender, e);
		}

		protected override void Item_Drop(object sender, DragEventArgs e)
		{
			if (ShouldDisableScrollingWhenDragAndDrop)
				ScrollViewer.SetVerticalScrollMode(FileList, ScrollMode.Enabled);

			base.Item_Drop(sender, e);
		}

		private void UpdateCheckboxVisibility(object sender, bool isPointerOver)
		{
			if (sender is GridViewItem control && control.FindDescendant<UserControl>() is UserControl userControl)
			{
				// Handle visual states
				// Show checkboxes when items are selected (as long as the setting is enabled)
				// Show checkboxes when hovering over the checkbox area (regardless of the setting to hide them)
				if (UserSettingsService.FoldersSettingsService.ShowCheckboxesWhenSelectingItems && control.IsSelected
					|| isPointerOver)
					VisualStateManager.GoToState(userControl, "ShowCheckbox", true);
				else
					VisualStateManager.GoToState(userControl, "HideCheckbox", true);
			}
		}
	}
}
