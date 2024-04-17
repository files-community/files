﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using static Files.App.Helpers.NativeFindStorageItemHelper;

namespace Files.App.Extensions
{
	public static class Win32FindDataExtensions
	{
		private const long MAX_DWORD = 4294967295;

		public static long GetSize(this WIN32_FIND_DATA findData)
		{
			long fDataFSize = findData.nFileSizeLow;

			return
				fDataFSize +
				(fDataFSize < 0 ? MAX_DWORD + 1 : 0) +
				(findData.nFileSizeHigh > 0 ? findData.nFileSizeHigh * (MAX_DWORD + 1) : 0);
		}
	}
}
