using Files.Enums;
using Files.Filesystem;
using Files.Helpers;
using Files.Interacts;
using Files.UserControls;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Files
{
    public sealed partial class GenericFileBrowser : BaseLayout
    {
        private string oldItemName;
        private DataGridColumn _sortedColumn;
        private static readonly MethodInfo SelectAllMethod = typeof(DataGrid).GetMethod("SelectAll", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);

        public DataGridColumn SortedColumn
        {
            get
            {
                return _sortedColumn;
            }
            set
            {
                if (value == nameColumn)
                    AppSettings.DirectorySortOption = SortOption.Name;
                else if (value == dateColumn)
                    AppSettings.DirectorySortOption = SortOption.DateModified;
                else if (value == typeColumn)
                    AppSettings.DirectorySortOption = SortOption.FileType;
                else if (value == sizeColumn)
                    AppSettings.DirectorySortOption = SortOption.Size;
                else
                    AppSettings.DirectorySortOption = SortOption.Name;

                if (value != _sortedColumn)
                {
                    // Remove arrow on previous sorted column
                    if (_sortedColumn != null)
                        _sortedColumn.SortDirection = null;
                }
                value.SortDirection = AppSettings.DirectorySortDirection == SortDirection.Ascending ? DataGridSortDirection.Ascending : DataGridSortDirection.Descending;
                _sortedColumn = value;
            }
        }

        public GenericFileBrowser()
        {
            InitializeComponent();
            base.BaseLayoutContextFlyout = this.BaseLayoutContextFlyout;
            base.BaseLayoutItemContextFlyout = this.BaseLayoutItemContextFlyout;
            this.tapDebounceTimer = new DispatcherTimer();
            switch (AppSettings.DirectorySortOption)
            {
                case SortOption.Name:
                    SortedColumn = nameColumn;
                    break;

                case SortOption.DateModified:
                    SortedColumn = dateColumn;
                    break;

                case SortOption.FileType:
                    SortedColumn = typeColumn;
                    break;

                case SortOption.Size:
                    SortedColumn = sizeColumn;
                    break;
            }

            var selectionRectangle = RectangleSelection.Create(AllView, SelectionRectangle, AllView_SelectionChanged);
            selectionRectangle.SelectionStarted += SelectionRectangle_SelectionStarted;
            selectionRectangle.SelectionEnded += SelectionRectangle_SelectionEnded;
            AllView.PointerCaptureLost += AllView_ItemPress;
            AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;
        }

        private void SelectionRectangle_SelectionStarted(object sender, EventArgs e)
        {
            // If drag selection is active do not trigger file open on pointer release
            AllView.PointerCaptureLost -= AllView_ItemPress;
        }

        private void SelectionRectangle_SelectionEnded(object sender, EventArgs e)
        {
            // Restore file open on pointer release
            AllView.PointerCaptureLost += AllView_ItemPress;
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            ParentShellPageInstance.FilesystemViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            ParentShellPageInstance.FilesystemViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void AppSettings_ThemeModeChanged(object sender, EventArgs e)
        {
            RequestedTheme = ThemeHelper.RootTheme;
        }

        public override void SetSelectedItemOnUi(ListedItem item)
        {
            ClearSelection();
            AllView.SelectedItems.Add(item);
        }

        public override void SetSelectedItemsOnUi(List<ListedItem> selectedItems)
        {
            ClearSelection();
            foreach (ListedItem selectedItem in selectedItems)
            {
                AllView.SelectedItems.Add(selectedItem);
            }
        }

        public override void SelectAllItems()
        {
            SelectAllMethod.Invoke(AllView, null);
        }

        public override void InvertSelection()
        {
            List<ListedItem> allItems = ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList();
            List<ListedItem> newSelectedItems = allItems.Except(SelectedItems).ToList();

            SetSelectedItemsOnUi(newSelectedItems);
        }

        public override void ClearSelection()
        {
            AllView.SelectedItems.Clear();
        }

        public override void SetDragModeForItems()
        {
            if (IsItemSelected)
            {
                var rows = new List<DataGridRow>();
                Interacts.Interaction.FindChildren<DataGridRow>(rows, AllView);
                foreach (DataGridRow row in rows)
                {
                    row.CanDrag = SelectedItems.Contains(row.DataContext);
                }
            }
        }

        public override void ScrollIntoView(ListedItem item)
        {
            AllView.ScrollIntoView(item, null);
        }

        public override int GetSelectedIndex()
        {
            return AllView.SelectedIndex;
        }

        public override void FocusSelectedItems()
        {
            AllView.ScrollIntoView(AllView.ItemsSource.Cast<ListedItem>().Last(), null);
        }

        public override void StartRenameItem()
        {
            AllView.CurrentColumn = AllView.Columns[1];
            AllView.BeginEdit();
        }

        public override void ResetItemOpacity()
        {
            IEnumerable items = AllView.ItemsSource;
            if (items == null)
            {
                return;
            }

            foreach (ListedItem listedItem in items)
            {
                listedItem.IsDimmed = false;
            }
        }

        public override void SetItemOpacity(ListedItem item)
        {
            item.IsDimmed = true;
        }

        private async void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DirectorySortOption")
            {
                switch (AppSettings.DirectorySortOption)
                {
                    case SortOption.Name:
                        SortedColumn = nameColumn;
                        break;

                    case SortOption.DateModified:
                        SortedColumn = dateColumn;
                        break;

                    case SortOption.FileType:
                        SortedColumn = typeColumn;
                        break;

                    case SortOption.Size:
                        SortedColumn = sizeColumn;
                        break;
                }
            }
            else if (e.PropertyName == "DirectorySortDirection")
            {
                // Swap arrows
                SortedColumn = _sortedColumn;
            }
            else if (e.PropertyName == "IsLoadingItems")
            {
                if (!ParentShellPageInstance.FilesystemViewModel.IsLoadingItems && ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.Count > 0)
                {
                    var allRows = new List<DataGridRow>();

                    Interacts.Interaction.FindChildren<DataGridRow>(allRows, AllView);
                    foreach (DataGridRow row in allRows.Take(25))
                    {
                        if (!(row.DataContext as ListedItem).ItemPropertiesInitialized)
                        {
                            await Window.Current.CoreWindow.Dispatcher.RunIdleAsync((e) =>
                            {
                                ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(row.DataContext as ListedItem);
                                (row.DataContext as ListedItem).ItemPropertiesInitialized = true;
                            });
                        }
                    }
                }
            }
        }

        private TextBox renamingTextBox;

        private DispatcherTimer tapDebounceTimer;

        private void AllView_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (ParentShellPageInstance.FilesystemViewModel.WorkingDirectory.StartsWith(AppSettings.RecycleBinPath))
            {
                // Do not rename files and folders inside the recycle bin
                AllView.CancelEdit(); // Cancel the edit operation
                return;
            }

            // Only cancel if this event was triggered by a tap
            // Do not cancel when user presses F2 or context menu
            if (e.EditingEventArgs is TappedRoutedEventArgs)
            {
                if (AppSettings.OpenItemsWithOneclick)
                {
                    AllView.CancelEdit(); // Cancel the edit operation
                    return;
                }
                if (!tapDebounceTimer.IsEnabled)
                {
                    tapDebounceTimer.Debounce(() =>
                    {
                        tapDebounceTimer.Stop();
                        AllView.BeginEdit(); // EditingEventArgs will be null
                    }, TimeSpan.FromMilliseconds(500), false);
                }
                else
                {
                    tapDebounceTimer.Stop();
                    ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null); // Open selected files
                }
                AllView.CancelEdit(); // Cancel the edit operation
                return;
            }

            int extensionLength = SelectedItem.FileExtension?.Length ?? 0;
            oldItemName = SelectedItem.ItemName;

            renamingTextBox = e.EditingElement as TextBox;
            renamingTextBox.Focus(FocusState.Programmatic); // Without this,the user cannot edit the text box when renaming via right-click

            int selectedTextLength = SelectedItem.ItemName.Length;
            if (AppSettings.ShowFileExtensions)
            {
                selectedTextLength -= extensionLength;
            }
            renamingTextBox.Select(0, selectedTextLength);
            renamingTextBox.TextChanged += TextBox_TextChanged;
            isRenamingItem = true;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;

            if (Interaction.ContainsRestrictedCharacters(textBox.Text))
            {
                FileNameTeachingTip.IsOpen = true;
            }
            else
            {
                FileNameTeachingTip.IsOpen = false;
            }
        }

        private async void AllView_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Cancel || renamingTextBox == null)
            {
                return;
            }

            renamingTextBox.Text = renamingTextBox.Text.Trim().TrimEnd('.');

            var selectedItem = e.Row.DataContext as ListedItem;
            string newItemName = renamingTextBox.Text;

            bool successful = await ParentShellPageInstance.InteractionOperations.RenameFileItem(selectedItem, oldItemName, newItemName);
            if (!successful)
            {
                selectedItem.ItemName = oldItemName;
                renamingTextBox.Text = oldItemName;
            }
        }

        private void AllView_CellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
        {
            if (renamingTextBox != null)
            {
                renamingTextBox.TextChanged -= TextBox_TextChanged;
            }
            FileNameTeachingTip.IsOpen = false;
            isRenamingItem = false;
        }

        private async void AllView_ItemPress(object sender, PointerRoutedEventArgs e)
        {
            var ctrlPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            var shiftPressed = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

            var cp = e.GetCurrentPoint((UIElement)sender);
            if (cp.Position.Y <= 38 // Return if click is on the header(38 = header height)
                || cp.Properties.IsLeftButtonPressed // Return if dragging an item
                || cp.Properties.IsRightButtonPressed // Return if the user right clicks an item
                || ctrlPressed || shiftPressed) // Allow for Ctrl+Shift selection
            {
                return;
            }

            // Check if the setting to open items with a single click is turned on
            if (AppSettings.OpenItemsWithOneclick)
            {
                await Task.Delay(200); // The delay gives time for the item to be selected
                ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);
            }
        }

        private void AllView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllView.CommitEdit();
            SelectedItems = AllView.SelectedItems.Cast<ListedItem>().ToList();
        }

        private async void AllView_Sorting(object sender, DataGridColumnEventArgs e)
        {
            if (e.Column == SortedColumn)
            {
                ParentShellPageInstance.FilesystemViewModel.IsSortedAscending = !ParentShellPageInstance.FilesystemViewModel.IsSortedAscending;
                e.Column.SortDirection = ParentShellPageInstance.FilesystemViewModel.IsSortedAscending ? DataGridSortDirection.Ascending : DataGridSortDirection.Descending;
            }
            else if (e.Column != iconColumn)
            {
                SortedColumn = e.Column;
                e.Column.SortDirection = DataGridSortDirection.Ascending;
                ParentShellPageInstance.FilesystemViewModel.IsSortedAscending = true;
            }

            if (!ParentShellPageInstance.FilesystemViewModel.IsLoadingItems && ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.Count > 0)
            {
                var allRows = new List<DataGridRow>();

                Interacts.Interaction.FindChildren<DataGridRow>(allRows, AllView);
                foreach (DataGridRow row in allRows.Take(25))
                {
                    if (!(row.DataContext as ListedItem).ItemPropertiesInitialized)
                    {
                        await Window.Current.CoreWindow.Dispatcher.RunIdleAsync((e) =>
                        {
                            ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(row.DataContext as ListedItem);
                            (row.DataContext as ListedItem).ItemPropertiesInitialized = true;
                        });
                    }
                }
            }
        }

        private void AllView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
            {
                if (isRenamingItem)
                {
                    AllView.CommitEdit();
                }
                else
                {
                    ParentShellPageInstance.InteractionOperations.OpenItem_Click(null, null);
                }
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
            {
                ParentShellPageInstance.InteractionOperations.ShowPropertiesButton_Click(null, null);
            }
            else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
            {
                // Unfocus the GridView so keyboard shortcut can be handled
                this.Focus(FocusState.Programmatic);
            }
        }

        public void AllView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            HandleRightClick(sender, e);
        }

        public void AllView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            HandleRightClick(sender, e);
        }

        private void HandleRightClick(object sender, RoutedEventArgs e)
        {
            var rowPressed = Interacts.Interaction.FindParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (rowPressed != null)
            {
                var objectPressed = ((ReadOnlyObservableCollection<ListedItem>)AllView.ItemsSource)[rowPressed.GetIndex()];

                // Check if RightTapped row is currently selected
                if (IsItemSelected)
                {
                    if (SelectedItems.Contains(objectPressed))
                    {
                        return;
                    }
                }

                // The following code is only reachable when a user RightTapped an unselected row
                SetSelectedItemOnUi(objectPressed);
            }
        }

        protected override void Page_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (ParentShellPageInstance != null)
            {
                if (ParentShellPageInstance.CurrentPageType == typeof(GenericFileBrowser))
                {
                    // Don't block the various uses of enter key (key 13)
                    var focusedElement = FocusManager.GetFocusedElement() as FrameworkElement;
                    if (args.KeyCode == 13 || focusedElement is Button || focusedElement is TextBox || focusedElement is PasswordBox ||
                        Interacts.Interaction.FindParent<ContentDialog>(focusedElement) != null)
                    {
                        return;
                    }

                    base.Page_CharacterReceived(sender, args);
                    AllView.Focus(FocusState.Keyboard);
                }
            }
        }

        private async void Icon_EffectiveViewportChanged(FrameworkElement sender, EffectiveViewportChangedEventArgs args)
        {
            var parentRow = Interacts.Interaction.FindParent<DataGridRow>(sender);
            if (parentRow.DataContext is ListedItem item &&
                !item.ItemPropertiesInitialized &&
                args.BringIntoViewDistanceX < sender.ActualHeight)
            {
                await Window.Current.CoreWindow.Dispatcher.RunIdleAsync((e) =>
                {
                    ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(parentRow.DataContext as ListedItem);
                    (parentRow.DataContext as ListedItem).ItemPropertiesInitialized = true;
                    //sender.EffectiveViewportChanged -= Icon_EffectiveViewportChanged;
                });
            }
        }

        private void AllView_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            InitializeDrag(e.Row);
        }

        protected override ListedItem GetItemFromElement(object element)
        {
            DataGridRow row = element as DataGridRow;
            return row.DataContext as ListedItem;
        }

        private void RadioMenuSortColumn_Click(object sender, RoutedEventArgs e)
        {
            DataGridColumnEventArgs args = null;

            switch ((sender as RadioMenuFlyoutItem).Tag)
            {
                case "nameColumn":
                    args = new DataGridColumnEventArgs(nameColumn);
                    break;

                case "dateColumn":
                    args = new DataGridColumnEventArgs(dateColumn);
                    break;

                case "typeColumn":
                    args = new DataGridColumnEventArgs(typeColumn);
                    break;

                case "sizeColumn":
                    args = new DataGridColumnEventArgs(sizeColumn);
                    break;
            }

            if (args != null)
            {
                AllView_Sorting(sender, args);
            }
        }

        private void RadioMenuSortDirection_Click(object sender, RoutedEventArgs e)
        {
            if (((sender as RadioMenuFlyoutItem).Tag as string) == "sortAscending")
            {
                SortedColumn.SortDirection = DataGridSortDirection.Ascending;
            }
            else if (((sender as RadioMenuFlyoutItem).Tag as string) == "sortDescending")
            {
                SortedColumn.SortDirection = DataGridSortDirection.Descending;
            }
        }
    }
}