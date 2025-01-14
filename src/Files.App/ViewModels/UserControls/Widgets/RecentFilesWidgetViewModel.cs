// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System.Collections.Specialized;
using System.IO;
using Windows.Foundation.Metadata;

namespace Files.App.ViewModels.UserControls.Widgets
{
	/// <summary>
	/// Represents view model of <see cref="RecentFilesWidget"/>.
	/// </summary>
	public sealed class RecentFilesWidgetViewModel : BaseWidgetViewModel, IWidgetViewModel
	{
		// Fields

		private readonly SemaphoreSlim _refreshRecentFilesSemaphore;
		private CancellationTokenSource _refreshRecentFilesCTS;

		// Properties

		public ObservableCollection<RecentItem> Items { get; } = [];

		public string WidgetName => nameof(RecentFilesWidget);
		public string AutomationProperties => "RecentFiles".GetLocalizedResource();
		public string WidgetHeader => "RecentFiles".GetLocalizedResource();
		public bool IsWidgetSettingEnabled => UserSettingsService.GeneralSettingsService.ShowRecentFilesWidget;
		public bool ShowMenuFlyout => false;
		public MenuFlyoutItem? MenuFlyoutItem => null;

		private bool _IsEmptyRecentFilesTextVisible;
		public bool IsEmptyRecentFilesTextVisible
		{
			get => _IsEmptyRecentFilesTextVisible;
			set => SetProperty(ref _IsEmptyRecentFilesTextVisible, value);
		}

		private bool _IsRecentFilesDisabledInWindows;
		public bool IsRecentFilesDisabledInWindows
		{
			get => _IsRecentFilesDisabledInWindows;
			set => SetProperty(ref _IsRecentFilesDisabledInWindows, value);
		}

		// Constructor

		public RecentFilesWidgetViewModel()
		{
			_refreshRecentFilesSemaphore = new SemaphoreSlim(1, 1);
			_refreshRecentFilesCTS = new CancellationTokenSource();

			// recent files could have changed while widget wasn't loaded
			_ = RefreshWidgetAsync();

			WindowsRecentItemsService.RecentFilesChanged += Manager_RecentFilesChanged;

			RemoveRecentItemCommand = new AsyncRelayCommand<RecentItem>(ExecuteRemoveRecentItemCommand);
			ClearAllItemsCommand = new AsyncRelayCommand(ExecuteClearRecentItemsCommand);
			OpenFileLocationCommand = new RelayCommand<RecentItem>(ExecuteOpenFileLocationCommand);
			OpenPropertiesCommand = new RelayCommand<RecentItem>(ExecuteOpenPropertiesCommand);
		}

		// Methods

		public async Task RefreshWidgetAsync()
		{
			IsRecentFilesDisabledInWindows = !CheckIsRecentItemsEnabled();
			await WindowsRecentItemsService.UpdateRecentFilesAsync();
		}


