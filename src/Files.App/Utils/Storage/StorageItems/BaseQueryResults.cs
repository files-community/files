// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;

namespace Files.App.Utils.Storage
{
	public class BaseStorageItemQueryResult
	{
		public BaseStorageFolder Folder { get; }
		public QueryOptions Options { get; }

		public BaseStorageItemQueryResult(BaseStorageFolder folder, QueryOptions options)
		{
			Folder = folder;
			Options = options;
		}

		public virtual IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxNumberOfItems)
		{
			return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
			{
				var items = (await GetItemsAsync()).Skip((int)startIndex).Take((int)Math.Min(maxNumberOfItems, int.MaxValue));
				return items.ToList();
			});
		}

		public virtual IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
		{
			return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
			{
				var items = await Folder.GetItemsAsync();
				var query = string.Join(' ', Options.ApplicationSearchFilter, Options.UserSearchFilter).Trim();
				if (!string.IsNullOrEmpty(query))
				{
					var spaceSplit = RegexHelpers.SpaceSplit().Split(query);
					foreach (var split in spaceSplit)
					{
						var colonSplit = split.Split(':');
						if (colonSplit.Length == 2)
						{
							if (colonSplit[0] == "System.FileName" || colonSplit[0] == "fileName" || colonSplit[0] == "name")
							{
								items = items.Where(x => Regex.IsMatch(x.Name, Regex.Escape(colonSplit[1].Replace("\"", "", StringComparison.Ordinal)).Replace("\\*", ".*").Replace("\\?", "."), RegexOptions.IgnoreCase)).ToList();
							}
						}
						else
						{
							items = items.Where(x => Regex.IsMatch(x.Name, Regex.Escape(split.Replace("\"", "", StringComparison.Ordinal)).Replace("\\*", ".*").Replace("\\?", "."), RegexOptions.IgnoreCase)).ToList();
						}
					}
				}
				return items.ToList();
			});
		}

		public virtual StorageItemQueryResult ToStorageItemQueryResult() => null;
	}

	public class BaseStorageFileQueryResult
	{
		public BaseStorageFolder Folder { get; }
		public QueryOptions Options { get; }

		public BaseStorageFileQueryResult(BaseStorageFolder folder, QueryOptions options)
		{
			Folder = folder;
			Options = options;
		}

		public virtual IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(uint startIndex, uint maxNumberOfItems)
		{
			return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
			{
				var items = (await GetFilesAsync()).Skip((int)startIndex).Take((int)Math.Min(maxNumberOfItems, int.MaxValue));
				return items.ToList();
			});
		}

		public virtual IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
		{
			return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
			{
				var items = await Folder.GetFilesAsync();
				var query = string.Join(' ', Options.ApplicationSearchFilter, Options.UserSearchFilter).Trim();
				if (!string.IsNullOrEmpty(query))
				{
					var spaceSplit = RegexHelpers.SpaceSplit().Split(query);
					foreach (var split in spaceSplit)
					{
						var colonSplit = split.Split(':');
						if (colonSplit.Length == 2)
						{
							if (colonSplit[0] == "System.FileName" || colonSplit[0] == "fileName" || colonSplit[0] == "name")
							{
								items = items.Where(x => Regex.IsMatch(x.Name, colonSplit[1].Replace("\"", "", StringComparison.Ordinal).Replace("*", "(.*?)", StringComparison.Ordinal), RegexOptions.IgnoreCase)).ToList();
							}
						}
						else
						{
							items = items.Where(x => Regex.IsMatch(x.Name, split.Replace("\"", "", StringComparison.Ordinal).Replace("*", "(.*?)", StringComparison.Ordinal), RegexOptions.IgnoreCase)).ToList();
						}
					}
				}
				return items.ToList();
			});
		}

		public virtual StorageFileQueryResult ToStorageFileQueryResult() => null;
	}

	public class BaseStorageFolderQueryResult
	{
		public BaseStorageFolder Folder { get; }
		public QueryOptions Options { get; }

		public BaseStorageFolderQueryResult(BaseStorageFolder folder, QueryOptions options)
		{
			Folder = folder;
			Options = options;
		}

		public virtual IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(uint startIndex, uint maxNumberOfItems)
		{
			return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
			{
				var items = (await GetFoldersAsync()).Skip((int)startIndex).Take((int)Math.Min(maxNumberOfItems, int.MaxValue));
				return items.ToList();
			});
		}

		public virtual IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
		{
			return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
			{
				var items = await Folder.GetFoldersAsync();
				var query = string.Join(' ', Options.ApplicationSearchFilter, Options.UserSearchFilter).Trim();
				if (!string.IsNullOrEmpty(query))
				{
					var spaceSplit = RegexHelpers.SpaceSplit().Split(query);
					foreach (var split in spaceSplit)
					{
						var colonSplit = split.Split(':');
						if (colonSplit.Length == 2)
						{
							if (colonSplit[0] == "System.FileName" || colonSplit[0] == "fileName" || colonSplit[0] == "name")
							{
								items = items.Where(x => Regex.IsMatch(x.Name, colonSplit[1].Replace("\"", "", StringComparison.Ordinal).Replace("*", "(.*?)", StringComparison.Ordinal), RegexOptions.IgnoreCase)).ToList();
							}
						}
						else
						{
							items = items.Where(x => Regex.IsMatch(x.Name, split.Replace("\"", "", StringComparison.Ordinal).Replace("*", "(.*?)", StringComparison.Ordinal), RegexOptions.IgnoreCase)).ToList();
						}
					}
				}
				return items.ToList();
			});
		}

		public virtual StorageFolderQueryResult ToStorageFolderQueryResult() => null;
	}

	public sealed class SystemStorageItemQueryResult : BaseStorageItemQueryResult
	{
		private StorageItemQueryResult StorageItemQueryResult { get; }

		public SystemStorageItemQueryResult(StorageItemQueryResult sfqr) : base(sfqr.Folder, sfqr.GetCurrentQueryOptions())
		{
			StorageItemQueryResult = sfqr;
		}

		public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxNumberOfItems)
		{
			return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
			{
				var items = await StorageItemQueryResult.GetItemsAsync(startIndex, maxNumberOfItems);
				return items.Select(x => x is StorageFolder ? (IStorageItem)new SystemStorageFolder(x as StorageFolder) : new SystemStorageFile(x as StorageFile)).ToList();
			});
		}

		public override IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
		{
			return AsyncInfo.Run<IReadOnlyList<IStorageItem>>(async (cancellationToken) =>
			{
				var items = await StorageItemQueryResult.GetItemsAsync();
				return items.Select(x => x is StorageFolder ? (IStorageItem)new SystemStorageFolder(x as StorageFolder) : new SystemStorageFile(x as StorageFile)).ToList();
			});
		}

		public override StorageItemQueryResult ToStorageItemQueryResult() => StorageItemQueryResult;
	}

	public sealed class SystemStorageFileQueryResult : BaseStorageFileQueryResult
	{
		private StorageFileQueryResult StorageFileQueryResult { get; }

		public SystemStorageFileQueryResult(StorageFileQueryResult sfqr) : base(sfqr.Folder, sfqr.GetCurrentQueryOptions())
		{
			StorageFileQueryResult = sfqr;
		}

		public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(uint startIndex, uint maxNumberOfItems)
		{
			return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
			{
				var items = await StorageFileQueryResult.GetFilesAsync(startIndex, maxNumberOfItems);
				return items.Select(x => new SystemStorageFile(x)).ToList();
			});
		}

		public override IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync()
		{
			return AsyncInfo.Run<IReadOnlyList<BaseStorageFile>>(async (cancellationToken) =>
			{
				var items = await StorageFileQueryResult.GetFilesAsync();
				return items.Select(x => new SystemStorageFile(x)).ToList();
			});
		}

		public override StorageFileQueryResult ToStorageFileQueryResult() => StorageFileQueryResult;
	}

	public sealed class SystemStorageFolderQueryResult : BaseStorageFolderQueryResult
	{
		private StorageFolderQueryResult StorageFolderQueryResult { get; }

		public SystemStorageFolderQueryResult(StorageFolderQueryResult sfqr) : base(sfqr.Folder, sfqr.GetCurrentQueryOptions())
		{
			StorageFolderQueryResult = sfqr;
		}

		public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(uint startIndex, uint maxNumberOfItems)
		{
			return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
			{
				var items = await StorageFolderQueryResult.GetFoldersAsync(startIndex, maxNumberOfItems);
				return items.Select(x => new SystemStorageFolder(x)).ToList();
			});
		}

		public override IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync()
		{
			return AsyncInfo.Run<IReadOnlyList<BaseStorageFolder>>(async (cancellationToken) =>
			{
				var items = await StorageFolderQueryResult.GetFoldersAsync();
				return items.Select(x => new SystemStorageFolder(x)).ToList();
			});
		}

		public override StorageFolderQueryResult ToStorageFolderQueryResult() => StorageFolderQueryResult;
	}
}
