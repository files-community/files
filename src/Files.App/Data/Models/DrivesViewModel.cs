// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Services.SizeProvider;
using Files.Core.Storage.Storables;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Files.App.Data.Models
{
	public sealed class DrivesViewModel : ObservableObject, IDisposable
	{
		public ObservableCollection<ILocatableFolder> Drives
		{
			get => drives;
			private set => SetProperty(ref drives, value);
		}

		public bool ShowUserConsentOnInit
		{
			get => showUserConsentOnInit;
			set => SetProperty(ref showUserConsentOnInit, value);
		}

		private bool showUserConsentOnInit;
		private ObservableCollection<ILocatableFolder> drives;
		private readonly IRemovableDrivesService removableDrivesService;
		private readonly ISizeProvider folderSizeProvider;
		private readonly IStorageDeviceWatcher watcher;
		private readonly ILogger<App> logger;

		public DrivesViewModel(IRemovableDrivesService removableDrivesService, ISizeProvider folderSizeProvider, ILogger<App> logger)
		{
			this.removableDrivesService = removableDrivesService;
			this.folderSizeProvider = folderSizeProvider;
			this.logger = logger;

			drives = [];

			watcher = removableDrivesService.CreateWatcher();
			watcher.DeviceAdded += Watcher_DeviceAdded;
			watcher.DeviceRemoved += Watcher_DeviceRemoved;
			watcher.DeviceModified += Watcher_DeviceModified;
			watcher.EnumerationCompleted += Watcher_EnumerationCompleted;
		}

		private async void Watcher_EnumerationCompleted(object? sender, System.EventArgs e)
		{
			logger.LogDebug("Watcher_EnumerationCompleted");
			await folderSizeProvider.CleanAsync();
		}

		private async void Watcher_DeviceModified(object? sender, string e)
		{
			var matchingDriveEjected = Drives.FirstOrDefault(x => Path.GetFullPath(x.Path) == Path.GetFullPath(e));
			if (matchingDriveEjected != null)
				await removableDrivesService.UpdateDrivePropertiesAsync(matchingDriveEjected);
		}

		private void Watcher_DeviceRemoved(object? sender, string e)
		{
			logger.LogInformation($"Drive removed: {e}");
			lock (Drives)
			{
				var drive = Drives.FirstOrDefault(x => x.Id == e);
				if (drive is not null)
					Drives.Remove(drive);
			}

			// Update the collection on the ui-thread.
			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		private void Watcher_DeviceAdded(object? sender, ILocatableFolder e)
		{
			lock (Drives)
			{
				// If drive already in list, remove it first.
				var matchingDrive = Drives.FirstOrDefault(x =>
					x.Id == e.Id ||
					string.IsNullOrEmpty(e.Path)
						? x.Path.Contains(e.Name, StringComparison.OrdinalIgnoreCase)
						: Path.GetFullPath(x.Path) == Path.GetFullPath(e.Path)
				);

				if (matchingDrive is not null)
					Drives.Remove(matchingDrive);

				logger.LogInformation($"Drive added: {e.Path}");
				Drives.Add(e);
			}

			Watcher_EnumerationCompleted(null, EventArgs.Empty);
		}

		public async Task UpdateDrivesAsync()
		{
			Drives.Clear();
			await foreach (ILocatableFolder item in removableDrivesService.GetDrivesAsync())
			{
				Drives.AddIfNotPresent(item);
			}

			var osDrive = await removableDrivesService.GetPrimaryDriveAsync();

			// Show consent dialog if the OS drive could not be accessed
			if (!Drives.Any(x => Path.GetFullPath(x.Path) == Path.GetFullPath(osDrive.Path)))
				ShowUserConsentOnInit = true;

			if (watcher.CanBeStarted)
				watcher.Start();
		}

		public void Dispose()
		{
			watcher.Stop();
			watcher.DeviceAdded -= Watcher_DeviceAdded;
			watcher.DeviceRemoved -= Watcher_DeviceRemoved;
			watcher.DeviceModified -= Watcher_DeviceModified;
			watcher.EnumerationCompleted -= Watcher_EnumerationCompleted;
		}
	}
}
