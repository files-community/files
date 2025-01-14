// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Extensions;
using System.Security.Principal;

namespace Files.App.Storage.Watchers
{
	public class RecycleBinWatcher : ITrashWatcher
	{
		private readonly List<SystemIO.FileSystemWatcher> _watchers = [];

		/// <inheritdoc/>
		public event EventHandler<SystemIO.FileSystemEventArgs>? ItemAdded;

		/// <inheritdoc/>
		public event EventHandler<SystemIO.FileSystemEventArgs>? ItemDeleted;

		/// <inheritdoc/>
		public event EventHandler<SystemIO.FileSystemEventArgs>? ItemChanged;

		/// <inheritdoc/>
		public event EventHandler<SystemIO.FileSystemEventArgs>? ItemRenamed;

		/// <inheritdoc/>
		public event EventHandler<SystemIO.FileSystemEventArgs>? RefreshRequested;

		/// <summary>
		/// Initializes an instance of <see cref="RecycleBinWatcher"/> class.
		/// </summary>
		public RecycleBinWatcher()
		{
			StartWatcher();
		}

		/// <inheritdoc/>
		public void StartWatcher()
		{
			// NOTE: SHChangeNotifyRegister only works if recycle bin is open in File Explorer.

			// Listen changes only on the Recycle Bin that the current logon user has
			var sid = WindowsIdentity.GetCurrent().User?.ToString() ?? string.Empty;
			if (string.IsNullOrEmpty(sid))
				return;

			foreach (var drive in SystemIO.DriveInfo.GetDrives())
			{
				var recyclePath = SystemIO.Path.Combine(drive.Name, "$RECYCLE.BIN", sid);

				if (drive.DriveType is SystemIO.DriveType.Network ||
					!SystemIO.Directory.Exists(recyclePath))
					continue;

				// NOTE: Suppressed NullReferenceException caused by EnableRaisingEvents in #15808
				SafetyExtensions.IgnoreExceptions(() =>
				{
					SystemIO.FileSystemWatcher watcher = new()
					{
						Path = recyclePath,
						Filter = "*.*",
						NotifyFilter = SystemIO.NotifyFilters.LastWrite | SystemIO.NotifyFilters.FileName | SystemIO.NotifyFilters.DirectoryName
					};

					watcher.Created += Watcher_Changed;
					watcher.Deleted += Watcher_Changed;
					watcher.EnableRaisingEvents = true;

					_watchers.Add(watcher);
				});
			}
		}

		/// <inheritdoc/>
		public void StopWatcher()
		{
			foreach (var watcher in _watchers)
				watcher.Dispose();
		}

		private void Watcher_Changed(object sender, SystemIO.FileSystemEventArgs e)
		{
			// Don't listen changes on files starting with '$I'
			if (string.IsNullOrEmpty(e.Name) ||
				e.Name.StartsWith("$I", StringComparison.Ordinal))
				return;

			switch (e.ChangeType)
			{
				case SystemIO.WatcherChangeTypes.Created:
					ItemAdded?.Invoke(this, e);
					break;
				case SystemIO.WatcherChangeTypes.Deleted:
					ItemDeleted?.Invoke(this, e);
					break;
				case SystemIO.WatcherChangeTypes.Renamed:
					ItemRenamed?.Invoke(this, e);
					break;
				default:
					RefreshRequested?.Invoke(this, e);
					break;
			}
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			StopWatcher();
		}
	}
}
