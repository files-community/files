// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Commands
{
	/// <summary>
	/// Represents a collection of <see cref="IRichCommand"/> and provides manager classes.
	/// </summary>
	public interface ICommandManager : IEnumerable<IRichCommand>
	{
		IRichCommand this[CommandCodes code] { get; }
		IRichCommand this[string code] { get; }
		IRichCommand this[HotKey customHotKey] { get; }

		IRichCommand None { get; }

		IRichCommand OpenHelp { get; }
		IRichCommand ToggleFullScreen { get; }
		IRichCommand EnterCompactOverlay { get; }
		IRichCommand ExitCompactOverlay { get; }
		IRichCommand ToggleCompactOverlay { get; }
		IRichCommand Search { get; }
		IRichCommand EditPath { get; }
		IRichCommand Redo { get; }
		IRichCommand Undo { get; }

		IRichCommand ToggleShowHiddenItems { get; }
		IRichCommand ToggleDotFilesSetting { get; }
		IRichCommand ToggleShowFileExtensions { get; }
		IRichCommand TogglePreviewPane { get; }
		IRichCommand ToggleDetailsPane { get; }
		IRichCommand ToggleInfoPane { get; }
		IRichCommand ToggleToolbar { get; }
		IRichCommand ToggleShelfPane { get; }

		IRichCommand CopyItem { get; }
		IRichCommand CopyItemPath { get; }
		IRichCommand CopyPath { get; }
		IRichCommand CopyItemPathWithQuotes { get; }
		IRichCommand CopyPathWithQuotes { get; }
		IRichCommand CutItem { get; }
		IRichCommand PasteItem { get; }
		IRichCommand PasteItemAsShortcut { get; }
		IRichCommand PasteItemToSelection { get; }
		IRichCommand DeleteItem { get; }
		IRichCommand DeleteItemPermanently { get; }
		IRichCommand SelectAll { get; }
		IRichCommand InvertSelection { get; }
		IRichCommand ClearSelection { get; }
		IRichCommand ToggleSelect { get; }
		IRichCommand ShareItem { get; }
		IRichCommand CreateFolder { get; }
		IRichCommand CreateFolderWithSelection { get; }
		IRichCommand AddItem { get; }
		IRichCommand CreateAlternateDataStream { get; }
		IRichCommand CreateShortcut { get; }
		IRichCommand CreateShortcutFromDialog { get; }
		IRichCommand EmptyRecycleBin { get; }
		IRichCommand RestoreRecycleBin { get; }
		IRichCommand RestoreAllRecycleBin { get; }
		IRichCommand FormatDrive { get; }
		IRichCommand FormatDriveFromHome { get; }
		IRichCommand FormatDriveFromSidebar { get; }
		IRichCommand OpenItem { get; }
		IRichCommand OpenItemWithApplicationPicker { get; }
		IRichCommand OpenParentFolder { get; }
		IRichCommand OpenFileLocation { get; }
		IRichCommand RefreshItems { get; }
		IRichCommand Rename { get; }

		IRichCommand PinToStart { get; }
		IRichCommand UnpinFromStart { get; }
		IRichCommand PinFolderToSidebar { get; }
		IRichCommand UnpinFolderFromSidebar { get; }

		IRichCommand SetAsWallpaperBackground { get; }
		IRichCommand SetAsSlideshowBackground { get; }
		IRichCommand SetAsLockscreenBackground { get; }
		IRichCommand SetAsAppBackground { get; }

		IRichCommand InstallFont { get; }
		IRichCommand InstallInfDriver { get; }
		IRichCommand InstallCertificate { get; }

		IRichCommand RunAsAdmin { get; }
		IRichCommand RunAsAnotherUser { get; }
		IRichCommand RunWithPowershell { get; }

		IRichCommand LaunchPreviewPopup { get; }

		IRichCommand CompressIntoArchive { get; }
		IRichCommand CompressIntoSevenZip { get; }
		IRichCommand CompressIntoZip { get; }
		IRichCommand DecompressArchive { get; }
		IRichCommand DecompressArchiveHere { get; }
		IRichCommand DecompressArchiveHereSmart { get; }
		IRichCommand DecompressArchiveToChildFolder { get; }

		IRichCommand FlattenFolder { get; }

		IRichCommand RotateLeft { get; }
		IRichCommand RotateRight { get; }

		IRichCommand OpenInVSCode { get; }
		IRichCommand OpenRepoInVSCode { get; }
		IRichCommand OpenProperties { get; }
		IRichCommand OpenReleaseNotes { get; }
		IRichCommand OpenClassicProperties { get; }
		IRichCommand OpenStorageSense { get; }
		IRichCommand OpenStorageSenseFromHome { get; }
		IRichCommand OpenStorageSenseFromSidebar { get; }
		IRichCommand OpenSettings { get; }
		IRichCommand OpenTerminal { get; }
		IRichCommand OpenTerminalAsAdmin { get; }
		IRichCommand OpenTerminalFromSidebar { get; }
		IRichCommand OpenTerminalFromHome { get; }
		IRichCommand OpenCommandPalette { get; }
		IRichCommand EditInNotepad { get; }

		IRichCommand LayoutDecreaseSize { get; }
		IRichCommand LayoutIncreaseSize { get; }
		IRichCommand LayoutDetails { get; }
		IRichCommand LayoutList { get; }
		IRichCommand LayoutCards{ get; }
		IRichCommand LayoutGrid { get; }
		IRichCommand LayoutColumns { get; }
		IRichCommand LayoutAdaptive { get; }

		IRichCommand SortByName { get; }
		IRichCommand SortByDateModified { get; }
		IRichCommand SortByDateCreated { get; }
		IRichCommand SortBySize { get; }
		IRichCommand SortByType { get; }
		IRichCommand SortBySyncStatus { get; }
		IRichCommand SortByTag { get; }
		IRichCommand SortByPath { get; }
		IRichCommand SortByOriginalFolder { get; }
		IRichCommand SortByDateDeleted { get; }
		IRichCommand SortAscending { get; }
		IRichCommand SortDescending { get; }
		IRichCommand ToggleSortDirection { get; }
		IRichCommand SortFoldersFirst { get; }
		IRichCommand SortFilesFirst { get; }
		IRichCommand SortFilesAndFoldersTogether { get; }

		IRichCommand GroupByNone { get; }
		IRichCommand GroupByName { get; }
		IRichCommand GroupByDateModified { get; }
		IRichCommand GroupByDateCreated { get; }
		IRichCommand GroupBySize { get; }
		IRichCommand GroupByType { get; }
		IRichCommand GroupBySyncStatus { get; }
		IRichCommand GroupByTag { get; }
		IRichCommand GroupByOriginalFolder { get; }
		IRichCommand GroupByDateDeleted { get; }
		IRichCommand GroupByFolderPath { get; }
		IRichCommand GroupByDateModifiedYear { get; }
		IRichCommand GroupByDateModifiedMonth { get; }
		IRichCommand GroupByDateModifiedDay { get; }
		IRichCommand GroupByDateCreatedYear { get; }
		IRichCommand GroupByDateCreatedMonth { get; }
		IRichCommand GroupByDateCreatedDay { get; }
		IRichCommand GroupByDateDeletedYear { get; }
		IRichCommand GroupByDateDeletedMonth { get; }
		IRichCommand GroupByDateDeletedDay { get; }
		IRichCommand GroupAscending { get; }
		IRichCommand GroupDescending { get; }
		IRichCommand ToggleGroupDirection { get; }
		IRichCommand GroupByYear { get; }
		IRichCommand GroupByMonth { get; }
		IRichCommand ToggleGroupByDateUnit { get; }

		IRichCommand NewWindow { get; }
		IRichCommand NewTab { get; }
		IRichCommand NavigateBack { get; }
		IRichCommand NavigateForward { get; }
		IRichCommand NavigateUp { get; }
		IRichCommand NavigateHome { get; }

		IRichCommand DuplicateCurrentTab { get; }
		IRichCommand DuplicateSelectedTab { get; }
		IRichCommand CloseTabsToTheLeftCurrent { get; }
		IRichCommand CloseTabsToTheLeftSelected { get; }
		IRichCommand CloseTabsToTheRightCurrent { get; }
		IRichCommand CloseTabsToTheRightSelected { get; }
		IRichCommand CloseOtherTabsCurrent { get; }
		IRichCommand CloseOtherTabsSelected { get; }
		IRichCommand ReopenClosedTab { get; }
		IRichCommand PreviousTab { get; }
		IRichCommand NextTab { get; }
		IRichCommand CloseSelectedTab { get; }
		IRichCommand CloseAllTabs { get; }

		IRichCommand CloseActivePane { get; }
		IRichCommand FocusOtherPane { get; }
		IRichCommand AddVerticalPane { get; }
		IRichCommand AddHorizontalPane { get; }
		IRichCommand ArrangePanesVertically { get; }
		IRichCommand ArrangePanesHorizontally { get; }

		IRichCommand OpenInNewPaneAction { get; }
		IRichCommand OpenInNewPaneFromHomeAction { get; }
		IRichCommand OpenInNewPaneFromSidebarAction { get; }
		IRichCommand OpenInNewTabAction { get; }
		IRichCommand OpenInNewTabFromHomeAction { get; }
		IRichCommand OpenInNewTabFromSidebarAction { get; }
		IRichCommand OpenInNewWindowAction { get; }
		IRichCommand OpenInNewWindowFromHomeAction { get; }
		IRichCommand OpenInNewWindowFromSidebarAction { get; }

		IRichCommand PlayAll { get; }

		IRichCommand GitFetch { get; }
		IRichCommand GitInit { get; }
		IRichCommand GitPull { get; }
		IRichCommand GitPush { get; }
		IRichCommand GitSync { get; }

		IRichCommand OpenAllTaggedItems { get; }
	}
}
