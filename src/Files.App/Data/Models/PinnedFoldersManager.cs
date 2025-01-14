// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.IO;
using System.Text.Json.Serialization;

namespace Files.App.Data.Models
{
	public sealed class PinnedFoldersManager
	{
		private IUserSettingsService userSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private IQuickAccessService QuickAccessService { get; } = Ioc.Default.GetRequiredService<IQuickAccessService>();

		public EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		private readonly SemaphoreSlim addSyncSemaphore = new(1, 1);

		public List<string> PinnedFolders { get; set; } = [];

		public readonly List<INavigationControlItem> _PinnedFolderItems = [];

		[JsonIgnore]
		public IReadOnlyList<INavigationControlItem> PinnedFolderItems
		{
			get
			{
				lock (_PinnedFolderItems)
					return _PinnedFolderItems.ToList().AsReadOnly();
			}
		}

		/// <summary>
		/// Updates items with the pinned items from the explorer sidebar
		/// </summary>
		public async Task UpdateItemsWithExplorerAsync()
		{
			await addSyncSemaphore.WaitAsync();

			try
			{
				var formerPinnedFolders = PinnedFolders.ToList();

				PinnedFolders = (await QuickAccessService.GetPinnedFoldersAsync())
					.Where(link => (bool?)link.Properties["System.Home.IsPinned"] ?? false)
					.Select(link => link.FilePath).ToList();

				if (formerPinnedFolders.SequenceEqual(PinnedFolders))
					return;

				RemoveStaleSidebarItems();
				await AddAllItemsToSidebarAsync();
			}
			finally
			{
				addSyncSemaphore.Release();
			}
		}

		/// <summary>
		/// Returns the index of the location item in the navigation sidebar
		/// </summary>
		/// <param name="locationItem">The location item</param>
		/// <returns>Index of the item</returns>
		public int IndexOfItem(INavigationControlItem locationItem)
		{
			lock (_PinnedFolderItems)
			{
				return _PinnedFolderItems.FindIndex(x => x.Path == locationItem.Path);
			}
		}

		public async Task<LocationItem> CreateLocationItemFromPathAsync(string path)
		{
			var item = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(path));
			var res = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(path, item));
			LocationItem locationItem;

			if (string.Equals(path, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
				locationItem = LocationItem.Create<RecycleBinLocationItem>();
			else
			{
				locationItem = LocationItem.Create<LocationItem>();

				if (path.Equals(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
					locationItem.Text = "ThisPC".GetLocalizedResource();
				else if (path.Equals(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
					locationItem.Text = "Network".GetLocalizedResource();
			}

			locationItem.Path = path;
			locationItem.Section = SectionType.Pinned;
			locationItem.MenuOptions = new ContextMenuOptions
			{
				IsLocationItem = true,
				ShowProperties = true,
				ShowUnpinItem = true,
				ShowShellItems = true,
				ShowEmptyRecycleBin = string.Equals(path, Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase)
			};
			locationItem.IsDefaultLocation = false;
			locationItem.Text = res?.Result?.DisplayName ?? Path.GetFileName(path.TrimEnd('\\'));

			if (res)
			{
				locationItem.IsInvalid = false;
				if (res.Result is not null)
				{
					var result = await FileThumbnailHelper.GetIconAsync(
						res.Result.Path,
						Constants.ShellIconSizes.Small,
						true,
						IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);

					locationItem.IconData = result;

					var bitmapImage = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => locationItem.IconData.ToBitmapAsync(), Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal);
					if (bitmapImage is not null)
						locationItem.Icon = bitmapImage;
				}
			}
			else
			{
				locationItem.Icon = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() => UIHelpers.GetSidebarIconResource(Constants.ImageRes.Folder));
				locationItem.IsInvalid = true;
				Debug.WriteLine($"Pinned item was invalid {res?.ErrorCode}, item: {path}");
			}

			return locationItem;
		}

		/// <summary>
		/// Adds the item (from a path) to the navigation sidebar
		/// </summary>
		/// <param name="path">The path which to save</param>
		/// <returns>Task</returns>
		public async Task AddItemToSidebarAsync(string path)
		{
			var locationItem = await CreateLocationItemFromPathAsync(path);

			AddLocationItemToSidebar(locationItem);
		}

		/// <summary>
		/// Adds the location item to the navigation sidebar
		/// </summary>
		/// <param name="locationItem">The location item which to save</param>
		private void AddLocationItemToSidebar(LocationItem locationItem)
		{
			int insertIndex = -1;
			lock (_PinnedFolderItems)
			{
				if (_PinnedFolderItems.Any(x => x.Path == locationItem.Path))
					return;

				var lastItem = _PinnedFolderItems.LastOrDefault(x => x.ItemType is NavigationControlItemType.Location);
				insertIndex = lastItem is not null ? _PinnedFolderItems.IndexOf(lastItem) + 1 : 0;
				_PinnedFolderItems.Insert(insertIndex, locationItem);
			}

			DataChanged?.Invoke(SectionType.Pinned, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, locationItem, insertIndex));
		}

		/// <summary>
		/// Adds all items to the navigation sidebar
		/// </summary>
		public async Task AddAllItemsToSidebarAsync()
		{
			if (userSettingsService.GeneralSettingsService.ShowPinnedSection)
				foreach (string path in PinnedFolders)
					await AddItemToSidebarAsync(path);
		}

		/// <summary>
		/// Removes stale items in the navigation sidebar
		/// </summary>
		public void RemoveStaleSidebarItems()
		{
			// Remove unpinned items from PinnedFolderItems
			foreach (var childItem in PinnedFolderItems)
			{
				if (childItem is LocationItem item && !item.IsDefaultLocation && !PinnedFolders.Contains(item.Path))
				{
					lock (_PinnedFolderItems)
					{
						_PinnedFolderItems.Remove(item);
					}
					DataChanged?.Invoke(SectionType.Pinned, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
				}
			}

			// Remove unpinned items from sidebar
			DataChanged?.Invoke(SectionType.Pinned, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public async void LoadAsync(object? sender, FileSystemEventArgs e)
		{
			await LoadAsync();
			App.QuickAccessManager.UpdateQuickAccessWidget?.Invoke(null, new ModifyQuickAccessEventArgs((await QuickAccessService.GetPinnedFoldersAsync()).ToArray(), true)
			{
				Reset = true
			});
		}

		public async Task LoadAsync()
		{
			await UpdateItemsWithExplorerAsync();
		}
	}
}
