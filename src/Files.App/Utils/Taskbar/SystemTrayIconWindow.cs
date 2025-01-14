// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Taskbar
{
	/// <summary>
	/// Represents an icon window of Notification Area so-called System Tray.
	/// <br/>
	/// This is provided to handle context menu and retrieve mouse events, not a regular window class.
	/// </summary>
	public sealed class SystemTrayIconWindow : IDisposable
	{
		private SystemTrayIcon _trayIcon;

		private readonly WNDPROC _windowProcedure;

		private HWND _windowHandle;
		internal HWND WindowHandle
			=> _windowHandle;

		public unsafe SystemTrayIconWindow(SystemTrayIcon icon)
		{
			_windowProcedure = WindowProc;
			_trayIcon = icon;
			string text = "FilesTrayIcon_" + _trayIcon.Id;

			fixed (char* ptr = text)
			{
				var pWindProc = Marshal.GetFunctionPointerForDelegate(_windowProcedure);
				var pfnWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)pWindProc;

				WNDCLASSEXW param = new()
				{
					cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEXW)),
					style = WNDCLASS_STYLES.CS_DBLCLKS,
					lpfnWndProc = pfnWndProc,
					cbClsExtra = 0,
					cbWndExtra = 0,
					hInstance = PInvoke.GetModuleHandle(default(PCWSTR)),
					lpszClassName = ptr
				};

				PInvoke.RegisterClassEx(in param);
			}

			_windowHandle = PInvoke.CreateWindowEx(
				WINDOW_EX_STYLE.WS_EX_LEFT,
				text,
				string.Empty,
				WINDOW_STYLE.WS_OVERLAPPED,
				0,
				0,
				1,
				1,
				default,
				null,
				null,
				null);

			if (_windowHandle == default)
				throw new Win32Exception("ERR: Message window handle was not a valid pointer.");
		}

		private LRESULT WindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
		{
			return _trayIcon.WindowProc(hWnd, uMsg, wParam, lParam);
		}

		public void Dispose()
		{
			if (_windowHandle != default)
			{
				PInvoke.DestroyWindow(_windowHandle);
				_windowHandle = default;
			}

			_trayIcon = null;
		}
	}
}
