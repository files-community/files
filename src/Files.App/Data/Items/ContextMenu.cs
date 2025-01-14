// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Data.Items
{
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

	public class Win32ContextMenu
	{
		public List<Win32ContextMenuItem> Items { get; set; }
	}

	public class Win32ContextMenuItem
	{
		public byte[] Icon { get; set; }
		public int ID { get; set; } // Valid only in current menu to invoke item
		public string Label { get; set; }
		public string CommandString { get; set; }
		public MENU_ITEM_TYPE Type { get; set; }
		public List<Win32ContextMenuItem> SubItems { get; set; }
	}
}
