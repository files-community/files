// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Files.App.UserControls.Sidebar;
using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Files.Core.Storage;
using Files.Core.Storage.Extensions;

namespace Files.App.ViewModels.UserControls
{
	public class SidebarViewModel : ObservableObject, IDisposable, ISidebarViewModel
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();
		private readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();
		private readonly IFileTagsService fileTagsService;

		private readonly NetworkDrivesViewModel networkDrivesViewModel = Ioc.Default.GetRequiredService<NetworkDrivesViewModel>();

		private IPaneHolder paneHolder;
		public IPaneHolder PaneHolder
		{
			get => paneHolder;
			set => SetProperty(ref paneHolder, value);
		}

		public MenuFlyout PaneFlyout;

		public IFilesystemHelpers FilesystemHelpers
			=> PaneHolder?.FilesystemHelpers;

		private Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue;
		private ISidebarItem rightClickedItem;

		public object SidebarItems => sidebarItems;
		public BulkConcurrentObservableCollection<ISidebarItem> sidebarItems { get; init; }
		public SidebarPinnedModel SidebarPinnedModel => App.QuickAccessManager.Model;
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
		public static event EventHandler<ISidebarItem?>? RightClickedItemChanged;

		private readonly SidebarSectionType[] SectionOrder =
			new SidebarSectionType[]
			{
				SidebarSectionType.Home,
				SidebarSectionType.Favorites,
				SidebarSectionType.Library,
				SidebarSectionType.Drives,
				SidebarSectionType.CloudDrives,
				SidebarSectionType.Network,
				SidebarSectionType.WSL,
				SidebarSectionType.FileTag
			};

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

			ISidebarItem? item = null;
			var filteredItems = sidebarItems
				.Where(x => !string.IsNullOrWhiteSpace(x.Path))
				.Concat(sidebarItems.Where(x => (x as SideBarLocationItem)?.ChildItems is not null).SelectMany(x => ((SideBarLocationItem)x).ChildItems).Where(x => !string.IsNullOrWhiteSpace(x.Path)))
				.ToList();

			if (string.IsNullOrEmpty(value))
			{
				//SidebarSelectedItem = sidebarItems.FirstOrDefault(x => x.Path.Equals("Home"));
				return;
			}

