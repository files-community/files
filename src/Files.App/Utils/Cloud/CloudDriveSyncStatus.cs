// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Cloud
{
	public enum CloudDriveSyncStatus
	{
		Unknown = -1,

		FolderOnline = 0,

		FolderOfflinePartial = 1,

		FolderOfflineFull = 2,

		FolderOfflinePinned = 3,

		FolderExcluded = 4,

		FolderEmpty = 5,

		NotSynced = 6,

		FileOnline = 8,

		FileSync = 9,

		FileOffline = 14,

		FileOfflinePinned = 15,
	}
}
