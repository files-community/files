// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Drawing;
using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Provides a helper for Win32 context menu.
	/// </summary>
	public class ContextMenu : Win32ContextMenu, IDisposable
	{
		private Shell32.IContextMenu _cMenu;
		
		private User32.SafeHMENU _hMenu;
		
		private readonly ThreadWithMessageQueue _owningThread;

		private readonly Func<string, bool>? _itemFilter;

		private readonly Dictionary<List<Win32ContextMenuItem>, Action> _loadSubMenuActions;

		// To detect redundant calls
		private bool disposedValue = false;

		public List<string> ItemsPath { get; }

		private ContextMenu(Shell32.IContextMenu cMenu, User32.SafeHMENU hMenu, IEnumerable<string> itemsPath, ThreadWithMessageQueue owningThread, Func<string, bool>? itemFilter)
		{
			_cMenu = cMenu;
			_hMenu = hMenu;
			_owningThread = owningThread;
			_itemFilter = itemFilter;
			_loadSubMenuActions = [];

			ItemsPath = itemsPath.ToList();
			Items = [];
		}

		public async static Task<bool> InvokeVerb(string verb, params string[] filePaths)
		{
			using var cMenu = await GetContextMenuForFiles(filePaths, PInvoke.CMF_DEFAULTONLY);

			return cMenu is not null && await cMenu.InvokeVerb(verb);
		}

		public async Task<bool> InvokeVerb(string? verb)
		{
			if (string.IsNullOrEmpty(verb))
				return false;

			var item = Items.FirstOrDefault(x => x.CommandString == verb);
			if (item is not null && item.ID >= 0)
				// Prefer invocation by ID
				return await InvokeItem(item.ID);

			try
			{
				var currentWindows = Win32Helper.GetDesktopWindows();

				var pici = new Shell32.CMINVOKECOMMANDINFOEX
				{
					lpVerb = new SafeResourceId(verb, CharSet.Ansi),
					nShow = ShowWindowCommand.SW_SHOWNORMAL,
				};

				pici.cbSize = (uint)Marshal.SizeOf(pici);

				await _owningThread.PostMethod(() => _cMenu.InvokeCommand(pici));
				Win32Helper.BringToForeground(currentWindows);

				return true;
			}
			catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
			}

			return false;
		}

		public async Task<bool> InvokeItem(int itemID)
		{
			if (itemID < 0)
				return false;

			try
			{
				var currentWindows = Win32Helper.GetDesktopWindows();
				var pici = new Shell32.CMINVOKECOMMANDINFOEX
				{
					lpVerb = Macros.MAKEINTRESOURCE(itemID),
					nShow = ShowWindowCommand.SW_SHOWNORMAL,
				};

				pici.cbSize = (uint)Marshal.SizeOf(pici);

				await _owningThread.PostMethod(() => _cMenu.InvokeCommand(pici));
				Win32Helper.BringToForeground(currentWindows);

				return true;
			}
			catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
			{
				Debug.WriteLine(ex);
			}

			return false;
		}

		public async static Task<ContextMenu?> GetContextMenuForFiles(string[] filePathList, uint flags, Func<string, bool>? itemFilter = null)
		{
			var owningThread = new ThreadWithMessageQueue();

			return await owningThread.PostMethod<ContextMenu>(() =>
			{
				var shellItems = new List<ShellItem>();

				try
				{
					foreach (var filePathItem in filePathList.Where(x => !string.IsNullOrEmpty(x)))
						shellItems.Add(ShellFolderExtensions.GetShellItemFromPathOrPIDL(filePathItem));

					return GetContextMenuForFiles([.. shellItems], flags, owningThread, itemFilter);
				}
				catch
				{
					// Return empty context menu
					return null;
				}
				finally
				{
					foreach (var item in shellItems)
						item.Dispose();
				}
			});
		}

		public async static Task<ContextMenu?> GetContextMenuForFiles(ShellItem[] shellItems, uint flags, Func<string, bool>? itemFilter = null)
		{
			var owningThread = new ThreadWithMessageQueue();

			return await owningThread.PostMethod<ContextMenu>(() => GetContextMenuForFiles(shellItems, flags, owningThread, itemFilter));
		}

		private static ContextMenu? GetContextMenuForFiles(ShellItem[] shellItems, uint flags, ThreadWithMessageQueue owningThread, Func<string, bool>? itemFilter = null)
		{
			if (!shellItems.Any())
				return null;

			try
			{
				// NOTE: The items are all in the same folder
				using var sf = shellItems[0].Parent;

				Shell32.IContextMenu menu = sf.GetChildrenUIObjects<Shell32.IContextMenu>(default, shellItems);
				var hMenu = User32.CreatePopupMenu();
				menu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, (Shell32.CMF)flags);
				var contextMenu = new ContextMenu(menu, hMenu, shellItems.Select(x => x.ParsingName), owningThread, itemFilter);
				contextMenu.EnumMenuItems(hMenu, contextMenu.Items);

				return contextMenu;
			}
			catch (COMException)
			{
				// Return empty context menu
				return null;
			}
		}

		public static async Task WarmUpQueryContextMenuAsync()
		{
			using var cMenu = await GetContextMenuForFiles(new string[] { $@"{Constants.UserEnvironmentPaths.SystemDrivePath}\" }, PInvoke.CMF_NORMAL);
		}

		private void EnumMenuItems(Vanara.PInvoke.HMENU hMenu, List<Win32ContextMenuItem> menuItemsResult, bool loadSubenus = false)
		{
			var itemCount = User32.GetMenuItemCount(hMenu);

			var menuItemInfo = new User32.MENUITEMINFO()
			{
				fMask =
					User32.MenuItemInfoMask.MIIM_BITMAP |
					User32.MenuItemInfoMask.MIIM_FTYPE |
					User32.MenuItemInfoMask.MIIM_STRING |
					User32.MenuItemInfoMask.MIIM_ID |
					User32.MenuItemInfoMask.MIIM_SUBMENU,
			};

			menuItemInfo.cbSize = (uint)Marshal.SizeOf(menuItemInfo);

			for (uint index = 0; index < itemCount; index++)
			{
				var menuItem = new ContextMenuItem();
				var container = new SafeCoTaskMemString(512);
				var cMenu2 = _cMenu as Shell32.IContextMenu2;

				menuItemInfo.dwTypeData = (IntPtr)container;

				// See also, https://devblogs.microsoft.com/oldnewthing/20040928-00/?p=37723
				menuItemInfo.cch = (uint)container.Capacity - 1;

				var result = User32.GetMenuItemInfo(hMenu, index, true, ref menuItemInfo);
				if (!result)
				{
					container.Dispose();
					continue;
				}

				menuItem.Type = (MENU_ITEM_TYPE)menuItemInfo.fType;

				// wID - idCmdFirst
				menuItem.ID = (int)(menuItemInfo.wID - 1);

				if (menuItem.Type == MENU_ITEM_TYPE.MFT_STRING)
				{
					Debug.WriteLine("Item {0} ({1}): {2}", index, menuItemInfo.wID, menuItemInfo.dwTypeData);

					menuItem.Label = menuItemInfo.dwTypeData;
					menuItem.CommandString = GetCommandString(_cMenu, menuItemInfo.wID - 1);

					if (_itemFilter is not null && (_itemFilter(menuItem.CommandString) || _itemFilter(menuItem.Label)))
					{
						// Skip items implemented in UWP
						container.Dispose();
						continue;
					}

					if (menuItemInfo.hbmpItem != HBITMAP.NULL && !Enum.IsDefined(typeof(HBITMAP_HMENU), ((IntPtr)menuItemInfo.hbmpItem).ToInt64()))
					{
						using var bitmap = Win32Helper.GetBitmapFromHBitmap(menuItemInfo.hbmpItem);

						if (bitmap is not null)
						{
							// Make the icon background transparent
							bitmap.MakeTransparent();

							byte[] bitmapData = (byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[]));
							menuItem.Icon = bitmapData;
						}
					}

					if (menuItemInfo.hSubMenu != Vanara.PInvoke.HMENU.NULL)
					{
						Debug.WriteLine("Item {0}: has submenu", index);
						var subItems = new List<Win32ContextMenuItem>();
						var hSubMenu = menuItemInfo.hSubMenu;

						if (loadSubenus)
							LoadSubMenu();
						else
							_loadSubMenuActions.Add(subItems, LoadSubMenu);

						menuItem.SubItems = subItems;

						Debug.WriteLine("Item {0}: done submenu", index);

						void LoadSubMenu()
						{
							try
							{
								cMenu2?.HandleMenuMsg((uint)User32.WindowMessage.WM_INITMENUPOPUP, (IntPtr)hSubMenu, new IntPtr(index));
							}
							catch (Exception ex) when (ex is InvalidCastException or ArgumentException)
							{
								// TODO: Investigate why this exception happen
								Debug.WriteLine(ex);
							}
							catch (Exception ex) when (ex is COMException or NotImplementedException)
							{
								// Only for dynamic/owner drawn? (open with, etc)
							}

							EnumMenuItems(hSubMenu, subItems, true);
						}
					}
				}
				else
				{
					Debug.WriteLine("Item {0}: {1}", index, menuItemInfo.fType.ToString());
				}

				container.Dispose();
				menuItemsResult.Add(menuItem);
			}
		}

		public Task<bool> LoadSubMenu(List<Win32ContextMenuItem> subItems)
		{
			if (_loadSubMenuActions.Remove(subItems, out var loadSubMenuAction))
			{
				return _owningThread.PostMethod<bool>(() =>
				{
					try
					{
						loadSubMenuAction!();
						return true;
					}
					catch
					{
						return false;
					}
				});
			}
			else
			{
				return Task.FromResult(false);
			}
		}

		private static string? GetCommandString(Shell32.IContextMenu cMenu, uint offset, Shell32.GCS flags = Shell32.GCS.GCS_VERBW)
		{
			// A workaround to avoid an AccessViolationException on some items,
			// notably the "Run with graphic processor" menu item of NVIDIA cards
			if (offset > 5000)
			{
				return null;
			}

			SafeCoTaskMemString? commandString = null;

			try
			{
				commandString = new SafeCoTaskMemString(512);
				cMenu.GetCommandString(new IntPtr(offset), flags, IntPtr.Zero, commandString, (uint)commandString.Capacity - 1);
				Debug.WriteLine("Verb {0}: {1}", offset, commandString);

				return commandString.ToString();
			}
			catch (Exception ex) when (ex is InvalidCastException or ArgumentException)
			{
				// TODO: Investigate why this exception happen
				Debug.WriteLine(ex);

				return null;
			}
			catch (Exception ex) when (ex is COMException or NotImplementedException)
			{
				// Not every item has an associated verb
				return null;
			}
			finally
			{
				commandString?.Dispose();
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: Dispose managed state (managed objects)
					if (Items is not null)
					{
						foreach (var si in Items)
						{
							(si as IDisposable)?.Dispose();
						}

						Items = null;
					}
				}

				// TODO: Free unmanaged resources (unmanaged objects) and override a finalizer below
				if (_hMenu is not null)
				{
					User32.DestroyMenu(_hMenu);
					_hMenu = null;
				}
				if (_cMenu is not null)
				{
					Marshal.ReleaseComObject(_cMenu);
					_cMenu = null;
				}

				_owningThread.Dispose();

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~ContextMenu()
		{
			Dispose(false);
		}
	}
}
