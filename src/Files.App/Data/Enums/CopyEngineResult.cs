// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	public struct CopyEngineResult
	{
		// https://github.com/RickStrahl/DeleteFiles/blob/master/DeleteFiles/ZetaLongPaths/Native/FileOperations/Interop/CopyEngineResult.cs
		// Ok
		public const int S_OK = 0;
		public const int COPYENGINE_S_DONT_PROCESS_CHILDREN = 2555912;
		public const int COPYENGINE_E_USER_CANCELLED = -2144927744;
		// Access denied
		public const int COPYENGINE_E_ACCESS_DENIED_SRC = -2144927711;
		public const int COPYENGINE_E_ACCESS_DENIED_DEST = -2144927710;
		public const int COPYENGINE_E_REQUIRES_ELEVATION = -2144927742;
		// Path too long
		public const int COPYENGINE_E_PATH_TOO_DEEP_SRC = -2144927715;
		public const int COPYENGINE_E_PATH_TOO_DEEP_DEST = -2144927714;
		public const int COPYENGINE_E_RECYCLE_PATH_TOO_LONG = -2144927688;
		public const int COPYENGINE_E_NEWFILE_NAME_TOO_LONG = -2144927685;
		public const int COPYENGINE_E_NEWFOLDER_NAME_TOO_LONG = -2144927684;
		// Not found
		public const int COPYENGINE_E_RECYCLE_BIN_NOT_FOUND = -2144927686;
		public const int COPYENGINE_E_PATH_NOT_FOUND_SRC = -2144927709;
		public const int COPYENGINE_E_PATH_NOT_FOUND_DEST = -2144927708;
		public const int COPYENGINE_E_NET_DISCONNECT_DEST = -2144927706;
		public const int COPYENGINE_E_NET_DISCONNECT_SRC = -2144927707;
		public const int COPYENGINE_E_CANT_REACH_SOURCE = -2144927691;
		// File in use
		public const int COPYENGINE_E_SHARING_VIOLATION_SRC = -2144927705;
		public const int COPYENGINE_E_SHARING_VIOLATION_DEST = -2144927704;
		// Already exists
		public const int COPYENGINE_E_ALREADY_EXISTS_NORMAL = -2144927703;
		public const int COPYENGINE_E_ALREADY_EXISTS_READONLY = -2144927702;
		public const int COPYENGINE_E_ALREADY_EXISTS_SYSTEM = -2144927701;
		public const int COPYENGINE_E_ALREADY_EXISTS_FOLDER = -2144927700;
		// File too big
		public const int COPYENGINE_E_FILE_TOO_LARGE = -2144927731;
		//public const int COPYENGINE_E_REMOVABLE_FULL = -2144927730;
		//public const int COPYENGINE_E_DISK_FULL = -2144927694;
		//public const int COPYENGINE_E_DISK_FULL_CLEAN = -2144927693;
		//public const int COPYENGINE_E_RECYCLE_SIZE_TOO_BIG = -2144927689;
		// Invalid path
		public const int COPYENGINE_E_FILE_IS_FLD_DEST = -2144927732;
		public const int COPYENGINE_E_FLD_IS_FILE_DEST = -2144927733;
		//public const int COPYENGINE_E_INVALID_FILES_SRC = -2144927717;
		//public const int COPYENGINE_E_INVALID_FILES_DEST = -2144927716;
		//public const int COPYENGINE_E_SAME_FILE = -2144927741;
		//public const int COPYENGINE_E_DEST_SAME_TREE = -2144927734;
		//public const int COPYENGINE_E_DEST_SUBTREE = -2144927735;
		// Property loss
		public const int COPYENGINE_E_STREAM_LOSS = -2144927699; // Secondary Stream information would be lost
		public const int COPYENGINE_E_EA_LOSS = -2144927698; // Extended Attributes would be lost
		public const int COPYENGINE_E_PROPERTY_LOSS = -2144927697; // Property would be lost
		public const int COPYENGINE_E_PROPERTIES_LOSS = -2144927696; // Properties would be lost
		public const int COPYENGINE_E_ENCRYPTION_LOSS = -2144927695; // Encryption would be lost

		public static FileSystemStatusCode Convert(int? hres)
		{
			return hres switch
			{
				CopyEngineResult.S_OK => FileSystemStatusCode.Success,
				CopyEngineResult.COPYENGINE_E_ACCESS_DENIED_SRC => FileSystemStatusCode.Unauthorized,
				CopyEngineResult.COPYENGINE_E_ACCESS_DENIED_DEST => FileSystemStatusCode.Unauthorized,
				CopyEngineResult.COPYENGINE_E_REQUIRES_ELEVATION => FileSystemStatusCode.Unauthorized,
				CopyEngineResult.COPYENGINE_E_RECYCLE_PATH_TOO_LONG => FileSystemStatusCode.NameTooLong,
				CopyEngineResult.COPYENGINE_E_NEWFILE_NAME_TOO_LONG => FileSystemStatusCode.NameTooLong,
				CopyEngineResult.COPYENGINE_E_NEWFOLDER_NAME_TOO_LONG => FileSystemStatusCode.NameTooLong,
				CopyEngineResult.COPYENGINE_E_PATH_TOO_DEEP_SRC => FileSystemStatusCode.NameTooLong,
				CopyEngineResult.COPYENGINE_E_PATH_TOO_DEEP_DEST => FileSystemStatusCode.NameTooLong,
				CopyEngineResult.COPYENGINE_E_PATH_NOT_FOUND_SRC => FileSystemStatusCode.NotFound,
				CopyEngineResult.COPYENGINE_E_PATH_NOT_FOUND_DEST => FileSystemStatusCode.NotFound,
				CopyEngineResult.COPYENGINE_E_NET_DISCONNECT_DEST => FileSystemStatusCode.NotFound,
				CopyEngineResult.COPYENGINE_E_NET_DISCONNECT_SRC => FileSystemStatusCode.NotFound,
				CopyEngineResult.COPYENGINE_E_CANT_REACH_SOURCE => FileSystemStatusCode.NotFound,
				CopyEngineResult.COPYENGINE_E_ALREADY_EXISTS_NORMAL => FileSystemStatusCode.AlreadyExists,
				CopyEngineResult.COPYENGINE_E_ALREADY_EXISTS_READONLY => FileSystemStatusCode.AlreadyExists,
				CopyEngineResult.COPYENGINE_E_ALREADY_EXISTS_SYSTEM => FileSystemStatusCode.AlreadyExists,
				CopyEngineResult.COPYENGINE_E_ALREADY_EXISTS_FOLDER => FileSystemStatusCode.AlreadyExists,
				CopyEngineResult.COPYENGINE_E_FILE_TOO_LARGE => FileSystemStatusCode.FileTooLarge,
				CopyEngineResult.COPYENGINE_E_FILE_IS_FLD_DEST => FileSystemStatusCode.NotAFile,
				CopyEngineResult.COPYENGINE_E_FLD_IS_FILE_DEST => FileSystemStatusCode.NotAFolder,
				CopyEngineResult.COPYENGINE_E_SHARING_VIOLATION_SRC => FileSystemStatusCode.InUse,
				CopyEngineResult.COPYENGINE_E_SHARING_VIOLATION_DEST => FileSystemStatusCode.InUse,
				CopyEngineResult.COPYENGINE_E_STREAM_LOSS => FileSystemStatusCode.PropertyLoss,
				CopyEngineResult.COPYENGINE_E_EA_LOSS => FileSystemStatusCode.PropertyLoss,
				CopyEngineResult.COPYENGINE_E_PROPERTY_LOSS => FileSystemStatusCode.PropertyLoss,
				CopyEngineResult.COPYENGINE_E_PROPERTIES_LOSS => FileSystemStatusCode.PropertyLoss,
				CopyEngineResult.COPYENGINE_E_ENCRYPTION_LOSS => FileSystemStatusCode.PropertyLoss,
				null => FileSystemStatusCode.Generic,
				_ => FileSystemStatusCode.Generic
			};
		}
	}
}
