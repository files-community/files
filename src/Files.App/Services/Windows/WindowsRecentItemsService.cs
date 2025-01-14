// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsRecentItemsService"/>
	public class WindowsRecentItemsService : IWindowsRecentItemsService
	{
		// Dependency injections

		private readonly IFoldersSettingsService FoldersSettingsService = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		// Fields

		private readonly SystemIO.FileSystemWatcher? _watcher;

		// Properties

		private readonly List<RecentItem> _RecentFiles = [];
		/// <inheritdoc/>
		public IReadOnlyList<RecentItem> RecentFiles
		{
			get
			{
				lock (_RecentFiles)
					return _RecentFiles.ToList().AsReadOnly();
			}
		}

		private readonly List<RecentItem> _RecentFolders = [];
		/// <inheritdoc/>
		public IReadOnlyList<RecentItem> RecentFolders
		{
			get
			{
				lock (_RecentFolders)
					return _RecentFolders.ToList().AsReadOnly();
			}
		}

		// Events 

		/// <inheritdoc/>
		public event EventHandler<NotifyCollectionChangedEventArgs>? RecentFilesChanged;

		/// <inheritdoc/>
		public event EventHandler<NotifyCollectionChangedEventArgs>? RecentFoldersChanged;

		// Constructor

		public WindowsRecentItemsService()
		{
			_watcher = new()
			{
				Path = SystemIO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Recent), "AutomaticDestinations"),
				Filter = "5f7b5f1e01b83767.automaticDestinations-ms",
				NotifyFilter = SystemIO.NotifyFilters.DirectoryName | SystemIO.NotifyFilters.FileName | SystemIO.NotifyFilters.LastWrite,
			};

			_watcher.Changed += Watcher_Changed;
			_watcher.Deleted += Watcher_Changed;
			_watcher.EnableRaisingEvents = true;
		}

		// Methods

		/// <inheritdoc/>
		public async Task<bool> UpdateRecentFilesAsync()
		{
			return await Task.Run(() =>
			{
				return UpdateRecentItems(false);
			});
		}

		/// <inheritdoc/>
		public async Task<bool> UpdateRecentFoldersAsync()
		{
			return await Task.Run(() =>
			{
				return UpdateRecentItems(true);
			});
		}

		/// <inheritdoc/>
		public unsafe bool Add(string path)
		{
			try
			{
				fixed (char* cPath = path)
					PInvoke.SHAddToRecentDocs((uint)SHARD.SHARD_PATHW, cPath);

				return true;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
				return false;
			}
		}

		/// <inheritdoc/>
		public unsafe bool Remove(RecentItem item)
		{
			try
			{
				var bhid = PInvoke.BHID_SFUIObject;
				var contextMenuIid = typeof(IContextMenu).GUID;
				using ComPtr<IContextMenu> pContextMenu = default;
				HRESULT hr = item.ShellItem.Get()->BindToHandler(null, &bhid, &contextMenuIid, (void**)pContextMenu.GetAddressOf());
				HMENU hMenu = PInvoke.CreatePopupMenu();
				hr = pContextMenu.Get()->QueryContextMenu(hMenu, 0, 1, 0x7FFF, PInvoke.CMF_OPTIMIZEFORINVOKE);

				// Initialize invocation info
				CMINVOKECOMMANDINFO cmi = default;
				cmi.cbSize = (uint)sizeof(CMINVOKECOMMANDINFO);
				cmi.nShow = (int)SHOW_WINDOW_CMD.SW_HIDE;

				// Unpin the item
				fixed (byte* pVerb1 = Encoding.ASCII.GetBytes("remove"),
					pVerb2 = Encoding.ASCII.GetBytes("unpinfromhome"),
					pVerb3 = Encoding.ASCII.GetBytes("removefromhome"))
				{
					// Try unpin files
					cmi.lpVerb = new(pVerb1);
					hr = pContextMenu.Get()->InvokeCommand(cmi);
					if (hr == HRESULT.S_OK)
						return true;

					// Try unpin folders
					cmi.lpVerb = new(pVerb2);
					hr = pContextMenu.Get()->InvokeCommand(cmi);
					if (hr == HRESULT.S_OK)
						return true;

					// NOTE:
					//  There seems to be an issue with unpinfromhome where some shell folders
					//  won't be removed via unpinfromhome verb.
					// Try unpin folders again
					cmi.lpVerb = new(pVerb3);
					hr = pContextMenu.Get()->InvokeCommand(cmi);
					if (hr == HRESULT.S_OK)
						return true;
				}

				return true;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
				return false;
			}
		}

		/// <inheritdoc/>
		public unsafe bool Clear()
		{
			try
			{
				PInvoke.SHAddToRecentDocs((uint)SHARD.SHARD_PIDL, null);

				return true;
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
				return false;
			}
		}

		private unsafe bool UpdateRecentItems(bool isFolder)
		{
			try
			{
				HRESULT hr = default;

				string szFolderShellPath =
					isFolder
						? "Shell:::{22877A6D-37A1-461A-91B0-DBDA5AAEBC99}"  // Recent Places folder (recent folders)
						: "Shell:::{679F85CB-0220-4080-B29B-5540CC05AAB6}"; // Quick Access folder (recent files)

				// Get IShellItem of the shell folder
				var shellItemIid = typeof(IShellItem).GUID;
				using ComPtr<IShellItem> pFolderShellItem = default;
				fixed (char* pszFolderShellPath = szFolderShellPath)
					hr = PInvoke.SHCreateItemFromParsingName(pszFolderShellPath, null, &shellItemIid, (void**)pFolderShellItem.GetAddressOf());

				// Get IEnumShellItems of the quick access shell folder
				var enumItemsBHID = PInvoke.BHID_EnumItems;
				Guid enumShellItemIid = typeof(IEnumShellItems).GUID;
				using ComPtr<IEnumShellItems> pEnumShellItems = default;
				hr = pFolderShellItem.Get()->BindToHandler(null, &enumItemsBHID, &enumShellItemIid, (void**)pEnumShellItems.GetAddressOf());

				// Enumerate recent items and populate the list
				int index = 0;
				List<RecentItem> recentItems = [];
				ComPtr<IShellItem> pShellItem = default; // Do not dispose in this method to use later to prepare for its deletion
				while (pEnumShellItems.Get()->Next(1, pShellItem.GetAddressOf()) == HRESULT.S_OK)
				{
					// Get top 20 items
					if (index is 20)
						break;

					// Exclude folders
					if (pShellItem.Get()->GetAttributes(SFGAO_FLAGS.SFGAO_FOLDER, out var attribute) == HRESULT.S_OK &&
						(attribute & SFGAO_FLAGS.SFGAO_FOLDER) == SFGAO_FLAGS.SFGAO_FOLDER)
						continue;

					// Get the target path
					pShellItem.Get()->GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEEDITING, out var szDisplayName);
					var targetPath = szDisplayName.ToString();
					PInvoke.CoTaskMemFree(szDisplayName.Value);

					// Get the display name
					pShellItem.Get()->GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY, out szDisplayName);
					var fileName = szDisplayName.ToString();
					PInvoke.CoTaskMemFree(szDisplayName.Value);

					// Strip the file extension except when the file name only contains extension (e.g. ".gitignore")
					if (!FoldersSettingsService.ShowFileExtensions &&
						SystemIO.Path.GetFileNameWithoutExtension(fileName) is string fileNameWithoutExtension)
						fileName = string.IsNullOrEmpty(fileNameWithoutExtension) ? SystemIO.Path.GetFileName(fileName) : fileNameWithoutExtension;

					// Get the date last modified
					var shellItem2Iid = typeof(IShellItem2).GUID;
					using ComPtr<IShellItem2> pShellItem2 = default;
					hr = pShellItem.Get()->QueryInterface(&shellItem2Iid, (void**)pShellItem2.GetAddressOf());
					hr = PInvoke.PSGetPropertyKeyFromName("System.DateModified", out var propertyKey);
					hr = pShellItem2.Get()->GetString(propertyKey, out var szPropertyValue);
					if (DateTime.TryParse(szPropertyValue.ToString(), out var lastModified))
						lastModified = DateTime.MinValue;

					recentItems.Add(new()
					{
						Path = targetPath,
						Name = fileName,
						ShellItem = pShellItem,
						LastModified = lastModified,
					});

					index++;
				}

				if (recentItems.Count is 0)
					return false;

				var snapshot = isFolder ? RecentFolders : RecentFiles;

				if (isFolder)
				{
					lock (_RecentFolders)
					{
						_RecentFolders.Clear();
						_RecentFolders.AddRange(recentItems);
					}
				}
				else
				{
					lock (_RecentFiles)
					{
						_RecentFiles.Clear();
						_RecentFiles.AddRange(recentItems);
					}
				}

				var eventArgs = GetChangedActionEventArgs(snapshot, recentItems);

				if (isFolder)
 					RecentFoldersChanged?.Invoke(this, eventArgs);
				else
 					RecentFilesChanged?.Invoke(this, eventArgs);

				return true;
			}
			catch
			{
				return false;
			}
		}

		private void Watcher_Changed(object sender, SystemIO.FileSystemEventArgs e)
		{
			_ = UpdateRecentFilesAsync();
			_ = UpdateRecentFoldersAsync();
		}

		private NotifyCollectionChangedEventArgs GetChangedActionEventArgs(IReadOnlyList<RecentItem> oldItems, IList<RecentItem> newItems)
		{
			if (newItems.Count - oldItems.Count is 1)
			{
				var differences = newItems.Except(oldItems);
				if (differences.Take(2).Count() == 1)
					return new(NotifyCollectionChangedAction.Add, newItems.First());
			}
			else if (oldItems.Count - newItems.Count is 1)
			{
				var differences = oldItems.Except(newItems);
				if (differences.Take(2).Count() == 1)
				{
					for (int i = 0; i < oldItems.Count; i++)
					{
						if (i >= newItems.Count || !newItems[i].Equals(oldItems[i]))
							return new(NotifyCollectionChangedAction.Remove, oldItems[i], index: i);
					}
				}
			}
			else if (newItems.Count == oldItems.Count)
			{
				var differences = oldItems.Except(newItems);
				if (differences.Any())
					return new(NotifyCollectionChangedAction.Reset);

				// First diff from reversed is the designated item
				for (int i = oldItems.Count - 1; i >= 0; i--)
				{
					if (!oldItems[i].Equals(newItems[i]))
						return new(NotifyCollectionChangedAction.Move, oldItems[i], index: 0, oldIndex: i);
				}
			}

			return new(NotifyCollectionChangedAction.Reset);
		}
	}
}
