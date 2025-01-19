// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Actions;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace Files.App.Data.Commands
{
	internal sealed class CommandManager : ICommandManager
	{
		// Dependency injections

		private IActionsSettingsService ActionsSettingsService { get; } = Ioc.Default.GetRequiredService<IActionsSettingsService>();

		// Fields

		private readonly FrozenDictionary<CommandCodes, IRichCommand> commands;
		private ImmutableDictionary<HotKey, IRichCommand> _allKeyBindings = new Dictionary<HotKey, IRichCommand>().ToImmutableDictionary();

		public IRichCommand this[CommandCodes code] => commands.TryGetValue(code, out var command) ? command : None;
		public IRichCommand this[string code]
		{
			get
			{
				try
				{
					return commands[Enum.Parse<CommandCodes>(code, true)];
				}
				catch
				{
					return None;
				}
			}
		}
		public IRichCommand this[HotKey hotKey]
			=> _allKeyBindings.TryGetValue(hotKey with { IsVisible = true }, out var command) ? command
			: _allKeyBindings.TryGetValue(hotKey with { IsVisible = false }, out command) ? command
			: None;

		#region Commands
		public IRichCommand None => commands[CommandCodes.None];
		public IRichCommand OpenHelp => commands[CommandCodes.OpenHelp];
		public IRichCommand ToggleFullScreen => commands[CommandCodes.ToggleFullScreen];
		public IRichCommand EnterCompactOverlay => commands[CommandCodes.EnterCompactOverlay];
		public IRichCommand ExitCompactOverlay => commands[CommandCodes.ExitCompactOverlay];
		public IRichCommand ToggleCompactOverlay => commands[CommandCodes.ToggleCompactOverlay];
		public IRichCommand Search => commands[CommandCodes.Search];
		public IRichCommand EditPath => commands[CommandCodes.EditPath];
		public IRichCommand Redo => commands[CommandCodes.Redo];
		public IRichCommand Undo => commands[CommandCodes.Undo];
		public IRichCommand ToggleShowHiddenItems => commands[CommandCodes.ToggleShowHiddenItems];
		public IRichCommand ToggleDotFilesSetting => commands[CommandCodes.ToggleDotFilesSetting];
		public IRichCommand ToggleShowFileExtensions => commands[CommandCodes.ToggleShowFileExtensions];
		public IRichCommand TogglePreviewPane => commands[CommandCodes.TogglePreviewPane];
		public IRichCommand ToggleDetailsPane => commands[CommandCodes.ToggleDetailsPane];
		public IRichCommand ToggleInfoPane => commands[CommandCodes.ToggleInfoPane];
		public IRichCommand ToggleToolbar => commands[CommandCodes.ToggleToolbar];
		public IRichCommand ToggleShelfPane => commands[CommandCodes.ToggleShelfPane];
		public IRichCommand SelectAll => commands[CommandCodes.SelectAll];
		public IRichCommand InvertSelection => commands[CommandCodes.InvertSelection];
		public IRichCommand ClearSelection => commands[CommandCodes.ClearSelection];
		public IRichCommand ToggleSelect => commands[CommandCodes.ToggleSelect];
		public IRichCommand ShareItem => commands[CommandCodes.ShareItem];
		public IRichCommand EmptyRecycleBin => commands[CommandCodes.EmptyRecycleBin];
		public IRichCommand RestoreRecycleBin => commands[CommandCodes.RestoreRecycleBin];
		public IRichCommand RestoreAllRecycleBin => commands[CommandCodes.RestoreAllRecycleBin];
		public IRichCommand RefreshItems => commands[CommandCodes.RefreshItems];
		public IRichCommand Rename => commands[CommandCodes.Rename];
		public IRichCommand CreateAlternateDataStream => commands[CommandCodes.CreateAlternateDataStream];
		public IRichCommand CreateShortcut => commands[CommandCodes.CreateShortcut];
		public IRichCommand CreateShortcutFromDialog => commands[CommandCodes.CreateShortcutFromDialog];
		public IRichCommand CreateFolder => commands[CommandCodes.CreateFolder];
		public IRichCommand CreateFolderWithSelection => commands[CommandCodes.CreateFolderWithSelection];
		public IRichCommand AddItem => commands[CommandCodes.AddItem];
		public IRichCommand PinToStart => commands[CommandCodes.PinToStart];
		public IRichCommand UnpinFromStart => commands[CommandCodes.UnpinFromStart];
		public IRichCommand PinFolderToSidebar => commands[CommandCodes.PinFolderToSidebar];
		public IRichCommand UnpinFolderFromSidebar => commands[CommandCodes.UnpinFolderFromSidebar];
		public IRichCommand SetAsWallpaperBackground => commands[CommandCodes.SetAsWallpaperBackground];
		public IRichCommand SetAsSlideshowBackground => commands[CommandCodes.SetAsSlideshowBackground];
		public IRichCommand SetAsLockscreenBackground => commands[CommandCodes.SetAsLockscreenBackground];
		public IRichCommand SetAsAppBackground => commands[CommandCodes.SetAsAppBackground];
		public IRichCommand CopyItem => commands[CommandCodes.CopyItem];
		public IRichCommand CopyItemPath => commands[CommandCodes.CopyItemPath];
		public IRichCommand CopyPath => commands[CommandCodes.CopyPath];
		public IRichCommand CopyItemPathWithQuotes => commands[CommandCodes.CopyItemPathWithQuotes];
		public IRichCommand CopyPathWithQuotes => commands[CommandCodes.CopyPathWithQuotes];
		public IRichCommand CutItem => commands[CommandCodes.CutItem];
		public IRichCommand PasteItem => commands[CommandCodes.PasteItem];
		public IRichCommand PasteItemAsShortcut => commands[CommandCodes.PasteItemAsShortcut];
		public IRichCommand PasteItemToSelection => commands[CommandCodes.PasteItemToSelection];
		public IRichCommand DeleteItem => commands[CommandCodes.DeleteItem];
		public IRichCommand DeleteItemPermanently => commands[CommandCodes.DeleteItemPermanently];
		public IRichCommand InstallFont => commands[CommandCodes.InstallFont];
		public IRichCommand InstallInfDriver => commands[CommandCodes.InstallInfDriver];
		public IRichCommand InstallCertificate => commands[CommandCodes.InstallCertificate];
		public IRichCommand RunAsAdmin => commands[CommandCodes.RunAsAdmin];
		public IRichCommand RunAsAnotherUser => commands[CommandCodes.RunAsAnotherUser];
		public IRichCommand RunWithPowershell => commands[CommandCodes.RunWithPowershell];
		public IRichCommand LaunchPreviewPopup => commands[CommandCodes.LaunchPreviewPopup];
		public IRichCommand CompressIntoArchive => commands[CommandCodes.CompressIntoArchive];
		public IRichCommand CompressIntoSevenZip => commands[CommandCodes.CompressIntoSevenZip];
		public IRichCommand CompressIntoZip => commands[CommandCodes.CompressIntoZip];
		public IRichCommand DecompressArchive => commands[CommandCodes.DecompressArchive];
		public IRichCommand DecompressArchiveHere => commands[CommandCodes.DecompressArchiveHere];
		public IRichCommand DecompressArchiveHereSmart => commands[CommandCodes.DecompressArchiveHereSmart];
		public IRichCommand DecompressArchiveToChildFolder => commands[CommandCodes.DecompressArchiveToChildFolder];
		public IRichCommand FlattenFolder => commands[CommandCodes.FlattenFolder];
		public IRichCommand RotateLeft => commands[CommandCodes.RotateLeft];
		public IRichCommand RotateRight => commands[CommandCodes.RotateRight];
		public IRichCommand OpenItem => commands[CommandCodes.OpenItem];
		public IRichCommand OpenItemWithApplicationPicker => commands[CommandCodes.OpenItemWithApplicationPicker];
		public IRichCommand OpenParentFolder => commands[CommandCodes.OpenParentFolder];
		public IRichCommand OpenInVSCode => commands[CommandCodes.OpenInVSCode];
		public IRichCommand OpenRepoInVSCode => commands[CommandCodes.OpenRepoInVSCode];
		public IRichCommand OpenProperties => commands[CommandCodes.OpenProperties];
		public IRichCommand OpenReleaseNotes => commands[CommandCodes.OpenReleaseNotes];
		public IRichCommand OpenClassicProperties => commands[CommandCodes.OpenClassicProperties];
		public IRichCommand OpenStorageSense => commands[CommandCodes.OpenStorageSense];
		public IRichCommand OpenStorageSenseFromHome => commands[CommandCodes.OpenStorageSenseFromHome];
		public IRichCommand OpenStorageSenseFromSidebar => commands[CommandCodes.OpenStorageSenseFromSidebar];
		public IRichCommand OpenSettings => commands[CommandCodes.OpenSettings];
		public IRichCommand OpenTerminal => commands[CommandCodes.OpenTerminal];
		public IRichCommand OpenTerminalAsAdmin => commands[CommandCodes.OpenTerminalAsAdmin];
		public IRichCommand OpenTerminalFromSidebar => commands[CommandCodes.OpenTerminalFromSidebar];
		public IRichCommand OpenTerminalFromHome => commands[CommandCodes.OpenTerminalFromHome];
		public IRichCommand OpenCommandPalette => commands[CommandCodes.OpenCommandPalette];
		public IRichCommand EditInNotepad => commands[CommandCodes.EditInNotepad];
		public IRichCommand LayoutDecreaseSize => commands[CommandCodes.LayoutDecreaseSize];
		public IRichCommand LayoutIncreaseSize => commands[CommandCodes.LayoutIncreaseSize];
		public IRichCommand LayoutDetails => commands[CommandCodes.LayoutDetails];
		public IRichCommand LayoutList => commands[CommandCodes.LayoutList];
		public IRichCommand LayoutCards=> commands[CommandCodes.LayoutCards];
		public IRichCommand LayoutGrid => commands[CommandCodes.LayoutGrid];
		public IRichCommand LayoutColumns => commands[CommandCodes.LayoutColumns];
		public IRichCommand LayoutAdaptive => commands[CommandCodes.LayoutAdaptive];
		public IRichCommand SortByName => commands[CommandCodes.SortByName];
		public IRichCommand SortByDateModified => commands[CommandCodes.SortByDateModified];
		public IRichCommand SortByDateCreated => commands[CommandCodes.SortByDateCreated];
		public IRichCommand SortBySize => commands[CommandCodes.SortBySize];
		public IRichCommand SortByType => commands[CommandCodes.SortByType];
		public IRichCommand SortBySyncStatus => commands[CommandCodes.SortBySyncStatus];
		public IRichCommand SortByTag => commands[CommandCodes.SortByTag];
		public IRichCommand SortByPath => commands[CommandCodes.SortByPath];
		public IRichCommand SortByOriginalFolder => commands[CommandCodes.SortByOriginalFolder];
		public IRichCommand SortByDateDeleted => commands[CommandCodes.SortByDateDeleted];
		public IRichCommand SortAscending => commands[CommandCodes.SortAscending];
		public IRichCommand SortDescending => commands[CommandCodes.SortDescending];
		public IRichCommand ToggleSortDirection => commands[CommandCodes.ToggleSortDirection];
		public IRichCommand SortFoldersFirst => commands[CommandCodes.SortFoldersFirst];
		public IRichCommand SortFilesFirst => commands[CommandCodes.SortFilesFirst];
		public IRichCommand SortFilesAndFoldersTogether => commands[CommandCodes.SortFilesAndFoldersTogether];
		public IRichCommand GroupByNone => commands[CommandCodes.GroupByNone];
		public IRichCommand GroupByName => commands[CommandCodes.GroupByName];
		public IRichCommand GroupByDateModified => commands[CommandCodes.GroupByDateModified];
		public IRichCommand GroupByDateCreated => commands[CommandCodes.GroupByDateCreated];
		public IRichCommand GroupBySize => commands[CommandCodes.GroupBySize];
		public IRichCommand GroupByType => commands[CommandCodes.GroupByType];
		public IRichCommand GroupBySyncStatus => commands[CommandCodes.GroupBySyncStatus];
		public IRichCommand GroupByTag => commands[CommandCodes.GroupByTag];
		public IRichCommand GroupByOriginalFolder => commands[CommandCodes.GroupByOriginalFolder];
		public IRichCommand GroupByDateDeleted => commands[CommandCodes.GroupByDateDeleted];
		public IRichCommand GroupByFolderPath => commands[CommandCodes.GroupByFolderPath];
		public IRichCommand GroupByDateModifiedYear => commands[CommandCodes.GroupByDateModifiedYear];
		public IRichCommand GroupByDateModifiedMonth => commands[CommandCodes.GroupByDateModifiedMonth];
		public IRichCommand GroupByDateModifiedDay => commands[CommandCodes.GroupByDateModifiedDay];
		public IRichCommand GroupByDateCreatedYear => commands[CommandCodes.GroupByDateCreatedYear];
		public IRichCommand GroupByDateCreatedMonth => commands[CommandCodes.GroupByDateCreatedMonth];
		public IRichCommand GroupByDateCreatedDay => commands[CommandCodes.GroupByDateCreatedDay];
		public IRichCommand GroupByDateDeletedYear => commands[CommandCodes.GroupByDateDeletedYear];
		public IRichCommand GroupByDateDeletedMonth => commands[CommandCodes.GroupByDateDeletedMonth];
		public IRichCommand GroupByDateDeletedDay => commands[CommandCodes.GroupByDateDeletedDay];
		public IRichCommand GroupAscending => commands[CommandCodes.GroupAscending];
		public IRichCommand GroupDescending => commands[CommandCodes.GroupDescending];
		public IRichCommand ToggleGroupDirection => commands[CommandCodes.ToggleGroupDirection];
		public IRichCommand GroupByYear => commands[CommandCodes.GroupByYear];
		public IRichCommand GroupByMonth => commands[CommandCodes.GroupByMonth];
		public IRichCommand ToggleGroupByDateUnit => commands[CommandCodes.ToggleGroupByDateUnit];
		public IRichCommand NewWindow => commands[CommandCodes.NewWindow];
		public IRichCommand NewTab => commands[CommandCodes.NewTab];
		public IRichCommand FormatDrive => commands[CommandCodes.FormatDrive];
		public IRichCommand FormatDriveFromHome => commands[CommandCodes.FormatDriveFromHome];
		public IRichCommand FormatDriveFromSidebar => commands[CommandCodes.FormatDriveFromSidebar];
		public IRichCommand NavigateBack => commands[CommandCodes.NavigateBack];
		public IRichCommand NavigateForward => commands[CommandCodes.NavigateForward];
		public IRichCommand NavigateUp => commands[CommandCodes.NavigateUp];
		public IRichCommand NavigateHome => commands[CommandCodes.NavigateHome];
		public IRichCommand DuplicateCurrentTab => commands[CommandCodes.DuplicateCurrentTab];
		public IRichCommand DuplicateSelectedTab => commands[CommandCodes.DuplicateSelectedTab];
		public IRichCommand CloseTabsToTheLeftCurrent => commands[CommandCodes.CloseTabsToTheLeftCurrent];
		public IRichCommand CloseTabsToTheLeftSelected => commands[CommandCodes.CloseTabsToTheLeftSelected];
		public IRichCommand CloseTabsToTheRightCurrent => commands[CommandCodes.CloseTabsToTheRightCurrent];
		public IRichCommand CloseTabsToTheRightSelected => commands[CommandCodes.CloseTabsToTheRightSelected];
		public IRichCommand CloseOtherTabsCurrent => commands[CommandCodes.CloseOtherTabsCurrent];
		public IRichCommand CloseOtherTabsSelected => commands[CommandCodes.CloseOtherTabsSelected];
		public IRichCommand CloseAllTabs => commands[CommandCodes.CloseAllTabs];
		public IRichCommand OpenInNewPaneAction => commands[CommandCodes.OpenInNewPane];
		public IRichCommand OpenInNewPaneFromHomeAction => commands[CommandCodes.OpenInNewPaneFromHome];
		public IRichCommand OpenInNewPaneFromSidebarAction => commands[CommandCodes.OpenInNewPaneFromSidebar];
		public IRichCommand OpenInNewTabAction => commands[CommandCodes.OpenInNewTab];
		public IRichCommand OpenInNewTabFromHomeAction => commands[CommandCodes.OpenInNewTabFromHome];
		public IRichCommand OpenInNewTabFromSidebarAction => commands[CommandCodes.OpenInNewTabFromSidebar];
		public IRichCommand OpenInNewWindowAction => commands[CommandCodes.OpenInNewWindow];
		public IRichCommand OpenInNewWindowFromHomeAction => commands[CommandCodes.OpenInNewWindowFromHome];
		public IRichCommand OpenInNewWindowFromSidebarAction => commands[CommandCodes.OpenInNewWindowFromSidebar];
		public IRichCommand ReopenClosedTab => commands[CommandCodes.ReopenClosedTab];
		public IRichCommand PreviousTab => commands[CommandCodes.PreviousTab];
		public IRichCommand NextTab => commands[CommandCodes.NextTab];
		public IRichCommand CloseSelectedTab => commands[CommandCodes.CloseSelectedTab];
		public IRichCommand CloseActivePane => commands[CommandCodes.CloseActivePane];
		public IRichCommand FocusOtherPane => commands[CommandCodes.FocusOtherPane];
		public IRichCommand AddVerticalPane => commands[CommandCodes.AddVerticalPane];
		public IRichCommand AddHorizontalPane => commands[CommandCodes.AddHorizontalPane];
		public IRichCommand ArrangePanesVertically => commands[CommandCodes.ArrangePanesVertically];
		public IRichCommand ArrangePanesHorizontally => commands[CommandCodes.ArrangePanesHorizontally];
		public IRichCommand OpenFileLocation => commands[CommandCodes.OpenFileLocation];
		public IRichCommand PlayAll => commands[CommandCodes.PlayAll];
		public IRichCommand GitFetch => commands[CommandCodes.GitFetch];
		public IRichCommand GitInit => commands[CommandCodes.GitInit];
		public IRichCommand GitPull => commands[CommandCodes.GitPull];
		public IRichCommand GitPush => commands[CommandCodes.GitPush];
		public IRichCommand GitSync => commands[CommandCodes.GitSync];
		public IRichCommand OpenAllTaggedItems => commands[CommandCodes.OpenAllTaggedItems];
		#endregion

		public CommandManager()
		{
			commands = CreateActions()
				.Select(action => new ActionCommand(this, action.Key, action.Value))
				.Cast<IRichCommand>()
				.Append(new NoneCommand())
				.ToFrozenDictionary(command => command.Code);

			ActionsSettingsService.PropertyChanged += (s, e) => { OverwriteKeyBindings(); };

			OverwriteKeyBindings();
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public IEnumerator<IRichCommand> GetEnumerator() =>
			(commands.Values as IEnumerable<IRichCommand>).GetEnumerator();

		private static Dictionary<CommandCodes, IAction> CreateActions() => new()
		{
			[CommandCodes.OpenHelp] = new OpenHelpAction(),
			[CommandCodes.ToggleFullScreen] = new ToggleFullScreenAction(),
			[CommandCodes.EnterCompactOverlay] = new EnterCompactOverlayAction(),
			[CommandCodes.ExitCompactOverlay] = new ExitCompactOverlayAction(),
			[CommandCodes.ToggleCompactOverlay] = new ToggleCompactOverlayAction(),
			[CommandCodes.Search] = new SearchAction(),
			[CommandCodes.EditPath] = new EditPathAction(),
			[CommandCodes.Redo] = new RedoAction(),
			[CommandCodes.Undo] = new UndoAction(),
			[CommandCodes.ToggleShowHiddenItems] = new ToggleShowHiddenItemsAction(),
			[CommandCodes.ToggleDotFilesSetting] = new ToggleDotFilesSettingAction(),
			[CommandCodes.ToggleShowFileExtensions] = new ToggleShowFileExtensionsAction(),
			[CommandCodes.TogglePreviewPane] = new TogglePreviewPaneAction(),
			[CommandCodes.ToggleDetailsPane] = new ToggleDetailsPaneAction(),
			[CommandCodes.ToggleInfoPane] = new ToggleInfoPaneAction(),
			[CommandCodes.ToggleToolbar] = new ToggleToolbarAction(),
			[CommandCodes.ToggleShelfPane] = new ToggleShelfPaneAction(),
			[CommandCodes.SelectAll] = new SelectAllAction(),
			[CommandCodes.InvertSelection] = new InvertSelectionAction(),
			[CommandCodes.ClearSelection] = new ClearSelectionAction(),
			[CommandCodes.ToggleSelect] = new ToggleSelectAction(),
			[CommandCodes.ShareItem] = new ShareItemAction(),
			[CommandCodes.EmptyRecycleBin] = new EmptyRecycleBinAction(),
			[CommandCodes.RestoreRecycleBin] = new RestoreRecycleBinAction(),
			[CommandCodes.RestoreAllRecycleBin] = new RestoreAllRecycleBinAction(),
			[CommandCodes.RefreshItems] = new RefreshItemsAction(),
			[CommandCodes.Rename] = new RenameAction(),
			[CommandCodes.CreateAlternateDataStream] = new CreateAlternateDataStreamAction(),
			[CommandCodes.CreateShortcut] = new CreateShortcutAction(),
			[CommandCodes.CreateShortcutFromDialog] = new CreateShortcutFromDialogAction(),
			[CommandCodes.CreateFolder] = new CreateFolderAction(),
			[CommandCodes.CreateFolderWithSelection] = new CreateFolderWithSelectionAction(),
			[CommandCodes.AddItem] = new AddItemAction(),
			[CommandCodes.PinToStart] = new PinToStartAction(),
			[CommandCodes.UnpinFromStart] = new UnpinFromStartAction(),
			[CommandCodes.PinFolderToSidebar] = new PinFolderToSidebarAction(),
			[CommandCodes.UnpinFolderFromSidebar] = new UnpinFolderFromSidebarAction(),
			[CommandCodes.SetAsWallpaperBackground] = new SetAsWallpaperBackgroundAction(),
			[CommandCodes.SetAsSlideshowBackground] = new SetAsSlideshowBackgroundAction(),
			[CommandCodes.SetAsLockscreenBackground] = new SetAsLockscreenBackgroundAction(),
			[CommandCodes.SetAsAppBackground] = new SetAsAppBackgroundAction(),
			[CommandCodes.CopyItem] = new CopyItemAction(),
			[CommandCodes.CopyItemPath] = new CopyItemPathAction(),
			[CommandCodes.CopyPath] = new CopyPathAction(),
			[CommandCodes.CopyItemPathWithQuotes] = new CopyItemPathWithQuotesAction(),
			[CommandCodes.CopyPathWithQuotes] = new CopyPathWithQuotesAction(),
			[CommandCodes.CutItem] = new CutItemAction(),
			[CommandCodes.PasteItem] = new PasteItemAction(),
			[CommandCodes.PasteItemAsShortcut] = new PasteItemAsShortcutAction(),
			[CommandCodes.PasteItemToSelection] = new PasteItemToSelectionAction(),
			[CommandCodes.DeleteItem] = new DeleteItemAction(),
			[CommandCodes.DeleteItemPermanently] = new DeleteItemPermanentlyAction(),
			[CommandCodes.InstallFont] = new InstallFontAction(),
			[CommandCodes.InstallInfDriver] = new InstallInfDriverAction(),
			[CommandCodes.InstallCertificate] = new InstallCertificateAction(),
			[CommandCodes.RunAsAdmin] = new RunAsAdminAction(),
			[CommandCodes.RunAsAnotherUser] = new RunAsAnotherUserAction(),
			[CommandCodes.RunWithPowershell] = new RunWithPowershellAction(),
			[CommandCodes.LaunchPreviewPopup] = new LaunchPreviewPopupAction(),
			[CommandCodes.CompressIntoArchive] = new CompressIntoArchiveAction(),
			[CommandCodes.CompressIntoSevenZip] = new CompressIntoSevenZipAction(),
			[CommandCodes.CompressIntoZip] = new CompressIntoZipAction(),
			[CommandCodes.DecompressArchive] = new DecompressArchive(),
			[CommandCodes.DecompressArchiveHere] = new DecompressArchiveHere(),
			[CommandCodes.DecompressArchiveHereSmart] = new DecompressArchiveHereSmart(),
			[CommandCodes.DecompressArchiveToChildFolder] = new DecompressArchiveToChildFolderAction(),
			[CommandCodes.FlattenFolder] = new FlattenFolderAction(),
			[CommandCodes.RotateLeft] = new RotateLeftAction(),
			[CommandCodes.RotateRight] = new RotateRightAction(),
			[CommandCodes.OpenItem] = new OpenItemAction(),
			[CommandCodes.OpenItemWithApplicationPicker] = new OpenItemWithApplicationPickerAction(),
			[CommandCodes.OpenParentFolder] = new OpenParentFolderAction(),
			[CommandCodes.OpenInVSCode] = new OpenInVSCodeAction(),
			[CommandCodes.OpenRepoInVSCode] = new OpenRepoInVSCodeAction(),
			[CommandCodes.OpenProperties] = new OpenPropertiesAction(),
			[CommandCodes.OpenReleaseNotes] = new OpenReleaseNotesAction(),
			[CommandCodes.OpenClassicProperties] = new OpenClassicPropertiesAction(),
			[CommandCodes.OpenStorageSense] = new OpenStorageSenseAction(),
			[CommandCodes.OpenStorageSenseFromHome] = new OpenStorageSenseFromHomeAction(),
			[CommandCodes.OpenStorageSenseFromSidebar] = new OpenStorageSenseFromSidebarAction(),
			[CommandCodes.OpenSettings] = new OpenSettingsAction(),
			[CommandCodes.OpenTerminal] = new OpenTerminalAction(),
			[CommandCodes.OpenTerminalAsAdmin] = new OpenTerminalAsAdminAction(),
			[CommandCodes.OpenTerminalFromSidebar] = new OpenTerminalFromSidebarAction(),
			[CommandCodes.OpenTerminalFromHome] = new OpenTerminalFromHomeAction(),
			[CommandCodes.OpenCommandPalette] = new OpenCommandPaletteAction(),
			[CommandCodes.EditInNotepad] = new EditInNotepadAction(),
			[CommandCodes.LayoutDecreaseSize] = new LayoutDecreaseSizeAction(),
			[CommandCodes.LayoutIncreaseSize] = new LayoutIncreaseSizeAction(),
			[CommandCodes.LayoutDetails] = new LayoutDetailsAction(),
			[CommandCodes.LayoutList] = new LayoutListAction(),
			[CommandCodes.LayoutCards] = new LayoutCardsAction(),
			[CommandCodes.LayoutGrid] = new LayoutGridAction(),
			[CommandCodes.LayoutColumns] = new LayoutColumnsAction(),
			[CommandCodes.LayoutAdaptive] = new LayoutAdaptiveAction(),
			[CommandCodes.SortByName] = new SortByNameAction(),
			[CommandCodes.SortByDateModified] = new SortByDateModifiedAction(),
			[CommandCodes.SortByDateCreated] = new SortByDateCreatedAction(),
			[CommandCodes.SortBySize] = new SortBySizeAction(),
			[CommandCodes.SortByType] = new SortByTypeAction(),
			[CommandCodes.SortBySyncStatus] = new SortBySyncStatusAction(),
			[CommandCodes.SortByTag] = new SortByTagAction(),
			[CommandCodes.SortByPath] = new SortByPathAction(),
			[CommandCodes.SortByOriginalFolder] = new SortByOriginalFolderAction(),
			[CommandCodes.SortByDateDeleted] = new SortByDateDeletedAction(),
			[CommandCodes.SortAscending] = new SortAscendingAction(),
			[CommandCodes.SortDescending] = new SortDescendingAction(),
			[CommandCodes.ToggleSortDirection] = new ToggleSortDirectionAction(),
			[CommandCodes.SortFoldersFirst] = new SortFoldersFirstAction(),
			[CommandCodes.SortFilesFirst] = new SortFilesFirstAction(),
			[CommandCodes.SortFilesAndFoldersTogether] = new SortFilesAndFoldersTogetherAction(),
			[CommandCodes.GroupByNone] = new GroupByNoneAction(),
			[CommandCodes.GroupByName] = new GroupByNameAction(),
			[CommandCodes.GroupByDateModified] = new GroupByDateModifiedAction(),
			[CommandCodes.GroupByDateCreated] = new GroupByDateCreatedAction(),
			[CommandCodes.GroupBySize] = new GroupBySizeAction(),
			[CommandCodes.GroupByType] = new GroupByTypeAction(),
			[CommandCodes.GroupBySyncStatus] = new GroupBySyncStatusAction(),
			[CommandCodes.GroupByTag] = new GroupByTagAction(),
			[CommandCodes.GroupByOriginalFolder] = new GroupByOriginalFolderAction(),
			[CommandCodes.GroupByDateDeleted] = new GroupByDateDeletedAction(),
			[CommandCodes.GroupByFolderPath] = new GroupByFolderPathAction(),
			[CommandCodes.GroupByDateModifiedYear] = new GroupByDateModifiedYearAction(),
			[CommandCodes.GroupByDateModifiedMonth] = new GroupByDateModifiedMonthAction(),
			[CommandCodes.GroupByDateModifiedDay] = new GroupByDateModifiedDayAction(),
			[CommandCodes.GroupByDateCreatedYear] = new GroupByDateCreatedYearAction(),
			[CommandCodes.GroupByDateCreatedMonth] = new GroupByDateCreatedMonthAction(),
			[CommandCodes.GroupByDateCreatedDay] = new GroupByDateCreatedDayAction(),
			[CommandCodes.GroupByDateDeletedYear] = new GroupByDateDeletedYearAction(),
			[CommandCodes.GroupByDateDeletedMonth] = new GroupByDateDeletedMonthAction(),
			[CommandCodes.GroupByDateDeletedDay] = new GroupByDateDeletedDayAction(),
			[CommandCodes.GroupAscending] = new GroupAscendingAction(),
			[CommandCodes.GroupDescending] = new GroupDescendingAction(),
			[CommandCodes.ToggleGroupDirection] = new ToggleGroupDirectionAction(),
			[CommandCodes.GroupByYear] = new GroupByYearAction(),
			[CommandCodes.GroupByMonth] = new GroupByMonthAction(),
			[CommandCodes.ToggleGroupByDateUnit] = new ToggleGroupByDateUnitAction(),
			[CommandCodes.NewWindow] = new NewWindowAction(),
			[CommandCodes.NewTab] = new NewTabAction(),
			[CommandCodes.FormatDrive] = new FormatDriveAction(),
			[CommandCodes.FormatDriveFromHome] = new FormatDriveFromHomeAction(),
			[CommandCodes.FormatDriveFromSidebar] = new FormatDriveFromSidebarAction(),
			[CommandCodes.NavigateBack] = new NavigateBackAction(),
			[CommandCodes.NavigateForward] = new NavigateForwardAction(),
			[CommandCodes.NavigateUp] = new NavigateUpAction(),
			[CommandCodes.NavigateHome] = new NavigateHomeAction(),
			[CommandCodes.DuplicateCurrentTab] = new DuplicateCurrentTabAction(),
			[CommandCodes.DuplicateSelectedTab] = new DuplicateSelectedTabAction(),
			[CommandCodes.CloseTabsToTheLeftCurrent] = new CloseTabsToTheLeftCurrentAction(),
			[CommandCodes.CloseTabsToTheLeftSelected] = new CloseTabsToTheLeftSelectedAction(),
			[CommandCodes.CloseTabsToTheRightCurrent] = new CloseTabsToTheRightCurrentAction(),
			[CommandCodes.CloseTabsToTheRightSelected] = new CloseTabsToTheRightSelectedAction(),
			[CommandCodes.CloseOtherTabsCurrent] = new CloseOtherTabsCurrentAction(),
			[CommandCodes.CloseOtherTabsSelected] = new CloseOtherTabsSelectedAction(),
			[CommandCodes.CloseAllTabs] = new CloseAllTabsAction(),
			[CommandCodes.OpenInNewPane] = new OpenInNewPaneAction(),
			[CommandCodes.OpenInNewPaneFromHome] = new OpenInNewPaneFromHomeAction(),
			[CommandCodes.OpenInNewPaneFromSidebar] = new OpenInNewPaneFromSidebarAction(),
			[CommandCodes.OpenInNewTab] = new OpenInNewTabAction(),
			[CommandCodes.OpenInNewTabFromHome] = new OpenInNewTabFromHomeAction(),
			[CommandCodes.OpenInNewTabFromSidebar] = new OpenInNewTabFromSidebarAction(),
			[CommandCodes.OpenInNewWindow] = new OpenInNewWindowAction(),
			[CommandCodes.OpenInNewWindowFromHome] = new OpenInNewWindowFromHomeAction(),
			[CommandCodes.OpenInNewWindowFromSidebar] = new OpenInNewWindowFromSidebarAction(),
			[CommandCodes.ReopenClosedTab] = new ReopenClosedTabAction(),
			[CommandCodes.PreviousTab] = new PreviousTabAction(),
			[CommandCodes.NextTab] = new NextTabAction(),
			[CommandCodes.CloseSelectedTab] = new CloseSelectedTabAction(),
			[CommandCodes.CloseActivePane] = new CloseActivePaneAction(),
			[CommandCodes.FocusOtherPane] = new FocusOtherPaneAction(),
			[CommandCodes.AddVerticalPane] = new AddVerticalPaneAction(),
			[CommandCodes.AddHorizontalPane] = new AddHorizontalPaneAction(),
			[CommandCodes.ArrangePanesVertically] = new ArrangePanesVerticallyAction(),
			[CommandCodes.ArrangePanesHorizontally] = new ArrangePanesHorizontallyAction(),
			[CommandCodes.OpenFileLocation] = new OpenFileLocationAction(),
			[CommandCodes.PlayAll] = new PlayAllAction(),
			[CommandCodes.GitFetch] = new GitFetchAction(),
			[CommandCodes.GitInit] = new GitInitAction(),
			[CommandCodes.GitPull] = new GitPullAction(),
			[CommandCodes.GitPush] = new GitPushAction(),
			[CommandCodes.GitSync] = new GitSyncAction(),
			[CommandCodes.OpenAllTaggedItems] = new OpenAllTaggedActions(),
		};

		/// <summary>
		/// Replaces default key binding collection with customized one(s) if exists.
		/// </summary>
		private void OverwriteKeyBindings()
		{
			var allCommands = commands.Values.OfType<ActionCommand>();

			if (ActionsSettingsService.ActionsV2 is null)
			{
				allCommands.ForEach(x => x.RestoreKeyBindings());
			}
			else
			{
				foreach (var command in allCommands)
				{
					var customizedKeyBindings = ActionsSettingsService.ActionsV2.FindAll(x => x.CommandCode == command.Code.ToString());

					if (customizedKeyBindings.IsEmpty())
					{
						// Could not find customized key bindings for the command
						command.RestoreKeyBindings();
					}
					else if (customizedKeyBindings.Count == 1 && customizedKeyBindings[0].KeyBinding == string.Empty)
					{
						// Do not assign any key binding even though there're default keys pre-defined
						command.OverwriteKeyBindings(HotKeyCollection.Empty);
					}
					else
					{
						var keyBindings = new HotKeyCollection(customizedKeyBindings.Select(x => HotKey.Parse(x.KeyBinding, false)));
						command.OverwriteKeyBindings(keyBindings);
					}
				}
			}

			try
			{
				// Set collection of a set of command code and key bindings to dictionary
				_allKeyBindings = commands.Values
					.SelectMany(command => command.HotKeys, (command, hotKey) => (Command: command, HotKey: hotKey))
					.ToImmutableDictionary(item => item.HotKey, item => item.Command);
			}
			catch (ArgumentException ex)
			{
				// The keys are not necessarily all different because they can be set manually in text editor
				// ISSUE: https://github.com/files-community/Files/issues/15331

				var flat = commands.Values.SelectMany(x => x.HotKeys).Select(x => x.LocalizedLabel);
				var duplicates = flat.GroupBy(x => x).Where(x => x.Count() > 1).Select(group => group.Key);

				foreach (var item in duplicates)
				{
					if (!string.IsNullOrEmpty(item))
					{
						var occurrences = allCommands.Where(x => x.HotKeys.Select(x => x.LocalizedLabel).Contains(item));

						// Restore the defaults for all occurrences in our cache
						occurrences.ForEach(x => x.RestoreKeyBindings());

						// Get all customized key bindings from user settings json
						var actions =
							ActionsSettingsService.ActionsV2 is not null
								? new List<ActionWithParameterItem>(ActionsSettingsService.ActionsV2)
								: [];

						// Remove the duplicated key binding from user settings JSON file
						actions.RemoveAll(x => x.KeyBinding.Contains(item));

						// Reset
						ActionsSettingsService.ActionsV2 = actions;
					}
				}

				// Set collection of a set of command code and key bindings to dictionary
				_allKeyBindings = commands.Values
					.SelectMany(command => command.HotKeys, (command, hotKey) => (Command: command, HotKey: hotKey))
					.ToImmutableDictionary(item => item.HotKey, item => item.Command);

				App.Logger.LogInformation(ex, "The app found some keys in different commands are duplicated and are using default key bindings for those commands.");
			}
			catch (Exception ex)
			{
				allCommands.ForEach(x => x.RestoreKeyBindings());

				// Set collection of a set of command code and key bindings to dictionary
				_allKeyBindings = commands.Values
					.SelectMany(command => command.HotKeys, (command, hotKey) => (Command: command, HotKey: hotKey))
					.ToImmutableDictionary(item => item.HotKey, item => item.Command);

				App.Logger.LogWarning(ex, "The app is temporarily using default key bindings for all because of a serious error of assigning custom keys.");
			}
		}

		public static HotKeyCollection GetDefaultKeyBindings(IAction action)
		{
			return new(action.HotKey, action.SecondHotKey, action.ThirdHotKey, action.MediaHotKey);
		}
	}
}
