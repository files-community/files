// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents contract of AddressToolbar view model.
	/// </summary>
	public interface IAddressToolbarViewModel
	{
		public bool IsSearchBoxVisible { get; set; }

		public bool IsEditModeEnabled { get; set; }

		public bool IsCommandPaletteOpen { get; set; }

		public bool CanRefresh { get; set; }

		public bool CanCopyPathInPage { get; set; }

		public bool CanNavigateToParent { get; set; }

		public bool CanGoBack { get; set; }

		public bool CanGoForward { get; set; }

		public bool IsSingleItemOverride { get; set; }

		public string PathControlDisplayText { get; set; }

		public ObservableCollection<PathBoxItem> PathComponents { get; }

		public delegate void ToolbarQuerySubmittedEventHandler(object sender, ToolbarQuerySubmittedEventArgs e);

		public delegate void ItemDraggedOverPathItemEventHandler(object sender, PathNavigationEventArgs e);

		public event ToolbarQuerySubmittedEventHandler PathBoxQuerySubmitted;

		public event EventHandler EditModeEnabled;

		public event ItemDraggedOverPathItemEventHandler ItemDraggedOverPathItem;

		public event EventHandler RefreshRequested;

		public event EventHandler RefreshWidgetsRequested;

		public void SwitchSearchBoxVisibility();

		public ISearchBoxViewModel SearchBox { get; }
	}
}