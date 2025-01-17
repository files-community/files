// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using IO = System.IO;

namespace Files.App.Utils.Storage
{
	public sealed class SystemStorageFile : BaseStorageFile
	{
		public StorageFile File { get; }

		public override string Path => File?.Path;
		public override string Name => File?.Name;
		public override string DisplayName => File?.DisplayName;
		public override string ContentType => File.ContentType;
		public override string DisplayType => File?.DisplayType;
		public override string FileType => File.FileType;
		public override string FolderRelativeId => File?.FolderRelativeId;

		public override DateTimeOffset DateCreated => File.DateCreated;
		public override Windows.Storage.FileAttributes Attributes => File.Attributes;
		public override IStorageItemExtraProperties Properties => File?.Properties;

		public SystemStorageFile(StorageFile file) => File = file;

		public static IAsyncOperation<BaseStorageFile> FromPathAsync(string path)
			=> AsyncInfo.Run<BaseStorageFile>(async (cancellationToken)
				=> new SystemStorageFile(await StorageFile.GetFileFromPathAsync(path))
			);

		public override IAsyncOperation<StorageFile> ToStorageFileAsync()
			=> Task.FromResult(File).AsAsyncOperation();

		public override bool IsEqual(IStorageItem item) => File.IsEqual(item);
		public override bool IsOfType(StorageItemTypes type) => File.IsOfType(type);

		public override IAsyncOperation<BaseStorageFolder> GetParentAsync()
			=> AsyncInfo.Run<BaseStorageFolder>(async (cancellationToken)
				=> new SystemStorageFolder(await File.GetParentAsync())
			);

		public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
			=> AsyncInfo.Run<BaseBasicProperties>(async (cancellationToken)
				=> new SystemFileBasicProperties(await File.GetBasicPropertiesAsync(), DateCreated)
			);

		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder)
			=> CopyAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> CopyAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);
		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				var destFolder = destinationFolder.AsBaseStorageFolder(); // Avoid calling IStorageFolder method
				try
				{
					if (destFolder is SystemStorageFolder sysFolder)
					{
						// File created by CreateFileAsync will get immediately deleted on MTP?! (#7206)
						return await File.CopyAsync(sysFolder.Folder, desiredNewName, option);
					}
					else if (destFolder is ICreateFileWithStream cwsf)
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
				}
				catch (UnauthorizedAccessException) // shortcuts & .url
				{
					if (!string.IsNullOrEmpty(destFolder.Path))
					{
						var destination = IO.Path.Combine(destFolder.Path, desiredNewName);
						var hFile = Win32Helper.CreateFileForWrite(destination,
							option == NameCollisionOption.ReplaceExisting);
						if (!hFile.IsInvalid)
						{
							await using (var inStream = await this.OpenStreamForReadAsync())
							await using (var outStream = new FileStream(hFile, FileAccess.Write))
							{
								await inStream.CopyToAsync(outStream);
								await outStream.FlushAsync();
							}
							return new NativeStorageFile(destination, desiredNewName, DateTime.Now);
						}
					}
					throw;
				}
			});
		}

		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode) => File.OpenAsync(accessMode);
		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options) => File.OpenAsync(accessMode, options);

		public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync() => File.OpenReadAsync();
		public override IAsyncOperation<IInputStream> OpenSequentialReadAsync() => File.OpenSequentialReadAsync();

		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync() => File.OpenTransactedWriteAsync();
		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options) => File.OpenTransactedWriteAsync(options);

		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder)
			=> MoveAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> MoveAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);
		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				var destFolder = destinationFolder.AsBaseStorageFolder(); // Avoid calling IStorageFolder method
				if (destFolder is SystemStorageFolder sysFolder)
				{
					// File created by CreateFileAsync will get immediately deleted on MTP?! (#7206)
					await File.MoveAsync(sysFolder.Folder, desiredNewName, option);
					return;
				}
				await CopyAsync(destinationFolder, desiredNewName, option);
				// Move unsupported, copy but do not delete original
			});
		}

		public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				await using var inStream = await this.OpenStreamForReadAsync();
				await using var outStream = await fileToReplace.OpenStreamForWriteAsync();

				await inStream.CopyToAsync(outStream);
				await outStream.FlushAsync();
			});
		}
		public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				await using var inStream = await this.OpenStreamForReadAsync();
				await using var outStream = await fileToReplace.OpenStreamForWriteAsync();

				await inStream.CopyToAsync(outStream);
				await outStream.FlushAsync();
				// Move unsupported, copy but do not delete original
			});
		}

		public override IAsyncAction RenameAsync(string desiredName) => File.RenameAsync(desiredName);
		public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option) => File.RenameAsync(desiredName, option);

		public override IAsyncAction DeleteAsync() => File.DeleteAsync();
		public override IAsyncAction DeleteAsync(StorageDeleteOption option) => File.DeleteAsync(option);

		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
			=> File.GetThumbnailAsync(mode);
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
			=> File.GetThumbnailAsync(mode, requestedSize);
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
			=> File.GetThumbnailAsync(mode, requestedSize, options);

		private sealed class SystemFileBasicProperties : BaseBasicProperties
		{
			private readonly IStorageItemExtraProperties basicProps;
			private readonly DateTimeOffset? dateCreated;

			public override ulong Size => (basicProps as BasicProperties)?.Size ?? 0;

			public override DateTimeOffset DateCreated => dateCreated ?? DateTimeOffset.Now;
			public override DateTimeOffset DateModified => (basicProps as BasicProperties)?.DateModified ?? DateTimeOffset.Now;

			public SystemFileBasicProperties(IStorageItemExtraProperties basicProps, DateTimeOffset dateCreated)
			{
				this.basicProps = basicProps;
				this.dateCreated = dateCreated;
			}

			public override IAsyncOperation<IDictionary<string, object>> RetrievePropertiesAsync(IEnumerable<string> propertiesToRetrieve)
				=> basicProps.RetrievePropertiesAsync(propertiesToRetrieve);

			public override IAsyncAction SavePropertiesAsync()
				=> basicProps.SavePropertiesAsync();
			public override IAsyncAction SavePropertiesAsync([HasVariant] IEnumerable<KeyValuePair<string, object>> propertiesToSave)
				=> basicProps.SavePropertiesAsync(propertiesToSave);
		}
	}
}
