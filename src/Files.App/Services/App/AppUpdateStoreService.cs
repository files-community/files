// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Net.Http;
using Windows.Foundation.Metadata;
using Windows.Services.Store;
using Windows.Storage;
using WinRT.Interop;

namespace Files.App.Services
{
	internal sealed class StoreUpdateService : ObservableObject, IUpdateService
	{
		private StoreContext? _storeContext;
		private List<StorePackageUpdate>? _updatePackages;

		private bool IsMandatory => _updatePackages?.Where(e => e.Mandatory).ToList().Count >= 1;

		private bool _isUpdateAvailable;
		public bool IsUpdateAvailable
		{
			get => _isUpdateAvailable;
			set => SetProperty(ref _isUpdateAvailable, value);
		}

		private bool _isUpdating;
		public bool IsUpdating
		{
			get => _isUpdating;
			private set => SetProperty(ref _isUpdating, value);
		}

		public bool IsAppUpdated
		{
			get => SystemInformation.Instance.IsAppUpdated;
		}

		private bool _areReleaseNotesAvailable = false;
		public bool AreReleaseNotesAvailable
		{
			get => _areReleaseNotesAvailable;
			private set => SetProperty(ref _areReleaseNotesAvailable, value);
		}

		public StoreUpdateService()
		{
			_updatePackages = [];
		}

		public async Task DownloadUpdatesAsync()
		{
			OnUpdateInProgress();

			if (!HasUpdates())
			{
				return;
			}

			// double check for Mandatory
			if (IsMandatory)
			{
				// Show dialog
				var dialog = await ShowDialogAsync();
				if (!dialog)
				{
					// User rejected mandatory update.
					OnUpdateCancelled();
					return;
				}
			}

			await DownloadAndInstallAsync();
			OnUpdateCompleted();
		}

		public async Task DownloadMandatoryUpdatesAsync()
		{
			// Prompt the user to download if the package list
			// contains mandatory updates.
			if (IsMandatory && HasUpdates())
			{
				if (await ShowDialogAsync())
				{
					App.Logger.LogInformation("STORE: Downloading updates...");
					OnUpdateInProgress();
					await DownloadAndInstallAsync();
					OnUpdateCompleted();
				}
			}
		}

		public async Task CheckForUpdatesAsync()
		{
			IsUpdateAvailable = false;
			App.Logger.LogInformation("STORE: Checking for updates...");

			await GetUpdatePackagesAsync();

			if (_updatePackages is not null && _updatePackages.Count > 0)
			{
				App.Logger.LogInformation("STORE: Update found.");
				IsUpdateAvailable = true;
			}
		}

		public async Task CheckForReleaseNotesAsync()
		{
			using var client = new HttpClient();

			try
			{
				var response = await client.GetAsync(Constants.ExternalUrl.ReleaseNotesUrl);
				AreReleaseNotesAvailable = response.IsSuccessStatusCode;
			}
			catch
			{
				AreReleaseNotesAvailable = false;
			}
		}

		private async Task DownloadAndInstallAsync()
		{
			// Save the updated tab list before installing the update
			AppLifecycleHelper.SaveSessionTabs();

			App.AppModel.ForceProcessTermination = true;

			var downloadOperation = _storeContext?.RequestDownloadAndInstallStorePackageUpdatesAsync(_updatePackages);
			var result = await downloadOperation.AsTask();

			if (result.OverallState == StorePackageUpdateState.Canceled)
				App.AppModel.ForceProcessTermination = false;
		}

		private async Task GetUpdatePackagesAsync()
		{
			try
			{
				_storeContext ??= await Task.Run(StoreContext.GetDefault);

				InitializeWithWindow.Initialize(_storeContext, MainWindow.Instance.WindowHandle);

				var updateList = await _storeContext.GetAppAndOptionalStorePackageUpdatesAsync();
				_updatePackages = updateList?.ToList();
			}
			catch (Exception ex)
			{
				// GetAppAndOptionalStorePackageUpdatesAsync throws for unknown reasons.
				App.Logger.LogWarning(ex, ex.Message);
			}
		}

		private static async Task<bool> ShowDialogAsync()
		{
			//TODO: Use IDialogService in future.
			ContentDialog dialog = new()
			{
				Title = "ConsentDialogTitle".GetLocalizedResource(),
				Content = "ConsentDialogContent".GetLocalizedResource(),
				CloseButtonText = "Close".GetLocalizedResource(),
				PrimaryButtonText = "ConsentDialogPrimaryButtonText".GetLocalizedResource()
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			ContentDialogResult result = await dialog.TryShowAsync();

			return result == ContentDialogResult.Primary;
		}

		public async Task CheckAndUpdateFilesLauncherAsync()
		{
			var destFolderPath = Path.Combine(UserDataPaths.GetDefault().LocalAppData, "Files");
			var destExeFilePath = Path.Combine(destFolderPath, "Files.App.Launcher.exe");

			if (Path.Exists(destExeFilePath))
			{
				var hashEqual = false;
				var srcHashFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FilesOpenDialog/Files.App.Launcher.exe.sha256"));
				var destHashFilePath = Path.Combine(destFolderPath, "Files.App.Launcher.exe.sha256");

				if (Path.Exists(destHashFilePath))
				{
					await using var srcStream = (await srcHashFile.OpenReadAsync()).AsStream();
					await using var destStream = File.OpenRead(destHashFilePath);

					hashEqual = HashEqual(srcStream, destStream);
				}

				if (!hashEqual)
				{
					var srcExeFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FilesOpenDialog/Files.App.Launcher.exe"));
					var destFolder = await StorageFolder.GetFolderFromPathAsync(destFolderPath);

					await srcExeFile.CopyAsync(destFolder, "Files.App.Launcher.exe", NameCollisionOption.ReplaceExisting);
					await srcHashFile.CopyAsync(destFolder, "Files.App.Launcher.exe.sha256", NameCollisionOption.ReplaceExisting);

					App.Logger.LogInformation("Files.App.Launcher updated.");
				}
			}

			bool HashEqual(Stream a, Stream b)
			{
				Span<byte> bufferA = stackalloc byte[64];
				Span<byte> bufferB = stackalloc byte[64];

				a.Read(bufferA);
				b.Read(bufferB);

				return bufferA.SequenceEqual(bufferB);
			}
		}

		private bool HasUpdates()
		{
			return _updatePackages is not null && _updatePackages.Count >= 1;
		}

		private void OnUpdateInProgress()
		{
			IsUpdating = true;
		}

		private void OnUpdateCompleted()
		{
			IsUpdating = false;
			IsUpdateAvailable = false;

			_updatePackages?.Clear();
		}

		private void OnUpdateCancelled()
		{
			IsUpdating = false;
		}
	}
}
