// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Win32;

namespace Files.App.UserControls.TabBar
{
	public sealed partial class TabBar : BaseTabBar, INotifyPropertyChanged
	{
		// Dependency injections

		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private readonly IAppearanceSettingsService AppearanceSettingsService = Ioc.Default.GetRequiredService<IAppearanceSettingsService>();
		private readonly IWindowContext WindowContext = Ioc.Default.GetRequiredService<IWindowContext>();

		// Fields

		private readonly DispatcherTimer tabHoverTimer = new();

		private TabViewItem? hoveredTabViewItem;

		private bool _lockDropOperation = false;

		// Starting position when dragging a tab
		private System.Drawing.Point dragStartPoint;

		// Starting time when dragging a tab
		private DateTimeOffset dragStartTime;

		// Indicates if drag operation should be canceled.
		// This value gets reset at the start of the drag operation
		private bool isCancelingDragOperation;

		//private string[] _droppableArchiveTypes = { "zip", "rar", "7z", "tar" };

		// Properties

		public bool ShowTabActionsButton
			=> AppearanceSettingsService.ShowTabActions;

		public bool AllowTabsDrag
			=> WindowContext.CanDragAndDrop;

		public Rectangle DragArea
			=> DragAreaRectangle;

		// Events

		public static event EventHandler<TabBarItem?>? SelectedTabItemChanged;

		// Constructor

		public TabBar()
		{
			InitializeComponent();

			tabHoverTimer.Interval = TimeSpan.FromMilliseconds(Constants.DragAndDrop.HoverToOpenTimespan);
			tabHoverTimer.Tick += TabHoverSelected;

			AppearanceSettingsService.PropertyChanged += (s, e) =>
			{
				switch (e.PropertyName)
				{
					case nameof(AppearanceSettingsService.ShowTabActions):
						NotifyPropertyChanged(nameof(ShowTabActionsButton));
						break;
				}
			};
		}

		private void TabView_TabItemsChanged(TabView sender, Windows.Foundation.Collections.IVectorChangedEventArgs args)
		{
			if (args.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemRemoved)
				App.AppModel.TabStripSelectedIndex = Items.IndexOf(HorizontalTabView.SelectedItem as TabBarItem);

			if (App.AppModel.TabStripSelectedIndex >= 0 && App.AppModel.TabStripSelectedIndex < Items.Count)
			{
				CurrentSelectedAppInstance = GetCurrentSelectedTabInstance();

				if (CurrentSelectedAppInstance is not null)
				{
					OnCurrentInstanceChanged(new CurrentInstanceChangedEventArgs()
					{
						CurrentInstance = CurrentSelectedAppInstance,
						PageInstances = GetAllTabInstances()
					});
				}
			}

			HorizontalTabView.SelectedIndex = App.AppModel.TabStripSelectedIndex;
		}

		private async void TabViewItem_Drop(object sender, DragEventArgs e)
		{
			await ((sender as TabViewItem).DataContext as TabBarItem).TabItemContent.TabItemDrop(sender, e);
			HorizontalTabView.CanReorderTabs = true;
			tabHoverTimer.Stop();
		}

		private async void TabViewItem_DragEnter(object sender, DragEventArgs e)
		{
			await ((sender as TabViewItem).DataContext as TabBarItem).TabItemContent.TabItemDragOver(sender, e);
			if (e.AcceptedOperation != DataPackageOperation.None)
			{
				HorizontalTabView.CanReorderTabs = false;
				tabHoverTimer.Start();
				hoveredTabViewItem = sender as TabViewItem;
			}
		}

		private void TabViewItem_DragLeave(object sender, DragEventArgs e)
		{
			tabHoverTimer.Stop();
			hoveredTabViewItem = null;
		}

		// Select tab that is hovered over for a certain duration
		private void TabHoverSelected(object sender, object e)
		{
			tabHoverTimer.Stop();
			if (hoveredTabViewItem is not null)
				App.AppModel.TabStripSelectedIndex = Items.IndexOf(hoveredTabViewItem.DataContext as TabBarItem);
		}

