// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Commands
{
	public interface ICommandManager : IEnumerable<IRichCommand>
	{
		IRichCommand this[CommandCodes code] { get; }
		IRichCommand this[string code] { get; }
		IRichCommand this[HotKey customHotKey] { get; }

		IRichCommand AddItem { get; }
		IRichCommand ClearSelection { get; }
		IRichCommand CloseOtherTabsCurrent { get; }
		IRichCommand CloseOtherTabsSelected { get; }
		IRichCommand ClosePane { get; }
		IRichCommand CloseSelectedTab { get; }
		IRichCommand CloseTabsToTheLeftCurrent { get; }
		IRichCommand CloseTabsToTheLeftSelected { get; }
		IRichCommand CloseTabsToTheRightCurrent { get; }
		IRichCommand CloseTabsToTheRightSelected { get; }
		IRichCommand CompressIntoArchive { get; }
		IRichCommand CompressIntoSevenZip { get; }
		IRichCommand CompressIntoZip { get; }
		IRichCommand CopyItem { get; }
		IRichCommand CopyPath { get; }
		IRichCommand CreateFolder { get; }
		IRichCommand CreateFolderWithSelection { get; }
		IRichCommand CreateShortcut { get; }
		IRichCommand CreateShortcutFromDialog { get; }
		IRichCommand CutItem { get; }
		IRichCommand DecompressArchive { get; }
		IRichCommand DecompressArchiveHere { get; }
		IRichCommand DecompressArchiveToChildFolder { get; }
		IRichCommand DeleteItem { get; }
		IRichCommand DeleteItemPermanently { get; }
		IRichCommand DuplicateCurrentTab { get; }
		IRichCommand DuplicateSelectedTab { get; }
		IRichCommand EditPath { get; }
		IRichCommand EmptyRecycleBin { get; }
		IRichCommand EnterCompactOverlay { get; }
		IRichCommand ExitCompactOverlay { get; }
		IRichCommand FormatDrive { get; }
		IRichCommand GitFetch { get; }
		IRichCommand GitInit { get; }
		IRichCommand GitPull { get; }
		IRichCommand GitPush { get; }
		IRichCommand GitSync { get; }
		IRichCommand GroupAscending { get; }
		IRichCommand GroupByDateCreated { get; }
		IRichCommand GroupByDateCreatedMonth { get; }
		IRichCommand GroupByDateCreatedYear { get; }
		IRichCommand GroupByDateDeleted { get; }
		IRichCommand GroupByDateDeletedMonth { get; }
		IRichCommand GroupByDateDeletedYear { get; }
		IRichCommand GroupByDateModified { get; }
		IRichCommand GroupByDateModifiedMonth { get; }
		IRichCommand GroupByDateModifiedYear { get; }
		IRichCommand GroupByFolderPath { get; }
		IRichCommand GroupByMonth { get; }
		IRichCommand GroupByName { get; }
		IRichCommand GroupByNone { get; }
		IRichCommand GroupByOriginalFolder { get; }
		IRichCommand GroupBySize { get; }
		IRichCommand GroupBySyncStatus { get; }
		IRichCommand GroupByTag { get; }
		IRichCommand GroupByType { get; }
		IRichCommand GroupByYear { get; }
		IRichCommand GroupDescending { get; }
		IRichCommand InstallCertificate { get; }
		IRichCommand InstallFont { get; }
		IRichCommand InstallInfDriver { get; }
		IRichCommand InvertSelection { get; }
		IRichCommand LaunchPreviewPopup { get; }
		IRichCommand LayoutAdaptive { get; }
		IRichCommand LayoutColumns { get; }
		IRichCommand LayoutDecreaseSize { get; }
		IRichCommand LayoutDetails { get; }
		IRichCommand LayoutGridLarge { get; }
		IRichCommand LayoutGridMedium { get; }
		IRichCommand LayoutGridSmall { get; }
		IRichCommand LayoutIncreaseSize { get; }
		IRichCommand LayoutTiles { get; }
		IRichCommand NavigateBack { get; }
		IRichCommand NavigateForward { get; }
		IRichCommand NavigateUp { get; }
		IRichCommand NewTab { get; }
		IRichCommand NextTab { get; }
		IRichCommand None { get; }
		IRichCommand OpenAllTaggedItems { get; }
		IRichCommand OpenCommandPalette { get; }
		IRichCommand OpenDirectoryInNewPaneAction { get; }
		IRichCommand OpenDirectoryInNewTabAction { get; }
		IRichCommand OpenFileLocation { get; }
		IRichCommand OpenHelp { get; }
		IRichCommand OpenInNewWindowItemAction { get; }
		IRichCommand OpenInVS { get; }
		IRichCommand OpenInVSCode { get; }
		IRichCommand OpenItem { get; }
		IRichCommand OpenItemWithApplicationPicker { get; }
		IRichCommand OpenNewPane { get; }
		IRichCommand OpenParentFolder { get; }
		IRichCommand OpenProperties { get; }
		IRichCommand OpenSettings { get; }
		IRichCommand OpenTerminal { get; }
		IRichCommand OpenTerminalAsAdmin { get; }
		IRichCommand PasteItem { get; }
		IRichCommand PasteItemToSelection { get; }
		IRichCommand PinItemToFavorites { get; }
		IRichCommand PinToStart { get; }
		IRichCommand PlayAll { get; }
		IRichCommand PreviousTab { get; }
		IRichCommand Redo { get; }
		IRichCommand RefreshItems { get; }
		IRichCommand Rename { get; }
		IRichCommand ReopenClosedTab { get; }
		IRichCommand RestoreAllRecycleBin { get; }
		IRichCommand RestoreRecycleBin { get; }
		IRichCommand RotateLeft { get; }
		IRichCommand RotateRight { get; }
		IRichCommand RunAsAdmin { get; }
		IRichCommand RunAsAnotherUser { get; }
		IRichCommand RunWithPowershell { get; }
		IRichCommand Search { get; }
		IRichCommand SearchUnindexedItems { get; }
		IRichCommand SelectAll { get; }
		IRichCommand SetAsLockscreenBackground { get; }
		IRichCommand SetAsSlideshowBackground { get; }
		IRichCommand SetAsWallpaperBackground { get; }
		IRichCommand ShareItem { get; }
		IRichCommand SortAscending { get; }
		IRichCommand SortByDateCreated { get; }
		IRichCommand SortByDateDeleted { get; }
		IRichCommand SortByDateModified { get; }
		IRichCommand SortByName { get; }
		IRichCommand SortByOriginalFolder { get; }
		IRichCommand SortByPath { get; }
		IRichCommand SortBySize { get; }
		IRichCommand SortBySyncStatus { get; }
		IRichCommand SortByTag { get; }
		IRichCommand SortByType { get; }
		IRichCommand SortDescending { get; }
		IRichCommand ToggleCompactOverlay { get; }
		IRichCommand ToggleFullScreen { get; }
		IRichCommand ToggleGroupByDateUnit { get; }
		IRichCommand ToggleGroupDirection { get; }
		IRichCommand TogglePreviewPane { get; }
		IRichCommand ToggleSelect { get; }
		IRichCommand ToggleShowFileExtensions { get; }
		IRichCommand ToggleShowHiddenItems { get; }
		IRichCommand ToggleSortDirection { get; }
		IRichCommand ToggleSortDirectoriesAlongsideFiles { get; }
		IRichCommand Undo { get; }
		IRichCommand UnpinFromStart { get; }
		IRichCommand UnpinItemFromFavorites { get; }
	}
}
