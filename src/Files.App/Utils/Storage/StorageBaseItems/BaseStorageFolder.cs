// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;

namespace Files.App.Utils.Storage
{
	public abstract class BaseStorageFolder : IBaseStorageFolder
	{
		public StorageProvider? Provider
			=> null;

		public abstract string Path { get; }

		public abstract string Name { get; }

		public abstract string DisplayName { get; }

		public abstract string DisplayType { get; }

		public abstract DateTimeOffset DateCreated { get; }

		public abstract FileAttributes Attributes { get; }

		public abstract string FolderRelativeId { get; }

		public abstract IStorageItemExtraProperties Properties { get; }

		StorageItemContentProperties IStorageItemProperties.Properties
			=> this is SystemStorageFolder folder ? folder.Folder.Properties : null;

		public static implicit operator BaseStorageFolder(StorageFolder value)
		{
			return
				value is not null
					? new SystemStorageFolder(value)
					: null;
		}

		public abstract IAsyncOperation<StorageFolder> ToStorageFolderAsync();

		public abstract bool IsEqual(IStorageItem item);

		public abstract bool IsOfType(StorageItemTypes type);

		public abstract IAsyncOperation<IndexedState> GetIndexedStateAsync();

		public abstract IAsyncOperation<BaseStorageFolder> GetParentAsync();

		IAsyncOperation<StorageFolder> IStorageItem2.GetParentAsync()
		{
			return AsyncInfo.Run(async (cancellationToken) => await (await GetParentAsync()).ToStorageFolderAsync());
		}

		public abstract IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync();

		IAsyncOperation<BasicProperties> IStorageItem.GetBasicPropertiesAsync()
		{
			return AsyncInfo.Run(async (cancellationToken) => await (await ToStorageFolderAsync()).GetBasicPropertiesAsync());
		}

		public abstract IAsyncOperation<IStorageItem> GetItemAsync(string name);

		public abstract IAsyncOperation<IStorageItem> TryGetItemAsync(string name);

		public abstract IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync();

		public abstract IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync(uint startIndex, uint maxItemsToRetrieve);

		public abstract IAsyncOperation<BaseStorageFile> GetFileAsync(string name);

		IAsyncOperation<StorageFile> IStorageFolder.GetFileAsync(string name)
		{
			return AsyncInfo.Run(async (cancellationToken) => await (await GetFileAsync(name)).ToStorageFileAsync());
		}

		public abstract IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync();

		IAsyncOperation<IReadOnlyList<StorageFile>> IStorageFolder.GetFilesAsync()
		{
			return
				AsyncInfo.Run<IReadOnlyList<StorageFile>>(async (cancellationToken)
					=> await Task.WhenAll((await GetFilesAsync()).Select(x => x.ToStorageFileAsync().AsTask())));
		}

		public abstract IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query);

		IAsyncOperation<IReadOnlyList<StorageFile>> IStorageFolderQueryOperations.GetFilesAsync(CommonFileQuery query)
		{
			return
				AsyncInfo.Run<IReadOnlyList<StorageFile>>(async (cancellationToken)
					=> await Task.WhenAll((await GetFilesAsync(query)).Select(x => x.ToStorageFileAsync().AsTask())));
		}

