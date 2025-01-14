// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Utils.Storage
{
	public sealed class ShortcutStorageFolder : ShellStorageFolder, IShortcutStorageItem
	{
		public string TargetPath { get; }
		public string Arguments { get; }
		public string WorkingDirectory { get; }
		public bool RunAsAdmin { get; }
		public SHOW_WINDOW_CMD ShowWindowCommand { get; set; }

		public ShortcutStorageFolder(ShellLinkItem item) : base(item)
		{
			TargetPath = item.TargetPath;
			Arguments = item.Arguments;
			WorkingDirectory = item.WorkingDirectory;
			RunAsAdmin = item.RunAsAdmin;
			ShowWindowCommand = item.ShowWindowCommand;
		}
	}

	public interface IShortcutStorageItem : IStorageItem
	{
		string TargetPath { get; }
		string Arguments { get; }
		string WorkingDirectory { get; }
		bool RunAsAdmin { get; }
	}

	public sealed class BinStorageFolder : ShellStorageFolder, IBinStorageItem
	{
		public string OriginalPath { get; }
		public DateTimeOffset DateDeleted { get; }

		public BinStorageFolder(ShellFileItem item) : base(item)
		{
			OriginalPath = item.FilePath;
			DateDeleted = item.RecycleDate;
		}
	}

	public interface IBinStorageItem : IStorageItem
	{
		string OriginalPath { get; }
		DateTimeOffset DateDeleted { get; }
	}

	public class ShellStorageFolder : BaseStorageFolder
	{
		public override string Path { get; }
		public override string Name { get; }
		public override string DisplayName => Name;
		public override string DisplayType { get; }
		public override string FolderRelativeId => $"0\\{Name}";

		public override DateTimeOffset DateCreated { get; }
		public override Windows.Storage.FileAttributes Attributes => Windows.Storage.FileAttributes.Directory;
		public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

		public ShellStorageFolder(ShellFileItem item)
		{
			Name = item.FileName;
			Path = item.RecyclePath; // True path on disk
			DateCreated = item.CreatedDate;
			DisplayType = item.FileType;
		}

		public static bool IsShellPath(string path)
		{
			return path is not null &&
				path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) ||
				path.StartsWith("::{", StringComparison.Ordinal) ||
				path.StartsWith(@"\\SHELL\", StringComparison.Ordinal);
		}

		public static ShellStorageFolder FromShellItem(ShellFileItem item)
		{
			if (item is ShellLinkItem linkItem)
				return new ShortcutStorageFolder(linkItem);

			if (item.RecyclePath.Contains("$Recycle.Bin", StringComparison.OrdinalIgnoreCase))
				return new BinStorageFolder(item);

			return new ShellStorageFolder(item);
		}

		public static IAsyncOperation<BaseStorageFolder> FromPathAsync(string path)
		{
			return AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) =>
			{
				if (IsShellPath(path))
				{
					var res = await GetFolderAndItems(path, false);
					if (res.Folder is not null)
					{
						return FromShellItem(res.Folder);
					}
				}
				return null;
			});
		}

		protected static async Task<(ShellFileItem Folder, List<ShellFileItem> Items)> GetFolderAndItems(string path, bool enumerate, int startIndex = 0, int maxItemsToRetrieve = int.MaxValue)
		{
			return await Win32Helper.GetShellFolderAsync(path, !enumerate, enumerate, startIndex, maxItemsToRetrieve);
		}

		public override IAsyncOperation<StorageFolder> ToStorageFolderAsync() => throw new NotSupportedException();

		public override bool IsEqual(IStorageItem item) => item?.Path == Path;
		public override bool IsOfType(StorageItemTypes type) => type == StorageItemTypes.Folder;

		public override IAsyncOperation<IndexedState> GetIndexedStateAsync() => Task.FromResult(IndexedState.NotIndexed).AsAsyncOperation();

		public override IAsyncOperation<BaseStorageFolder> GetParentAsync() => throw new NotSupportedException();

		public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				var res = await GetFolderAndItems(Path, false);
				return res.Folder is not null ? new ShellFolderBasicProperties(res.Folder) : new BaseBasicProperties();
			});
		}

		public override IAsyncOperation<IStorageItem> GetItemAsync(string name)
		{
			return AsyncInfo.Run<IStorageItem>(async (cancellationToken) =>
			{
				var res = await GetFolderAndItems(Path, true);
				if (res.Items is null)
				{
					return null;
				}

				var entry = res.Items.FirstOrDefault(x => x.FileName is not null && x.FileName.Equals(name, StringComparison.OrdinalIgnoreCase));
				if (entry is null)
				{
					return null;
				}

				if (entry.IsFolder)
				{
					return ShellStorageFolder.FromShellItem(entry);
				}

				return ShellStorageFile.FromShellItem(entry);
			});
		}
		public override IAsyncOperation<IStorageItem> TryGetItemAsync(string name)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				try
				{
					return await GetItemAsync(name);
				}
				catch
				{
					return null;
				}
			});
		}
		public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
			=> AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken)
				=> (await GetItemsAsync(0, int.MaxValue)).ToList()
			);
		public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxItemsToRetrieve)
		{
			return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
			{
				var res = await GetFolderAndItems(Path, true, (int)startIndex, (int)maxItemsToRetrieve);
				if (res.Items is null)
				{
					return null;
				}

				var items = new List<IStorageItem>();
				foreach (var entry in res.Items)
				{
					if (entry.IsFolder)
					{
						items.Add(ShellStorageFolder.FromShellItem(entry));
					}
					else
					{
						items.Add(ShellStorageFile.FromShellItem(entry));
					}
				}
				return items;
			});
		}

		public override IAsyncOperation<BaseStorageFile> GetFileAsync(string name)
			=> AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) => await GetItemAsync(name) as ShellStorageFile);
		public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) => (await GetItemsAsync())?.OfType<ShellStorageFile>().ToList());
		public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query)
			=> AsyncInfo.Run(async (cancellationToken) => await GetFilesAsync());
		public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve)
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken)
				=> (await GetFilesAsync()).Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList()
			);

		public override IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name)
			=> AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken) => await GetItemAsync(name) as ShellStorageFolder);
		public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
			=> AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) => (await GetItemsAsync())?.OfType<ShellStorageFolder>().ToList());
		public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query)
			=> AsyncInfo.Run(async (cancellationToken) => await GetFoldersAsync());
		public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve)
		{
			return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
			{
				var items = await GetFoldersAsync();
				return items.Skip((int)startIndex).Take((int)maxItemsToRetrieve).ToList();
			});
		}

		public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName) => throw new NotSupportedException();
		public override IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
			=> throw new NotSupportedException();

		public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName) => throw new NotSupportedException();
		public override IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options)
			=> throw new NotSupportedException();

		public override IAsyncOperation<BaseStorageFolder> MoveAsync(IStorageFolder destinationFolder) => throw new NotSupportedException();
		public override IAsyncOperation<BaseStorageFolder> MoveAsync(IStorageFolder destinationFolder, NameCollisionOption option) => throw new NotSupportedException();

		public override IAsyncAction RenameAsync(string desiredName) => throw new NotSupportedException();
		public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option) => throw new NotSupportedException();

		public override IAsyncAction DeleteAsync() => throw new NotSupportedException();
		public override IAsyncAction DeleteAsync(StorageDeleteOption option) => throw new NotSupportedException();

		public override bool AreQueryOptionsSupported(QueryOptions queryOptions) => false;
		public override bool IsCommonFileQuerySupported(CommonFileQuery query) => false;
		public override bool IsCommonFolderQuerySupported(CommonFolderQuery query) => false;

		public override StorageItemQueryResult CreateItemQuery() => throw new NotSupportedException();
		public override BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions) => new(this, queryOptions);

		public override StorageFileQueryResult CreateFileQuery() => throw new NotSupportedException();
		public override StorageFileQueryResult CreateFileQuery(CommonFileQuery query) => throw new NotSupportedException();
		public override BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions) => new(this, queryOptions);

		public override StorageFolderQueryResult CreateFolderQuery() => throw new NotSupportedException();
		public override StorageFolderQueryResult CreateFolderQuery(CommonFolderQuery query) => throw new NotSupportedException();
		public override BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions) => new(this, queryOptions);

		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				if (IsShellPath(Path))
				{
					return null;
				}
				var zipFolder = await StorageFolder.GetFolderFromPathAsync(Path);
				return await zipFolder.GetThumbnailAsync(mode);
			});
		}
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				if (IsShellPath(Path))
				{
					return null;
				}
				var zipFolder = await StorageFolder.GetFolderFromPathAsync(Path);
				return await zipFolder.GetThumbnailAsync(mode, requestedSize);
			});
		}
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				if (IsShellPath(Path))
				{
					return null;
				}
				var zipFolder = await StorageFolder.GetFolderFromPathAsync(Path);
				return await zipFolder.GetThumbnailAsync(mode, requestedSize, options);
			});
		}

		private sealed class ShellFolderBasicProperties : BaseBasicProperties
		{
			private readonly ShellFileItem folder;

			public ShellFolderBasicProperties(ShellFileItem folder) => this.folder = folder;

			public override ulong Size => folder.FileSizeBytes;

			public override DateTimeOffset DateCreated => folder.CreatedDate;
			public override DateTimeOffset DateModified => folder.ModifiedDate;
		}
	}
}
