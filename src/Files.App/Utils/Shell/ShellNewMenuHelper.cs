// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using System.IO;
using System.Security;
using System.Text;
using Windows.Storage;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Provides static helper to get extension-specific shell context menu from Windows Registry.
	/// </summary>
	public static class ShellNewMenuHelper
	{
		public static async Task<List<ShellNewEntry>> GetNewContextMenuEntries()
		{
			var newMenuItems = new List<ShellNewEntry>();

			var shortcutExtensions = new string[] { ShellLibraryItem.EXTENSION, ".url", ".lnk" };

			foreach (var keyName in Registry.ClassesRoot.GetSubKeyNames().Where(x => x.StartsWith('.') && !shortcutExtensions.Contains(x, StringComparer.OrdinalIgnoreCase)))
			{
				using var key = Registry.ClassesRoot.OpenSubKeySafe(keyName);

				if (key is not null)
				{
					var ret = await GetShellNewRegistryEntries(key, key);
					if (ret is not null)
						newMenuItems.Add(ret);
				}
			}

			if (!newMenuItems.Any(x => ".txt".Equals(x.Extension, StringComparison.OrdinalIgnoreCase)))
				newMenuItems.Add(await CreateShellNewEntry(".txt", null, null, null));

			return [.. newMenuItems.OrderBy(item => item.Name)];
		}

		public static async Task<ShellNewEntry> GetNewContextMenuEntryForType(string extension)
		{
			if (string.IsNullOrEmpty(extension))
				return null;

			using var key = Registry.ClassesRoot.OpenSubKeySafe(extension);

			return key is not null ? await GetShellNewRegistryEntries(key, key) : null;
		}

		private static async Task<ShellNewEntry> GetShellNewRegistryEntries(RegistryKey current, RegistryKey root)
		{
			try
			{
				foreach (var keyName in current.GetSubKeyNames())
				{
					using var key = current.OpenSubKeySafe(keyName);

					if (key is null)
						continue;

					if (keyName == "ShellNew")
						return await ParseShellNewRegistryEntry(key, root);
					else
					{
						var ret = await GetShellNewRegistryEntries(key, root);
						if (ret is not null)
							return ret;
					}
				}
			}
			catch {
				// Ignore exceptions when the registry is inaccessible to avoid freezes
			}

			return null;
		}

		private static Task<ShellNewEntry> ParseShellNewRegistryEntry(RegistryKey key, RegistryKey root)
		{
			var valueNames = key.GetValueNames();

			if (!valueNames.Contains("NullFile", StringComparer.OrdinalIgnoreCase) &&
				!valueNames.Contains("Name", StringComparer.OrdinalIgnoreCase) &&
				!valueNames.Contains("FileName", StringComparer.OrdinalIgnoreCase) &&
				!valueNames.Contains("Command", StringComparer.OrdinalIgnoreCase) &&
				!valueNames.Contains("ItemName", StringComparer.OrdinalIgnoreCase) &&
				!valueNames.Contains("Data", StringComparer.OrdinalIgnoreCase))
			{
				return Task.FromResult<ShellNewEntry>(null);
			}

			var extension = root.Name.Substring(root.Name.LastIndexOf('\\') + 1);
			var fileName = (string)key.GetValue("FileName");
			var command = (string)key.GetValue("Command");

			byte[] data = null;
			var dataObj = key.GetValue("Data");

			if (dataObj is not null)
			{
				data = key.GetValueKind("Data") switch
				{
					RegistryValueKind.Binary => (byte[])dataObj,
					RegistryValueKind.String => Encoding.UTF8.GetBytes((string)dataObj),
					_ => (byte[])dataObj,
				};
			}

			return CreateShellNewEntry(extension, fileName, command, data);
		}

		private static async Task<ShellNewEntry> CreateShellNewEntry(string extension, string? fileName, string? command, byte[]? data)
		{
			var folder = await SafetyExtensions.IgnoreExceptions(() => ApplicationData.Current.LocalFolder.CreateFolderAsync("extensions", CreationCollisionOption.OpenIfExists).AsTask());
			var sampleFile = folder is not null ? await SafetyExtensions.IgnoreExceptions(() => folder.CreateFileAsync("file" + extension, CreationCollisionOption.OpenIfExists).AsTask()) : null;

			var displayType = sampleFile is not null ? sampleFile.DisplayType : string.Format("{0} {1}", "file", extension);
			var thumbnail = sampleFile is not null ? await SafetyExtensions.IgnoreExceptions(() => sampleFile.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.ListView, 24, Windows.Storage.FileProperties.ThumbnailOptions.UseCurrentScale).AsTask()) : null;

			string iconString = null;

			if (thumbnail is not null)
			{
				var readStream = thumbnail.AsStreamForRead();
				var bitmapData = new byte[readStream.Length];
				await readStream.ReadAsync(bitmapData, 0, bitmapData.Length);
				iconString = Convert.ToBase64String(bitmapData, 0, bitmapData.Length);
			}

			var entry = new ShellNewEntry()
			{
				Extension = extension,
				Template = fileName,
				Name = displayType,
				Command = command,
				IconBase64 = iconString,
				Data = data
			};

			return entry;
		}

		private static RegistryKey OpenSubKeySafe(this RegistryKey root, string keyName)
		{
			try
			{
				return root.OpenSubKey(keyName);
			}
			catch (SecurityException)
			{
				return null;
			}
		}
	}
}