		public abstract IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve);

		IAsyncOperation<IReadOnlyList<StorageFile>> IStorageFolderQueryOperations.GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve)
		{
			return
				AsyncInfo.Run<IReadOnlyList<StorageFile>>(async (cancellationToken)
					=> await Task.WhenAll((await GetFilesAsync(query, startIndex, maxItemsToRetrieve)).Select(x => x.ToStorageFileAsync().AsTask())));
		}

		public abstract IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name);

		IAsyncOperation<StorageFolder> IStorageFolder.GetFolderAsync(string name)
		{ 
			return
				AsyncInfo.Run(async (cancellationToken)
					=> await (await GetFolderAsync(name)).ToStorageFolderAsync());
		}

		public static IAsyncOperation<BaseStorageFolder> GetFolderFromPathAsync(string path)
		{
			return
				AsyncInfo.Run(async (cancellationToken)
					=> await ZipStorageFolder.FromPathAsync(path) ?? await FtpStorageFolder.FromPathAsync(path) ?? await ShellStorageFolder.FromPathAsync(path) ?? await SystemStorageFolder.FromPathAsync(path));
		}

		public abstract IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync();

		IAsyncOperation<IReadOnlyList<StorageFolder>> IStorageFolder.GetFoldersAsync()
		{
			return
				AsyncInfo.Run<IReadOnlyList<StorageFolder>>(async (cancellationToken)
					=> await Task.WhenAll((await GetFoldersAsync()).Select(x => x.ToStorageFolderAsync().AsTask())));
		}

		public abstract IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query);

		IAsyncOperation<IReadOnlyList<StorageFolder>> IStorageFolderQueryOperations.GetFoldersAsync(CommonFolderQuery query)
		{
			return
				AsyncInfo.Run<IReadOnlyList<StorageFolder>>(async (cancellationToken)
					=> await Task.WhenAll((await GetFoldersAsync(query)).Select(x => x.ToStorageFolderAsync().AsTask())));
		}

		public abstract IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve);

		IAsyncOperation<IReadOnlyList<StorageFolder>> IStorageFolderQueryOperations.GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve)
		{
			return
				AsyncInfo.Run<IReadOnlyList<StorageFolder>>(async (cancellationToken)
					=> await Task.WhenAll((await GetFoldersAsync(query, startIndex, maxItemsToRetrieve)).Select(x => x.ToStorageFolderAsync().AsTask())));
		}

		public abstract IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName);

		IAsyncOperation<StorageFile> IStorageFolder.CreateFileAsync(string desiredName)
		{
			return
				AsyncInfo.Run(async (cancellationToken)
					=> await (await CreateFileAsync(desiredName)).ToStorageFileAsync());
		}

		public abstract IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options);

		IAsyncOperation<StorageFile> IStorageFolder.CreateFileAsync(string desiredName, CreationCollisionOption options)
		{
			return
				AsyncInfo.Run(async (cancellationToken)
					=> await (await CreateFileAsync(desiredName, options)).ToStorageFileAsync());
		}

		public abstract IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName);

		IAsyncOperation<StorageFolder> IStorageFolder.CreateFolderAsync(string desiredName)
		{
			return
				AsyncInfo.Run(async (cancellationToken)
					=> await (await CreateFolderAsync(desiredName)).ToStorageFolderAsync());
		}

		public abstract IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options);

		IAsyncOperation<StorageFolder> IStorageFolder.CreateFolderAsync(string desiredName, CreationCollisionOption options)
		{
			return
				AsyncInfo.Run(async (cancellationToken)
					=> await (await CreateFolderAsync(desiredName, options)).ToStorageFolderAsync());
		}

		public abstract IAsyncOperation<BaseStorageFolder> MoveAsync(IStorageFolder destinationFolder);

		public abstract IAsyncOperation<BaseStorageFolder> MoveAsync(IStorageFolder destinationFolder, NameCollisionOption option);

		public abstract IAsyncAction RenameAsync(string desiredName);

		public abstract IAsyncAction RenameAsync(string desiredName, NameCollisionOption option);

		public abstract IAsyncAction DeleteAsync();

		public abstract IAsyncAction DeleteAsync(StorageDeleteOption option);

		public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode);

		public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize);

		public abstract IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options);

		public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode)
		{
			return Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
		}

		public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();

		public IAsyncOperation<StorageItemThumbnail> GetScaledImageAsThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
		{
			return Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
		}

		public abstract bool AreQueryOptionsSupported(QueryOptions queryOptions);

		public abstract StorageItemQueryResult CreateItemQuery();

		public abstract BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions);

		StorageItemQueryResult IStorageFolderQueryOperations.CreateItemQueryWithOptions(QueryOptions queryOptions) => throw new NotSupportedException();

		public abstract bool IsCommonFileQuerySupported(CommonFileQuery query);

		public abstract StorageFileQueryResult CreateFileQuery();

		public abstract StorageFileQueryResult CreateFileQuery(CommonFileQuery query);

		public abstract BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions);

		StorageFileQueryResult IStorageFolderQueryOperations.CreateFileQueryWithOptions(QueryOptions queryOptions) => throw new NotSupportedException();

		public abstract bool IsCommonFolderQuerySupported(CommonFolderQuery query);

		public abstract StorageFolderQueryResult CreateFolderQuery();

		public abstract StorageFolderQueryResult CreateFolderQuery(CommonFolderQuery query);

		public abstract BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions);

		StorageFolderQueryResult IStorageFolderQueryOperations.CreateFolderQueryWithOptions(QueryOptions queryOptions) => throw new NotSupportedException();
	}
}
