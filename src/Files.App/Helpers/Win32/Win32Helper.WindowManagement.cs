// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for Win32.
	/// </summary>
	public static partial class Win32Helper
	{
		/// <summary>
		/// Brings the app window to foreground.
		/// </summary>
		/// <remarks>
		/// For more information, visit
		/// <br/>
		/// - <a href="https://stackoverflow.com/questions/1544179/what-are-the-differences-between-bringwindowtotop-setforegroundwindow-setwindo" />
		/// <br/>
		/// - <a href="https://stackoverflow.com/questions/916259/win32-bring-a-window-to-top" />
		/// </remarks>
		/// <param name="hWnd">The window handle to bring.</param>
		public static unsafe void BringToForegroundEx(Windows.Win32.Foundation.HWND hWnd)
		{
			var hCurWnd = PInvoke.GetForegroundWindow();
			var dwMyID = PInvoke.GetCurrentThreadId();
			var dwCurID = PInvoke.GetWindowThreadProcessId(hCurWnd);

			PInvoke.AttachThreadInput(dwCurID, dwMyID, true);

			PInvoke.SetWindowPos(hWnd, (Windows.Win32.Foundation.HWND)(-1), 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
			PInvoke.SetWindowPos(hWnd, (Windows.Win32.Foundation.HWND)(-2), 0, 0, 0, 0, SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
			PInvoke.SetForegroundWindow(hWnd);
			PInvoke.SetFocus(hWnd);
			PInvoke.SetActiveWindow(hWnd);
			PInvoke.AttachThreadInput(dwCurID, dwMyID, false);
		}

		/// <summary>
		/// Sets cursor when hovering on a specific element.
		/// </summary>
		/// <param name="uiElement">An element to be changed.</param>
		/// <param name="cursor">Cursor to change.</param>
		public static void ChangeCursor(this UIElement uiElement, InputCursor cursor)
		{
			Type type = typeof(UIElement);

			type.InvokeMember(
				"ProtectedCursor",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance,
				null,
				uiElement,
				[cursor]
			);
		}

		/// <summary>
		/// Force window to stay at bottom of other upper windows.
		/// </summary>
		/// <param name="lParam">The lParam of the message.</param>
		public static void ForceWindowPosition(nint lParam)
		{
			var windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
			windowPos.flags |= SET_WINDOW_POS_FLAGS.SWP_NOZORDER;
			Marshal.StructureToPtr(windowPos, lParam, false);
		}
	}
}
