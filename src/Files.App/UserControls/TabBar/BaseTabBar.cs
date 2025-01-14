// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;

namespace Files.App.UserControls.TabBar
{
	/// <summary>
	/// Represents base class for <see cref="TabBar"/>.
	/// </summary>
	public abstract class BaseTabBar : UserControl, ITabBar, INotifyPropertyChanged
	{
		protected ITabBarItemContent CurrentSelectedAppInstance;

		public static event EventHandler<ITabBar>? OnLoaded;
		public static event PropertyChangedEventHandler? StaticPropertyChanged;
		public event PropertyChangedEventHandler? PropertyChanged;

		public const string TabDropHandledIdentifier = "FilesTabViewItemDropHandled";

		public const string TabPathIdentifier = "FilesTabViewItemPath";

		// RecentlyClosedTabs is shared between all multitasking controls
		public static Stack<TabBarItemParameter[]> RecentlyClosedTabs { get; private set; } = new();

		public ObservableCollection<TabBarItem> Items
			=> MainPageViewModel.AppInstances;

		public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

		private static bool _IsRestoringClosedTab;
		public static bool IsRestoringClosedTab
		{
			get => _IsRestoringClosedTab;
			private set
			{
				_IsRestoringClosedTab = value;
				StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(IsRestoringClosedTab)));
			}
		}

		public BaseTabBar()
		{
			Loaded += TabView_Loaded;
		}

		public virtual DependencyObject ContainerFromItem(ITabBarItem item)
		{
			return null;
		}

		private void TabView_CurrentInstanceChanged(object sender, CurrentInstanceChangedEventArgs e)
		{
			foreach (ITabBarItemContent instance in e.PageInstances)
			{
				if (instance is not null)
				{
					instance.IsCurrentInstance = instance == e.CurrentInstance;
				}
			}
		}

		protected void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (App.AppModel.TabStripSelectedIndex >= 0 && App.AppModel.TabStripSelectedIndex < Items.Count)
			{
				CurrentSelectedAppInstance = GetCurrentSelectedTabInstance();

				if (CurrentSelectedAppInstance is not null)
				{
					CurrentInstanceChanged?.Invoke(this, new CurrentInstanceChangedEventArgs()
					{
						CurrentInstance = CurrentSelectedAppInstance,
						PageInstances = GetAllTabInstances()
					});
				}
			}
		}

		protected void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
		{
			CloseTab(args.Item as TabBarItem);
		}

		protected void OnCurrentInstanceChanged(CurrentInstanceChangedEventArgs args)
		{
			CurrentInstanceChanged?.Invoke(this, args);
		}

		protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void TabView_Loaded(object sender, RoutedEventArgs e)
		{
			CurrentInstanceChanged += TabView_CurrentInstanceChanged;
			OnLoaded?.Invoke(null, this);
		}

		public ITabBarItemContent GetCurrentSelectedTabInstance()
		{
			return MainPageViewModel.AppInstances[App.AppModel.TabStripSelectedIndex].TabItemContent;
		}

		public void SelectionChanged()
		{
			TabView_SelectionChanged(null, null);
		}

		public static void PushRecentTab(TabBarItemParameter[] tab)
		{
			RecentlyClosedTabs.Push(tab);
			StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(RecentlyClosedTabs)));
		}

		public List<ITabBarItemContent> GetAllTabInstances()
		{
			return MainPageViewModel.AppInstances.Select(x => x.TabItemContent).ToList();
		}

		public async Task ReopenClosedTabAsync()
		{
			if (!IsRestoringClosedTab && RecentlyClosedTabs.Count > 0)
			{
				IsRestoringClosedTab = true;
				var lastTab = RecentlyClosedTabs.Pop();
				foreach (var item in lastTab)
					await NavigationHelpers.AddNewTabByParamAsync(item.InitialPageType, item.NavigationParameter);

				IsRestoringClosedTab = false;
			}
		}

		public async void MoveTabToNewWindowAsync(object sender, RoutedEventArgs e)
		{
			await MultitaskingTabsHelpers.MoveTabToNewWindow(((FrameworkElement)sender).DataContext as TabBarItem, this);
		}

		public void CloseTab(TabBarItem tabItem)
		{
			if (tabItem is null)
				return;

			Items.Remove(tabItem);
			tabItem.Unload();
			
			// Dispose and save tab arguments
			PushRecentTab(
			[
				tabItem.NavigationParameter,
			]);

			// Save the updated tab list
			AppLifecycleHelper.SaveSessionTabs();

			if (Items.Count == 0)
				MainWindow.Instance.Close();
		}

		public void SetLoadingIndicatorStatus(ITabBarItem item, bool loading)
		{
			if (ContainerFromItem(item) is not Control tabItem)
				return;

			var stateToGoName = (loading) ? "Loading" : "NotLoading";

			VisualStateManager.GoToState(tabItem, stateToGoName, false);
		}
	}
}
