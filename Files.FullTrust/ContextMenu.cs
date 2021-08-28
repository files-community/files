using Files.Common;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace FilesFullTrust
{
    public class ContextMenu : Win32ContextMenu, IDisposable
    {
        private Shell32.IContextMenu cMenu;
        private User32.SafeHMENU hMenu;
        public List<string> ItemsPath { get; }

        public ContextMenu(Shell32.IContextMenu cMenu, User32.SafeHMENU hMenu, IEnumerable<string> itemsPath)
        {
            this.cMenu = cMenu;
            this.hMenu = hMenu;
            this.ItemsPath = itemsPath.ToList();
            this.Items = new List<Win32ContextMenuItem>();
        }

        public bool InvokeVerb(string verb)
        {
            if (string.IsNullOrEmpty(verb))
            {
                return false;
            }

            try
            {
                var currentWindows = Win32API.GetDesktopWindows();
                var pici = new Shell32.CMINVOKECOMMANDINFOEX();
                pici.lpVerb = new SafeResourceId(verb, CharSet.Ansi);
                pici.nShow = ShowWindowCommand.SW_SHOWNORMAL;
                pici.cbSize = (uint)Marshal.SizeOf(pici);
                cMenu.InvokeCommand(pici);
                Win32API.BringToForeground(currentWindows);
                return true;
            }
            catch (Exception ex) when (
                ex is COMException
                || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine(ex);
            }
            return false;
        }

        public void InvokeItem(int itemID)
        {
            if (itemID < 0)
            {
                return;
            }

            try
            {
                var currentWindows = Win32API.GetDesktopWindows();
                var pici = new Shell32.CMINVOKECOMMANDINFOEX();
                pici.lpVerb = Macros.MAKEINTRESOURCE(itemID);
                pici.nShow = ShowWindowCommand.SW_SHOWNORMAL;
                pici.cbSize = (uint)Marshal.SizeOf(pici);
                cMenu.InvokeCommand(pici);
                Win32API.BringToForeground(currentWindows);
            }
            catch (Exception ex) when (
                ex is COMException
                || ex is UnauthorizedAccessException)
            {
                Debug.WriteLine(ex);
            }
        }

        #region FactoryMethods

        public static ContextMenu GetContextMenuForFiles(string[] filePathList, Shell32.CMF flags, Func<string, bool> itemFilter = null)
        {
            List<ShellItem> shellItems = new List<ShellItem>();
            try
            {
                foreach (var fp in filePathList.Where(x => !string.IsNullOrEmpty(x)))
                {
                    shellItems.Add(new ShellItem(fp));
                }

                return GetContextMenuForFiles(shellItems.ToArray(), flags, itemFilter);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is FileNotFoundException)
            {
                // Return empty context menu
                return null;
            }
            finally
            {
                foreach (var si in shellItems)
                {
                    si.Dispose();
                }
            }
        }

        private static ContextMenu GetContextMenuForFiles(ShellItem[] shellItems, Shell32.CMF flags, Func<string, bool> itemFilter = null)
        {
            if (shellItems == null || !shellItems.Any())
            {
                return null;
            }

            using var sf = shellItems.First().Parent; // HP: the items are all in the same folder
            Shell32.IContextMenu menu = sf.GetChildrenUIObjects<Shell32.IContextMenu>(null, shellItems);
            var hMenu = User32.CreatePopupMenu();
            menu.QueryContextMenu(hMenu, 0, 1, 0x7FFF, flags);
            var contextMenu = new ContextMenu(menu, hMenu, shellItems.Select(x => x.ParsingName));
            ContextMenu.EnumMenuItems(menu, hMenu, contextMenu.Items, itemFilter);
            return contextMenu;
        }

        #endregion FactoryMethods

        private static void EnumMenuItems(
            Shell32.IContextMenu cMenu,
            HMENU hMenu,
            List<Win32ContextMenuItem> menuItemsResult,
            Func<string, bool> itemFilter = null)
        {
            var itemCount = User32.GetMenuItemCount(hMenu);
            var mii = new User32.MENUITEMINFO();
            mii.cbSize = (uint)Marshal.SizeOf(mii);
            mii.fMask = User32.MenuItemInfoMask.MIIM_BITMAP
                | User32.MenuItemInfoMask.MIIM_FTYPE
                | User32.MenuItemInfoMask.MIIM_STRING
                | User32.MenuItemInfoMask.MIIM_ID
                | User32.MenuItemInfoMask.MIIM_SUBMENU;
            for (uint ii = 0; ii < itemCount; ii++)
            {
                var menuItem = new ContextMenuItem();
                var container = new SafeCoTaskMemString(512);
                mii.dwTypeData = (IntPtr)container;
                mii.cch = (uint)container.Capacity - 1; // https://devblogs.microsoft.com/oldnewthing/20040928-00/?p=37723
                var retval = User32.GetMenuItemInfo(hMenu, ii, true, ref mii);
                if (!retval)
                {
                    container.Dispose();
                    continue;
                }
                menuItem.Type = (MenuItemType)mii.fType;
                menuItem.ID = (int)(mii.wID - 1); // wID - idCmdFirst
                if (menuItem.Type == MenuItemType.MFT_STRING)
                {
                    Debug.WriteLine("Item {0} ({1}): {2}", ii, mii.wID, mii.dwTypeData);
                    menuItem.Label = mii.dwTypeData;
                    menuItem.CommandString = GetCommandString(cMenu, mii.wID - 1);
                    if (itemFilter != null && (itemFilter(menuItem.CommandString) || itemFilter(menuItem.Label)))
                    {
                        // Skip items implemented in UWP
                        container.Dispose();
                        continue;
                    }
                    if (mii.hbmpItem != HBITMAP.NULL && !Enum.IsDefined(typeof(HBITMAP_HMENU), ((IntPtr)mii.hbmpItem).ToInt64()))
                    {
                        using var bitmap = Win32API.GetBitmapFromHBitmap(mii.hbmpItem);
                        if (bitmap != null)
                        {
                            byte[] bitmapData = (byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[]));
                            menuItem.IconBase64 = Convert.ToBase64String(bitmapData, 0, bitmapData.Length);
                        }
                    }
                    if (mii.hSubMenu != HMENU.NULL)
                    {
                        Debug.WriteLine("Item {0}: has submenu", ii);
                        var subItems = new List<Win32ContextMenuItem>();
                        try
                        {
                            (cMenu as Shell32.IContextMenu2)?.HandleMenuMsg((uint)User32.WindowMessage.WM_INITMENUPOPUP, (IntPtr)mii.hSubMenu, new IntPtr(ii));
                        }
                        catch (NotImplementedException)
                        {
                            // Only for dynamic/owner drawn? (open with, etc)
                        }
                        EnumMenuItems(cMenu, mii.hSubMenu, subItems, itemFilter);
                        menuItem.SubItems = subItems;
                        Debug.WriteLine("Item {0}: done submenu", ii);
                    }
                }
                else
                {
                    Debug.WriteLine("Item {0}: {1}", ii, mii.fType.ToString());
                }
                container.Dispose();
                menuItemsResult.Add(menuItem);
            }
        }

        private static string GetCommandString(Shell32.IContextMenu cMenu, uint offset, Shell32.GCS flags = Shell32.GCS.GCS_VERBW)
        {
            if (offset > 5000)
            {
                // Hackish workaround to avoid an AccessViolationException on some items,
                // notably the "Run with graphic processor" menu item of NVidia cards
                return null;
            }
            SafeCoTaskMemString commandString = null;
            try
            {
                commandString = new SafeCoTaskMemString(512);
                cMenu.GetCommandString(new IntPtr(offset), flags, IntPtr.Zero, commandString, (uint)commandString.Capacity - 1);
                Debug.WriteLine("Verb {0}: {1}", offset, commandString);
                return commandString.ToString();
            }
            catch (Exception ex) when (ex is InvalidCastException || ex is ArgumentException)
            {
                // TODO: investigate this..
                Debug.WriteLine(ex);
                return null;
            }
            catch (Exception ex) when (ex is COMException || ex is NotImplementedException)
            {
                // Not every item has an associated verb
                return null;
            }
            finally
            {
                commandString?.Dispose();
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (Items != null)
                    {
                        foreach (var si in Items)
                        {
                            (si as IDisposable)?.Dispose();
                        }

                        Items = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                if (hMenu != null)
                {
                    User32.DestroyMenu(hMenu);
                    hMenu = null;
                }
                if (cMenu != null)
                {
                    Marshal.ReleaseComObject(cMenu);
                    cMenu = null;
                }

                disposedValue = true;
            }
        }

        ~ContextMenu()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support

        public enum HBITMAP_HMENU : long
        {
            HBMMENU_CALLBACK = -1,
            HBMMENU_MBAR_CLOSE = 5,
            HBMMENU_MBAR_CLOSE_D = 6,
            HBMMENU_MBAR_MINIMIZE = 3,
            HBMMENU_MBAR_MINIMIZE_D = 7,
            HBMMENU_MBAR_RESTORE = 2,
            HBMMENU_POPUP_CLOSE = 8,
            HBMMENU_POPUP_MAXIMIZE = 10,
            HBMMENU_POPUP_MINIMIZE = 11,
            HBMMENU_POPUP_RESTORE = 9,
            HBMMENU_SYSTEM = 1
        }
    }

    public class ContextMenuItem : Win32ContextMenuItem, IDisposable
    {
        public ContextMenuItem()
        {
            this.SubItems = new List<Win32ContextMenuItem>();
        }

        public void Dispose()
        {
            if (SubItems != null)
            {
                foreach (var si in SubItems)
                {
                    (si as IDisposable)?.Dispose();
                }

                SubItems = null;
            }
        }
    }
}