		private void TabView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
		{
			// Reset value
			isCancelingDragOperation = false;

			var tabViewItemArgs = (args.Item as TabBarItem).NavigationParameter;
			args.Data.Properties.Add(TabPathIdentifier, tabViewItemArgs.Serialize());
			args.Data.RequestedOperation = DataPackageOperation.Move;

			// Get cursor position & time to track how far the tab was dragged.
			PInvoke.GetCursorPos(out dragStartPoint);
			dragStartTime = DateTimeOffset.UtcNow;

			// Focus the UI Element, without this the focus sometimes changes
			// and the PreviewKeyDown event won't trigger.
			Focus(FocusState.Programmatic);
			PreviewKeyDown += TabDragging_PreviewKeyDown;
		}

		private void TabDragging_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			// Pressing escape will automatically complete the drag event but we need to set the
			// isCancelingDragOperation field in order to detect if escape was pressed.
			if (e.Key is Windows.System.VirtualKey.Escape)
				isCancelingDragOperation = true;
		}

		private void TabView_TabStripDragOver(object sender, DragEventArgs e)
		{
			if (e.DataView.Properties.ContainsKey(TabPathIdentifier))
			{
				HorizontalTabView.CanReorderTabs = WindowContext.CanDragAndDrop;

				e.AcceptedOperation = DataPackageOperation.Move;
				e.DragUIOverride.Caption = "TabStripDragAndDropUIOverrideCaption".GetLocalizedResource();
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.IsGlyphVisible = false;
			}
			else
			{
				HorizontalTabView.CanReorderTabs = false;
			}
		}

		private void TabView_DragLeave(object sender, DragEventArgs e)
		{
			HorizontalTabView.CanReorderTabs = WindowContext.CanDragAndDrop;
		}

