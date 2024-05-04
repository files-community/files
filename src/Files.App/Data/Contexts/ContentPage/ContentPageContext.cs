﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.UserControls.TabBar;
using System.Collections.Immutable;

namespace Files.App.Data.Contexts
{
	internal sealed class ContentPageContext : ObservableObject, IContentPageContext
	{
		private static readonly IReadOnlyList<ListedItem> emptyItems = Enumerable.Empty<ListedItem>().ToImmutableList();

		private readonly IPageContext context = Ioc.Default.GetRequiredService<IPageContext>();

		private ShellViewModel? filesystemViewModel;

		public IShellPage? ShellPage => context?.PaneOrColumn;

		public Type PageLayoutType => ShellPage?.CurrentPageType ?? typeof(DetailsLayoutPage);

		private ContentPageTypes pageType = ContentPageTypes.None;
		public ContentPageTypes PageType => pageType;

		public ListedItem? Folder => ShellPage?.ShellViewModel?.CurrentFolder;

		public bool HasItem => ShellPage?.ToolbarViewModel?.HasItem ?? false;

		public bool HasSelection => SelectedItems.Count is not 0;
		public ListedItem? SelectedItem => SelectedItems.Count is 1 ? SelectedItems[0] : null;

		private IReadOnlyList<ListedItem> selectedItems = emptyItems;
		public IReadOnlyList<ListedItem> SelectedItems => selectedItems;

		public bool CanRefresh => ShellPage is not null && ShellPage.ToolbarViewModel.CanRefresh;

		public bool CanGoBack => ShellPage is not null && ShellPage.ToolbarViewModel.CanGoBack;

		public bool CanGoForward => ShellPage is not null && ShellPage.ToolbarViewModel.CanGoForward;

		public bool CanNavigateToParent => ShellPage is not null && ShellPage.ToolbarViewModel.CanNavigateToParent;

		public bool IsSearchBoxVisible => ShellPage is not null && ShellPage.ToolbarViewModel.IsSearchBoxVisible;

		public bool CanCreateItem => GetCanCreateItem();

		public bool IsMultiPaneEnabled => ShellPage is not null && ShellPage.PaneHolder is not null && ShellPage.PaneHolder.IsMultiPaneEnabled;

		public bool IsMultiPaneActive => ShellPage is not null && ShellPage.PaneHolder is not null && ShellPage.PaneHolder.IsMultiPaneActive;

		public bool IsGitRepository => ShellPage is not null && ShellPage.CurrentShellViewModel.IsGitRepository;

		public bool CanExecuteGitAction => IsGitRepository && !GitHelpers.IsExecutingGitAction;

		public string? SolutionFilePath => ShellPage?.ShellViewModel?.SolutionFilePath;

		public ContentPageContext()
		{
			context.Changing += Context_Changing;
			context.Changed += Context_Changed;
			GitHelpers.IsExecutingGitActionChanged += GitHelpers_IsExecutingGitActionChanged;

			Update();
		}

		private void GitHelpers_IsExecutingGitActionChanged(object? sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(CanExecuteGitAction));
		}

		private void Context_Changing(object? sender, EventArgs e)
		{
			if (ShellPage is IShellPage page)
			{
				page.PropertyChanged -= Page_PropertyChanged;
				page.ContentChanged -= Page_ContentChanged;
				page.CurrentShellViewModel.PropertyChanged -= InstanceViewModel_PropertyChanged;
				page.ToolbarViewModel.PropertyChanged -= ToolbarViewModel_PropertyChanged;

				if (page.PaneHolder is not null)
					page.PaneHolder.PropertyChanged -= PaneHolder_PropertyChanged;
			}

			if (filesystemViewModel is not null)
				filesystemViewModel.PropertyChanged -= FilesystemViewModel_PropertyChanged;
			filesystemViewModel = null;

			OnPropertyChanging(nameof(ShellPage));
		}
		private void Context_Changed(object? sender, EventArgs e)
		{
			if (ShellPage is IShellPage page)
			{
				page.PropertyChanged += Page_PropertyChanged;
				page.ContentChanged += Page_ContentChanged;
				page.CurrentShellViewModel.PropertyChanged += InstanceViewModel_PropertyChanged;
				page.ToolbarViewModel.PropertyChanged += ToolbarViewModel_PropertyChanged;
				
				if (page.PaneHolder is not null)
					page.PaneHolder.PropertyChanged += PaneHolder_PropertyChanged;
			}

			filesystemViewModel = ShellPage?.ShellViewModel;
			if (filesystemViewModel is not null)
				filesystemViewModel.PropertyChanged += FilesystemViewModel_PropertyChanged;

			Update();
			OnPropertyChanged(nameof(ShellPage));
			OnPropertyChanged(nameof(Folder));
		}

