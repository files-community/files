// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32
{
	namespace Graphics.Gdi
	{
		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		public unsafe delegate BOOL MONITORENUMPROC([In] HMONITOR param0, [In] HDC param1, [In][Out] RECT* param2, [In] LPARAM param3);
	}

	namespace UI.WindowsAndMessaging
	{
		[UnmanagedFunctionPointer(CallingConvention.Winapi)]
		public delegate LRESULT WNDPROC(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam);
	}

	public static partial class PInvoke
	{
		public static nint SetWindowLongPlat(HWND hWnd, UI.WindowsAndMessaging.WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong)
		{
#if x86
			return SetWindowLong(hWnd, nIndex, (int)dwNewLong);
#else
			return SetWindowLongPtr(hWnd, nIndex, dwNewLong);
#endif
		}
	}
}
