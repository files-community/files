// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using Windows.Storage;
using Vanara.Windows.Shell;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides an utility for Google Drive Cloud detection.
	/// </summary>
	public sealed class GoogleDriveCloudDetector : AbstractCloudDetector
	{
		private static readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<App>>();

		private const string _googleDriveRegKeyName = @"Software\Google\DriveFS";
		private const string _googleDriveRegValName = "PerAccountPreferences";
		private const string _googleDriveRegValPropName = "value";
		private const string _googleDriveRegValPropPropName = "mount_point_path";

		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			// Google Drive's sync database can be in a couple different locations. Go find it.
			string appDataPath = UserDataPaths.GetDefault().LocalAppData;

			await StorageFile.GetFileFromPathAsync(Path.Combine(appDataPath, @"Google\DriveFS\root_preference_sqlite.db")).AsTask()
				.AndThen(c => c.CopyAsync(ApplicationData.Current.TemporaryFolder, "google_drive.db", NameCollisionOption.ReplaceExisting).AsTask());

			// The wal file may not exist but that's ok
			await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(Path.Combine(appDataPath, @"Google\DriveFS\root_preference_sqlite.db-wal")).AsTask()
				.AndThen(c => c.CopyAsync(ApplicationData.Current.TemporaryFolder, "google_drive.db-wal", NameCollisionOption.ReplaceExisting).AsTask()));

			var syncDbPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "google_drive.db");

			// Build the connection and sql command
			SQLitePCL.Batteries_V2.Init();
			using var database = new SqliteConnection($"Data Source='{syncDbPath}'");
			using var cmdRoot = new SqliteCommand("SELECT * FROM roots", database);
			using var cmdMedia = new SqliteCommand("SELECT * FROM media WHERE fs_type=10", database);

			// Open the connection and execute the command
			database.Open();

			var reader = cmdRoot.ExecuteReader(); // Google synced folders
			while (reader.Read())
			{
				// Extract the data from the reader
				string? path = reader["last_seen_absolute_path"]?.ToString();
				if (string.IsNullOrWhiteSpace(path))
				{
					continue;
				}

				if ((long)reader["is_my_drive"] == 1)
					continue;

				// By default, the path will be prefixed with "\\?\" (unless another app has explicitly changed it).
				// \\?\ indicates to Win32 that the filename may be longer than MAX_PATH (see MSDN).
				// Parts of .NET (e.g. the File class) don't handle this very well, so remove this prefix.
				if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
				{
					path = path.Substring(@"\\?\".Length);
				}

				var folder = await StorageFolder.GetFolderFromPathAsync(path);
				string title = reader["title"]?.ToString() ?? folder.Name;

				App.AppModel.GoogleDrivePath = path;

#if DEBUG
				Debug.WriteLine($"In GDCD in roots table: App.AppModel.GoogleDrivePath being set to: {path}");
				Debug.WriteLine("YIELD RETURNING from GoogleDriveCloudDetector#GetProviders (roots): ");
				Debug.WriteLine($"name=Google Drive ({title}); path={path}");
#endif

				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = $"Google Drive ({title})",
					SyncFolder = path,
				};
			}

			// Google virtual drive
			reader = cmdMedia.ExecuteReader();

			while (reader.Read())
			{
				string? path = reader["last_mount_point"]?.ToString();
				if (string.IsNullOrWhiteSpace(path))
					continue;

				if (!AddMyDriveToPathAndValidate(ref path))
				{ 
					_logger.LogWarning($"Validation failed for {path} (media)");
					continue;
				}

				var folder = await StorageFolder.GetFolderFromPathAsync(path);
				string title = reader["name"]?.ToString() ?? folder.Name;

				App.AppModel.GoogleDrivePath = path;

				var iconFile = await GetGoogleDriveIconFileAsync();

#if DEBUG
				Debug.WriteLine($"In GDCD in media table: App.AppModel.GoogleDrivePath being set to: {path}");
				Debug.WriteLine("YIELD RETURNING from GoogleDriveCloudDetector#GetProviders (media): ");
				Debug.WriteLine($"name={title}; path={path}");
#endif

				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = title,
					SyncFolder = path,
					IconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null,
				};
			}

#if DEBUG
			await Inspect(database, "SELECT * FROM roots", "root_preferences db, roots table");
			await Inspect(database, "SELECT * FROM media", "root_preferences db, media table");
			await Inspect(database, "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY 1", "root_preferences db, all tables");