		private void Page_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ShellPage.CurrentPageType):
					OnPropertyChanged(nameof(PageLayoutType));
					break;
				case nameof(ShellPage.PaneHolder):
					OnPropertyChanged(nameof(IsMultiPaneEnabled));
					OnPropertyChanged(nameof(IsMultiPaneActive));
					break;
			}
		}

		private void Page_ContentChanged(object? sender, CustomTabViewItemParameter e) => Update();

		private void PaneHolder_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IPaneHolder.IsMultiPaneEnabled):
				case nameof(IPaneHolder.IsMultiPaneActive):
					OnPropertyChanged(e.PropertyName);
					break;
			}
		}

		private void InstanceViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(CurrentShellViewModel.IsPageTypeNotHome):
				case nameof(CurrentShellViewModel.IsPageTypeRecycleBin):
				case nameof(CurrentShellViewModel.IsPageTypeZipFolder):
				case nameof(CurrentShellViewModel.IsPageTypeFtp):
				case nameof(CurrentShellViewModel.IsPageTypeLibrary):
				case nameof(CurrentShellViewModel.IsPageTypeCloudDrive):
				case nameof(CurrentShellViewModel.IsPageTypeMtpDevice):
				case nameof(CurrentShellViewModel.IsPageTypeSearchResults):
					UpdatePageType();
					break;
				case nameof(CurrentShellViewModel.IsGitRepository):
					OnPropertyChanged(nameof(IsGitRepository));
					OnPropertyChanged(nameof(CanExecuteGitAction));
					break;
			}
		}

		private void ToolbarViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(AddressToolbarViewModel.CanGoBack):
				case nameof(AddressToolbarViewModel.CanGoForward):
				case nameof(AddressToolbarViewModel.CanNavigateToParent):
				case nameof(AddressToolbarViewModel.HasItem):
				case nameof(AddressToolbarViewModel.CanRefresh):
				case nameof(AddressToolbarViewModel.IsSearchBoxVisible):
					OnPropertyChanged(e.PropertyName);
					break;
				case nameof(AddressToolbarViewModel.SelectedItems):
					UpdateSelectedItems();
					break;
			}
		}

		private void FilesystemViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ShellViewModel.CurrentFolder):
					OnPropertyChanged(nameof(Folder));
					break;
				case nameof(ShellViewModel.SolutionFilePath):
					OnPropertyChanged(nameof(SolutionFilePath));
					break;
			}
		}

		private void Update()
		{
			UpdatePageType();
			UpdateSelectedItems();

			OnPropertyChanged(nameof(HasItem));
			OnPropertyChanged(nameof(CanGoBack));
			OnPropertyChanged(nameof(CanGoForward));
			OnPropertyChanged(nameof(CanNavigateToParent));
			OnPropertyChanged(nameof(CanRefresh));
			OnPropertyChanged(nameof(CanCreateItem));
			OnPropertyChanged(nameof(IsMultiPaneEnabled));
			OnPropertyChanged(nameof(IsMultiPaneActive));
			OnPropertyChanged(nameof(IsGitRepository));
			OnPropertyChanged(nameof(CanExecuteGitAction));
		}

		private void UpdatePageType()
		{
			var type = ShellPage?.CurrentShellViewModel switch
			{
				null => ContentPageTypes.None,
				{ IsPageTypeNotHome: false } => ContentPageTypes.Home,
				{ IsPageTypeRecycleBin: true } => ContentPageTypes.RecycleBin,
				{ IsPageTypeZipFolder: true } => ContentPageTypes.ZipFolder,
				{ IsPageTypeFtp: true } => ContentPageTypes.Ftp,
				{ IsPageTypeLibrary: true } => ContentPageTypes.Library,
				{ IsPageTypeCloudDrive: true } => ContentPageTypes.CloudDrive,
				{ IsPageTypeMtpDevice: true } => ContentPageTypes.MtpDevice,
				{ IsPageTypeSearchResults: true } => ContentPageTypes.SearchResults,
				_ => ContentPageTypes.Folder,
			};
			SetProperty(ref pageType, type, nameof(PageType));
			OnPropertyChanged(nameof(CanCreateItem));
		}

		private void UpdateSelectedItems()
		{
			bool oldHasSelection = HasSelection;
			ListedItem? oldSelectedItem = SelectedItem;

			IReadOnlyList<ListedItem> items = ShellPage?.ToolbarViewModel?.SelectedItems?.AsReadOnly() ?? emptyItems;
			if (SetProperty(ref selectedItems, items, nameof(SelectedItems)))
			{
				if (HasSelection != oldHasSelection)
					OnPropertyChanged(nameof(HasSelection));
				if (SelectedItem != oldSelectedItem)
					OnPropertyChanged(nameof(SelectedItem));
			}
		}

		private bool GetCanCreateItem()
		{
			return ShellPage is not null &&
				pageType is not ContentPageTypes.None
				and not ContentPageTypes.Home
				and not ContentPageTypes.RecycleBin
				and not ContentPageTypes.ZipFolder
				and not ContentPageTypes.SearchResults
				and not ContentPageTypes.MtpDevice;
		}
	}
}