		private async void TabView_TabStripDrop(object sender, DragEventArgs e)
		{
			HorizontalTabView.CanReorderTabs = WindowContext.CanDragAndDrop;

			if (!(sender is TabView tabStrip))
				return;

			if (!e.DataView.Properties.TryGetValue(TabPathIdentifier, out object tabViewItemPathObj) ||
				tabViewItemPathObj is not string tabViewItemString)
				return;

			var index = -1;

			for (int i = 0; i < tabStrip.TabItems.Count; i++)
			{
				var item = tabStrip.ContainerFromIndex(i) as TabViewItem;

				if (e.GetPosition(item).X - item.ActualWidth < 0)
				{
					index = i;
					break;
				}
			}

			var tabViewItemArgs = TabBarItemParameter.Deserialize(tabViewItemString);
			ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier] = true;
			await NavigationHelpers.AddNewTabByParamAsync(tabViewItemArgs.InitialPageType, tabViewItemArgs.NavigationParameter, index);
		}

		private void TabView_TabDragCompleted(TabView sender, TabViewTabDragCompletedEventArgs args)
		{
			// Unsubscribe from the key down event, it's only needed when a tab is actively being dragged
			PreviewKeyDown -= TabDragging_PreviewKeyDown;

			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier) &&
				(bool)ApplicationData.Current.LocalSettings.Values[TabDropHandledIdentifier])
				CloseTab(args.Item as TabBarItem);
			else
				HorizontalTabView.SelectedItem = args.Tab;

			if (ApplicationData.Current.LocalSettings.Values.ContainsKey(TabDropHandledIdentifier))
				ApplicationData.Current.LocalSettings.Values.Remove(TabDropHandledIdentifier);
		}

		private async void TabView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
		{
			// Unsubscribe from the key down event, it's only needed when a tab is actively being dragged
			PreviewKeyDown -= TabDragging_PreviewKeyDown;

			if (isCancelingDragOperation)
				return;

			PInvoke.GetCursorPos(out var droppedPoint);
			var droppedTime = DateTimeOffset.UtcNow;
			var dragTime = droppedTime - dragStartTime;
			var dragDistance = Math.Sqrt(Math.Pow(dragStartPoint.X - droppedPoint.X, 2) + Math.Pow(dragStartPoint.Y - droppedPoint.Y, 2));

			if (sender.TabItems.Count == 1 ||
				(dragTime.TotalSeconds < 1 &&
				dragDistance < 100))
				return;

			var indexOfTabViewItem = sender.TabItems.IndexOf(args.Item);
			var tabViewItemArgs = (args.Item as TabBarItem).NavigationParameter;
			var selectedTabViewItemIndex = sender.SelectedIndex;

			Items.Remove(args.Item as TabBarItem);
			if (!await NavigationHelpers.OpenTabInNewWindowAsync(tabViewItemArgs.Serialize()))
			{
				Items.Insert(indexOfTabViewItem, args.Item as TabBarItem);
				sender.SelectedIndex = selectedTabViewItemIndex;
			}
			else
				// Dispose tab arguments
				(args.Item as TabBarItem)?.Unload();
		}

		private void TabView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
		{
			var delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;

			if (delta > 0)
			{
				// Scroll up, select the next tab
				if (HorizontalTabView.SelectedIndex < HorizontalTabView.TabItems.Count - 1)
					HorizontalTabView.SelectedIndex++;
			}
			else
			{
				// Scroll down, select the previous tab
				if (HorizontalTabView.SelectedIndex > 0)
					HorizontalTabView.SelectedIndex--;
			}

			e.Handled = true;
		}

		private void TabItemContextMenu_Opening(object sender, object e)
		{
			MenuItemMoveTabToNewWindow.IsEnabled = Items.Count > 1;
			SelectedTabItemChanged?.Invoke(null, ((MenuFlyout)sender).Target.DataContext as TabBarItem);
		}

		private void TabItemContextMenu_Closing(object sender, object e)
		{
			SelectedTabItemChanged?.Invoke(null, null);
		}

		private async void TabBarAddNewTabButton_Drop(object sender, DragEventArgs e)
		{
			if (_lockDropOperation || !FilesystemHelpers.HasDraggedStorageItems(e.DataView))
				return;

			_lockDropOperation = true;

			//|| _droppableArchiveTypes.Contains(x.Name.Split('.').Last().ToLower())
			var items = (await FilesystemHelpers.GetDraggedStorageItems(e.DataView))
				.Where(x => x.ItemType is FilesystemItemType.Directory);

			var deferral = e.GetDeferral();
			try
			{
				foreach (var item in items)
					await NavigationHelpers.OpenPathInNewTab(item.Path, true);

				deferral.Complete();
			}
			catch { }

			_lockDropOperation = false;
		}

		private async void TabBarAddNewTabButton_DragOver(object sender, DragEventArgs e)
		{
			if (!FilesystemHelpers.HasDraggedStorageItems(e.DataView))
			{
				e.AcceptedOperation = DataPackageOperation.None;
				return;
			}

			//|| _droppableArchiveTypes.Contains(x.Name.Split('.').Last().ToLower())
			bool hasValidDraggedItems =
				(await FilesystemHelpers.GetDraggedStorageItems(e.DataView)).Any(x => x.ItemType is FilesystemItemType.Directory);

			if (!hasValidDraggedItems)
			{
				e.AcceptedOperation = DataPackageOperation.None;
				return;
			}

			try
			{
				e.Handled = true;
				var deferral = e.GetDeferral();
				e.DragUIOverride.IsCaptionVisible = true;
				e.DragUIOverride.Caption = string.Format("OpenInNewTab".GetLocalizedResource());
				e.AcceptedOperation = DataPackageOperation.Link;
				deferral.Complete();
			}
			catch { }
		}

		public override DependencyObject ContainerFromItem(ITabBarItem item)
		{
			return HorizontalTabView.ContainerFromItem(item);
		}

		private void TabViewItem_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is TabViewItem tvi && tvi.FindDescendant("IconControl") is ContentControl control)
			{
				control.Content = (tvi.IconSource as ImageIconSource)?.CreateIconElement();
				tvi.RegisterPropertyChangedCallback(TabViewItem.IconSourceProperty, (s, args) =>
				{
					if (s is TabViewItem tabViewItem && tabViewItem.FindDescendant("IconControl") is ContentControl iconControl)
						iconControl.Content = (tabViewItem.IconSource as ImageIconSource)?.CreateIconElement();
				});
			}
		}

		private async void DragAreaRectangle_Loaded(object sender, RoutedEventArgs e)
		{
			if (HorizontalTabView.ActualWidth <= 0 && TabBarAddNewTabButton.Width <= 0)
				await Task.Delay(100);

			var titleBarInset = ((FilePropertiesHelpers.FlowDirectionSettingIsRightToLeft
				? MainWindow.Instance.AppWindow.TitleBar.LeftInset
				: MainWindow.Instance.AppWindow.TitleBar.RightInset) / DragAreaRectangle.XamlRoot.RasterizationScale) + 40;

			RightPaddingColumn.Width = new(titleBarInset > 40 ? titleBarInset : 138);
			HorizontalTabView.Measure(new(
				HorizontalTabView.ActualWidth - TabBarAddNewTabButton.Width - titleBarInset,
				HorizontalTabView.ActualHeight));
		}
	}
}