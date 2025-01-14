// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;

namespace Files.App.Utils.Storage
{
	public interface IBaseStorageFolder : IStorageItem2, IStorageFolder, IStorageFolder2, IStorageItemProperties2, IStorageItemPropertiesWithProvider, IStorageFolderQueryOperations
	{
		new IStorageItemExtraProperties Properties { get; }

		IAsyncOperation<StorageFolder> ToStorageFolderAsync();

		new IAsyncOperation<BaseStorageFolder> GetParentAsync();

		new IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync();

		new IAsyncOperation<IStorageItem> GetItemAsync(string name);

		new IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync();

		new IAsyncOperation<BaseStorageFile> GetFileAsync(string name);

		new IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync();

		new IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query);

		new IAsyncOperation<IReadOnlyList<BaseStorageFile>> GetFilesAsync(CommonFileQuery query, uint startIndex, uint maxItemsToRetrieve);

		new IAsyncOperation<BaseStorageFolder> GetFolderAsync(string name);

		new IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync();

		new IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query);

		new IAsyncOperation<IReadOnlyList<BaseStorageFolder>> GetFoldersAsync(CommonFolderQuery query, uint startIndex, uint maxItemsToRetrieve);

		new IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName);

		new IAsyncOperation<BaseStorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options);

		new IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName);

		new IAsyncOperation<BaseStorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options);

		IAsyncOperation<BaseStorageFolder> MoveAsync(IStorageFolder destinationFolder);
		IAsyncOperation<BaseStorageFolder> MoveAsync(IStorageFolder destinationFolder, NameCollisionOption option);

		new BaseStorageItemQueryResult CreateItemQueryWithOptions(QueryOptions queryOptions);

		new BaseStorageFileQueryResult CreateFileQueryWithOptions(QueryOptions queryOptions);

		new BaseStorageFolderQueryResult CreateFolderQueryWithOptions(QueryOptions queryOptions);
	}
}
