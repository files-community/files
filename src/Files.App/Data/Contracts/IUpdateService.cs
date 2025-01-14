// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IUpdateService : INotifyPropertyChanged
	{
		/// <summary>
		/// Gets a value indicating whether updates are available.
		/// </summary>
		bool IsUpdateAvailable { get; }

		/// <summary>
		/// Gets a value indicating if an update is in progress.
		/// </summary>
		bool IsUpdating { get; }

		/// <summary>
		/// Gets a value indicating if the app is being used the first time after an update.
		/// </summary>
		bool IsAppUpdated { get; }

		/// <summary>
		/// Gets a value that indicates if there are release notes available for the current version of the app.
		/// </summary>
		bool AreReleaseNotesAvailable { get; }

		Task DownloadUpdatesAsync();

		Task DownloadMandatoryUpdatesAsync();

		Task CheckForUpdatesAsync();

		Task CheckForReleaseNotesAsync();

		/// <summary>
		/// Replace Files.App.Launcher.exe if it is used and has been updated
		/// </summary>
		Task CheckAndUpdateFilesLauncherAsync();
	}
}
