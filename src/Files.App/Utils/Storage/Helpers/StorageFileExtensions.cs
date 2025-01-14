// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.IO;
using System.Text;
using Windows.Storage;
using Windows.Storage.Search;

namespace Files.App.Utils.Storage
{
	public static class StorageFileExtensions
	{
		private const int SINGLE_DOT_DIRECTORY_LENGTH = 2;
		private const int DOUBLE_DOT_DIRECTORY_LENGTH = 3;

		public static readonly ImmutableHashSet<string> _ftpPaths =
			new HashSet<string>() { "ftp:/", "ftps:/", "ftpes:/" }.ToImmutableHashSet();

		public static BaseStorageFile? AsBaseStorageFile(this IStorageItem item)
		{
			if (item is null || !item.IsOfType(StorageItemTypes.File))
				return null;

			return item is StorageFile file ? (BaseStorageFile)file : item as BaseStorageFile;
		}

		public static async Task<List<IStorageItem>> ToStandardStorageItemsAsync(this IEnumerable<IStorageItem> items)
		{
			var newItems = new List<IStorageItem>();
			foreach (var item in items)
			{
				try
				{
					if (item is null)
					{
					}
					else if (item.IsOfType(StorageItemTypes.File))
					{
						newItems.Add(await item.AsBaseStorageFile().ToStorageFileAsync());
					}
					else if (item.IsOfType(StorageItemTypes.Folder))
					{
						newItems.Add(await item.AsBaseStorageFolder().ToStorageFolderAsync());
					}
				}
				catch (NotSupportedException)
				{
					// Ignore items that can't be converted
				}
			}
			return newItems;
		}

		public static bool AreItemsInSameDrive(this IEnumerable<string> itemsPath, string destinationPath)
		{
			try
			{
				var destinationRoot = Path.GetPathRoot(destinationPath);
				return itemsPath.Any(itemPath => Path.GetPathRoot(itemPath).Equals(destinationRoot, StringComparison.OrdinalIgnoreCase));
			}
			catch
			{
				return false;
			}
		}
		public static bool AreItemsInSameDrive(this IEnumerable<IStorageItem> storageItems, string destinationPath)
			=> storageItems.Select(x => x.Path).AreItemsInSameDrive(destinationPath);
		public static bool AreItemsInSameDrive(this IEnumerable<IStorageItemWithPath> storageItems, string destinationPath)
			=> storageItems.Select(x => x.Path).AreItemsInSameDrive(destinationPath);

		public static bool AreItemsAlreadyInFolder(this IEnumerable<string> itemsPath, string destinationPath)
		{
			try
			{
				var trimmedPath = destinationPath.TrimPath();
				return itemsPath.All(itemPath => Path.GetDirectoryName(itemPath).Equals(trimmedPath, StringComparison.OrdinalIgnoreCase));
			}
			catch
			{
				return false;
			}
		}
		public static bool AreItemsAlreadyInFolder(this IEnumerable<IStorageItem> storageItems, string destinationPath)
			=> storageItems.Select(x => x.Path).AreItemsAlreadyInFolder(destinationPath);
		public static bool AreItemsAlreadyInFolder(this IEnumerable<IStorageItemWithPath> storageItems, string destinationPath)
			=> storageItems.Select(x => x.Path).AreItemsAlreadyInFolder(destinationPath);

		public static BaseStorageFolder? AsBaseStorageFolder(this IStorageItem item)
		{
			if (item is not null && item.IsOfType(StorageItemTypes.Folder))
				return item is StorageFolder folder ? (BaseStorageFolder)folder : item as BaseStorageFolder;

			return null;
		}

