// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Cloud;
using Microsoft.UI.Xaml;

namespace Files.App.Utils.Cloud
{
	public sealed class CloudDriveSyncStatusUI : ObservableObject
	{
		public string Glyph { get; }

		public Style ThemedIconStyle { get; }

		public CloudDriveSyncStatus SyncStatus { get; }

		public bool LoadSyncStatus { get; }

		public string SyncStatusString { get; } = "CloudDriveSyncStatus_Unknown".GetLocalizedResource();

		public CloudDriveSyncStatusUI()
		{
			SyncStatus = CloudDriveSyncStatus.Unknown;
		}

		private CloudDriveSyncStatusUI(CloudDriveSyncStatus syncStatus)
		{
			SyncStatus = syncStatus;
		}

		private CloudDriveSyncStatusUI(string glyph, Style themedIconStyle, CloudDriveSyncStatus syncStatus, string SyncStatusStringKey)
		{
			SyncStatus = syncStatus;
			Glyph = glyph;
			ThemedIconStyle = themedIconStyle;
			LoadSyncStatus = true;
			SyncStatusString = SyncStatusStringKey.GetLocalizedResource();
		}

		public static CloudDriveSyncStatusUI FromCloudDriveSyncStatus(CloudDriveSyncStatus syncStatus) => syncStatus switch
		{
			// File
			CloudDriveSyncStatus.FileOnline
				=> new CloudDriveSyncStatusUI("\uE753", (Style)Application.Current.Resources["App.ThemedIcons.Status.Cloud"], syncStatus, "CloudDriveSyncStatus_Online"),
			CloudDriveSyncStatus.FileOffline
				=> new CloudDriveSyncStatusUI("\uE73E", (Style)Application.Current.Resources["App.ThemedIcons.Status.Available"], syncStatus, "CloudDriveSyncStatus_Offline"),
			CloudDriveSyncStatus.FileOfflinePinned
				=> new CloudDriveSyncStatusUI("\uE73E", (Style)Application.Current.Resources["App.ThemedIcons.Status.KeepOffline"], syncStatus, "CloudDriveSyncStatus_Offline"),
			CloudDriveSyncStatus.FileSync
				=> new CloudDriveSyncStatusUI("\uE895", (Style)Application.Current.Resources["App.ThemedIcons.Status.Syncing"], syncStatus, "CloudDriveSyncStatus_Sync"),

			//// Folder
			CloudDriveSyncStatus.FolderOnline or CloudDriveSyncStatus.FolderOfflinePartial
				=> new CloudDriveSyncStatusUI("\uE753", (Style)Application.Current.Resources["App.ThemedIcons.Status.Cloud"], syncStatus, "CloudDriveSyncStatus_PartialOffline"),
			CloudDriveSyncStatus.FolderOfflineFull or CloudDriveSyncStatus.FolderEmpty
				=> new CloudDriveSyncStatusUI("\uE73E", (Style)Application.Current.Resources["App.ThemedIcons.Status.Available"], syncStatus, "CloudDriveSyncStatus_Offline"),
			CloudDriveSyncStatus.FolderOfflinePinned
				=> new CloudDriveSyncStatusUI("\uE73E", (Style)Application.Current.Resources["App.ThemedIcons.Status.KeepOffline"], syncStatus, "CloudDriveSyncStatus_Offline"),
			CloudDriveSyncStatus.FolderExcluded
				=> new CloudDriveSyncStatusUI("\uF140", (Style)Application.Current.Resources["App.ThemedIcons.Status.Unavailable"], syncStatus, "CloudDriveSyncStatus_Excluded"),

			// Unknown
			_ => new CloudDriveSyncStatusUI(syncStatus),
		};
	}
}
