// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using Windows.Storage;
using Vanara.Windows.Shell;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides a utility for Google Drive Cloud detection.
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

				// By default, the path will be prefixed with "\\?\" (unless another app has explicitly changed it).
				// \\?\ indicates to Win32 that the filename may be longer than MAX_PATH (see MSDN).
				// Parts of .NET (e.g. the File class) don't handle this very well, so remove this prefix.
				if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
				{
					path = path.Substring(@"\\?\".Length);
				}

				var folder = await StorageFolder.GetFolderFromPathAsync(path);
				string title = reader["title"]?.ToString() ?? folder.Name;

				Debug.WriteLine("YIELD RETURNING from `GoogleDriveCloudDetector.GetProviders()` (roots): ");
				Debug.WriteLine($"Name: Google Drive ({title}); SyncFolder: {path}");

				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = $"Google Drive ({title})",
					SyncFolder = path,
				};
			}

			var iconFile = await GetGoogleDriveIconFileAsync();
			// Google virtual drive
			reader = cmdMedia.ExecuteReader();

			while (reader.Read())
			{
				string? path = reader["last_mount_point"]?.ToString();
				if (string.IsNullOrWhiteSpace(path))
					continue;

				if (!AddMyDriveToPathAndValidate(ref path))
					continue;

				var folder = await StorageFolder.GetFolderFromPathAsync(path);
				string title = reader["name"]?.ToString() ?? folder.Name;

				Debug.WriteLine("YIELD RETURNING from `GoogleDriveCloudDetector.GetProviders` (media): ");
				Debug.WriteLine($"Name: {title}; SyncFolder: {path}");

				yield return new CloudProvider(CloudProviders.GoogleDrive)
				{
					Name = title,
					SyncFolder = path,
					IconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null,
				};
			}

			await Inspect(database, "SELECT * FROM roots", "root_preferences db, roots table");
			await Inspect(database, "SELECT * FROM media", "root_preferences db, media table");
			await Inspect(database, "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY 1", "root_preferences db, all tables");

			var registryPath = App.AppModel.GoogleDrivePath;
			if (!AddMyDriveToPathAndValidate(ref registryPath))
				yield break;
			yield return new CloudProvider(CloudProviders.GoogleDrive)
			{
				Name = "Google Drive",
				SyncFolder = registryPath,
				IconData = iconFile is not null ? await iconFile.ToByteArrayAsync() : null
			};
		}

		private static async Task Inspect(SqliteConnection database, string sqlCommand, string targetDescription)
		{
			await using var cmdTablesAll = new SqliteCommand(sqlCommand, database);
			var reader = await cmdTablesAll.ExecuteReaderAsync();
			var colNamesList = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToList();

			Debug.WriteLine($"BEGIN LOGGING of {targetDescription}");

			for (int rowIdx = 0; reader.Read() is not false; rowIdx++)
			{
				var colVals = new object[reader.FieldCount];
				reader.GetValues(colVals);

				colVals.Select((val, colIdx) => $"row {rowIdx}: column {colIdx}: {colNamesList[colIdx]}: {val}")
					.ToList().ForEach(s => Debug.WriteLine(s));
			}

			Debug.WriteLine($"END LOGGING of {targetDescription} contents");
		}

		private static JsonDocument? GetGoogleDriveRegValJson()
		{
			// This will be null if the key name is not found.
			using var googleDriveRegKey = Registry.CurrentUser.OpenSubKey(_googleDriveRegKeyName);

			if (googleDriveRegKey is null)
				return null;

			var googleDriveRegVal = googleDriveRegKey.GetValue(_googleDriveRegValName);

			if (googleDriveRegVal is null)
				return null;

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

		public static string? GetRegistryBasePath()
		{
			var googleDriveRegValJson = GetGoogleDriveRegValJson();

			if (googleDriveRegValJson is null)
				return null;

			var googleDriveRegValJsonProperty = googleDriveRegValJson
				.RootElement.EnumerateObject()
				.FirstOrDefault();

			// A default JsonProperty struct has an "Undefined" Value#ValueKind and throws an
			// error if you try to call EnumerateArray on its Value.
			if (googleDriveRegValJsonProperty.Value.ValueKind == JsonValueKind.Undefined)
			{
				_logger.LogWarning($"Root element of Google Drive registry value for value name '{_googleDriveRegValName}' was empty.");
				return null;
			}

			Debug.WriteLine("REGISTRY LOGGING");
			Debug.WriteLine(googleDriveRegValJsonProperty.ToString());

			var item = googleDriveRegValJsonProperty.Value.EnumerateArray().FirstOrDefault();
			if (item.ValueKind == JsonValueKind.Undefined)
			{
				_logger.LogWarning($"Array in the root element of Google Drive registry value for value name '{_googleDriveRegValName}' was empty.");
				return null;
			}

			if (!item.TryGetProperty(_googleDriveRegValPropName, out var googleDriveRegValProp))
			{
				_logger.LogWarning($"First element in the Google Drive Registry Root Array did not have property named {_googleDriveRegValPropName}");
				return null;
			}

			if (!googleDriveRegValProp.TryGetProperty(_googleDriveRegValPropPropName, out var googleDriveRegValPropProp))
			{
				_logger.LogWarning($"Value from {_googleDriveRegValPropName} did not have property named {_googleDriveRegValPropPropName}");
				return null;
			}

			var path = googleDriveRegValPropProp.GetString();
			if (path is not null) 
				return ConvertDriveLetterToPathAndValidate(ref path) ? path : null;

			_logger.LogWarning($"Could not get string from value from {_googleDriveRegValPropPropName}");
			return null;
		}

		/// <summary>
		/// If Google Drive is mounted as a drive, then the path found in the registry will be
		/// *just* the drive letter (e.g. just "G" as opposed to "G:\"), and therefore must be
		/// reformatted as a valid path.
		/// </summary>
		private static bool ConvertDriveLetterToPathAndValidate(ref string path)
		{
			if (path.Length > 1) 
				return ValidatePath(path);

			DriveInfo driveInfo;
			try
			{
				driveInfo = new DriveInfo(path);
			}
			catch (ArgumentException e)
			{
				_logger.LogWarning(e, $"Could not resolve drive letter '{path}' to a valid drive.");
				return false;
			}

			path = driveInfo.RootDirectory.Name;
			return true;
		}

		private static bool ValidatePath(string path)
		{ 
			if (Directory.Exists(path))
				return true;
			_logger.LogWarning($"Invalid path: {path}");
			return false;
		}

		private static async Task<StorageFile?> GetGoogleDriveIconFileAsync()
		{
			var programFilesEnvVar = Environment.GetEnvironmentVariable("ProgramFiles");

			if (programFilesEnvVar is null)
				return null;

			var iconPath = Path.Combine(programFilesEnvVar, "Google", "Drive File Stream", "drive_fs.ico");

			return await FilesystemTasks.Wrap(() => StorageFile.GetFileFromPathAsync(iconPath).AsTask());
		}

		private static bool AddMyDriveToPathAndValidate(ref string path)
		{ 
			// If `path` contains a shortcut named "My Drive", store its target in `shellFolderBaseFirst`.
			// This happens when "My Drive syncing options" is set to "Mirror files".
			// TODO: Avoid to use Vanara (#15000)
			using var rootFolder = ShellFolderExtensions.GetShellItemFromPathOrPIDL(path) as ShellFolder;
			var myDriveFolder = Environment.ExpandEnvironmentVariables((
					rootFolder?.FirstOrDefault(si =>
						si.Name?.Equals("My Drive") ?? false) as ShellLink)?.TargetPath
				?? string.Empty);

			Debug.WriteLine("SHELL FOLDER LOGGING");
			rootFolder?.ForEach(si => Debug.WriteLine(si.Name));

			if (!string.IsNullOrEmpty(myDriveFolder))
			{
				path = myDriveFolder;
				return true;
			}

			path = Path.Combine(path, "My Drive");
			return ValidatePath(path);
		}
	}
}
