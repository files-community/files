// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Foundation;
using Windows.Storage;

namespace Files.App.Utils.Storage
{
	public interface ICreateFileWithStream
	{
		IAsyncOperation<BaseStorageFile> CreateFileAsync(Stream contents, string desiredName);

		IAsyncOperation<BaseStorageFile> CreateFileAsync(Stream contents, string desiredName, CreationCollisionOption options);
	}
}
