// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Storage.Storables;
using FluentFTP;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using IO = System.IO;

namespace Files.App.Utils.Storage
{
	public sealed class FtpStorageFile : BaseStorageFile, IPasswordProtectedItem
	{
		public override string Path { get; }
		public override string Name { get; }
		public override string DisplayName => Name;
		public override string ContentType => "application/octet-stream";
		public override string FileType => IO.Path.GetExtension(Name);
		public string FtpPath { get; }
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

		public override DateTimeOffset DateCreated { get; }
		public override Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Normal;
		public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

		public StorageCredential Credentials { get; set; }

		public Func<IPasswordProtectedItem, Task<StorageCredential>> PasswordRequestedCallback { get; set; }

		public FtpStorageFile(string path, string name, DateTimeOffset dateCreated)
		{
			Path = path;
			Name = name;
			FtpPath = FtpHelpers.GetFtpPath(path);
			DateCreated = dateCreated;
		}
		public FtpStorageFile(string folder, FtpListItem ftpItem)
		{
			Path = PathNormalization.Combine(folder, ftpItem.Name);
			Name = ftpItem.Name;
			FtpPath = FtpHelpers.GetFtpPath(Path);
			DateCreated = ftpItem.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : ftpItem.RawCreated;
		}
		public FtpStorageFile(IStorageItemWithPath item)
		{
			Path = item.Path;
			Name = IO.Path.GetFileName(item.Path);
			FtpPath = FtpHelpers.GetFtpPath(item.Path);
		}

		public static IAsyncOperation<BaseStorageFile> FromPathAsync(string path)
			=> FtpHelpers.IsFtpPath(path) && FtpHelpers.VerifyFtpPath(path)
				? Task.FromResult<BaseStorageFile>(new FtpStorageFile(new StorageFileWithPath(null, path))).AsAsyncOperation()
				: Task.FromResult<BaseStorageFile>(null).AsAsyncOperation();

		public override IAsyncOperation<StorageFile> ToStorageFileAsync()
			=> StorageFile.CreateStreamedFileAsync(Name, FtpDataStreamingHandlerAsync, null);

		public override bool IsEqual(IStorageItem item) => item?.Path == Path;
		public override bool IsOfType(StorageItemTypes type) => type is StorageItemTypes.File;

		public override IAsyncOperation<BaseStorageFolder> GetParentAsync() => throw new NotSupportedException();