#endif

			await foreach (var provider in GetGoogleDriveProvidersFromRegistryAsync())
			{

#if DEBUG
				Debug.WriteLine("YIELD RETURNING from GoogleDriveCloudDetector#GetProviders (registry): ");
				Debug.WriteLine($"name={provider.Name}; path={provider.SyncFolder}");
#endif

				yield return provider;
			}
		}

		private async Task Inspect(SqliteConnection database, string sqlCommand, string contentsOf)
		{
			await using var cmdTablesAll = new SqliteCommand(sqlCommand, database);
			var reader = await cmdTablesAll.ExecuteReaderAsync();
			var colNamesList = Enumerable.Range(0, reader.FieldCount).Select(j => reader.GetName(j)).ToList();

#if DEBUG
			Debug.WriteLine($"BEGIN LOGGING of {contentsOf} contents");
#endif

			for (int index = 0; reader.Read() is not false; index++)
			{
				var colVals = new object[reader.FieldCount];
				reader.GetValues(colVals);

				colVals.Select((val, j) => $"row {index}: column {j}: {colNamesList[j]}: {val}")
					.ToList().ForEach(s => Debug.WriteLine(s));
			}

#if DEBUG
			Debug.WriteLine($"END LOGGING of {contentsOf} contents");
#endif
		}

		private static JsonDocument? GetGoogleDriveRegValJson()
		{
			// This will be null if the key name is not found.
			using var googleDriveRegKey = Registry.CurrentUser.OpenSubKey(_googleDriveRegKeyName);

			if (googleDriveRegKey is null)
			{
				_logger.LogWarning($"Google Drive registry key for key name '{_googleDriveRegKeyName}' not found.");
				return null;
			}

			var googleDriveRegVal = googleDriveRegKey.GetValue(_googleDriveRegValName);

			if (googleDriveRegVal is null)
			{
				_logger.LogWarning($"Google Drive registry value for value name '{_googleDriveRegValName}' not found.");
				return null;
			}

			JsonDocument? googleDriveRegValueJson = null;
			try
			{
				googleDriveRegValueJson = JsonDocument.Parse(googleDriveRegVal.ToString() ?? "");
			}
			catch (JsonException je)
			{
				_logger.LogWarning(je, $"Google Drive registry value for value name '{_googleDriveRegValName}' could not be parsed as a JsonDocument.");
			}

			return googleDriveRegValueJson;
		}

		public static async IAsyncEnumerable<ICloudProvider> GetGoogleDriveProvidersFromRegistryAsync(bool addMyDriveToPath = true)
		{
			var googleDriveRegValJson = GetGoogleDriveRegValJson();

			if (googleDriveRegValJson is null)
				yield break;

			var googleDriveRegValJsonProperty = googleDriveRegValJson
				.RootElement.EnumerateObject()
				.FirstOrDefault();

			// A default JsonProperty struct has an "Undefined" Value#ValueKind and throws an
			// error if you try to call EnumerateArray on its Value.
			if (googleDriveRegValJsonProperty.Value.ValueKind == JsonValueKind.Undefined)
			{
				_logger.LogWarning($"Root element of Google Drive registry value for value name '{_googleDriveRegValName}' was empty.");
				yield break;
			}

#if DEBUG
			Debug.WriteLine("REGISTRY LOGGING");
			Debug.WriteLine(googleDriveRegValJsonProperty.ToString());
#endif

			foreach (var item in googleDriveRegValJsonProperty.Value.EnumerateArray())
			{
				if (!item.TryGetProperty(_googleDriveRegValPropName, out var googleDriveRegValProp))
					continue;

				if (!googleDriveRegValProp.TryGetProperty(_googleDriveRegValPropPropName, out var googleDriveRegValPropProp))
					continue;

				var path = googleDriveRegValPropProp.GetString();
				if (path is null)
					continue;

				if (!AddMyDriveToPathAndValidate(ref path, addMyDriveToPath))
				{
					_logger.LogWarning($"Validation failed for {path} (registry)");
					continue;
				}

				App.AppModel.GoogleDrivePath = path;
#if DEBUG
				Debug.WriteLine($"In GDCD in registry: App.AppModel.GoogleDrivePath being set to: {path}");
#endif

				var iconFile = await GetGoogleDriveIconFileAsync();

				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = "Google Drive",
					SyncFolder = path,
					IconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null,
				};
			}
		}

		private static async Task<StorageFile?> GetGoogleDriveIconFileAsync()
		{
			var programFilesEnvVar = Environment.GetEnvironmentVariable("ProgramFiles");

			if (programFilesEnvVar is null)
				return null;

			var iconPath = Path.Combine(programFilesEnvVar, "Google", "Drive File Stream", "drive_fs.ico");

			return await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(iconPath).AsTask());
		}

		private static bool AddMyDriveToPathAndValidate(ref string path, bool addMyDrive = true)
		{
			// If Google Drive is mounted as a drive, then the path found in the registry will be
			// *just* the drive letter (e.g. just "G" as opposed to "G:\"), and therefore must be
			// reformatted as a valid path.
			if (path.Length == 1)
			{
				DriveInfo temp;
				try
				{
					temp = new DriveInfo(path);
				}
				catch (ArgumentException e)
				{
					_logger.LogWarning(e, $"Could not resolve drive letter '{path}' to a valid drive.");
					return false;
				}

				path = temp.RootDirectory.Name;
			}

			if (addMyDrive)
			{
				// If `path` contains a shortcut named "My Drive", store its target in `shellFolderBaseFirst`.
				// This happens when "My Drive syncing options" is set to "Mirror files".
				// TODO: Avoid to use Vanara (#15000)
				using var shellFolderBase = ShellFolderExtensions.GetShellItemFromPathOrPIDL(path) as ShellFolder;
				var shellFolderBaseFirst = Environment.ExpandEnvironmentVariables((
						shellFolderBase?.FirstOrDefault(si =>
							si.Name?.Equals("My Drive") ?? false) as ShellLink)?.TargetPath
					?? string.Empty);

#if DEBUG
				Debug.WriteLine("INVALID PATHS LOGGER");
				shellFolderBase?.ForEach(si => Debug.WriteLine(si.Name));
#endif

				if (!string.IsNullOrEmpty(shellFolderBaseFirst))
				{
					path = shellFolderBaseFirst;
					return true;
				}

				path = Path.Combine(path, "My Drive");
			}

			if (Directory.Exists(path))
				return true;
			_logger.LogWarning($"Invalid Google Drive mount point path: {path}");
			return false;
		}
	}
}