		public static List<PathBoxItem> GetDirectoryPathComponents(string value)
		{
			List<PathBoxItem> pathBoxItems = [];

			if (value.Contains('/', StringComparison.Ordinal))
			{
				if (!value.EndsWith('/'))
					value += "/";
			}
			else if (!value.EndsWith('\\'))
			{
				value += "\\";
			}

			int lastIndex = 0;

			for (var i = 0; i < value.Length; i++)
			{
				if (value[i] is '?' || value[i] == Path.DirectorySeparatorChar || value[i] == Path.AltDirectorySeparatorChar)
				{
					if (lastIndex == i)
					{
						++lastIndex;
						continue;
					}

					var component = value.Substring(lastIndex, i - lastIndex);
					var path = value.Substring(0, i + 1);
					if (!_ftpPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
						pathBoxItems.Add(GetPathItem(component, path));

					lastIndex = i + 1;
				}
			}

			return pathBoxItems;
		}

		public static async Task<List<PathBoxItem>> GetDirectoryPathComponentsWithDisplayNameAsync(string value)
		{
			var pathBoxItems = GetDirectoryPathComponents(value);

			foreach (var item in pathBoxItems)
			{
				if (item.Path == "Home")
					item.Title = "Home".GetLocalizedResource();
				else
				{
					BaseStorageFolder folder = await FilesystemTasks.Wrap(() => DangerousGetFolderFromPathAsync(item.Path));

					if (!string.IsNullOrEmpty(folder?.DisplayName))
						item.Title = folder.DisplayName;
				}
			}

			return pathBoxItems;
		}

		public static string GetResolvedPath(string path, bool isFtp)
		{
			var withoutEnvirnment = GetPathWithoutEnvironmentVariable(path);
			return ResolvePath(withoutEnvirnment, isFtp);
		}

		public async static Task<BaseStorageFile> DangerousGetFileFromPathAsync
			(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
				=> (await DangerousGetFileWithPathFromPathAsync(value, rootFolder, parentFolder)).Item;
		public async static Task<StorageFileWithPath> DangerousGetFileWithPathFromPathAsync
			(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
		{
			if (rootFolder is not null)
			{
				var currComponents = GetDirectoryPathComponents(value);

				if (parentFolder is not null && value.IsSubPathOf(parentFolder.Path))
				{
					var folder = parentFolder.Item;
					var prevComponents = GetDirectoryPathComponents(parentFolder.Path);
					var path = parentFolder.Path;
					foreach (var component in currComponents.ExceptBy(prevComponents, c => c.Path).SkipLast(1))
					{
						folder = await folder.GetFolderAsync(component.Title);
						path = PathNormalization.Combine(path, folder.Name);
					}
					var file = await folder.GetFileAsync(currComponents.Last().Title);
					path = PathNormalization.Combine(path, file.Name);
					return new StorageFileWithPath(file, path);
				}
				else if (value.IsSubPathOf(rootFolder.Path))
				{
					var folder = rootFolder.Item;
					var path = rootFolder.Path;
					foreach (var component in currComponents.Skip(1).SkipLast(1))
					{
						folder = await folder.GetFolderAsync(component.Title);
						path = PathNormalization.Combine(path, folder.Name);
					}
					var file = await folder.GetFileAsync(currComponents.Last().Title);
					path = PathNormalization.Combine(path, file.Name);
					return new StorageFileWithPath(file, path);
				}
			}

			var fullPath = (parentFolder is not null && !FtpHelpers.IsFtpPath(value) && !Path.IsPathRooted(value) && !ShellStorageFolder.IsShellPath(value)) // "::{" not a valid root
				? Path.GetFullPath(Path.Combine(parentFolder.Path, value)) // Relative path
				: value;
			var item = await BaseStorageFile.GetFileFromPathAsync(fullPath);

			if (parentFolder is not null && parentFolder.Item is IPasswordProtectedItem ppis && item is IPasswordProtectedItem ppid)
				ppid.Credentials = ppis.Credentials;

			return new StorageFileWithPath(item);
		}
		public async static Task<IList<StorageFileWithPath>> GetFilesWithPathAsync
			(this StorageFolderWithPath parentFolder, uint maxNumberOfItems = uint.MaxValue)
				=> (await parentFolder.Item.GetFilesAsync(CommonFileQuery.DefaultQuery, 0, maxNumberOfItems))
					.Select(x => new StorageFileWithPath(x, string.IsNullOrEmpty(x.Path) ? PathNormalization.Combine(parentFolder.Path, x.Name) : x.Path)).ToList();

		public async static Task<BaseStorageFolder> DangerousGetFolderFromPathAsync
			(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
				=> (await DangerousGetFolderWithPathFromPathAsync(value, rootFolder, parentFolder)).Item;
		public async static Task<StorageFolderWithPath> DangerousGetFolderWithPathFromPathAsync
			(string value, StorageFolderWithPath rootFolder = null, StorageFolderWithPath parentFolder = null)
		{
			if (rootFolder is not null)
			{
				var currComponents = GetDirectoryPathComponents(value);

				if (rootFolder.Path == value)
				{
					return rootFolder;
				}
				else if (parentFolder is not null && value.IsSubPathOf(parentFolder.Path))
				{
					var folder = parentFolder.Item;
					var prevComponents = GetDirectoryPathComponents(parentFolder.Path);
					var path = parentFolder.Path;
					foreach (var component in currComponents.ExceptBy(prevComponents, c => c.Path))
					{
						folder = await folder.GetFolderAsync(component.Title);
						path = PathNormalization.Combine(path, folder.Name);
					}
					return new StorageFolderWithPath(folder, path);
				}
				else if (value.IsSubPathOf(rootFolder.Path))
				{
					var folder = rootFolder.Item;
					var path = rootFolder.Path;
					foreach (var component in currComponents.Skip(1))
					{
						folder = await folder.GetFolderAsync(component.Title);
						path = PathNormalization.Combine(path, folder.Name);
					}
					return new StorageFolderWithPath(folder, path);
				}
			}

			var fullPath = (parentFolder is not null && !FtpHelpers.IsFtpPath(value) && !Path.IsPathRooted(value) && !ShellStorageFolder.IsShellPath(value)) // "::{" not a valid root
				? Path.GetFullPath(Path.Combine(parentFolder.Path, value)) // Relative path
				: value;
			var item = await BaseStorageFolder.GetFolderFromPathAsync(fullPath);

			if (parentFolder is not null && parentFolder.Item is IPasswordProtectedItem ppis && item is IPasswordProtectedItem ppid)
				ppid.Credentials = ppis.Credentials;

			return new StorageFolderWithPath(item);
		}
		public async static Task<IList<StorageFolderWithPath>> GetFoldersWithPathAsync
			(this StorageFolderWithPath parentFolder, uint maxNumberOfItems = uint.MaxValue)
				=> (await parentFolder.Item.GetFoldersAsync(CommonFolderQuery.DefaultQuery, 0, maxNumberOfItems))
					.Select(x => new StorageFolderWithPath(x, string.IsNullOrEmpty(x.Path) ? PathNormalization.Combine(parentFolder.Path, x.Name) : x.Path)).ToList();
		public async static Task<IList<StorageFolderWithPath>> GetFoldersWithPathAsync
			(this StorageFolderWithPath parentFolder, string nameFilter, uint maxNumberOfItems = uint.MaxValue)
		{
			if (parentFolder is null)
				return null;

			var queryOptions = new QueryOptions
			{
				ApplicationSearchFilter = $"System.FileName:{nameFilter}*"
			};
			BaseStorageFolderQueryResult queryResult = parentFolder.Item.CreateFolderQueryWithOptions(queryOptions);

			return (await queryResult.GetFoldersAsync(0, maxNumberOfItems))
				.Select(x => new StorageFolderWithPath(x, string.IsNullOrEmpty(x.Path) ? PathNormalization.Combine(parentFolder.Path, x.Name) : x.Path)).ToList();
		}

		private static PathBoxItem GetPathItem(string component, string path)
		{
			var title = string.Empty;
			if (component.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
			{
				// Handle the recycle bin: use the localized folder name
				title = "RecycleBin".GetLocalizedResource();
			}
			else if (component.StartsWith(Constants.UserEnvironmentPaths.MyComputerPath, StringComparison.Ordinal))
			{
				title = "ThisPC".GetLocalizedResource();
			}
			else if (component.StartsWith(Constants.UserEnvironmentPaths.NetworkFolderPath, StringComparison.Ordinal))
			{
				title = "Network".GetLocalizedResource();
			}
			else if (component.EndsWith(':'))
			{
				var drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

				var drives = drivesViewModel.Drives.Cast<DriveItem>();
				var drive = drives.FirstOrDefault(y => y.ItemType is NavigationControlItemType.Drive && y.Path.Contains(component, StringComparison.OrdinalIgnoreCase));
				title = drive is not null ? drive.Text : string.Format("DriveWithLetter".GetLocalizedResource(), component);
			}
			else
			{
				if (path.EndsWith('\\') || path.EndsWith('/'))
					path = path.Remove(path.Length - 1);

				title = component;
			}

			return new PathBoxItem()
			{
				Title = title,
				Path = path
			};
		}

		private static string GetPathWithoutEnvironmentVariable(string path)
		{
			if (path.StartsWith("~\\", StringComparison.Ordinal) || path.StartsWith("~/", StringComparison.Ordinal) || path.Equals("~", StringComparison.Ordinal))
				path = $"{Constants.UserEnvironmentPaths.HomePath}{path.Remove(0, 1)}";

			path = path.Replace("%temp%", Constants.UserEnvironmentPaths.TempPath, StringComparison.OrdinalIgnoreCase);

			path = path.Replace("%tmp%", Constants.UserEnvironmentPaths.TempPath, StringComparison.OrdinalIgnoreCase);

			path = path.Replace("%localappdata%", Constants.UserEnvironmentPaths.LocalAppDataPath, StringComparison.OrdinalIgnoreCase);

			path = path.Replace("%homepath%", Constants.UserEnvironmentPaths.HomePath, StringComparison.OrdinalIgnoreCase);

			return Environment.ExpandEnvironmentVariables(path);
		}

		private static string ResolvePath(string path, bool isFtp)
		{
			if (path.StartsWith("Home"))
				return "Home";

			if (ShellStorageFolder.IsShellPath(path))
				return ShellHelpers.ResolveShellPath(path);

			var pathBuilder = new StringBuilder(path);
			var lastPathIndex = path.Length - 1;
			var separatorChar = isFtp || path.Contains('/', StringComparison.Ordinal) ? '/' : '\\';
			var rootIndex = isFtp ? FtpHelpers.GetRootIndex(path) + 1 : path.IndexOf($":{separatorChar}", StringComparison.Ordinal) + 2;

			for (int i = 0, lastIndex = 0; i < pathBuilder.Length; i++)
			{
				if (pathBuilder[i] is not '?' &&
					pathBuilder[i] != Path.DirectorySeparatorChar &&
					pathBuilder[i] != Path.AltDirectorySeparatorChar &&
					i != lastPathIndex)
					continue;

				if (lastIndex == i)
				{
					++lastIndex;
					continue;
				}

				var component = pathBuilder.ToString().Substring(lastIndex, i - lastIndex);
				if (component is "..")
				{
					if (lastIndex is 0)
					{
						SetCurrentWorkingDirectory(pathBuilder, separatorChar, lastIndex, ref i);
					}
					else if (lastIndex == rootIndex)
					{
						pathBuilder.Remove(lastIndex, DOUBLE_DOT_DIRECTORY_LENGTH);
						i = lastIndex - 1;
					}
					else
					{
						var directoryIndex = pathBuilder.ToString().LastIndexOf(
							separatorChar,
							lastIndex - DOUBLE_DOT_DIRECTORY_LENGTH);

						if (directoryIndex is not -1)
						{
							pathBuilder.Remove(directoryIndex, i - directoryIndex);
							i = directoryIndex;
						}
					}

					lastPathIndex = pathBuilder.Length - 1;
				}
				else if (component is ".")
				{
					if (lastIndex is 0)
					{
						SetCurrentWorkingDirectory(pathBuilder, separatorChar, lastIndex, ref i);
					}
					else
					{
						pathBuilder.Remove(lastIndex, SINGLE_DOT_DIRECTORY_LENGTH);
						i -= 3;
					}
					lastPathIndex = pathBuilder.Length - 1;
				}

				lastIndex = i + 1;
			}

			return pathBuilder.ToString();
		}

		private static void SetCurrentWorkingDirectory(StringBuilder path, char separator, int substringIndex, ref int i)
		{
			var context = Ioc.Default.GetRequiredService<IContentPageContext>();
			var subPath = path.ToString().Substring(substringIndex);

			path.Clear();
			path.Append(context.ShellPage?.ShellViewModel.WorkingDirectory);
			path.Append(separator);
			path.Append(subPath);
			i = -1;
		}
	}
}