		public override List<ContextMenuFlyoutItemViewModel> GetItemMenuItems(WidgetCardItem item, bool isPinned, bool isFolder = false)
		{
			return new List<ContextMenuFlyoutItemViewModel>()
			{
				new()
				{
					Text = "OpenWith".GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.OpenWith" },
					Tag = "OpenWithPlaceholder",
				},
				new()
				{
					Text = "RecentItemRemove/Text".GetLocalizedResource(),
					Glyph = "\uE738",
					Command = RemoveRecentItemCommand,
					CommandParameter = item
				},
				new()
				{
					Text = "RecentItemClearAll/Text".GetLocalizedResource(),
					Glyph = "\uE74D",
					Command = ClearAllItemsCommand
				},
				new()
				{
					Text = "OpenFileLocation".GetLocalizedResource(),
					Glyph = "\uED25",
					Command = OpenFileLocationCommand,
					CommandParameter = item
				},
				new()
				{
					Text = "SendTo".GetLocalizedResource(),
					Tag = "SendToPlaceholder",
					ShowItem = UserSettingsService.GeneralSettingsService.ShowSendToMenu
				},
				new()
				{
					Text = "Properties".GetLocalizedResource(),
					ThemedIconModel = new() { ThemedIconStyle = "App.ThemedIcons.Properties" },
					Command = OpenPropertiesCommand,
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
					Items = [],
					ID = "ItemOverflow",
					Tag = "ItemOverflow",
					IsEnabled = false,
				}
			}.Where(x => x.ShowItem).ToList();
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
						var recentFiles = WindowsRecentItemsService.RecentFiles; // already sorted, add all in order
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

		private bool AddItemToRecentList(RecentItem? recentItem, int index = -1)
		{
			if (recentItem is null)
				return false;

			if (!Items.Any(x => x.Equals(recentItem)))
			{
				Items.Insert(index < 0 ? Items.Count : Math.Min(index, Items.Count), recentItem);
				_ = recentItem.LoadRecentItemIconAsync()
					.ContinueWith(t => App.Logger.LogWarning(t.Exception, null), TaskContinuationOptions.OnlyOnFaulted);
				return true;
			}
			return false;
		}

		public void NavigateToPath(string path)
		{
			try
			{
				var directoryName = Path.GetDirectoryName(path);

				_ = Win32Helper.InvokeWin32ComponentAsync(path, ContentPageContext.ShellPage!, workingDirectory: directoryName ?? string.Empty);
			}
			catch (Exception) { }
		}

		public bool CheckIsRecentItemsEnabled()
		{
			using var explorerSubKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer");
			using var advSubkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
			using var userPolicySubkey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer");
			using var sysPolicySubkey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer");
			var policySubkey = userPolicySubkey ?? sysPolicySubkey;

			if (Convert.ToBoolean(explorerSubKey?.GetValue("ShowRecent", true)) &&
				Convert.ToBoolean(advSubkey?.GetValue("Start_TrackDocs", true)) &&
				!Convert.ToBoolean(policySubkey?.GetValue("NoRecentDocsHistory", false)))
				return true;

			return false;
		}

		// Event methods

		private async void Manager_RecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
			{
				// e.Action can only be Reset right now; naively refresh everything for simplicity
				await UpdateRecentFilesListAsync(e);
			});
		}

		// Command methods

		private async Task ExecuteRemoveRecentItemCommand(RecentItem? item)
		{
			if (item is null)
				return;

			await _refreshRecentFilesSemaphore.WaitAsync();

			try
			{
				await Task.Run(() =>
				{
					return WindowsRecentItemsService.Remove(item);
				});
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
				WindowsRecentItemsService.Clear();
			}
			finally
			{
				_refreshRecentFilesSemaphore.Release();
			}
		}

		private void ExecuteOpenFileLocationCommand(RecentItem? item)
		{
			if (item is null)
				return;

			var itemPath = Directory.GetParent(item.Path)?.FullName ?? string.Empty;
			var itemName = Path.GetFileName(item.Path);

			ContentPageContext.ShellPage!.NavigateWithArguments(
				ContentPageContext.ShellPage!.InstanceViewModel.FolderSettings.GetLayoutType(itemPath),
				new NavigationArguments()
				{
					NavPathParam = itemPath,
					SelectItems = new[] { itemName },
					AssociatedTabInstance = ContentPageContext.ShellPage!
				});
		}

		private void ExecuteOpenPropertiesCommand(RecentItem? item)
		{
			var flyout = HomePageContext.ItemContextFlyoutMenu;

			if (item is null || flyout is null)
				return;

			EventHandler<object> flyoutClosed = null!;
			flyoutClosed = async (s, e) =>
			{
				flyout!.Closed -= flyoutClosed;

				BaseStorageFile file = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(item.Path));
				if (file is null)
				{
					ContentDialog dialog = new()
					{
						Title = "CannotAccessPropertiesTitle".GetLocalizedResource(),
						Content = "CannotAccessPropertiesContent".GetLocalizedResource(),
						PrimaryButtonText = "Ok".GetLocalizedResource()
					};

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					await dialog.TryShowAsync();
				}
				else
				{
					var listedItem = await UniversalStorageEnumerator.AddFileAsync(file, null!, default);
					FilePropertiesHelpers.OpenPropertiesWindow(listedItem, ContentPageContext.ShellPage!);
				}
			};

			flyout!.Closed += flyoutClosed;
		}

		// Disposer

		public void Dispose()
		{
			WindowsRecentItemsService.RecentFilesChanged -= Manager_RecentFilesChanged;

			foreach (var item in Items)
				item.Dispose();
		}
	}
}
