// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Cloud;
using Microsoft.Data.Sqlite;
using System.IO;
using Windows.Storage;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides an utility for Synology Drive Cloud detection.
	/// </summary>
	public sealed class SynologyDriveCloudDetector : AbstractCloudDetector
	{
		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			// Synology Drive stores its information on some files, but we only need sys.sqlite, which is placed on % LocalAppData %\SynologyDrive\data\db
			// In this database we need "connection_table" and "session_table" tables:
			// connection_table has the ids of each connection in the field "id", and the type of connection in the field "conn_type" (1 for sync tasks and 2 for backups)
			// Also it has "host_name" field where it's placed the name of each server.
			// session_table has the next fields:
			// "conn_id", which has the id that we check on connection_table to see if it's a sync or backup task
			// "remote_path", which has the server folder.Currently it's not needed, just adding in case in the future is needed.
			// "sync_folder", which has the local folder to sync.

			string appDataPath = UserDataPaths.GetDefault().LocalAppData;

			var configFile = await StorageFile.GetFileFromPathAsync(Path.Combine(appDataPath, @"SynologyDrive\data\db\sys.sqlite"));
			await configFile.CopyAsync(ApplicationData.Current.TemporaryFolder, "synology_drive.db", NameCollisionOption.ReplaceExisting);

			var syncDbPath = Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "synology_drive.db");

			// Build the connection and SQL command
			SQLitePCL.Batteries_V2.Init();
			await using var database = new SqliteConnection($"Data Source='{syncDbPath}'");
			await using var cmdConnection = new SqliteCommand("SELECT * FROM connection_table", database);
			await using var cmdTable = new SqliteCommand("SELECT * FROM session_table", database);

			// Open the connection and execute the command
			database.Open();
			var connections = new Dictionary<string, (string ConnectionType, string HostName)>();

			var reader = cmdConnection.ExecuteReader();
			while (reader.Read())
			{
				var connection =
				(
					ConnectionType: reader["conn_type"]?.ToString(),
					HostName: reader["host_name"]?.ToString()
				);

				connections.Add(reader["id"]?.ToString(), connection);
			}

			reader = cmdTable.ExecuteReader();
			while (reader.Read())
			{
				// Extract the data from the reader
				if (connections[reader["conn_id"]?.ToString()].ConnectionType is "1")
				{
					string? path = reader["sync_folder"]?.ToString();

					if (string.IsNullOrWhiteSpace(path))
						continue;

					var folder = await StorageFolder.GetFolderFromPathAsync(path);

					yield return new CloudProvider(CloudProviders.SynologyDrive)
					{
						SyncFolder = path,
						Name = $"Synology Drive - {connections[reader["conn_id"]?.ToString()].HostName} ({folder.Name})",
					};
				}
			}
		}
	}
}
