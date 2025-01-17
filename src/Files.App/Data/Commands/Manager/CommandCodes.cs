// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Commands
{
	public enum CommandCodes
	{
		None,

		// Global
		OpenHelp,
		ToggleFullScreen,
		EnterCompactOverlay,
		ExitCompactOverlay,
		ToggleCompactOverlay,
		Search,
		EditPath,
		Redo,
		Undo,

		// Show
		ToggleShowHiddenItems,
		ToggleDotFilesSetting,
		ToggleShowFileExtensions,
		TogglePreviewPane,
		ToggleDetailsPane,
		ToggleInfoPane,
		ToggleToolbar,
		ToggleShelfPane,

		// File System
		CopyItem,
		CopyItemPath,
		CopyPath,
		CopyItemPathWithQuotes,
		CopyPathWithQuotes,
		CutItem,
		PasteItem,
		PasteItemAsShortcut,
		PasteItemToSelection,
		DeleteItem,
		DeleteItemPermanently,
		CreateFolder,
		CreateFolderWithSelection,
		AddItem,
		CreateAlternateDataStream,
		CreateShortcut,
		CreateShortcutFromDialog,
		EmptyRecycleBin,
		FormatDrive,
		FormatDriveFromHome,
		FormatDriveFromSidebar,
		RestoreRecycleBin,
		RestoreAllRecycleBin,
		OpenItem,
		OpenItemWithApplicationPicker,
		OpenParentFolder,
		OpenFileLocation,
		RefreshItems,
		Rename,

		// Selection
		SelectAll,
		InvertSelection,
		ClearSelection,
		ToggleSelect,

		// Share
		ShareItem,

		// Start
		PinToStart,
		UnpinFromStart,

		// Sidebar
		PinFolderToSidebar,
		UnpinFolderFromSidebar,

		// Backgrounds
		SetAsWallpaperBackground,
		SetAsSlideshowBackground,
		SetAsLockscreenBackground,
		SetAsAppBackground,

		// Install
		InstallFont,
		InstallInfDriver,
		InstallCertificate,

		// Run
		RunAsAdmin,
		RunAsAnotherUser,
		RunWithPowershell,

		// Preview Popup
		LaunchPreviewPopup,

		// Archives
		CompressIntoArchive,
		CompressIntoSevenZip,
		CompressIntoZip,
		DecompressArchive,
		DecompressArchiveHere,
		DecompressArchiveHereSmart,
		DecompressArchiveToChildFolder,

		// Folders
		FlattenFolder,

		// Image Manipulation
		RotateLeft,
		RotateRight,

		// Open
		OpenInVSCode,
		OpenRepoInVSCode,
		OpenProperties,
		OpenReleaseNotes,
		OpenClassicProperties,
		OpenSettings,
		OpenStorageSense,
		OpenStorageSenseFromHome,
		OpenStorageSenseFromSidebar,
		OpenTerminal,
		OpenTerminalAsAdmin,
		OpenTerminalFromSidebar,
		OpenTerminalFromHome,
		OpenCommandPalette,
		EditInNotepad,

		// Layout
		LayoutDecreaseSize,
		LayoutIncreaseSize,
		LayoutDetails,
		LayoutList,
		LayoutCards,
		LayoutGrid,
		LayoutColumns,
		LayoutAdaptive,

		// Sort by
		SortByName,
		SortByDateModified,
		SortByDateCreated,
		SortBySize,
		SortByType,
		SortBySyncStatus,
		SortByTag,
		SortByPath,
		SortByOriginalFolder,
		SortByDateDeleted,
		SortAscending,
		SortDescending,
		ToggleSortDirection,
		SortFoldersFirst,
		SortFilesFirst,
		SortFilesAndFoldersTogether,

		// Group by
		GroupByNone,
		GroupByName,
		GroupByDateModified,
		GroupByDateCreated,
		GroupBySize,
		GroupByType,
		GroupBySyncStatus,
		GroupByTag,
		GroupByOriginalFolder,
		GroupByDateDeleted,
		GroupByFolderPath,
		GroupByDateModifiedYear,
		GroupByDateModifiedMonth,
		GroupByDateModifiedDay,
		GroupByDateCreatedYear,
		GroupByDateCreatedMonth,
		GroupByDateCreatedDay,
		GroupByDateDeletedYear,
		GroupByDateDeletedMonth,
		GroupByDateDeletedDay,
		GroupAscending,
		GroupDescending,
		ToggleGroupDirection,
		GroupByYear,
		GroupByMonth,
		ToggleGroupByDateUnit,

		// Navigation
		NewWindow,
		NewTab,
		NavigateBack,
		NavigateForward,
		NavigateUp,
		NavigateHome,

		// Other
		DuplicateCurrentTab,
		DuplicateSelectedTab,
		CloseTabsToTheLeftCurrent,
		CloseTabsToTheLeftSelected,
		CloseTabsToTheRightCurrent,
		CloseTabsToTheRightSelected,
		CloseOtherTabsCurrent,
		CloseOtherTabsSelected,
		ReopenClosedTab,
		PreviousTab,
		NextTab,
		CloseSelectedTab,
		CloseAllTabs,

		// Shell Panes
		CloseActivePane,
		FocusOtherPane,
		AddVerticalPane,
		AddHorizontalPane,
		ArrangePanesVertically,
		ArrangePanesHorizontally,

		// OpenInNew
		OpenInNewPane,
		OpenInNewPaneFromHome,
		OpenInNewPaneFromSidebar,
		OpenInNewTab,
		OpenInNewTabFromHome,
		OpenInNewTabFromSidebar,
		OpenInNewWindow,
		OpenInNewWindowFromHome,
		OpenInNewWindowFromSidebar,

		// Play
		PlayAll,

		// Git
		GitFetch,
		GitInit,
		GitPull,
		GitPush,
		GitSync,

		// Tags
		OpenAllTaggedItems,
	}
}
