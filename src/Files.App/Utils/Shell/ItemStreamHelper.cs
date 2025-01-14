// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32.System.Com;

namespace Files.App.Utils.Shell
{
	public static class ItemStreamHelper
	{
		static readonly Guid IShellItemIid = Guid.ParseExact("43826d1e-e718-42ee-bc55-a1e261c37bfe", "d");

		public static IntPtr IShellItemFromPath(string path)
		{
			IntPtr psi;
			Guid iid = IShellItemIid;
			var hr = Win32PInvoke.SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out psi);
			if ((int)hr < 0)
				return IntPtr.Zero;
			return psi;
		}

		public static IntPtr IStreamFromPath(string path)
		{
			var hr = Win32PInvoke.SHCreateStreamOnFileEx(
				path,
				STGM.STGM_READ | STGM.STGM_FAILIFTHERE | STGM.STGM_SHARE_DENY_NONE,
				0, 0,
				IntPtr.Zero, out var pstm);

			if ((int)hr < 0)
				return IntPtr.Zero;

			return pstm;
		}

		public static void ReleaseObject(IntPtr obj)
		{
			Marshal.Release(obj);
		}
	}
}
