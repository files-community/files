// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.UserControls.TabBar
{
	/// <summary>
	/// Represents an interface for <see cref="UserControls.TabBar"/>.
	/// </summary>
	public interface ITabBar
	{
		public event EventHandler<CurrentInstanceChangedEventArgs> CurrentInstanceChanged;

		public ObservableCollection<TabBarItem> Items { get; }

		public ITabBarItemContent GetCurrentSelectedTabInstance();

		public List<ITabBarItemContent> GetAllTabInstances();

		public Task ReopenClosedTabAsync();

		public void CloseTab(TabBarItem tabItem);

		public void SetLoadingIndicatorStatus(ITabBarItem item, bool loading);
	}
}