			item = filteredItems.FirstOrDefault(x => x.Path.Equals(value, StringComparison.OrdinalIgnoreCase));
			item ??= filteredItems.FirstOrDefault(x => x.Path.Equals(value + "\\", StringComparison.OrdinalIgnoreCase));
			item ??= filteredItems.Where(x => value.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase)).MaxBy(x => x.Path.Length);
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

		public bool ShowFavoritesSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowFavoritesSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowFavoritesSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowFavoritesSection = value;
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

		public bool ShowNetworkDrivesSection
		{
			get => UserSettingsService.GeneralSettingsService.ShowNetworkDrivesSection;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowNetworkDrivesSection)
					return;

				UserSettingsService.GeneralSettingsService.ShowNetworkDrivesSection = value;
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

		private ISidebarItem selectedSidebarItem;

		public ISidebarItem SidebarSelectedItem
		{
			get => selectedSidebarItem;
			set => SetProperty(ref selectedSidebarItem, value);
		}

		public SidebarViewModel()
		{
			dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
			fileTagsService = Ioc.Default.GetRequiredService<IFileTagsService>();

			sidebarItems = new BulkConcurrentObservableCollection<ISidebarItem>();
			UserSettingsService.OnSettingChangedEvent += UserSettingsService_OnSettingChangedEvent;
			CreateItemHomeAsync();

			Manager_DataChanged(SidebarSectionType.Favorites, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SidebarSectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SidebarSectionType.Drives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SidebarSectionType.CloudDrives, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SidebarSectionType.Network, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SidebarSectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			Manager_DataChanged(SidebarSectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			App.QuickAccessManager.Model.DataChanged += Manager_DataChanged;
			App.LibraryManager.DataChanged += Manager_DataChanged;
			drivesViewModel.Drives.CollectionChanged += (x, args) => Manager_DataChanged(SidebarSectionType.Drives, args);
			CloudDrivesManager.DataChanged += Manager_DataChanged;
			networkDrivesViewModel.Drives.CollectionChanged += (x, args) => Manager_DataChanged(SidebarSectionType.Network, args);
			WSLDistroManager.DataChanged += Manager_DataChanged;
			App.FileTagsManager.DataChanged += Manager_DataChanged;
			SidebarDisplayMode = UserSettingsService.AppearanceSettingsService.IsSidebarOpen ? SidebarDisplayMode.Expanded : SidebarDisplayMode.Compact;

			HideSectionCommand = new RelayCommand(HideSection);
			UnpinItemCommand = new RelayCommand(UnpinItem);
			PinItemCommand = new RelayCommand(PinItem);
			OpenInNewTabCommand = new AsyncRelayCommand(OpenInNewTabAsync);
			OpenInNewWindowCommand = new AsyncRelayCommand(OpenInNewWindowAsync);
			OpenInNewPaneCommand = new AsyncRelayCommand(OpenInNewPaneAsync);
			EjectDeviceCommand = new AsyncRelayCommand(EjectDeviceAsync);
			FormatDriveCommand = new RelayCommand(FormatDrive);
			OpenPropertiesCommand = new RelayCommand<CommandBarFlyout>(OpenProperties);
			ReorderItemsCommand = new AsyncRelayCommand(ReorderItemsAsync);
		}

		private Task CreateItemHomeAsync()
		{
			return CreateSectionAsync(SidebarSectionType.Home);
		}

		private async void Manager_DataChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			await dispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				var sectionType = (SidebarSectionType)sender;
				var section = await GetOrCreateSectionAsync(sectionType);
				Func<IReadOnlyList<ISidebarItem>> getElements = () => sectionType switch
				{
					SidebarSectionType.Favorites => App.QuickAccessManager.Model.Favorites,
					SidebarSectionType.CloudDrives => CloudDrivesManager.Drives,
					SidebarSectionType.Drives => drivesViewModel.Drives.Cast<SideBarDriveItem>().ToList().AsReadOnly(),
					SidebarSectionType.Network => networkDrivesViewModel.Drives.Cast<SideBarDriveItem>().ToList().AsReadOnly(),
					SidebarSectionType.WSL => WSLDistroManager.Distros,
					SidebarSectionType.Library => App.LibraryManager.Libraries,
					SidebarSectionType.FileTag => App.FileTagsManager.FileTags,
					_ => null
				};
				await SyncSidebarItemsAsync(section, getElements, e);
			});
		}

		private async Task SyncSidebarItemsAsync(SideBarLocationItem section, Func<IReadOnlyList<ISidebarItem>> getElements, NotifyCollectionChangedEventArgs e)
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
							await AddElementToSectionAsync((ISidebarItem)e.NewItems[i], section, index);
						}

						break;
					}

				case NotifyCollectionChangedAction.Move:
				case NotifyCollectionChangedAction.Remove:
				case NotifyCollectionChangedAction.Replace:
					{
						foreach (ISidebarItem elem in e.OldItems)
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
						foreach (ISidebarItem elem in getElements())
						{
							await AddElementToSectionAsync(elem, section);
						}
						foreach (ISidebarItem elem in section.ChildItems.ToList())
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

		private async Task AddElementToSectionAsync(ISidebarItem elem, SideBarLocationItem section, int index = -1)
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
			else if (elem is SideBarDriveItem drive)
			{
				if (section.Section is SidebarSectionType.Network or SidebarSectionType.CloudDrives)
				{
					// Already sorted
					if (!section.ChildItems.Any(x => x.Path == drive.Path))
					{
						section.ChildItems.Insert(index < 0 ? section.ChildItems.Count : Math.Min(index, section.ChildItems.Count), drive);
						await drive.LoadThumbnailAsync(true);
					}
				}
				else
				{
					string drivePath = drive.Path;
					IList<string> paths = section.ChildItems.Select(item => item.Path).ToList();

					if (!paths.Contains(drivePath))
					{
						paths.AddSorted(drivePath);
						int position = paths.IndexOf(drivePath);

						section.ChildItems.Insert(position, drive);
						await drive.LoadThumbnailAsync(true);
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

			section.IsExpanded = Ioc.Default.GetRequiredService<SettingsViewModel>().Get(section.Text == "SidebarFavorites".GetLocalizedResource(), $"section:{section.Text.Replace('\\', '_')}");
			section.PropertyChanged += Section_PropertyChanged;
		}

		private void Section_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (sender is SideBarLocationItem section && e.PropertyName == nameof(section.IsExpanded))
			{
				Ioc.Default.GetRequiredService<SettingsViewModel>().Set(section.IsExpanded, $"section:{section.Text.Replace('\\', '_')}");
			}
		}

		private async Task<SideBarLocationItem> GetOrCreateSectionAsync(SidebarSectionType sectionType)
		{
			SideBarLocationItem? section = GetSection(sectionType) ?? await CreateSectionAsync(sectionType);
			return section;
		}

		private SideBarLocationItem? GetSection(SidebarSectionType sectionType)
		{
			return sidebarItems.FirstOrDefault(x => x.Section == sectionType) as SideBarLocationItem;
		}

		private async Task<SideBarLocationItem> CreateSectionAsync(SidebarSectionType sectionType)
		{
			SideBarLocationItem section = null;
			BitmapImage icon = null;
			int iconIdex = -1;

			switch (sectionType)
			{
				case SidebarSectionType.Home:
					{
						section = BuildSection("Home".GetLocalizedResource(), sectionType, new SidebarContextMenuOptions { IsLocationItem = true }, true);
						section.Path = "Home";
						section.Icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.HomeIcon));
						section.IsHeader = true;

						break;
					}

				case SidebarSectionType.Favorites:
					{
						if (ShowFavoritesSection == false)
						{
							break;
						}

						section = BuildSection("SidebarFavorites".GetLocalizedResource(), sectionType, new SidebarContextMenuOptions { ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.FavoritesIcon));
						section.IsHeader = true;

						break;
					}

				case SidebarSectionType.Library:
					{
						if (ShowLibrarySection == false)
						{
							break;
						}
						section = BuildSection("SidebarLibraries".GetLocalizedResource(), sectionType, new SidebarContextMenuOptions { IsLibrariesHeader = true, ShowHideSection = true }, false);
						iconIdex = Constants.ImageRes.Libraries;
						section.IsHeader = true;

						break;
					}

				case SidebarSectionType.Drives:
					{
						if (ShowDrivesSection == false)
						{
							break;
						}
						section = BuildSection("Drives".GetLocalizedResource(), sectionType, new SidebarContextMenuOptions { ShowHideSection = true }, false);
						iconIdex = Constants.ImageRes.ThisPC;
						section.IsHeader = true;

						break;
					}

				case SidebarSectionType.CloudDrives:
					{
						if (ShowCloudDrivesSection == false || CloudDrivesManager.Drives.Any() == false)
						{
							break;
						}
						section = BuildSection("SidebarCloudDrives".GetLocalizedResource(), sectionType, new SidebarContextMenuOptions { ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.CloudDriveIcon));
						section.IsHeader = true;

						break;
					}

				case SidebarSectionType.Network:
					{
						if (!ShowNetworkDrivesSection)
						{
							break;
						}
						section = BuildSection("SidebarNetworkDrives".GetLocalizedResource(), sectionType, new SidebarContextMenuOptions { ShowHideSection = true }, false);
						iconIdex = Constants.ImageRes.NetworkDrives;
						section.IsHeader = true;

						break;
					}

				case SidebarSectionType.WSL:
					{
						if (ShowWslSection == false || WSLDistroManager.Distros.Any() == false)
						{
							break;
						}
						section = BuildSection("WSL".GetLocalizedResource(), sectionType, new SidebarContextMenuOptions { ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.WslIconsPaths.GenericIcon));
						section.IsHeader = true;

						break;
					}

				case SidebarSectionType.FileTag:
					{
						if (!ShowFileTagsSection)
						{
							break;
						}
						section = BuildSection("FileTags".GetLocalizedResource(), sectionType, new SidebarContextMenuOptions { ShowHideSection = true }, false);
						icon = new BitmapImage(new Uri(Constants.FluentIconsPaths.FileTagsIcon));
						section.IsHeader = true;

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

		private SideBarLocationItem BuildSection(string sectionName, SidebarSectionType sectionType, SidebarContextMenuOptions options, bool selectsOnInvoked)
		{
			return new SideBarLocationItem()
			{
				Text = sectionName,
				Section = sectionType,
				MenuOptions = options,
				SelectsOnInvoked = selectsOnInvoked,
				ChildItems = new BulkConcurrentObservableCollection<ISidebarItem>()
			};
		}

		private void AddSectionToSideBar(SideBarLocationItem section)
		{
			var index = SectionOrder.TakeWhile(x => x != section.Section).Select(x => sidebarItems.Any(item => item.Section == x) ? 1 : 0).Sum();
			sidebarItems.Insert(Math.Min(index, sidebarItems.Count), section);
		}

		public async Task UpdateSectionVisibilityAsync(SidebarSectionType sectionType, bool show)
		{
			if (show)
			{
				var generalSettingsService = UserSettingsService.GeneralSettingsService;

				Func<Task> action = sectionType switch
				{
					SidebarSectionType.CloudDrives when generalSettingsService.ShowCloudDrivesSection => CloudDrivesManager.UpdateDrivesAsync,
					SidebarSectionType.Drives => drivesViewModel.UpdateDrivesAsync,
					SidebarSectionType.Network when generalSettingsService.ShowNetworkDrivesSection => networkDrivesViewModel.UpdateDrivesAsync,
					SidebarSectionType.WSL when generalSettingsService.ShowWslSection => WSLDistroManager.UpdateDrivesAsync,
					SidebarSectionType.FileTag when generalSettingsService.ShowFileTagsSection => App.FileTagsManager.UpdateFileTagsAsync,
					SidebarSectionType.Library => App.LibraryManager.UpdateLibrariesAsync,
					SidebarSectionType.Favorites => App.QuickAccessManager.Model.AddAllItemsToSidebarAsync,
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
				case nameof(UserSettingsService.GeneralSettingsService.ShowFavoritesSection):
					await UpdateSectionVisibilityAsync(SidebarSectionType.Favorites, ShowFavoritesSection);
					OnPropertyChanged(nameof(ShowFavoritesSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowLibrarySection):
					await UpdateSectionVisibilityAsync(SidebarSectionType.Library, ShowLibrarySection);
					OnPropertyChanged(nameof(ShowLibrarySection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection):
					await UpdateSectionVisibilityAsync(SidebarSectionType.CloudDrives, ShowCloudDrivesSection);
					OnPropertyChanged(nameof(ShowCloudDrivesSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowDrivesSection):
					await UpdateSectionVisibilityAsync(SidebarSectionType.Drives, ShowDrivesSection);
					OnPropertyChanged(nameof(ShowDrivesSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowNetworkDrivesSection):
					await UpdateSectionVisibilityAsync(SidebarSectionType.Network, ShowNetworkDrivesSection);
					OnPropertyChanged(nameof(ShowNetworkDrivesSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowWslSection):
					await UpdateSectionVisibilityAsync(SidebarSectionType.WSL, ShowWslSection);
					OnPropertyChanged(nameof(ShowWslSection));
					break;
				case nameof(UserSettingsService.GeneralSettingsService.ShowFileTagsSection):
					await UpdateSectionVisibilityAsync(SidebarSectionType.FileTag, ShowFileTagsSection);
					OnPropertyChanged(nameof(ShowFileTagsSection));
					break;
			}
		}

		public void Dispose()
		{
			UserSettingsService.OnSettingChangedEvent -= UserSettingsService_OnSettingChangedEvent;

			App.QuickAccessManager.Model.DataChanged -= Manager_DataChanged;
			App.LibraryManager.DataChanged -= Manager_DataChanged;
			drivesViewModel.Drives.CollectionChanged -= (x, args) => Manager_DataChanged(SidebarSectionType.Drives, args);
			CloudDrivesManager.DataChanged -= Manager_DataChanged;
			networkDrivesViewModel.Drives.CollectionChanged -= (x, args) => Manager_DataChanged(SidebarSectionType.Network, args);
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

			if (args.Item is not ISidebarItem item)
			{
				// We are in the pane context requested path
				PaneFlyout.ShowAt(sender as FrameworkElement, args.Position);

				return;
			}

			if (item is SideBarFileTagItem tagItem)
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
			itemContextMenuFlyout.Closed += (sender, e) => RightClickedItemChanged?.Invoke(this, null);

			var menuItems = GetLocationItemMenuItems(item, itemContextMenuFlyout);
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

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
			await ShellContextmenuHelper.LoadShellMenuItemsAsync(rightClickedItem.Path, itemContextMenuFlyout, rightClickedItem.MenuOptions);
		}

		public async void HandleItemInvokedAsync(object item, PointerUpdateKind pointerUpdateKind)
		{
			if (item is not ISidebarItem navigationControlItem) return;
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
				case SidebarItemType.Location:
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

				case SidebarItemType.FileTag:
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

		private ICommand OpenInNewTabCommand { get; }

		private ICommand OpenInNewWindowCommand { get; }

		private ICommand OpenInNewPaneCommand { get; }

		private ICommand EjectDeviceCommand { get; }

		private ICommand FormatDriveCommand { get; }

		private ICommand OpenPropertiesCommand { get; }

		private ICommand ReorderItemsCommand { get; }

		private async Task OpenInNewPaneAsync()
		{
			if (await DriveHelpers.CheckEmptyDrive(rightClickedItem.Path))
				return;
			PaneHolder.OpenPathInNewPane(rightClickedItem.Path);
		}

		private async Task OpenInNewTabAsync()
		{
			if (await DriveHelpers.CheckEmptyDrive(rightClickedItem.Path))
				return;

			await NavigationHelpers.OpenPathInNewTab(rightClickedItem.Path);
		}

		private async Task OpenInNewWindowAsync()
		{
			if (await DriveHelpers.CheckEmptyDrive(rightClickedItem.Path))
				return;

			await NavigationHelpers.OpenPathInNewWindowAsync(rightClickedItem.Path);
		}

		private void PinItem()
		{
			if (rightClickedItem is SideBarDriveItem)
				_ = QuickAccessService.PinToSidebarAsync(new[] { rightClickedItem.Path });
		}
		private void UnpinItem()
		{
			if (rightClickedItem.Section == SidebarSectionType.Favorites || rightClickedItem is SideBarDriveItem)
				_ = QuickAccessService.UnpinFromSidebarAsync(rightClickedItem.Path);
		}

		private void HideSection()
		{
			switch (rightClickedItem.Section)
			{
				case SidebarSectionType.Favorites:
					UserSettingsService.GeneralSettingsService.ShowFavoritesSection = false;
					break;
				case SidebarSectionType.Library:
					UserSettingsService.GeneralSettingsService.ShowLibrarySection = false;
					break;
				case SidebarSectionType.CloudDrives:
					UserSettingsService.GeneralSettingsService.ShowCloudDrivesSection = false;
					break;
				case SidebarSectionType.Drives:
					UserSettingsService.GeneralSettingsService.ShowDrivesSection = false;
					break;
				case SidebarSectionType.Network:
					UserSettingsService.GeneralSettingsService.ShowNetworkDrivesSection = false;
					break;
				case SidebarSectionType.WSL:
					UserSettingsService.GeneralSettingsService.ShowWslSection = false;
					break;
				case SidebarSectionType.FileTag:
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
				if (rightClickedItem is SideBarDriveItem)
					FilePropertiesHelpers.OpenPropertiesWindow(rightClickedItem, PaneHolder.ActivePane);
				else if (rightClickedItem is LibraryLocationItem library)
					FilePropertiesHelpers.OpenPropertiesWindow(new LibraryItem(library), PaneHolder.ActivePane);
				else if (rightClickedItem is SideBarLocationItem locationItem)
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
						BaseStorageFolder matchingStorageFolder = await PaneHolder.ActivePane.FilesystemViewModel.GetFolderFromPathAsync(locationItem.Path);
						if (matchingStorageFolder is not null)
						{
							var syncStatus = await PaneHolder.ActivePane.FilesystemViewModel.CheckCloudDriveSyncStatusAsync(matchingStorageFolder);
							listedItem.SyncStatusUI = CloudDriveSyncStatusUI.FromCloudDriveSyncStatus(syncStatus);
						}
					}

					FilePropertiesHelpers.OpenPropertiesWindow(listedItem, PaneHolder.ActivePane);
				}
			};
			menu.Closed += flyoutClosed;
		}

		private async Task EjectDeviceAsync()
		{
			var result = await DriveHelpers.EjectDeviceAsync(rightClickedItem.Path);
			await UIHelpers.ShowDeviceEjectResultAsync(rightClickedItem is SideBarDriveItem driveItem ? driveItem.Type : Data.Items.DriveType.Unknown, result);
		}

		private void FormatDrive()
		{
			Win32API.OpenFormatDriveDialog(rightClickedItem.Path);
		}

		private List<ContextMenuFlyoutItemViewModel> GetLocationItemMenuItems(ISidebarItem item, CommandBarFlyout menu)
		{
			var options = item.MenuOptions;

			var favoriteModel = App.QuickAccessManager.Model;
			var favoriteIndex = favoriteModel.IndexOfItem(item);
			var favoriteCount = favoriteModel.FavoriteItems.Count;

			var isFavoriteItem = item.Section is SidebarSectionType.Favorites && favoriteIndex is not -1;
			var showMoveItemUp = isFavoriteItem && favoriteIndex > 0;
			var showMoveItemDown = isFavoriteItem && favoriteIndex < favoriteCount - 1;

			var isDriveItem = item is SideBarDriveItem;
			var isDriveItemPinned = isDriveItem && ((SideBarDriveItem)item).IsPinned;

			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarCreateNewLibrary/Text".GetLocalizedResource(),
					Glyph = "\uE710",
					Command = CreateLibraryCommand,
					ShowItem = options.IsLibrariesHeader
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "SideBarRestoreLibraries/Text".GetLocalizedResource(),
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
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab",
					},
					Command = OpenInNewTabCommand,
					ShowItem = options.IsLocationItem && UserSettingsService.GeneralSettingsService.ShowOpenInNewTab
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewWindow",
					},
					Command = OpenInNewWindowCommand,
					ShowItem = options.IsLocationItem && UserSettingsService.GeneralSettingsService.ShowOpenInNewTab
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = OpenInNewPaneCommand,
					ShowItem = options.IsLocationItem && UserSettingsService.GeneralSettingsService.ShowOpenInNewPane
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "PinToFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconPinToFavorites",
					},
					Command = PinItemCommand,
					ShowItem = isDriveItem && !isDriveItemPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "UnpinFromFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconUnpinFromFavorites",
					},
					Command = UnpinItemCommand,
					ShowItem = options.ShowUnpinItem || isDriveItemPinned
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "ReorderSidebarItemsDialogText".GetLocalizedResource(),
					Glyph = "\uE8D8",
					Command = ReorderItemsCommand,
					ShowItem = isFavoriteItem || item.Section is SidebarSectionType.Favorites
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
					Text = "FormatDriveText".GetLocalizedResource(),
					Command = FormatDriveCommand,
					CommandParameter = item,
					ShowItem = options.ShowFormatDrive
				},
				new ContextMenuFlyoutItemViewModel()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = OpenPropertiesCommand,
					CommandParameter = menu,
					ShowItem = options.ShowProperties
				},
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
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
					IsHidden = !options.ShowShellItems,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		public async void HandleItemDragOverAsync(ItemDragOverEventArgs args)
		{
			if (args.DropTarget is SideBarLocationItem locationItem)
			{
				HandleLocationItemDragOverAsync(locationItem, args);
			}
			else if (args.DropTarget is SideBarDriveItem driveItem)
			{
				HandleDriveItemDragOverAsync(driveItem, args);
			}
			else if (args.DropTarget is SideBarFileTagItem fileTagItem)
			{
				HandleTagItemDragOverAsync(fileTagItem, args);
			}
		}

		private async void HandleLocationItemDragOverAsync(SideBarLocationItem locationItem, ItemDragOverEventArgs args)
		{
			var rawEvent = args.RawEvent;
			var deferral = rawEvent.GetDeferral();

			if (Utils.Storage.FilesystemHelpers.HasDraggedStorageItems(args.DroppedItem))
			{
				args.RawEvent.Handled = true;

				var isPathNull = string.IsNullOrEmpty(locationItem.Path);
				var storageItems = await Utils.Storage.FilesystemHelpers.GetDraggedStorageItems(args.DroppedItem);
				var hasStorageItems = storageItems.Any();

				if (isPathNull && hasStorageItems && SidebarSectionType.Favorites.Equals(locationItem.Section))
				{
					var haveFoldersToPin = storageItems.Any(item => item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.FavoriteItems.Contains(item.Path));

					if (!haveFoldersToPin)
					{
						rawEvent.AcceptedOperation = DataPackageOperation.None;
					}
					else
					{
						var captionText = "PinToFavorites".GetLocalizedResource();
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
						operationType = DataPackageOperation.Move;
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
						operationType = DataPackageOperation.Move;
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
						operationType = DataPackageOperation.Move;
					}
					else
					{
						captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), locationItem.Text);
						operationType = DataPackageOperation.Copy;
					}
					CompleteDragEventArgs(rawEvent, captionText, operationType);
				}
			}

			deferral.Complete();
		}

		private async void HandleDriveItemDragOverAsync(SideBarDriveItem driveItem, ItemDragOverEventArgs args)
		{
			if (!Utils.Storage.FilesystemHelpers.HasDraggedStorageItems(args.DroppedItem))
				return;

			var deferral = args.RawEvent.GetDeferral();
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
					operationType = DataPackageOperation.Move;
				}
				else if (storageItems.AreItemsInSameDrive(driveItem.Path))
				{
					captionText = string.Format("MoveToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					operationType = DataPackageOperation.Move;
				}
				else
				{
					captionText = string.Format("CopyToFolderCaptionText".GetLocalizedResource(), driveItem.Text);
					operationType = DataPackageOperation.Copy;
				}
				CompleteDragEventArgs(args.RawEvent, captionText, operationType);
			}

			deferral.Complete();
		}

		private async void HandleTagItemDragOverAsync(SideBarFileTagItem tagItem, ItemDragOverEventArgs args)
		{
			if (!Utils.Storage.FilesystemHelpers.HasDraggedStorageItems(args.DroppedItem))
				return;

			var deferral = args.RawEvent.GetDeferral();
			args.RawEvent.Handled = true;

			var storageItems = await Utils.Storage.FilesystemHelpers.GetDraggedStorageItems(args.DroppedItem);

			if (!storageItems.Any())
			{
				args.RawEvent.AcceptedOperation = DataPackageOperation.None;
			}
			else
			{
				args.RawEvent.DragUIOverride.IsCaptionVisible = true;
				args.RawEvent.DragUIOverride.Caption = string.Format("LinkToFolderCaptionText".GetLocalizedResource(), tagItem.Text);
				args.RawEvent.AcceptedOperation = DataPackageOperation.Link;
			}

			deferral.Complete();
		}


		public async void HandleItemDroppedAsync(ItemDroppedEventArgs args)
		{
			if (args.DropTarget is SideBarLocationItem locationItem)
			{
				HandleLocationItemDroppedAsync(locationItem, args);
			}
			else if (args.DropTarget is SideBarDriveItem driveItem)
			{
				HandleDriveItemDroppedAsync(driveItem, args);
			}
			else if (args.DropTarget is SideBarFileTagItem fileTagItem)
			{
				HandleTagItemDroppedAsync(fileTagItem, args);
			}
		}

		private async void HandleLocationItemDroppedAsync(SideBarLocationItem locationItem, ItemDroppedEventArgs args)
		{
			if (Utils.Storage.FilesystemHelpers.HasDraggedStorageItems(args.DroppedItem))
			{
				var deferral = args.RawEvent.GetDeferral();
				if (string.IsNullOrEmpty(locationItem.Path) && SidebarSectionType.Favorites.Equals(locationItem.Section)) // Pin to Favorites section
				{
					var storageItems = await Utils.Storage.FilesystemHelpers.GetDraggedStorageItems(args.DroppedItem);
					foreach (var item in storageItems)
					{
						if (item.ItemType == FilesystemItemType.Directory && !SidebarPinnedModel.FavoriteItems.Contains(item.Path))
							await QuickAccessService.PinToSidebarAsync(item.Path);
					}
				}
				else
				{
					await FilesystemHelpers.PerformOperationTypeAsync(args.RawEvent.AcceptedOperation, args.DroppedItem, locationItem.Path, false, true);
				}
				deferral.Complete();
			}
		}

		private async void HandleDriveItemDroppedAsync(SideBarDriveItem driveItem, ItemDroppedEventArgs args)
		{
			var deferral = args.RawEvent.GetDeferral();

			await FilesystemHelpers.PerformOperationTypeAsync(args.RawEvent.AcceptedOperation, args.RawEvent.DataView, driveItem.Path, false, true);

			deferral.Complete();
			await Task.Yield();
		}

		private async void HandleTagItemDroppedAsync(SideBarFileTagItem fileTagItem, ItemDroppedEventArgs args)
		{
			var deferral = args.RawEvent.GetDeferral();

			var storageItems = await Utils.Storage.FilesystemHelpers.GetDraggedStorageItems(args.DroppedItem);
			foreach (var item in storageItems.Where(x => !string.IsNullOrEmpty(x.Path)))
			{
				var listedItem = new ListedItem(null)
				{
					ItemPath = item.Path,
					FileFRN = await FileTagsHelper.GetFileFRN(item.Item),
					FileTags = new[] { fileTagItem.FileTag.Uid }
				};
			}

			deferral.Complete();
			await Task.Yield();
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
