// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Data.Contracts
{
	public interface IShellPage : ITabBarItemContent, IMultiPaneInfo, IDisposable, INotifyPropertyChanged
	{
		ShellViewModel ShellViewModel { get; }

		CurrentInstanceViewModel InstanceViewModel { get; }

		StorageHistoryHelpers StorageHistoryHelpers { get; }

		IList<PageStackEntry> ForwardStack { get; }

		IList<PageStackEntry> BackwardStack { get; }

		IBaseLayoutPage SlimContentPage { get; }

		Type CurrentPageType { get; }

		IFilesystemHelpers FilesystemHelpers { get; }

		AddressToolbarViewModel ToolbarViewModel { get; }

		bool CanNavigateBackward { get; }

		bool CanNavigateForward { get; }

		/// <summary>
		/// True if the pane that contains this page is current.
		/// </summary>
		bool IsCurrentPane { get; }

		/// <summary>
		/// Returns a <see cref="Task"/> to wait until the pane and column become current.
		/// </summary>
		/// <returns>A <see cref="Task"/> to wait until the pane and column become current.</returns>
		Task WhenIsCurrent();

		Task RefreshIfNoWatcherExistsAsync();

		Task Refresh_Click();

		void Back_Click();

		void Forward_Click();

		void Up_Click();

		Task UpdatePathUIToWorkingDirectoryAsync(string newWorkingDir, string singleItemOverride = null);

		void NavigateToPath(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null);

		/// <summary>
		/// Gets the layout mode for the specified path then navigates to it
		/// </summary>
		/// <param name="navigationPath"></param>
		/// <param name="navArgs"></param>
		public void NavigateToPath(string navigationPath, NavigationArguments navArgs = null);

		/// <summary>
		/// Navigates to the home page
		/// </summary>
		public void NavigateHome();

		void NavigateWithArguments(Type sourcePageType, NavigationArguments navArgs);

		void RemoveLastPageFromBackStack();

		/// <summary>
		/// Replaces any outdated entries with those of the correct page type
		/// </summary>
		void ResetNavigationStackLayoutMode();

		void SubmitSearch(string query);

		/// <summary>
		/// Used to make commands in the column view work properly
		/// </summary>
		public bool IsColumnView { get; }
	}
}
