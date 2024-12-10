// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Helpers
{
	public static partial class Win32PInvoke
	{
		public static readonly Guid DataTransferManagerInteropIID =
			new(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

		public const int RmRebootReasonNone = 0;
		public const int CCH_RM_MAX_APP_NAME = 255;
		public const int CCH_RM_MAX_SVC_NAME = 63;

		public const int FILE_NOTIFY_CHANGE_FILE_NAME = 1;
		public const int FILE_NOTIFY_CHANGE_DIR_NAME = 2;
		public const int FILE_NOTIFY_CHANGE_ATTRIBUTES = 4;
		public const int FILE_NOTIFY_CHANGE_SIZE = 8;
		public const int FILE_NOTIFY_CHANGE_LAST_WRITE = 16;
		public const int FILE_NOTIFY_CHANGE_LAST_ACCESS = 32;
		public const int FILE_NOTIFY_CHANGE_CREATION = 64;
		public const int FILE_NOTIFY_CHANGE_SECURITY = 256;

		public const int INVALID_HANDLE_VALUE = -1;
		public const int FILE_SHARE_READ = 0x00000001;
		public const int FILE_SHARE_WRITE = 0x00000002;
		public const uint FILE_SHARE_DELETE = 0x00000004;
		public const int OPEN_EXISTING = 3;
		public const int IOCTL_STORAGE_EJECT_MEDIA = 0x2D4808;
		public const int IOCTL_STORAGE_MEDIA_REMOVAL = 0x002D4804;

		public const uint FILE_APPEND_DATA = 0x0004;


		public const uint FILE_BEGIN = 0;
		public const uint FILE_END = 2;

		public const uint CREATE_ALWAYS = 2;
		public const uint CREATE_NEW = 1;
		public const uint OPEN_ALWAYS = 4;
		public const uint TRUNCATE_EXISTING = 5;

		// FSCTL
		public const int FSCTL_GET_REPARSE_POINT = 0x000900A8;

		public const int MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16 * 1024;
		public const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
		public const uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;

		public const int FIND_FIRST_EX_CASE_SENSITIVE = 1;
		public const int FIND_FIRST_EX_LARGE_FETCH = 2;

		public const int NORM_IGNORECASE = 0x00000001;
		public const int NORM_IGNORENONSPACE = 0x00000002;
		public const int NORM_IGNORESYMBOLS = 0x00000004;
		public const int LINGUISTIC_IGNORECASE = 0x00000010;
		public const int LINGUISTIC_IGNOREDIACRITIC = 0x00000020;
		public const int NORM_IGNOREKANATYPE = 0x00010000;
		public const int NORM_IGNOREWIDTH = 0x00020000;
		public const int NORM_LINGUISTIC_CASING = 0x08000000;
		public const int SORT_STRINGSORT = 0x00001000;
		public const int SORT_DIGITSASNUMBERS = 0x00000008;

		public const string LOCALE_NAME_USER_DEFAULT = null;
		public const string LOCALE_NAME_INVARIANT = "";
		public const string LOCALE_NAME_SYSTEM_DEFAULT = "!sys-default-locale";
	}
}
