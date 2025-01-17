// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using IO = System.IO;

namespace Files.App.Utils.Storage
{
	public sealed class VirtualStorageFile : BaseStorageFile
	{
		public override string Path { get; }
		public override string Name { get; }
		public override string DisplayName => Name;
		public override string ContentType => "application/octet-stream";
		public override string FileType => IO.Path.GetExtension(Name);
		public override string FolderRelativeId => $"0\\{Name}";

		public override string DisplayType
		{
			get
			{
				var itemType = "File".GetLocalizedResource();
				if (Name.Contains('.', StringComparison.Ordinal))
				{
					itemType = IO.Path.GetExtension(Name).Trim('.') + " " + itemType;
				}
				return itemType;
			}
		}

		private Stream Contents { get; init; }

		public override DateTimeOffset DateCreated { get; }
		public override Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Normal;
		public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

		public VirtualStorageFile(Stream contents, string cFileName)
		{
			Contents = contents;
			Name = cFileName;
			Path = "";
		}

		private async void StreamedFileWriterAsync(StreamedFileDataRequest request)
		{
			try
			{
				await using (var stream = request.AsStreamForWrite())
				{
					await Contents.CopyToAsync(stream);
					await stream.FlushAsync();
				}
				request.Dispose();
			}
			catch (Exception)
			{
				request.FailAndClose(StreamedFileFailureMode.Incomplete);
			}
		}

		public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				return new BaseBasicProperties();
			});
		}

		public override bool IsOfType(StorageItemTypes type)
		{
			return Attributes.HasFlag(Windows.Storage.FileAttributes.Directory) ? type == StorageItemTypes.Folder : type == StorageItemTypes.File;
		}

		public override IAsyncOperation<StorageFile> ToStorageFileAsync()
		{
			return StorageFile.CreateStreamedFileAsync(Name, StreamedFileWriterAsync, null);
		}

		public override bool IsEqual(IStorageItem item) => item?.Path == Path;

		public override IAsyncOperation<BaseStorageFolder> GetParentAsync() => throw new NotSupportedException();

		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
		{
			return Task.FromResult(Contents.AsRandomAccessStream()).AsAsyncOperation();
		}

		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options) => OpenAsync(accessMode);

		public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
		{
			return Task.FromResult<IRandomAccessStreamWithContentType>(new StreamWithContentType(Contents.AsRandomAccessStream()))
				.AsAsyncOperation();
		}

		public override IAsyncOperation<IInputStream> OpenSequentialReadAsync()
		{
			return Task.FromResult(Contents.AsInputStream()).AsAsyncOperation();
		}

		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync() => throw new NotSupportedException();
		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options) => throw new NotSupportedException();

		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder)
			=> CopyAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> CopyAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);

		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				BaseStorageFolder destFolder = destinationFolder.AsBaseStorageFolder();

				if (destFolder is ICreateFileWithStream cwsf)
				{
					await using var inStream = await this.OpenStreamForReadAsync();
					return await cwsf.CreateFileAsync(inStream, desiredNewName, option.Convert());
				}
				else
				{
					var destFile = await destFolder.CreateFileAsync(desiredNewName, option.Convert());
					await using (var inStream = await this.OpenStreamForReadAsync())
					await using (var outStream = await destFile.OpenStreamForWriteAsync())
					{
						await inStream.CopyToAsync(outStream);
						await outStream.FlushAsync();
					}
					return destFile;
				}
			});
		}

		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder) => throw new NotSupportedException();
		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName) => throw new NotSupportedException();
		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option) => throw new NotSupportedException();

		public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();
		public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();

		public override IAsyncAction RenameAsync(string desiredName)
			=> RenameAsync(desiredName, NameCollisionOption.FailIfExists);

		public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
			=> throw new NotSupportedException();

		public override IAsyncAction DeleteAsync() => throw new NotSupportedException();

		public override IAsyncAction DeleteAsync(StorageDeleteOption option) => throw new NotSupportedException();

		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
	}
}
