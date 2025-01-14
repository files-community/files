// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Helpers.ContextFlyouts;
using Files.App.UserControls.Sidebar;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.ViewModels.UserControls
{
	public sealed class SidebarViewModel : ObservableObject, IDisposable, ISidebarViewModel
	{
		private INetworkService NetworkService { get; } = Ioc.Default.GetRequiredService<INetworkService>();
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();
		private readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
		private readonly IFileTagsService fileTagsService;

		private IShellPanesPage paneHolder;
		public IShellPanesPage PaneHolder
		{
			get => paneHolder;
			set => SetProperty(ref paneHolder, value);
		}

		public MenuFlyout PaneFlyout;

		public IFilesystemHelpers FilesystemHelpers
			=> PaneHolder?.FilesystemHelpers;

		private Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;
		private INavigationControlItem rightClickedItem;

		public object SidebarItems => sidebarItems;
		public BulkConcurrentObservableCollection<INavigationControlItem> sidebarItems { get; init; }
		public PinnedFoldersManager SidebarPinnedModel => App.QuickAccessManager.Model;
		public IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		private SidebarDisplayMode sidebarDisplayMode;
		public SidebarDisplayMode SidebarDisplayMode
		{
			get => sidebarDisplayMode;
			set
			{
				// We only want to track non minimal mode
				if (value == SidebarDisplayMode.Minimal) return;
				if (SetProperty(ref sidebarDisplayMode, value))
				{
					OnPropertyChanged(nameof(IsSidebarCompactSize));
					IsSidebarOpen = sidebarDisplayMode == SidebarDisplayMode.Expanded;
					UpdateTabControlMargin();
				}
			}
		}

		public delegate void SelectedTagChangedEventHandler(object sender, SelectedTagChangedEventArgs e);

		public static event SelectedTagChangedEventHandler? SelectedTagChanged;
		public static event EventHandler<INavigationControlItem?>? RightClickedItemChanged;

		private readonly SectionType[] SectionOrder =
			[
				SectionType.Home,
				SectionType.Pinned,
				SectionType.Library,
				SectionType.Drives,
				SectionType.CloudDrives,
				SectionType.Network,
				SectionType.WSL,
				SectionType.FileTag
			];

		public bool IsSidebarCompactSize
			=> SidebarDisplayMode == SidebarDisplayMode.Compact || SidebarDisplayMode == SidebarDisplayMode.Minimal;

		public void NotifyInstanceRelatedPropertiesChanged(string? arg)
		{
			UpdateSidebarSelectedItemFromArgs(arg);

			OnPropertyChanged(nameof(SidebarSelectedItem));
		}

		public void UpdateSidebarSelectedItemFromArgs(string? arg)
		{
			var value = arg;

			INavigationControlItem? item = null;
			var filteredItems = sidebarItems
				.Where(x => !string.IsNullOrWhiteSpace(x.Path))
				.Concat(sidebarItems.Where(x => (x as LocationItem)?.ChildItems is not null).SelectMany(x => ((LocationItem)x).ChildItems).Where(x => !string.IsNullOrWhiteSpace(x.Path)))
				.ToList();

			if (string.IsNullOrEmpty(value))
			{
				//SidebarSelectedItem = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home"));
				return;
			}

			item = filteredItems.FirstOrDefault(x => x.Path.Equals(value, StringComparison.OrdinalIgnoreCase));
			item ??= filteredItems.Where(x => value.StartsWith(x.Path + "\\", StringComparison.OrdinalIgnoreCase)).MaxBy(x => x.Path.Length);
			item ??= filteredItems.FirstOrDefault(x => x.Path.Equals(Path.GetPathRoot(value), StringComparison.OrdinalIgnoreCase));

			if (item is null && value == "Home")
				item = filteredItems.FirstOrDefault(x => x.Path.Equals("Home"));

			if (SidebarSelectedItem != item)
				SidebarSelectedItem = item;

		}

		public bool IsSidebarOpen
		{
			get => UserSettingsService.AppearanceSettingsService.IsSidebarOpen;
			set
			{
				if (value == UserSettingsService.AppearanceSettingsService.IsSidebarOpen)
					return;

				UserSettingsService.AppearanceSettingsService.IsSidebarOpen = value;

				OnPropertyChanged();
			}
		}

		public bool ShowPinnedFoldersSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowPinnedSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowPinnedSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowPinnedSection = value;
			}
		}

		public bool ShowLibrarySection
		{
			get => UserSettingsService.GeneralSettingsService.ShowLibrarySection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowLibrarySection)
					return;

				UserSettingsService.GeneralSettingsService.ShowLibrarySection = value;
			}
		}

		public bool ShowDrivesSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowDrivesSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowDrivesSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowDrivesSection = value;
			}
		}

		public bool ShowCloudDrivesSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection = value;
			}
		}

		public bool ShowNetworkSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowNetworkSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowNetworkSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowNetworkSection = value;
			}
		}

		public bool ShowWslSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowWslSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowWslSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowWslSection = value;
			}
		}

		public bool ShowFileTagsSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowFileTagsSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowFileTagsSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowFileTagsSection = value;
			}
		}

		private INavigationControlItem selectedSidebarItem;

		public INavigationControlItem SidebarSelectedItem
		{
			get => selectedSidebarItem;
			set => SetProperty(ref selectedSidebarItem, value);
		}

		public SidebarViewModel()
		{
			dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
			fileTagsService = Ioc.Default.GetRequiredService<IFileTagsService>();

			sidebarItems = [];
			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
			CreateItemHomeAsync();

			Manager_DataChanged(SectionType.Pinned, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.CloudDrives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			App.QuickAccessManager.Model.DataChanged += Manager_DataChanged;
			App.LibraryManager.DataChanged += Manager_DataChanged;
			drivesViewModel.Drives.CollectionChanged += Manager_DataChangedForDrives;
			CloudDrivesManager.DataChanged += Manager_DataChanged;
			NetworkService.Computers.CollectionChanged += Manager_DataChangedForNetworkComputers;
			WSLDistroManager.DataChanged += Manager_DataChanged;
			App.FileTagsManager.DataChanged += Manager_DataChanged;
			SidebarDisplayMode = UserSettingsService.AppearanceSettingsService.IsSidebarOpen ? SidebarDisplayMode.Expanded : SidebarDisplayMode.Compact;

			HideSectionCommand = new RelayCommand(HideSection);
			UnpinItemCommand = new RelayCommand(UnpinItem);
			PinItemCommand = new RelayCommand(PinItem);
			EjectDeviceCommand = new RelayCommand(EjectDevice);
			OpenPropertiesCommand = new RelayCommand<CommandBarFlyout>(OpenProperties);
			ReorderItemsCommand = new AsyncRelayCommand(ReorderItemsAsync);
		}

		private Task<LocationItem> CreateItemHomeAsync()
		{
			return CreateSectionAsync(SectionType.Home);
		}

		private async void Manager_DataChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				var sectionType = (SectionType)sender;
				var section = await GetOrCreateSectionAsync(sectionType);
				Func<IReadOnlyList<INavigationControlItem>> getElements = () => sectionType switch
				{
					SectionType.Pinned => App.QuickAccessManager.Model.PinnedFolderItems,
					SectionType.CloudDrives => CloudDrivesManager.Drives,
					SectionType.Drives => drivesViewModel.Drives.Cast<DriveItem>().ToList().AsReadOnly(),
					SectionType.Network => NetworkService.Computers.Cast<DriveItem>().ToList().AsReadOnly(),
					SectionType.WSL => WSLDistroManager.Distros,
					SectionType.Library => App.LibraryManager.Libraries,
					SectionType.FileTag => App.FileTagsManager.FileTags,
					_ => null
				};
				await SyncSidebarItemsAsync(section, getElements, e);
			});
		}

		private void Manager_DataChangedForDrives(object? sender, NotifyCollectionChangedEventArgs e) => Manager_DataChanged(SectionType.Drives, e);

		private void Manager_DataChangedForNetworkComputers(object? sender, NotifyCollectionChangedEventArgs e) => Manager_DataChanged(SectionType.Network, e);

		private async Task SyncSidebarItemsAsync(LocationItem section, Func<IReadOnlyList<INavigationControlItem>> getElements, NotifyCollectionChangedEventArgs e)
		{
			if (section is null)
			{
				return;
			}

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					{
						for (int i = 0; i < e.NewItems.Count; i++)
						{
							var index = e.NewStartingIndex < 0 ? -1 : i + e.NewStartingIndex;
							await AddElementToSectionAsync((INavigationControlItem)e.NewItems[i], section, index);
						}

						break;
					}

				case NotifyCollectionChangedAction.Move:
				case NotifyCollectionChangedAction.Remove:
				case NotifyCollectionChangedAction.Replace:
					{
						foreach (INavigationControlItem elem in e.OldItems)
						{
							var match = section.ChildItems.FirstOrDefault(x => x.Path == elem.Path);
							section.ChildItems.Remove(match);
						}
						if (e.Action != NotifyCollectionChangedAction.Remove)
						{
							goto case NotifyCollectionChangedAction.Add;
						}

						break;
					}

				case NotifyCollectionChangedAction.Reset:
					{
						foreach (INavigationControlItem elem in getElements())
						{
							await AddElementToSectionAsync(elem, section);
						}
						foreach (INavigationControlItem elem in section.ChildItems.ToList())
						{
							if (!getElements().Any(x => x.Path == elem.Path))
							{
								section.ChildItems.Remove(elem);
							}
						}

						break;
					}
			}
		}

		private bool IsLibraryOnSidebar(LibraryLocationItem item)
			=> item is not null && !item.IsEmpty && item.IsDefaultLocation;

		private async Task AddElementToSectionAsync(INavigationControlItem elem, LocationItem section, int index = -1)
		{
			if (elem is LibraryLocationItem lib)
			{
				if (IsLibraryOnSidebar(lib) &&
					await lib.CheckDefaultSaveFolderAccess() &&
					!section.ChildItems.Any(x => x.Path == lib.Path))
				{
					section.ChildItems.AddSorted(elem);
					await lib.LoadLibraryIconAsync();
				}
			}
			else if (elem is DriveItem drive)
			{
				if (section.Section is SectionType.Network or SectionType.CloudDrives)
				{
					// Already sorted
					if (!section.ChildItems.Any(x => x.Path == drive.Path))
					{
						section.ChildItems.Insert(index < 0 ? section.ChildItems.Count : Math.Min(index, section.ChildItems.Count), drive);
						await drive.LoadThumbnailAsync();
					}
				}
				else
				{
					string drivePath = drive.Path;
					var paths = section.ChildItems.Select(item => item.Path).ToList();

					if (!paths.Contains(drivePath))
					{
						paths.AddSorted(drivePath);
						int position = paths.IndexOf(drivePath);

						section.ChildItems.Insert(position, drive);
						await drive.LoadThumbnailAsync();
					}
				}
			}
			else
			{
				if (!section.ChildItems.Any(x => x.Path == elem.Path))
				{
					section.ChildItems.Insert(index < 0 ? section.ChildItems.Count : Math.Min(index, section.ChildItems.Count), elem);
				}
			}

			section.PropertyChanged += Section_PropertyChanged;
		}

		private void Section_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is LocationItem section && e.PropertyName == nameof(section.IsExpanded))
			{
				switch (section.Text)
				{
					case var text when text == "Pinned".GetLocalizedResource():
						UserSettingsService.GeneralSettingsService.IsPinnedSectionExpanded = section.IsExpanded;
						break;
					case var text when text == "SidebarLibraries".GetLocalizedResource():
						UserSettingsService.GeneralSettingsService.IsLibrarySectionExpanded = section.IsExpanded;
						break;
					case var text when text == "Drives".GetLocalizedResource():
						UserSettingsService.GeneralSettingsService.IsDriveSectionExpanded = section.IsExpanded;
						break;
					case var text when text == "SidebarCloudDrives".GetLocalizedResource():
						UserSettingsService.GeneralSettingsService.IsCloudDriveSectionExpanded = section.IsExpanded;
						break;
					case var text when text == "Network".GetLocalizedResource():
						UserSettingsService.GeneralSettingsService.IsNetworkSectionExpanded = section.IsExpanded;
						break;
					case var text when text == "WSL".GetLocalizedResource():
						UserSettingsService.GeneralSettingsService.IsWslSectionExpanded = section.IsExpanded;
						break;
					case var text when text == "FileTags".GetLocalizedResource():
						UserSettingsService.GeneralSettingsService.IsFileTagsSectionExpanded = section.IsExpanded;
						break;
				}
			}
		}

		private async Task<LocationItem> GetOrCreateSectionAsync(SectionType sectionType)
		{
			LocationItem? section = GetSection(sectionType) ?? await CreateSectionAsync(sectionType);
			return section;
		}

		private LocationItem? GetSection(SectionType sectionType)
		{
			return sidebarItems.FirstOrDefault(x => x.Section == sectionType) as LocationItem;
		}

		private async Task<LocationItem> CreateSectionAsync(SectionType sectionType)
		{
			LocationItem section = null;
			BitmapImage icon = null;
			int iconIdex = -1;

			switch (sectionType)
			{
				case SectionType.Home:
					{
						section = BuildSection("Home".GetLocalizedResource(), sectionType, new ContextMenuOptions { IsLocationItem = true }, true);
						section.Path = "Home";
						section.Icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.HomeIcon));
						section.IsHeader = true;

						break;
					}

				case SectionType.Pinned:
					{
						if (ShowPinnedFoldersSection == false)
						{
							break;
						}

						section = BuildSection("Pinned".GetLocalizedResource(), sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.StarIcon));
						section.IsHeader = true;
						section.IsExpanded = UserSettingsService.GeneralSettingsService.IsPinnedSectionExpanded;

						break;
					}

				case SectionType.Library:
					{
						if (ShowLibrarySection == false)
						{
							break;
						}
						section = BuildSection("SidebarLibraries".GetLocalizedResource(), sectionType, new ContextMenuOptions { IsLibrariesHeader = true, ShowHideSection = true }, false);
						iconIdex = Constants.ImageRes.Libraries;
						section.IsHeader = true;
						section.IsExpanded = UserSettingsService.GeneralSettingsService.IsLibrarySectionExpanded;

						break;
					}

				case SectionType.Drives:
					{
						if (ShowDrivesSection == false)
						{
							break;
						}
						section = BuildSection("Drives".GetLocalizedResource(), sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
						iconIdex = Constants.ImageRes.ThisPC;
						section.IsHeader = true;
						section.IsExpanded = UserSettingsService.GeneralSettingsService.IsDriveSectionExpanded;

						break;
					}

				case SectionType.CloudDrives:
					{
						if (ShowCloudDrivesSection == false || CloudDrivesManager.Drives.Any() == false)
						{
							break;
						}
						section = BuildSection("SidebarCloudDrives".GetLocalizedResource(), sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.CloudDriveIcon));
						section.IsHeader = true;
						section.IsExpanded = UserSettingsService.GeneralSettingsService.IsCloudDriveSectionExpanded;

						break;
					}

				case SectionType.Network:
					{
						if (!ShowNetworkSection)
						{
							break;
						}
						section = BuildSection("Network".GetLocalizedResource(), sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
						iconIdex = Constants.ImageRes.Network;
						section.IsHeader = true;
						section.IsExpanded = UserSettingsService.GeneralSettingsService.IsNetworkSectionExpanded;

						break;
					}

				case SectionType.WSL:
					{
						if (ShowWslSection == false || WSLDistroManager.Distros.Any() == false)
						{
							break;
						}
						section = BuildSection("WSL".GetLocalizedResource(), sectionType, new ContextMenuOptions { ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.WslIconsPaths.GenericIcon));
						section.IsHeader = true;
						section.IsExpanded = UserSettingsService.GeneralSettingsService.IsWslSectionExpanded;

						break;
					}

				case SectionType.FileTag:
					{
						if (!ShowFileTagsSection)
						{
							break;
						}
						section = BuildSection("FileTags".GetLocalizedResource(), sectionType, new ContextMenuOptions { IsTagsHeader = true, ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.FileTagsIcon));
						section.IsHeader = true;
						section.IsExpanded = UserSettingsService.GeneralSettingsService.IsFileTagsSectionExpanded;

						break;
					}
			}

			if (section is not null)
			{
				if (icon is not null)
				{
					section.Icon = icon;
				}

				AddSectionToSideBar(section);

				if (iconIdex != -1)
				{
					section.Icon = await UIHelpers.GetSidebarIconResource(iconIdex);
				}
			}

			return section;
		}

		private LocationItem BuildSection(string sectionName, SectionType sectionType, ContextMenuOptions options, bool selectsOnInvoked)
		{
			return new LocationItem()
			{
				Text = sectionName,
				Section = sectionType,
				MenuOptions = options,
				SelectsOnInvoked = selectsOnInvoked,
				ChildItems = []
			};
		}

		private void AddSectionToSideBar(LocationItem section)
		{
			var index = SectionOrder.TakeWhile(x => x != section.Section).Select(x => sidebarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
			sidebarItems.Insert(Math.Min(index, sidebarItems.Count), section);
		}

		public async Task UpdateSectionVisibilityAsync(SectionType sectionType, bool show)
		{
			if (show)
			{
				var generalSettingsService = UserSettingsService.GeneralSettingsService;

				Func<Task> action = sectionType switch
				{
					SectionType.CloudDrives when generalSettingsService.ShowCloudDrivesSection => CloudDrivesManager.UpdateDrivesAsync,
					SectionType.Drives => drivesViewModel.UpdateDrivesAsync,
					SectionType.Network when generalSettingsService.ShowNetworkSection => NetworkService.UpdateComputersAsync,
					SectionType.WSL when generalSettingsService.ShowWslSection => WSLDistroManager.UpdateDrivesAsync,
					SectionType.FileTag when generalSettingsService.ShowFileTagsSection => App.FileTagsManager.UpdateFileTagsAsync,
					SectionType.Library => App.LibraryManager.UpdateLibrariesAsync,
					SectionType.Pinned => App.QuickAccessManager.Model.AddAllItemsToSidebarAsync,
					_ => () => Task.CompletedTask
				};

				Manager_DataChanged(sectionType, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

				await action();
			}
			else
			{
				sidebarItems.Remove(sidebarItems.FirstOrDefault(x => x.Section == sectionType));
			}
		}

		private async void UserSettingsService_OnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			switch (e.SettingName)
			{
				case nameof(UserSettingsService.AppearanceSettingsService.IsSidebarOpen):
					if (UserSettingsService.AppearanceSettingsService.IsSidebarOpen != IsSidebarOpen)
					{
						OnPropertyChanged(nameof(IsSidebarOpen));
					}
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowPinnedSection):
					await UpdateSectionVisibilityAsync(SectionType.Pinned, ShowPinnedFoldersSection);
					OnPropertyChanged(nameof(ShowPinnedFoldersSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowLibrarySection):
					await UpdateSectionVisibilityAsync(SectionType.Library, ShowLibrarySection);
					OnPropertyChanged(nameof(ShowLibrarySection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection):
					await UpdateSectionVisibilityAsync(SectionType.CloudDrives, ShowCloudDrivesSection);
					OnPropertyChanged(nameof(ShowCloudDrivesSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowDrivesSection):
					await UpdateSectionVisibilityAsync(SectionType.Drives, ShowDrivesSection);
					OnPropertyChanged(nameof(ShowDrivesSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowNetworkSection):
					await UpdateSectionVisibilityAsync(SectionType.Network, ShowNetworkSection);
					OnPropertyChanged(nameof(ShowNetworkSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowWslSection):
					await UpdateSectionVisibilityAsync(SectionType.WSL, ShowWslSection);
					OnPropertyChanged(nameof(ShowWslSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowFileTagsSection):
					await UpdateSectionVisibilityAsync(SectionType.FileTag, ShowFileTagsSection);
					OnPropertyChanged(nameof(ShowFileTagsSection));
					break;
			}
		}

		public void Dispose()
		{
			UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;

			App.QuickAccessManager.Model.DataChanged -= Manager_DataChanged;
			App.LibraryManager.DataChanged -= Manager_DataChanged;
			drivesViewModel.Drives.CollectionChanged -= Manager_DataChangedForDrives;
			CloudDrivesManager.DataChanged -= Manager_DataChanged;
			NetworkService.Computers.CollectionChanged -= Manager_DataChangedForNetworkComputers;
			WSLDistroManager.DataChanged -= Manager_DataChanged;
			App.FileTagsManager.DataChanged -= Manager_DataChanged;
		}

		public void UpdateTabControlMargin()
		{
			TabControlMargin = SidebarDisplayMode switch
			{
				// This prevents the pane toggle button from overlapping the tab control in minimal mode
				SidebarDisplayMode.Minimal => new GridLength(44, GridUnitType.Pixel),
				_ => new GridLength(0, GridUnitType.Pixel),
			};
		}

		public async void HandleItemContextInvokedAsync(object sender, ItemContextInvokedArgs args)
		{
			if (sender is not FrameworkElement sidebarItem)
				return;

			if (args.Item is not INavigationControlItem item)
			{
				// We are in the pane context requested path
				PaneFlyout.ShowAt(sender as FrameworkElement, args.Position);

				return;
			}

			if (item is FileTagItem tagItem)
			{
				var cts = new CancellationTokenSource();
				var items = new List<(string path, bool isFolder)>();

				await foreach (var taggedItem in fileTagsService.GetItemsForTagAsync(tagItem.FileTag.Uid, cts.Token))
				{
					items.Add((
						taggedItem.Storable.TryGetPath() ?? string.Empty,
						taggedItem.Storable is IFolder));
				}

				SelectedTagChanged?.Invoke(this, new SelectedTagChangedEventArgs(items));
			}

			rightClickedItem = item;
			RightClickedItemChanged?.Invoke(this, item);

			var itemContextMenuFlyout = new CommandBarFlyout()
			{
				Placement = FlyoutPlacementMode.Full
			};

			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;

			var menuItems = GetLocationItemMenuItems(item, itemContextMenuFlyout);
			var (_, secondaryElements) = ContextFlyoutModelToElementHelper.GetAppBarItemsFromModel(menuItems);

			secondaryElements
				.OfType<FrameworkElement>()
				.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);

			if (item.MenuOptions.ShowShellItems)
				itemContextMenuFlyout.Opened += ItemContextMenuFlyout_Opened;

			itemContextMenuFlyout.ShowAt(sidebarItem, new() { Position = args.Position });
		}

		private async void ItemContextMenuFlyout_Opened(object? sender, object e)
		{
			if (sender is not CommandBarFlyout itemContextMenuFlyout)
				return;

			itemContextMenuFlyout.Opened -= ItemContextMenuFlyout_Opened;
			await ShellContextFlyoutFactory.LoadShellMenuItemsAsync(rightClickedItem.Path, itemContextMenuFlyout, rightClickedItem.MenuOptions);
		}

		public async void HandleItemInvokedAsync(object item, PointerUpdateKind pointerUpdateKind)
		{
			if (item is not INavigationControlItem navigationControlItem) return;
			var navigationPath = item as string;

			if (await DriveHelpers.CheckEmptyDrive(navigationPath))
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var middleClickPressed = pointerUpdateKind == PointerUpdateKind.MiddleButtonReleased;
			if ((ctrlPressed ||
				middleClickPressed) &&
				navigationControlItem.Path is not null)
			{
				await NavigationHelpers.OpenPathInNewTab(navigationControlItem.Path);
				return;
			}

			// Type of page to navigate
			Type? sourcePageType = null;

			switch (navigationControlItem.ItemType)
			{
				case NavigationControlItemType.Location:
					{
						// Get the path of the invoked item
						var ItemPath = navigationControlItem.Path;

						if (ItemPath is null)
							ItemPath = navigationControlItem.Text;

						// Home item
						if (ItemPath != null && ItemPath.Equals("Home", StringComparison.OrdinalIgnoreCase))
						{
							navigationPath = "Home";
							sourcePageType = typeof(HomePage);
						}
						else
						{
							navigationPath = navigationControlItem.Path;
						}
						break;
					}

				case NavigationControlItemType.FileTag:
					var tagPath = navigationControlItem.Path; // Get the path of the invoked item
					if (PaneHolder?.ActivePane is IShellPage shp)
					{
						shp.NavigateToPath(tagPath, new NavigationArguments()
						{
							IsSearchResultPage = true,
							SearchPathParam = "Home",
							SearchQuery = tagPath,
							AssociatedTabInstance = shp,
							NavPathParam = tagPath
						});
					}
					return;

				default:
					{
						navigationPath = navigationControlItem.Path;
						break;
					}
			}

			if (PaneHolder?.ActivePane is IShellPage shellPage)
				shellPage.NavigateToPath(navigationPath, sourcePageType);
		}

		public readonly ICommand CreateLibraryCommand = new AsyncRelayCommand(LibraryManager.ShowCreateNewLibraryDialogAsync);

		public readonly ICommand RestoreLibrariesCommand = new AsyncRelayCommand(LibraryManager.ShowRestoreDefaultLibrariesDialogAsync);

		private ICommand HideSectionCommand { get; }

		private ICommand PinItemCommand { get; }

		private ICommand UnpinItemCommand { get; }

		private ICommand EjectDeviceCommand { get; }

		private ICommand OpenPropertiesCommand { get; }

		private ICommand ReorderItemsCommand { get; }

		private void PinItem()
		{
			if (rightClickedItem is DriveItem)
				_ = QuickAccessService.PinToSidebarAsync(new[] { rightClickedItem.Path });
		}
		private void UnpinItem()
		{
			if (rightClickedItem.Section == SectionType.Pinned || rightClickedItem is DriveItem)
				_ = QuickAccessService.UnpinFromSidebarAsync(rightClickedItem.Path);
		}

		private void HideSection()
		{
			switch (rightClickedItem.Section)
			{
				case SectionType.Pinned:
					UserSettingsService.GeneralSettingsService.ShowPinnedSection = false;
					break;
				case SectionType.Library:
					UserSettingsService.GeneralSettingsService.ShowLibrarySection = false;
					break;
				case SectionType.CloudDrives:
					UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection = false;
					break;
				case SectionType.Drives:
					UserSettingsService.GeneralSettingsService.ShowDrivesSection = false;
					break;
				case SectionType.Network:
					UserSettingsService.GeneralSettingsService.ShowNetworkSection = false;
					break;
				case SectionType.WSL:
					UserSettingsService.GeneralSettingsService.ShowWslSection = false;
					break;
				case SectionType.FileTag:
					UserSettingsService.GeneralSettingsService.ShowFileTagsSection = false;
					break;
			}
		}

		private async Task ReorderItemsAsync()
		{
			var dialog = new ReorderSidebarItemsDialogViewModel();
			var dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			var result = await dialogService.ShowDialogAsync(dialog);
		}

		private void OpenProperties(CommandBarFlyout menu)
		{
			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = async (s, e) =>
			{
				menu.Closed -= flyoutClosed;
				if (rightClickedItem is DriveItem)
					FilePropertiesHelpers.OpenPropertiesWindow(rightClickedItem, PaneHolder.ActivePane);
				else if (rightClickedItem is LibraryLocationItem library)
					FilePropertiesHelpers.OpenPropertiesWindow(new LibraryItem(library), PaneHolder.ActivePane);
				else if (rightClickedItem is LocationItem locationItem)
				{
					var listedItem = new ListedItem(null!)
					{
						ItemPath = locationItem.Path,
						ItemNameRaw = locationItem.Text,
						PrimaryItemAttribute = StorageItemTypes.Folder,
						ItemType = "Folder".GetLocalizedResource(),
					};

					if (!string.Equals(locationItem.Path, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
					{
						BaseStorageFolder matchingStorageFolder = await PaneHolder.ActivePane.ShellViewModel.GetFolderFromPathAsync(locationItem.Path);
						if (matchingStorageFolder is not null)
						{
							var syncStatus = await PaneHolder.ActivePane.ShellViewModel.CheckCloudDriveSyncStatusAsync(matchingStorageFolder);
							listedItem.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
						}
					}

					FilePropertiesHelpers.OpenPropertiesWindow(listedItem, PaneHolder.ActivePane);
				}
			};
			menu.Closed += flyoutClosed;
		}

		private void EjectDevice()
		{
			DriveHelpers.EjectDeviceAsync(rightClickedItem.Path);
		}

		private List<ContextMenuFlyoutItemViewModel> GetLocationItemMenuItems(INavigationControlItem item, CommandBarFlyout menu)
		{
			var options = item.MenuOptions;

			var pinnedFolderModel = App.QuickAccessManager.Model;
			var pinnedFolderIndex = pinnedFolderModel.IndexOfItem(item);
			var pinnedFolderCount = pinnedFolderModel.PinnedFolders.Count;

			var isPinnedItem = item.Section is SectionType.Pinned && pinnedFolderIndex is not -1;
			var showMoveItemUp = isPinnedItem && pinnedFolderIndex > 0;
			var showMoveItemDown = isPinnedItem && pinnedFolderIndex < pinnedFolderCount - 1;

			var isDriveItem = item is DriveItem;
			var isDriveItemPinned = isDriveItem && ((DriveItem)item).IsPinned;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = Strings.SideBarCreateNewLibrary_Text.GetLocalizedResource(),
					Glyph = "\uE710",
					Command = CreateLibraryCommand,
					ShowItem = options.IsLibrariesHeader
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = Strings.SideBarRestoreLibraries_Text.GetLocalizedResource(),
					Glyph = "\uE10E",
					Command = RestoreLibrariesCommand,
					ShowItem = options.IsLibrariesHeader
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.EmptyRecycleBin)
				{
					IsVisible = options.ShowEmptyRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.RestoreAllRecycleBin)
				{
					IsVisible = options.ShowEmptyRecycleBin,
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenInNewTabFromSidebarAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewTab && Commands.OpenInNewTabFromSidebarAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenInNewWindowFromSidebarAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewWindow && Commands.OpenInNewWindowFromSidebarAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenInNewPaneFromSidebarAction)
				{
					IsVisible = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane && Commands.OpenInNewPaneFromSidebarAction.IsExecutable
				}.Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "PinFolderToSidebar".GetLocalizedResource(),
					ThemedIconModel = new ThemedIconModel()
					{
						ThemedIconStyle = "App.ThemedIcons.FavoritePin",
					},
					Command = PinItemCommand,
					ShowItem = isDriveItem && !isDriveItemPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "UnpinFolderFromSidebar".GetLocalizedResource(),
					ThemedIconModel = new ThemedIconModel()
					{
						ThemedIconStyle = "App.ThemedIcons.FavoritePinRemove",
					},
					Command = UnpinItemCommand,
					ShowItem = options.ShowUnpinItem || isDriveItemPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "ReorderSidebarItemsDialogText".GetLocalizedResource(),
					Glyph = "\uE8D8",
					Command = ReorderItemsCommand,
					ShowItem = isPinnedItem || item.Section is SectionType.Pinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = string.Format("SideBarHideSectionFromSideBar/Text".GetLocalizedResource(), rightClickedItem.Text),
					Glyph = "\uE77A",
					Command = HideSectionCommand,
					ShowItem = options.ShowHideSection
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Eject".GetLocalizedResource(),
					Command = EjectDeviceCommand,
					ShowItem = options.ShowEjectDevice
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Properties".GetLocalizedResource(),
					ThemedIconModel = new ThemedIconModel()
					{
						ThemedIconStyle = "App.ThemedIcons.Properties",
					},
					Command = OpenPropertiesCommand,
					CommandParameter = menu,
					ShowItem = options.ShowProperties
				},
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					ShowItem = Commands.OpenTerminalFromSidebar.IsExecutable ||
						Commands.OpenStorageSenseFromSidebar.IsExecutable ||
						Commands.FormatDriveFromSidebar.IsExecutable
				},
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenTerminalFromSidebar).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.OpenStorageSenseFromSidebar).Build(),
				new ContextMenuFlyoutItemViewModelBuilder(Commands.FormatDriveFromSidebar).Build(),
				new ContextMenuFlyoutItemViewModel()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
					IsHidden = !options.ShowShellItems,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = [],
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
					IsHidden = !options.ShowShellItems,
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "ManageTags".GetLocalizedResource(),
					Glyph = "\uE8EC",
					Command = Commands.OpenSettings,
					CommandParameter = new SettingsNavigationParams() { PageKind = SettingsPageKind.TagsPage },
					ShowItem = options.IsTagsHeader
				}
			}.Where(x => x.ShowItem).ToList();
		}

		public async Task HandleItemDragOverAsync(ItemDragOverEventArgs args)
		{
			if (args.DropTarget is LocationItem locationItem)
				await HandleLocationItemDragOverAsync(locationItem, args);
			else if (args.DropTarget is DriveItem driveItem)
				await HandleDriveItemDragOverAsync(driveItem, args);
			else if (args.DropTarget is FileTagItem fileTagItem)
				await HandleTagItemDragOverAsync(fileTagItem, args);
		}

		private async Task HandleLocationItemDragOverAsync(LocationItem locationItem, ItemDragOverEventArgs args)
		{
			var rawEvent = args.RawEvent;

			if (Utils.Storage.FilesystemHelpers.HasDraggedStorageItems(args.DroppedItem))
			{
				args.RawEvent.Handled = true;

				var isPathNull = string.IsNullOrEmpty(locationItem.Path);
				var storageItems = await Utils.Storage.FilesystemHelpers.GetDraggedStorageItems(args.DroppedItem);
				var hasStorageItems = storageItems.Any();

				if (isPathNull && hasStorageItems && SectionType.Pinned.Equals(locationItem.Section))
				{
					var haveFoldersToPin = storageItems.Any(item => item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.PinnedFolders.Contains(item.Path));

					if (!haveFoldersToPin)
					{
						rawEvent.AcceptedOperation = DataPackageOperation.None;
					}
					else
					{
						var captionText = "PinFolderToSidebar".GetLocalizedResource();
						CompleteDragEventArgs(rawEvent, captionText, DataPackageOperation.Move);
					}
				}
				else if (isPathNull ||
					(hasStorageItems && storageItems.AreItemsAlreadyInFolder(locationItem.Path)) ||
					locationItem.Path.StartsWith("Home", StringComparison.OrdinalIgnoreCase))
				{
					rawEvent.AcceptedOperation = DataPackageOperation.None;
				}
				else if (hasStorageItems is false)
				{
					rawEvent.AcceptedOperation = DataPackageOperation.None;
				}
				else
				{
					string captionText;
					DataPackageOperation operationType;
					if (locationItem.Path.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
					{
						captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
						operationType = DataPackageOperation.Move | DataPackageOperation.Copy;
					}
					else if (rawEvent.Modifiers.HasFlag(DragDropModifiers.Alt) || rawEvent.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
					{
						captionText = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Link;
					}
					else if (rawEvent.Modifiers.HasFlag(DragDropModifiers.Control))
					{
						captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Copy;
					}
					else if (rawEvent.Modifiers.HasFlag(DragDropModifiers.Shift))
					{
						captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
						operationType = DataPackageOperation.Move | DataPackageOperation.Copy;
					}
					else if (storageItems.Any(x => x.Item is ZipStorageFile || x.Item is ZipStorageFolder)
						|| ZipStorageFolder.IsZipPath(locationItem.Path))
					{
						captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Copy;
					}
					else if (locationItem.IsDefaultLocation || storageItems.AreItemsInSameDrive(locationItem.Path))
					{
						captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
						operationType = DataPackageOperation.Move | DataPackageOperation.Copy;
					}
					else
					{
						captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Copy;
					}
					CompleteDragEventArgs(rawEvent, captionText, operationType);
				}
			}
		}

		private async Task HandleDriveItemDragOverAsync(DriveItem driveItem, ItemDragOverEventArgs args)
		{
			if (!Utils.Storage.FilesystemHelpers.HasDraggedStorageItems(args.DroppedItem))
				return;

			args.RawEvent.Handled = true;

			var storageItems = await Utils.Storage.FilesystemHelpers.GetDraggedStorageItems(args.DroppedItem);
			var hasStorageItems = storageItems.Any();

			if ("Unknown".GetLocalizedResource().Equals(driveItem.SpaceText, StringComparison.OrdinalIgnoreCase) ||
				(hasStorageItems && storageItems.AreItemsAlreadyInFolder(driveItem.Path)))
			{
				args.RawEvent.AcceptedOperation = DataPackageOperation.None;
			}
			else if (!hasStorageItems)
			{
				args.RawEvent.AcceptedOperation = DataPackageOperation.None;
			}
			else
			{
				string captionText;
				DataPackageOperation operationType;
				if (args.RawEvent.Modifiers.HasFlag(DragDropModifiers.Alt) || args.RawEvent.Modifiers.HasFlag(DragDropModifiers.Control | DragDropModifiers.Shift))
				{
					captionText = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					operationType = DataPackageOperation.Link;
				}
				else if (args.RawEvent.Modifiers.HasFlag(DragDropModifiers.Control))
				{
					captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					operationType = DataPackageOperation.Copy;
				}
				else if (args.RawEvent.Modifiers.HasFlag(DragDropModifiers.Shift))
				{
					captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
					operationType = DataPackageOperation.Move | DataPackageOperation.Copy;
				}
				else if (storageItems.AreItemsInSameDrive(driveItem.Path))
				{
					captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					// Some applications such as Edge can't raise the drop event by the Move flag (#14008), so we set the Copy flag as well.
					operationType = DataPackageOperation.Move | DataPackageOperation.Copy;
				}
				else
				{
					captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					operationType = DataPackageOperation.Copy;
				}
				CompleteDragEventArgs(args.RawEvent, captionText, operationType);
			}
		}

		private async Task HandleTagItemDragOverAsync(FileTagItem tagItem, ItemDragOverEventArgs args)
		{
			if (!Utils.Storage.FilesystemHelpers.HasDraggedStorageItems(args.DroppedItem))
				return;

			args.RawEvent.Handled = true;

			var storageItems = await Utils.Storage.FilesystemHelpers.GetDraggedStorageItems(args.DroppedItem);

			if (!storageItems.Any(x => !string.IsNullOrEmpty(x.Path)))
			{
				args.RawEvent.AcceptedOperation = DataPackageOperation.None;
			}
			else
			{
				args.RawEvent.DragUIOverride.IsCaptionVisible = true;
				args.RawEvent.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), tagItem.Text);
				args.RawEvent.AcceptedOperation = DataPackageOperation.Link;
			}
		}

		public async Task HandleItemDroppedAsync(ItemDroppedEventArgs args)
		{
			if (args.DropTarget is LocationItem locationItem)
				await HandleLocationItemDroppedAsync(locationItem, args);
			else if (args.DropTarget is DriveItem driveItem)
				await HandleDriveItemDroppedAsync(driveItem, args);
			else if (args.DropTarget is FileTagItem fileTagItem)
				await HandleTagItemDroppedAsync(fileTagItem, args);
		}

		private async Task HandleLocationItemDroppedAsync(LocationItem locationItem, ItemDroppedEventArgs args)
		{
			if (Utils.Storage.FilesystemHelpers.HasDraggedStorageItems(args.DroppedItem))
			{
				if (string.IsNullOrEmpty(locationItem.Path) && SectionType.Pinned.Equals(locationItem.Section)) // Pin to "Pinned" section
				{
					var storageItems = await Utils.Storage.FilesystemHelpers.GetDraggedStorageItems(args.DroppedItem);
					foreach (var item in storageItems)
					{
						if (item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.PinnedFolders.Contains(item.Path))
							await QuickAccessService.PinToSidebarAsync(item.Path);
					}
				}
				else
				{
					await FilesystemHelpers.PerformOperationTypeAsync(args.RawEvent.AcceptedOperation, args.DroppedItem, locationItem.Path, false, true);
				}
			}
		}

		private Task<ReturnResult> HandleDriveItemDroppedAsync(DriveItem driveItem, ItemDroppedEventArgs args)
		{
			return FilesystemHelpers.PerformOperationTypeAsync(args.RawEvent.AcceptedOperation, args.RawEvent.DataView, driveItem.Path, false, true);
		}

		private async Task HandleTagItemDroppedAsync(FileTagItem fileTagItem, ItemDroppedEventArgs args)
		{
			var storageItems = await Utils.Storage.FilesystemHelpers.GetDraggedStorageItems(args.DroppedItem);
			var dbInstance = FileTagsHelper.GetDbInstance();
			foreach (var item in storageItems.Where(x => !string.IsNullOrEmpty(x.Path)))
			{
				var filesTags = FileTagsHelper.ReadFileTag(item.Path);
				if (!filesTags.Contains(fileTagItem.FileTag.Uid))
				{
					filesTags = [.. filesTags, fileTagItem.FileTag.Uid];
					var fileFRN = await FileTagsHelper.GetFileFRN(item.Item);
					dbInstance.SetTags(item.Path, fileFRN, filesTags);
					FileTagsHelper.WriteFileTag(item.Path, filesTags);
				}
			}
		}

		private static DragEventArgs CompleteDragEventArgs(DragEventArgs e, string captionText, DataPackageOperation operationType)
		{
			e.DragUIOverride.IsCaptionVisible = true;
			e.DragUIOverride.Caption = captionText;
			e.AcceptedOperation = operationType;
			return e;
		}

		private GridLength tabControlMargin;
		public GridLength TabControlMargin
		{
			get => tabControlMargin;
			set => SetProperty(ref tabControlMargin, value);
		}
	}
}
