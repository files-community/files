// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using System.IO;
using System.Runtime.Versioning;

namespace Files.App.Utils.Cloud
{
	[SupportedOSPlatform("Windows10.0.10240")]
	public sealed class CloudDrivesDetector
	{
		private readonly static string programFilesFolder = Environment.GetEnvironmentVariable("ProgramFiles");

		public static async Task<IEnumerable<ICloudProvider>> DetectCloudDrives()
		{
			var tasks = new Task<IEnumerable<ICloudProvider>>[]
			{
				SafetyExtensions.IgnoreExceptions(DetectOneDrive, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectSharepoint, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectGenericCloudDrive, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectYandexDisk, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectpCloudDrive, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectNutstoreDrive, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectSeadriveDrive, App.Logger),
				SafetyExtensions.IgnoreExceptions(DetectAutodeskDrive, App.Logger),
			};

			await Task.WhenAll(tasks);

			return tasks
				.Where(o => o.Result is not null)
				.SelectMany(o => o.Result)
				.OrderBy(o => o.ID.ToString())
				.ThenBy(o => o.Name)
				.Distinct();
		}

		private static Task<IEnumerable<ICloudProvider>> DetectYandexDisk()
		{
			var results = new List<ICloudProvider>();
			using var yandexKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Yandex\Yandex.Disk.2");

			var syncedFolder = (string?)yandexKey?.GetValue("RootFolder");
			if (syncedFolder is not null)
			{
				results.Add(new CloudProvider(CloudProviders.Yandex)
				{
					Name = $"Yandex Disk",
					SyncFolder = syncedFolder,
				});
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectGenericCloudDrive()
		{
			var results = new List<ICloudProvider>();
			using var clsidKey = Registry.ClassesRoot.OpenSubKey(@"CLSID");
			using var namespaceKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace");
			using var syncRootManagerKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager");

			foreach (var subKeyName in namespaceKey?.GetSubKeyNames() ?? [])
			{
				using var clsidSubKey = SafetyExtensions.IgnoreExceptions(() => clsidKey?.OpenSubKey(subKeyName));
				if (clsidSubKey is not null && (int?)clsidSubKey.GetValue("System.IsPinnedToNameSpaceTree") is 1)
				{
					using var namespaceSubKey = namespaceKey?.OpenSubKey(subKeyName);
					var driveIdentifier = (string?)namespaceSubKey?.GetValue(string.Empty);
					if (driveIdentifier is null)
						continue;

					var driveType = GetDriveType(driveIdentifier, namespaceSubKey, syncRootManagerKey);

					using var bagKey = clsidSubKey.OpenSubKey(@"Instance\InitPropertyBag");
					var syncedFolder = (string?)bagKey?.GetValue("TargetFolderPath");
					if (syncedFolder is null)
						continue;

					// Also works for OneDrive, Box, Dropbox
					CloudProviders? cloudProvider = driveType switch
					{
						"MEGA" => CloudProviders.Mega,
						"Nextcloud" => CloudProviders.Nextcloud,
						"Jottacloud" => CloudProviders.Jottacloud,
						"iCloudDrive" => CloudProviders.AppleCloudDrive,
						"iCloudPhotos" => CloudProviders.AppleCloudPhotos,
						"Creative Cloud Files" => CloudProviders.AdobeCreativeCloud,
						"ownCloud" => CloudProviders.ownCloud,
						"ProtonDrive" => CloudProviders.ProtonDrive,
						"kDrive" => CloudProviders.kDrive,
						_ => null,
					};

					if (cloudProvider is null)
						continue;

					var nextCloudValue = (string?)namespaceSubKey?.GetValue(string.Empty);
					var ownCloudValue = (string?)clsidSubKey?.GetValue(string.Empty);
					var kDriveValue = (string?)clsidSubKey?.GetValue(string.Empty);

					using var defaultIconKey = clsidSubKey?.OpenSubKey(@"DefaultIcon");
					var iconPath = (string?)defaultIconKey?.GetValue(string.Empty);

					results.Add(new CloudProvider(cloudProvider.Value)
					{
						Name = cloudProvider switch
						{
							CloudProviders.Mega => $"MEGA ({Path.GetFileName(syncedFolder.TrimEnd('\\'))})",
							CloudProviders.Nextcloud => !string.IsNullOrEmpty(nextCloudValue) ? nextCloudValue : "Nextcloud",
							CloudProviders.Jottacloud => $"Jottacloud",
							CloudProviders.AppleCloudDrive => $"iCloud Drive",
							CloudProviders.AppleCloudPhotos => $"iCloud Photos",
							CloudProviders.AdobeCreativeCloud => $"Creative Cloud Files",
							CloudProviders.ownCloud => !string.IsNullOrEmpty(ownCloudValue) ? ownCloudValue : "ownCloud",
							CloudProviders.ProtonDrive => $"Proton Drive",
							CloudProviders.kDrive => !string.IsNullOrEmpty(kDriveValue) ? kDriveValue : "kDrive",
							_ => null
						},
						SyncFolder = syncedFolder,
						IconData = cloudProvider switch
						{
							CloudProviders.ProtonDrive => Win32Helper.ExtractSelectedIconsFromDLL(iconPath, new List<int>() { 32512 }).FirstOrDefault()?.IconData,
							_ => null
						}
					});
				}
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectOneDrive()
		{
			using var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts");
			if (oneDriveAccountsKey is null)
			{
				return Task.FromResult<IEnumerable<ICloudProvider>>(null);
			}

			var oneDriveAccounts = new List<ICloudProvider>();
			foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
			{
				var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
				var displayName = (string?)Registry.GetValue(accountKeyName, "DisplayName", null);
				var userFolder = (string?)Registry.GetValue(accountKeyName, "UserFolder", null);
				var accountName = string.IsNullOrWhiteSpace(displayName) ? "OneDrive" : $"OneDrive - {displayName}";

				if (!string.IsNullOrWhiteSpace(userFolder) && !oneDriveAccounts.Any(x => x.Name == accountName))
				{
					oneDriveAccounts.Add(new CloudProvider(CloudProviders.OneDrive)
					{
						Name = accountName,
						SyncFolder = userFolder,
						IconData = UIHelpers.GetSidebarIconResourceInfo(Constants.ImageRes.OneDrive).IconData,
					});
				}
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(oneDriveAccounts);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectSharepoint()
		{
			using var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts");
			if (oneDriveAccountsKey is null)
			{
				return Task.FromResult<IEnumerable<ICloudProvider>>(null);
			}

			var sharepointAccounts = new List<ICloudProvider>();
			foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
			{
				var accountKey = oneDriveAccountsKey.OpenSubKey(account);
				if (accountKey is null)
					continue;

				var userFolderToExcludeFromResults = (string)accountKey.GetValue("UserFolder", "");

				var sharePointParentFolders = new List<DirectoryInfo>();
				using (var mountPointsKey = accountKey.OpenSubKey("ScopeIdToMountPointPathCache"))
				{
					if (mountPointsKey is null)
					{
						continue;
					}

					var valueNames = mountPointsKey.GetValueNames();
					foreach (var valueName in valueNames)
					{
						var directory = (string?)mountPointsKey.GetValue(valueName, null);
						if (directory != null && !string.Equals(directory, userFolderToExcludeFromResults, StringComparison.OrdinalIgnoreCase))
						{
							var parentFolder = Directory.GetParent(directory);
							if (parentFolder != null)
								sharePointParentFolders.Add(parentFolder);
						}
					}
				}

				sharePointParentFolders.Sort((left, right) => left.FullName.CompareTo(right.FullName));

				foreach (var sharePointParentFolder in sharePointParentFolders)
				{
					string name = $"SharePoint - {sharePointParentFolder.Name}";
					if (!sharepointAccounts.Any(acc => string.Equals(acc.Name, name, StringComparison.OrdinalIgnoreCase)))
					{
						sharepointAccounts.Add(new CloudProvider(CloudProviders.OneDriveCommercial)
						{
							Name = name,
							SyncFolder = sharePointParentFolder.FullName,
						});
					}
				}
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(sharepointAccounts);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectpCloudDrive()
		{
			var results = new List<ICloudProvider>();
			using var pCloudDriveKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\pCloud");

			var syncedFolder = (string?)pCloudDriveKey?.GetValue("SyncDrive");
			if (syncedFolder is not null)
			{
				string iconPath = Path.Combine(programFilesFolder, "pCloud Drive", "pCloud.exe");
				var iconFile = Win32Helper.ExtractSelectedIconsFromDLL(iconPath, new List<int>() { 32512 }, 32).FirstOrDefault();

				App.AppModel.PCloudDrivePath = syncedFolder;

				results.Add(new CloudProvider(CloudProviders.pCloud)
				{
					Name = $"pCloud Drive",
					SyncFolder = syncedFolder,
					IconData = iconFile?.IconData
				});
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectNutstoreDrive()
		{
			var results = new List<ICloudProvider>();
			using var NutstoreKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Nutstore");

			if (NutstoreKey is not null)
			{
				string iconPath = Path.Combine(programFilesFolder, "Nutstore", "Nutstore.exe");
				var iconFile = Win32Helper.ExtractSelectedIconsFromDLL(iconPath, new List<int>() { 101 }).FirstOrDefault();

				using var syncRootMangerKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager");
				if (syncRootMangerKey is not null)
				{
					var syncRootIds = syncRootMangerKey.GetSubKeyNames();
					foreach (var syncRootId in syncRootIds)
					{
						if (!syncRootId.StartsWith("Nutstore-")) continue;

						var sid = syncRootId.Split('!')[1];
						using var syncRootKey = syncRootMangerKey.OpenSubKey($@"{syncRootId}\UserSyncRoots");
						var userSyncRoot = syncRootKey?.GetValue(sid)?.ToString();
						if (string.IsNullOrEmpty(userSyncRoot)) continue;

						results.Add(new CloudProvider(CloudProviders.Nutstore)
						{
							Name = $"Nutstore",
							SyncFolder = userSyncRoot,
							IconData = iconFile?.IconData
						});
					}
				}
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectSeadriveDrive()
		{
			var results = new List<ICloudProvider>();
			using var SeadriveKey = Registry.CurrentUser.OpenSubKey(@"Software\SeaDrive\Seafile Drive Client\Settings");

			var syncFolder = (string?)SeadriveKey?.GetValue("seadriveRoot");
			if (SeadriveKey is not null)
			{
				string iconPath = Path.Combine(programFilesFolder, "SeaDrive", "bin", "seadrive.exe");
				var iconFile = Win32Helper.ExtractSelectedIconsFromDLL(iconPath, new List<int>() { 101 }).FirstOrDefault();

				results.Add(new CloudProvider(CloudProviders.Seadrive)
				{
					Name = $"Seadrive",
					SyncFolder = syncFolder,
					IconData = iconFile?.IconData
				});
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}

		private static Task<IEnumerable<ICloudProvider>> DetectAutodeskDrive()
		{
			var results = new List<ICloudProvider>();
			using var AutodeskKey = Registry.LocalMachine.OpenSubKey(@"Software\Autodesk\Desktop Connector");

			if (AutodeskKey is not null)
			{
				string iconPath = Path.Combine(programFilesFolder, "Autodesk", "Desktop Connector", "DesktopConnector.Applications.Tray.exe");
				var iconFile = Win32Helper.ExtractSelectedIconsFromDLL(iconPath, new List<int>() { 32512 }).FirstOrDefault();
				var mainFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "DC");
				var autodeskFolders = Directory.GetDirectories(mainFolder, "", SearchOption.AllDirectories);

				foreach (var autodeskFolder in autodeskFolders)
				{
					var folderName = Path.GetFileName(autodeskFolder);
					if (folderName is not null)
						results.Add(new CloudProvider(CloudProviders.Autodesk)
						{
							Name = $"Autodesk - {Path.GetFileName(autodeskFolder)}",
							SyncFolder = autodeskFolder,
							IconData = iconFile?.IconData
						});
				}
			}

			return Task.FromResult<IEnumerable<ICloudProvider>>(results);
		}

		private static string GetDriveType(string driveIdentifier, RegistryKey? namespaceSubKey, RegistryKey? syncRootManagerKey)
		{
			// Drive specific
			if (driveIdentifier.StartsWith("iCloudDrive"))
				return "iCloudDrive";
			if (driveIdentifier.StartsWith("iCloudPhotos"))
				return "iCloudPhotos";
			if (driveIdentifier.StartsWith("ownCloud"))
				return "ownCloud";
			if (driveIdentifier.StartsWith("ProtonDrive"))
				return "ProtonDrive";

			// Nextcloud specific
			var appNameFromNamespace = (string?)namespaceSubKey?.GetValue("ApplicationName");
			if (!string.IsNullOrEmpty(appNameFromNamespace) && appNameFromNamespace == "Nextcloud")
				return appNameFromNamespace;

			// kDrive specific
			var appNameFromSyncRoot = (string?)syncRootManagerKey?.OpenSubKey(driveIdentifier)?.GetValue(string.Empty);
			if (!string.IsNullOrEmpty(appNameFromNamespace) && appNameFromNamespace == "kDrive")
				return appNameFromNamespace;
			if (!string.IsNullOrEmpty(appNameFromSyncRoot) && appNameFromSyncRoot == "kDrive")
				return appNameFromSyncRoot;

			return driveIdentifier;
		}
	}
}