		public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.Wrap(async () =>
			{
				using var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					return new BaseBasicProperties();
				}

				var item = await ftpClient.GetObjectInfo(FtpPath);
				return item is null ? new BaseBasicProperties() : new FtpFileBasicProperties(item);
			}, (_, _) => Task.FromResult(new BaseBasicProperties())));
		}

		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.Wrap<IRandomAccessStream>(async () =>
			{
				var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					return null;
				}

				if (accessMode is FileAccessMode.Read)
				{
					var inStream = await ftpClient.OpenRead(FtpPath, token: cancellationToken);
					return new NonSeekableRandomAccessStreamForRead(inStream, (ulong)inStream.Length)
					{
						DisposeCallback = ftpClient.Dispose
					};
				}
				return new NonSeekableRandomAccessStreamForWrite(await ftpClient.OpenWrite(FtpPath, token: cancellationToken))
				{
					DisposeCallback = ftpClient.Dispose
				};
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}
		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options) => OpenAsync(accessMode);

		public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.Wrap<IRandomAccessStreamWithContentType>(async () =>
			{
				var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					return null;
				}

				var inStream = await ftpClient.OpenRead(FtpPath, token: cancellationToken);
				var nsStream = new NonSeekableRandomAccessStreamForRead(inStream, (ulong)inStream.Length) { DisposeCallback = ftpClient.Dispose };
				return new StreamWithContentType(nsStream);
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}
		public override IAsyncOperation<IInputStream> OpenSequentialReadAsync()
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.Wrap<IInputStream>(async () =>
			{
				var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					return null;
				}

				var inStream = await ftpClient.OpenRead(FtpPath, token: cancellationToken);
				return new InputStreamWithDisposeCallback(inStream) { DisposeCallback = () => ftpClient.Dispose() };
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}

		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync() => throw new NotSupportedException();
		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options) => throw new NotSupportedException();

		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder)
			=> CopyAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> CopyAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);
		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.Wrap<BaseStorageFile>(async () =>
			{
				using var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					return null;
				}

				BaseStorageFolder destFolder = destinationFolder.AsBaseStorageFolder();

				if (destFolder is ICreateFileWithStream cwsf)
				{
					await using var inStream = await ftpClient.OpenRead(FtpPath, token: cancellationToken);
					return await cwsf.CreateFileAsync(inStream, desiredNewName, option.Convert());
				}
				else
				{
					BaseStorageFile file = await destFolder.CreateFileAsync(desiredNewName, option.Convert());
					await using var stream = await file.OpenStreamForWriteAsync();
					return await ftpClient.DownloadStream(stream, FtpPath, token: cancellationToken) ? file : null;
				}
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}

		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder)
			=> MoveAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> MoveAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);
		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.WrapAsync(async () =>
			{
				using var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
					throw new IOException($"Failed to connect to FTP server.");

				BaseStorageFolder destFolder = destinationFolder.AsBaseStorageFolder();

				if (destFolder is FtpStorageFolder ftpFolder)
				{
					string destName = $"{ftpFolder.FtpPath}/{Name}";
					FtpRemoteExists ftpRemoteExists = option is NameCollisionOption.ReplaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;

					bool isSuccessful = await ftpClient.MoveFile(FtpPath, destName, ftpRemoteExists, cancellationToken);
					if (!isSuccessful)
						throw new IOException($"Failed to move file from {Path} to {destFolder}.");
				}
				else
					throw new NotSupportedException();
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}


		public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();
		public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();

		public override IAsyncAction RenameAsync(string desiredName)
			=> RenameAsync(desiredName, NameCollisionOption.FailIfExists);
		public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.WrapAsync(async () =>
			{
				using var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					return;
				}

				string destination = $"{PathNormalization.GetParentDir(FtpPath)}/{desiredName}";
				var remoteExists = option is NameCollisionOption.ReplaceExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip;
				bool isSuccessful = await ftpClient.MoveFile(FtpPath, destination, remoteExists, cancellationToken);
				if (!isSuccessful && option is NameCollisionOption.GenerateUniqueName)
				{
					// TODO: handle name generation
				}
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}

		public override IAsyncAction DeleteAsync()
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.WrapAsync(async () =>
			{
				using var ftpClient = GetFtpClient();
				if (await ftpClient.EnsureConnectedAsync())
				{
					await ftpClient.DeleteFile(FtpPath, cancellationToken);
				}
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}
		public override IAsyncAction DeleteAsync(StorageDeleteOption option) => DeleteAsync();

		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();

		private AsyncFtpClient GetFtpClient()
		{
			string host = FtpHelpers.GetFtpHost(Path);
			ushort port = FtpHelpers.GetFtpPort(Path);
			var credentials = Credentials is not null ?
				new NetworkCredential(Credentials.UserName, Credentials.SecurePassword) :
				FtpManager.Credentials.Get(host, FtpManager.Anonymous);

			return new(host, credentials, port);
		}

		private async void FtpDataStreamingHandlerAsync(StreamedFileDataRequest request)
		{
			try
			{
				using var ftpClient = GetFtpClient();
				if (!await ftpClient.EnsureConnectedAsync())
				{
					request.FailAndClose(StreamedFileFailureMode.CurrentlyUnavailable);
					return;
				}

				await using (var outStream = request.AsStreamForWrite())
				{
					await ftpClient.DownloadStream(outStream, FtpPath);
					await outStream.FlushAsync();
				}
				request.Dispose();
			}
			catch
			{
				request.FailAndClose(StreamedFileFailureMode.Incomplete);
			}
		}

		private sealed class FtpFileBasicProperties : BaseBasicProperties
		{
			public override ulong Size { get; }

			public override DateTimeOffset DateCreated { get; }
			public override DateTimeOffset DateModified { get; }

			public FtpFileBasicProperties(FtpItem item)
			{
				Size = (ulong)item.FileSizeBytes;
				DateCreated = item.ItemDateCreatedReal;
				DateModified = item.ItemDateModifiedReal;
			}

			public FtpFileBasicProperties(FtpListItem item)
			{
				Size = (ulong)item.Size;
				DateCreated = item.RawCreated < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawCreated;
				DateModified = item.RawModified < DateTime.FromFileTimeUtc(0) ? DateTimeOffset.MinValue : item.RawModified;
			}
		}
	}
}