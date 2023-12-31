﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Helpers.ContextFlyouts;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.CompilerServices;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents ViewModel for <see cref="RecentFilesWidget"/>.
	/// </summary>
	public class RecentFilesWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel, INotifyPropertyChanged
	{
		// Fields

		private readonly SemaphoreSlim _refreshRecentFilesSemaphore;

		private CancellationTokenSource _refreshRecentFilesCTS;

		// Properties

		public ObservableCollection<RecentItem> Items = new();

		public string WidgetName => nameof(RecentFilesWidgetViewModel);
		public string AutomationProperties => "RecentFilesWidgetAutomationProperties/Name".GetLocalizedResource();
		public string WidgetHeader => "RecentFiles".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;

		private bool _IsEmptyRecentFilesTextVisible = false;
		public bool IsEmptyRecentFilesTextVisible
		{
			get => _IsEmptyRecentFilesTextVisible;
			internal set
			{
				if (_IsEmptyRecentFilesTextVisible != value)
				{
					_IsEmptyRecentFilesTextVisible = value;
					NotifyPropertyChanged(nameof(IsEmptyRecentFilesTextVisible));
				}
			}
		}

		private bool _IsRecentFilesDisabledInWindows = false;
		public bool IsRecentFilesDisabledInWindows
		{
			get => _IsRecentFilesDisabledInWindows;
			internal set
			{
				if (_IsRecentFilesDisabledInWindows != value)
				{
					_IsRecentFilesDisabledInWindows = value;
					NotifyPropertyChanged(nameof(IsRecentFilesDisabledInWindows));
				}
			}
		}

		private IShellPage? _AppInstance;
		public IShellPage? AppInstance
		{
			get => _AppInstance;
			set
			{
				if (value != _AppInstance)
				{
					_AppInstance = value;
					NotifyPropertyChanged(nameof(AppInstance));
				}
			}
		}

		// Events

		public delegate void RecentFilesOpenLocationInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public delegate void RecentFileInvokedEventHandler(object sender, PathNavigationEventArgs e);
		public event RecentFilesOpenLocationInvokedEventHandler? RecentFilesOpenLocationInvoked;
		public event RecentFileInvokedEventHandler? RecentFileInvoked;
		public event PropertyChangedEventHandler? PropertyChanged;

		// Constructor

		public RecentFilesWidgetViewModel()
		{
			_refreshRecentFilesSemaphore = new SemaphoreSlim(1, 1);
			_refreshRecentFilesCTS = new CancellationTokenSource();

			// recent files could have changed while widget wasn't loaded
			_ = RefreshWidgetAsync();

			App.RecentItemsManager.RecentFilesChanged += Manager_RecentFilesChanged;

			RemoveRecentItemCommand = new AsyncRelayCommand<RecentItem>(ExecuteRemoveRecentItemCommand);
			ClearAllItemsCommand = new AsyncRelayCommand(ExecuteClearRecentItemsCommand);
			OpenFileLocationCommand = new RelayCommand<RecentItem>(ExecuteOpenFileLocationCommand);
			OpenPropertiesCommand = new RelayCommand<RecentItem>(ExecuteOpenPropertiesCommand);
		}

		// Methods

		public async Task RefreshWidgetAsync()
		{
			IsRecentFilesDisabledInWindows = App.RecentItemsManager.CheckIsRecentFilesEnabled() is false;

			await App.RecentItemsManager.UpdateRecentFilesAsync();
		}

		private async Task UpdateRecentFilesListAsync(NotifyCollectionChangedEventArgs e)
		{
			try
			{
				await _refreshRecentFilesSemaphore.WaitAsync(_refreshRecentFilesCTS.Token);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			try
			{
				// drop other waiting instances
				_refreshRecentFilesCTS.Cancel();
				_refreshRecentFilesCTS = new CancellationTokenSource();

				IsEmptyRecentFilesTextVisible = false;

				switch (e.Action)
				{
					case NotifyCollectionChangedAction.Add:
						if (e.NewItems is not null)
						{
							var addedItem = e.NewItems.Cast<RecentItem>().Single();
							AddItemToRecentList(addedItem, 0);
						}
						break;

					case NotifyCollectionChangedAction.Move:
						if (e.OldItems is not null)
						{
							var movedItem = e.OldItems.Cast<RecentItem>().Single();
							Items.RemoveAt(e.OldStartingIndex);
							AddItemToRecentList(movedItem, 0);
						}
						break;

					case NotifyCollectionChangedAction.Remove:
						if (e.OldItems is not null)
						{
							var removedItem = e.OldItems.Cast<RecentItem>().Single();
							Items.RemoveAt(e.OldStartingIndex);
						}
						break;

					// case NotifyCollectionChangedAction.Reset:
					default:
						var recentFiles = App.RecentItemsManager.RecentFiles; // already sorted, add all in order
						if (!recentFiles.SequenceEqual(Items))
						{
							Items.Clear();
							foreach (var item in recentFiles)
							{
								AddItemToRecentList(item);
							}
						}
						break;
				}

				// update chevron if there aren't any items
				if (Items.Count == 0 && !IsRecentFilesDisabledInWindows)
				{
					IsEmptyRecentFilesTextVisible = true;
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogInformation(ex, "Could not populate recent files");
			}
			finally
			{
				_refreshRecentFilesSemaphore.Release();
			}
		}

		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
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
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "RecentItemRemove/Text".GetLocalizedResource(),
					Glyph = "\uE738",
					Command = RemoveRecentItemCommand!,
					CommandParameter = item
				},
				new()
				{
					Text = "RecentItemClearAll/Text".GetLocalizedResource(),
					Glyph = "\uE74D",
					Command = ClearAllItemsCommand!
				},
				new()
				{
					Text = "OpenFileLocation".GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand!,
					CommandParameter = item
				},
				new()
				{
					Text = "Properties".GetLocalizedResource(),
					OpacityIcon = new OpacityIconModel()
					{
						OpacityIconStyle = "ColorIconProperties",
					},
					Command = OpenPropertiesCommand!,
					CommandParameter = item
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

		private bool AddItemToRecentList(RecentItem recentItem, int index = -1)
		{
			if (!Items.Any(x => x.Equals(recentItem)))
			{
				Items.Insert(index < 0 ? Items.Count : Math.Min(index, Items.Count), recentItem);
				_ = recentItem.LoadRecentItemIconAsync()
					.ContinueWith(t => App.Logger.LogWarning(t.Exception, null), TaskContinuationOptions.OnlyOnFaulted);
				return true;
			}
			return false;
		}

		// Event methods

		private async void Manager_RecentFilesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				// e.Action can only be Reset right now; naively refresh everything for simplicity
				await UpdateRecentFilesListAsync(e);
			});
		}

		private void ListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			// Ensure values are not null
			if (e.OriginalSource is not FrameworkElement element ||
				element.DataContext is not RecentItem item)
				return;

			// Create a new Flyout
			var itemContextMenuFlyout = new CommandBarFlyout()
			{
				Placement = FlyoutPlacementMode.Full
			};

			// Hook events
			itemContextMenuFlyout.Opening += (sender, e) => App.LastOpenedFlyout = sender as CommandBarFlyout;
			itemContextMenuFlyout.Opened += (sender, e) => OnRightClickedItemChanged(null, null);

			_flyoutItemPath = item.Path;

			// Notify of the change on right clicked item
			OnRightClickedItemChanged(item, itemContextMenuFlyout);

			// Get items for the flyout
			var menuItems = GetItemMenuItems(item, QuickAccessService.IsItemPinned(item.Path));
			var (_, secondaryElements) = ItemModelListToContextFlyoutHelper.GetAppBarItemsFromModel(menuItems);

			// Set max width of the flyout
			secondaryElements
				.OfType<FrameworkElement>()
				.ForEach(i => i.MinWidth = Constants.UI.ContextMenuItemsMaxWidth);

			// Add menu items to the secondary flyout
			secondaryElements.ForEach(itemContextMenuFlyout.SecondaryCommands.Add);

			// Show the flyout
			itemContextMenuFlyout.ShowAt(element, new() { Position = e.GetPosition(element) });

			// Load shell menu items
			_ = ShellContextmenuHelper.LoadShellMenuItemsAsync(_flyoutItemPath, itemContextMenuFlyout);
		}

		private void RecentsView_ItemClick(object sender, ItemClickEventArgs e)
		{
			var recentItem = e.ClickedItem as RecentItem;
			RecentFileInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = recentItem.RecentPath,
				IsFile = recentItem.IsFile
			});
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// Command methods

		private void ExecuteOpenFileLocationCommand(RecentItem? item)
		{
			RecentFilesOpenLocationInvoked?.Invoke(this, new PathNavigationEventArgs()
			{
				ItemPath = Directory.GetParent(item.RecentPath).FullName,    // parent directory
				ItemName = Path.GetFileName(item.RecentPath),                // file name w extension
			});
		}

		private void ExecuteOpenPropertiesCommand(RecentItem? item)
		{
			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = async (s, e) =>
			{
				HomePageContext.ItemContextFlyoutMenu!.Closed -= flyoutClosed;
				var listedItem = await UniversalStorageEnumerator.AddFileAsync(await BaseStorageFile.GetFileFromPathAsync(item.Path), null, default);
				FilePropertiesHelpers.OpenPropertiesWindow(listedItem, _AppInstance);
			};
			HomePageContext.ItemContextFlyoutMenu!.Closed += flyoutClosed;
		}

		private async Task ExecuteRemoveRecentItemCommand(RecentItem? item)
		{
			await _refreshRecentFilesSemaphore.WaitAsync();

			try
			{
				await App.RecentItemsManager.UnpinFromRecentFiles(item);
			}
			finally
			{
				_refreshRecentFilesSemaphore.Release();
			}
		}

		private async Task ExecuteClearRecentItemsCommand()
		{
			await _refreshRecentFilesSemaphore.WaitAsync();
			try
			{
				Items.Clear();
				bool success = App.RecentItemsManager.ClearRecentItems();

				if (success)
				{
					IsEmptyRecentFilesTextVisible = true;
				}
			}
			finally
			{
				_refreshRecentFilesSemaphore.Release();
			}
		}

		// Disposer

		public void Dispose()
		{
			App.RecentItemsManager.RecentFilesChanged -= Manager_RecentFilesChanged;
		}
	}
}
