// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.IO;
using Windows.Storage;
using Windows.UI.StartScreen;

namespace Files.App.Services
{
	public sealed class WindowsJumpListService : IWindowsJumpListService
	{
		private const string JumpListRecentGroupHeader = "ms-resource:///Resources/JumpListRecentGroupHeader";
		private const string JumpListPinnedGroupHeader = "ms-resource:///Resources/JumpListPinnedGroupHeader";

		public async Task InitializeAsync()
		{
			try
			{
				App.QuickAccessManager.UpdateQuickAccessWidget -= UpdateQuickAccessWidget_Invoked;
				App.QuickAccessManager.UpdateQuickAccessWidget += UpdateQuickAccessWidget_Invoked;

				await RefreshPinnedFoldersAsync();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		public async Task AddFolderAsync(string path)
		{
			try
			{
				if (JumpList.IsSupported())
				{
					var instance = await JumpList.LoadCurrentAsync();
					// Disable automatic jumplist. It doesn't work.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;

					// Saving to jumplist may fail randomly with error: ERROR_UNABLE_TO_REMOVE_REPLACED
					// In that case app should just catch the error and proceed as usual
					if (instance is not null)
					{
						AddFolder(path, JumpListRecentGroupHeader, instance);
						await instance.SaveAsync();
					}
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		public async Task<IEnumerable<string>> GetFoldersAsync()
		{
			if (JumpList.IsSupported())
			{
				try
				{
					var instance = await JumpList.LoadCurrentAsync();
					// Disable automatic jumplist. It doesn't work.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;

					return instance.Items.Select(item => item.Arguments).ToList();
				}
				catch
				{
					return [];
				}
			}
			else
			{
				return [];
			}
		}

		public async Task RefreshPinnedFoldersAsync()
		{
			try
			{
				App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = false;

				if (JumpList.IsSupported())
				{
					var instance = await JumpList.LoadCurrentAsync();
					// Disable automatic jumplist. It doesn't work with Files UWP.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;

					if (instance is null)
						return;

					var itemsToRemove = instance.Items.Where(x => string.Equals(x.GroupName, JumpListPinnedGroupHeader, StringComparison.OrdinalIgnoreCase)).ToList();
					itemsToRemove.ForEach(x => instance.Items.Remove(x));
					App.QuickAccessManager.Model.PinnedFolders.ForEach(x => AddFolder(x, JumpListPinnedGroupHeader, instance));
					await instance.SaveAsync();
				}
			}
			catch
			{
			}
			finally
			{
				SafetyExtensions.IgnoreExceptions(() => App.QuickAccessManager.PinnedItemsWatcher.EnableRaisingEvents = true);
			}
		}

		public async Task RemoveFolderAsync(string path)
		{
			if (JumpList.IsSupported())
			{
				try
				{
					var instance = await JumpList.LoadCurrentAsync();
					// Disable automatic jumplist. It doesn't work.
					instance.SystemGroupKind = JumpListSystemGroupKind.None;

					var itemToRemove = instance.Items.Where(x => x.Arguments == path).Select(x => x).FirstOrDefault();
					instance.Items.Remove(itemToRemove);
					await instance.SaveAsync();
				}
				catch { }
			}
		}

		private void AddFolder(string path, string group, JumpList instance)
		{
			if (instance is not null)
			{
				string? displayName = null;

				if (path.StartsWith("\\\\SHELL", StringComparison.OrdinalIgnoreCase))
					displayName = "ThisPC".GetLocalizedResource();

				if (path.EndsWith('\\'))
				{
					var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

					// Jumplist item argument can't end with a slash so append a character that can't exist in a directory name to support listing drives.
					var drive = drivesViewModel.Drives.FirstOrDefault(drive => drive.Path == path);
					if (drive is null)
						return;

					displayName = (drive as DriveItem)?.Text;
					path += '?';
				}

				if (displayName is null)
				{
					if (path.Equals(Constants.UserEnvironmentPaths.DesktopPath, StringComparison.OrdinalIgnoreCase))
						displayName = "ms-resource:///Resources/Desktop";
					else if (path.Equals(Constants.UserEnvironmentPaths.DownloadsPath, StringComparison.OrdinalIgnoreCase))
						displayName = "ms-resource:///Resources/Downloads";
					else if (path.Equals(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.OrdinalIgnoreCase))
						displayName = "Network".GetLocalizedResource();
					else if (path.Equals(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.OrdinalIgnoreCase))
						displayName = "RecycleBin".GetLocalizedResource();
					else if (path.Equals(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.OrdinalIgnoreCase))
						displayName = "ThisPC".GetLocalizedResource();
					else if (App.LibraryManager.TryGetLibrary(path, out LibraryLocationItem library))
					{
						var libName = Path.GetFileNameWithoutExtension(library.Path);
						displayName = libName switch
						{
							"Documents" or "Pictures" or "Music" or "Videos" => $"ms-resource:///Resources/{libName}",// Use localized name
							_ => library.Text,// Use original name
						};
					}
					else
						displayName = Path.GetFileName(path);
				}

				var jumplistItem = JumpListItem.CreateWithArguments(path, displayName);
				jumplistItem.Description = jumplistItem.Arguments ?? string.Empty;
				jumplistItem.GroupName = group;
				jumplistItem.Logo = new Uri("ms-appx:///Assets/FolderIcon.png");

				if (string.Equals(group, JumpListRecentGroupHeader, StringComparison.OrdinalIgnoreCase))
				{
					// Keep newer items at the top.
					instance.Items.Remove(instance.Items.FirstOrDefault(x => x.Arguments.Equals(path, StringComparison.OrdinalIgnoreCase)));
					instance.Items.Insert(0, jumplistItem);
				}
				else
				{
					var pinnedItemsCount = instance.Items.Count(x => x.GroupName == JumpListPinnedGroupHeader);
					instance.Items.Insert(pinnedItemsCount, jumplistItem);
				}
			}
		}

		private async void UpdateQuickAccessWidget_Invoked(object? sender, ModifyQuickAccessEventArgs e)
		{
			await RefreshPinnedFoldersAsync();
		}
	}
}
