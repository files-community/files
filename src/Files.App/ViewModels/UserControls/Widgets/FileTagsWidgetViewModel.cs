﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Files.Shared.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Windows.Storage;

namespace Files.App.ViewModels.UserControls.Widgets
{
	public sealed partial class FileTagsWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel, IAsyncInitialize
	{
		// Properties

		public ObservableCollection<WidgetFileTagsContainerItem> Containers { get; } = new();

		// NOTE:
		//  Second function is layered on top to ensure that
		//  OpenPath function is late initialized and a null reference is not passed-in.
		public Func<string, Task>? OpenAction { get; set; }

		private IShellPage? _AppInstance;
		public IShellPage? AppInstance
		{
			get => _AppInstance;
			set => SetProperty(ref _AppInstance, value);
		}

		public string WidgetName => nameof(FileTagsWidgetViewModel);
		public string WidgetHeader => "FileTags".GetLocalizedResource();
		public string AutomationProperties => "FileTags".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowFileTagsWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;

		// Events

		public delegate void FileTagsOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public delegate void FileTagsNewPaneInvokedEventHandler(object sender, QuickAccessCardInvokedEventArgs e);
		public static event EventHandler<IEnumerable<WidgetFileTagsItem>>? SelectedTaggedItemsChanged;
		public event FileTagsOpenLocationInvokedEventHandler? FileTagsOpenLocationInvoked;
		public event FileTagsNewPaneInvokedEventHandler? FileTagsNewPaneInvoked;

		// Commands

		public ICommand OpenInNewPaneCommand { get; private set; }

		// Constructor

		public FileTagsWidgetViewModel()
		{
			OpenInNewTabCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewTabCommand);
			OpenInNewWindowCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteOpenInNewWindowCommand);
			OpenFileLocationCommand = new RelayCommand<WidgetCardItem>(ExecuteOpenFileLocationCommand);
			OpenInNewPaneCommand = new RelayCommand<WidgetCardItem>(ExecuteOpenInNewPaneCommand);
			PinToFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(ExecutePinToFavoritesCommand);
			UnpinFromFavoritesCommand = new AsyncRelayCommand<WidgetCardItem>(ExecuteUnpinFromFavoritesCommand);
			OpenPropertiesCommand = new RelayCommand<WidgetCardItem>(ExecuteOpenPropertiesCommand);
		}

		// Methods

		public Task RefreshWidgetAsync()
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			await foreach (var item in FileTagsService.GetTagsAsync(cancellationToken))
			{
				var container = new WidgetFileTagsContainerItem(item.Uid, OpenAction!)
				{
					Name = item.Name,
					Color = item.Color
				};

				Containers.Add(container);

				_ = container.InitAsync(cancellationToken);
			}
		}

		protected override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new()
				{
					Text = "OpenWith".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenWith",
					},
					Tag = "OpenWithPlaceholder",
					ShowItem = !isFolder
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = !isFolder && UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "OpenInNewTab".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewTab",
					},
					Command = OpenInNewTabCommand!,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new()
				{
					Text = "OpenInNewWindow".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconOpenInNewWindow",
					},
					Command = OpenInNewWindowCommand!,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new()
				{
					Text = "OpenFileLocation".GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand!,
					CommandParameter = item,
					ShowItem = !isFolder
				},
				new()
				{
					Text = "OpenInNewPane".GetLocalizedResource(),
					Command = OpenInNewPaneCommand,
					CommandParameter = item,
					ShowItem = UserSettingsService.GeneralSettingsService.ShowOpenInNewPane && isFolder
				},
				new()
				{
					Text = "PinToFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconPinToFavorites",
					},
					Command = PinToFavoritesCommand!,
					CommandParameter = item,
					ShowItem = !isPinned && isFolder
				},
				new()
				{
					Text = "UnpinFromFavorites".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconUnpinFromFavorites",
					},
					Command = UnpinFromFavoritesCommand!,
					CommandParameter = item,
					ShowItem = isPinned && isFolder
				},
				new()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = OpenPropertiesCommand!,
					CommandParameter = item,
					ShowItem = isFolder
				},
				new()
				{
					ItemType = ContextMenuFlyoutItemType.Separator,
					Tag = "OverflowSeparator",
				},
				new()
				{
					Text = "Loading".GetLocalizedResource(),
					Glyph = "\xE712",
					Items = new List<ContextMenuFlyoutItemViewModel>(),
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
		}

		// Command methods

		private void ExecuteOpenFileLocationCommand(WidgetCardItem? item)
		{
			FileTagsOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = Directory.GetParent(item?.Path ?? string.Empty)?.FullName ?? string.Empty,
				ItemName = Path.GetFileName(item?.Path ?? string.Empty),
			});
		}

		private void ExecuteOpenPropertiesCommand(WidgetCardItem? item)
		{
			if (!HomePageContext.IsAnyItemRightClicked)
				return;

			EventHandler<object> flyoutClosed = null!;

			flyoutClosed = (s, e) =>
			{
				HomePageContext.ItemContextFlyoutMenu!.Closed -= flyoutClosed;

				ListedItem listedItem = new(null!)
				{
					ItemPath = (item!.Item as WidgetFileTagsItem)?.Path ?? string.Empty,
					ItemNameRaw = (item.Item as WidgetFileTagsItem)?.Name ?? string.Empty,
					PrimaryItemAttribute = StorageItemTypes.Folder,
					ItemType = "Folder".GetLocalizedResource(),
				};

				FilePropertiesHelpers.OpenPropertiesWindow(listedItem, AppInstance!);
			};

			HomePageContext.ItemContextFlyoutMenu!.Closed += flyoutClosed;
		}

		private void ExecuteOpenInNewPaneCommand(WidgetCardItem? item)
		{
			FileTagsNewPaneInvoked?.Invoke(this, new QuickAccessCardInvokedEventArgs()
			{
				Path = item?.Path ?? string.Empty
			});
		}

		// Disposer

		public void Dispose()
		{
		}
	}
}
