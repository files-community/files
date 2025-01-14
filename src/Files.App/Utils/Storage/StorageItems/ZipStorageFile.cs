// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using SevenZip;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.Win32;
using IO = System.IO;

namespace Files.App.Utils.Storage
{
	public sealed class ZipStorageFile : BaseStorageFile, IPasswordProtectedItem
	{
		private readonly string containerPath;
		private readonly BaseStorageFile backingFile;

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
					itemType = FileType.Trim('.') + " " + itemType;
				}
				return itemType;
			}
		}

		public override DateTimeOffset DateCreated { get; }
		public override Windows.Storage.FileAttributes Attributes => Windows.Storage.FileAttributes.Normal | Windows.Storage.FileAttributes.ReadOnly;

		private IStorageItemExtraProperties properties;
		public override IStorageItemExtraProperties Properties => properties ??= new BaseBasicStorageItemExtraProperties(this);

		public StorageCredential Credentials { get; set; } = new();

		public Func<IPasswordProtectedItem, Task<StorageCredential>> PasswordRequestedCallback { get; set; }

		public ZipStorageFile(string path, string containerPath)
		{
			Name = IO.Path.GetFileName(path.TrimEnd('\\', '/'));
			Path = path;
			this.containerPath = containerPath;
		}
		public ZipStorageFile(string path, string containerPath, BaseStorageFile backingFile) : this(path, containerPath)
			=> this.backingFile = backingFile;
		public ZipStorageFile(string path, string containerPath, ArchiveFileInfo entry) : this(path, containerPath)
			=> DateCreated = entry.CreationTime == DateTime.MinValue ? DateTimeOffset.MinValue : entry.CreationTime;
		public ZipStorageFile(string path, string containerPath, ArchiveFileInfo entry, BaseStorageFile backingFile) : this(path, containerPath, entry)
			=> this.backingFile = backingFile;

		public override IAsyncOperation<StorageFile> ToStorageFileAsync()
			=> StorageFile.CreateStreamedFileAsync(Name, ZipDataStreamingHandler(Path), null);

		public static IAsyncOperation<BaseStorageFile> FromPathAsync(string path)
		{
			if (!FileExtensionHelpers.IsBrowsableZipFile(path, out var ext))
			{
				return Task.FromResult<BaseStorageFile>(null).AsAsyncOperation();
			}
			var marker = path.IndexOf(ext, StringComparison.OrdinalIgnoreCase);
			if (marker is not -1)
			{
				var containerPath = path.Substring(0, marker + ext.Length);
				if (path == containerPath)
				{
					return Task.FromResult<BaseStorageFile>(null).AsAsyncOperation(); // Root
				}
				if (CheckAccess(containerPath))
				{
					return Task.FromResult<BaseStorageFile>(new ZipStorageFile(path, containerPath)).AsAsyncOperation();
				}
			}
			return Task.FromResult<BaseStorageFile>(null).AsAsyncOperation();
		}

		public override bool IsEqual(IStorageItem item) => item?.Path == Path;
		public override bool IsOfType(StorageItemTypes type) => type is StorageItemTypes.File;

		public override IAsyncOperation<BaseStorageFolder> GetParentAsync() => throw new NotSupportedException();
		public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync() => GetBasicProperties().AsAsyncOperation();

		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.Wrap<IRandomAccessStream>(async () =>
			{
				bool rw = accessMode is FileAccessMode.ReadWrite;
				if (Path == containerPath)
				{
					if (backingFile is not null)
					{
						return await backingFile.OpenAsync(accessMode);
					}

					var file = Win32Helper.OpenFileForRead(containerPath, rw);
					return file.IsInvalid ? null : new FileStream(file, rw ? FileAccess.ReadWrite : FileAccess.Read).AsRandomAccessStream();
				}

				if (!rw)
				{
					SevenZipExtractor zipFile = await OpenZipFileAsync();
					if (zipFile is null || zipFile.ArchiveFileData is null)
					{
						return null;
					}

					//zipFile.IsStreamOwner = true;
					var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(containerPath, x.FileName) == Path);

					if (entry.FileName is not null)
					{
						var ms = new MemoryStream();
						await zipFile.ExtractFileAsync(entry.Index, ms);
						ms.Position = 0;
						return new NonSeekableRandomAccessStreamForRead(ms, entry.Size)
						{
							DisposeCallback = () => zipFile.Dispose()
						};
					}
					return null;
				}

				throw new NotSupportedException("Can't open zip file as RW");
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}
		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options)
			=> OpenAsync(accessMode);

		public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.Wrap<IRandomAccessStreamWithContentType>(async () =>
			{
				if (Path == containerPath)
				{
					if (backingFile is not null)
					{
						return await backingFile.OpenReadAsync();
					}

					var hFile = Win32Helper.OpenFileForRead(containerPath);
					return hFile.IsInvalid ? null : new StreamWithContentType(new FileStream(hFile, FileAccess.Read).AsRandomAccessStream());
				}

				SevenZipExtractor zipFile = await OpenZipFileAsync();
				if (zipFile is null || zipFile.ArchiveFileData is null)
				{
					return null;
				}

				//zipFile.IsStreamOwner = true;
				var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(containerPath, x.FileName) == Path);
				if (entry.FileName is null)
				{
					return null;
				}

				var ms = new MemoryStream();
				await zipFile.ExtractFileAsync(entry.Index, ms);
				ms.Position = 0;
				var nsStream = new NonSeekableRandomAccessStreamForRead(ms, entry.Size)
				{
					DisposeCallback = () => zipFile.Dispose()
				};
				return new StreamWithContentType(nsStream);
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}

		public override IAsyncOperation<IInputStream> OpenSequentialReadAsync()
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.Wrap<IInputStream>(async () =>
			{
				if (Path == containerPath)
				{
					if (backingFile is not null)
					{
						return await backingFile.OpenSequentialReadAsync();
					}

					var hFile = Win32Helper.OpenFileForRead(containerPath);
					return hFile.IsInvalid ? null : new FileStream(hFile, FileAccess.Read).AsInputStream();
				}

				SevenZipExtractor zipFile = await OpenZipFileAsync();
				if (zipFile is null || zipFile.ArchiveFileData is null)
				{
					return null;
				}
				//zipFile.IsStreamOwner = true;
				var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(containerPath, x.FileName) == Path);
				if (entry.FileName is null)
				{
					return null;
				}

				var ms = new MemoryStream();
				await zipFile.ExtractFileAsync(entry.Index, ms);
				ms.Position = 0;
				return new NonSeekableRandomAccessStreamForRead(ms, entry.Size)
				{
					DisposeCallback = () => zipFile.Dispose()
				};
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}

		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync()
			=> throw new NotSupportedException();
		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options)
			=> throw new NotSupportedException();

		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder)
			=> CopyAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);
		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> CopyAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);
		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.Wrap<BaseStorageFile>(async () =>
			{
				using SevenZipExtractor zipFile = await OpenZipFileAsync();
				if (zipFile is null || zipFile.ArchiveFileData is null)
				{
					return null;
				}

				//zipFile.IsStreamOwner = true;
				var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(containerPath, x.FileName) == Path);
				if (entry.FileName is null)
				{
					return null;
				}

				var destFolder = destinationFolder.AsBaseStorageFolder();

				if (destFolder is ICreateFileWithStream cwsf)
				{
					var ms = new MemoryStream();
					await zipFile.ExtractFileAsync(entry.Index, ms);
					ms.Position = 0;
					using var inStream = new NonSeekableRandomAccessStreamForRead(ms, entry.Size);
					return await cwsf.CreateFileAsync(inStream.AsStreamForRead(), desiredNewName, option.Convert());
				}
				else
				{
					var destFile = await destFolder.CreateFileAsync(desiredNewName, option.Convert());
					using var outStream = await destFile.OpenStreamForWriteAsync();
					await SafetyExtensions.WrapAsync(() => zipFile.ExtractFileAsync(entry.Index, outStream), async (_, exception) =>
					{
						await destFile.DeleteAsync();
						throw exception;
					});
					return destFile;
				}
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}
		public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace)
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.WrapAsync(async () =>
			{
				using SevenZipExtractor zipFile = await OpenZipFileAsync();
				if (zipFile is null || zipFile.ArchiveFileData is null)
				{
					return;
				}
				//zipFile.IsStreamOwner = true;
				var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(containerPath, x.FileName) == Path);
				if (entry.FileName is null)
				{
					return;
				}

				using var hDestFile = fileToReplace.CreateSafeFileHandle(FileAccess.ReadWrite);
				using (var outStream = new FileStream(hDestFile, FileAccess.Write))
				{
					await zipFile.ExtractFileAsync(entry.Index, outStream);
				}
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}

		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder)
			=> throw new NotSupportedException();
		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> throw new NotSupportedException();
		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
			=> throw new NotSupportedException();
		public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
			=> throw new NotSupportedException();

		public override IAsyncAction RenameAsync(string desiredName) => RenameAsync(desiredName, NameCollisionOption.FailIfExists);
		public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.WrapAsync(async () =>
			{
				if (Path == containerPath)
				{
					if (backingFile is not null)
					{
						await backingFile.RenameAsync(desiredName, option);
					}
					else
					{
						var fileName = IO.Path.Combine(IO.Path.GetDirectoryName(Path), desiredName);
						PInvoke.MoveFileFromApp(Path, fileName);
					}
				}
				else
				{
					var index = await FetchZipIndex();
					if (index < 0)
					{
						return;
					}
					using (var ms = new MemoryStream())
					{
						using (var archiveStream = await OpenZipFileAsync(FileAccessMode.Read))
						{
							SevenZipCompressor compressor = new SevenZipCompressor() { CompressionMode = CompressionMode.Append };
							compressor.SetFormatFromExistingArchive(archiveStream);
							var fileName = IO.Path.GetRelativePath(containerPath, IO.Path.Combine(IO.Path.GetDirectoryName(Path), desiredName));
							await compressor.ModifyArchiveAsync(archiveStream, new Dictionary<int, string>() { { index, fileName } }, Credentials.Password, ms);
						}
						using (var archiveStream = await OpenZipFileAsync(FileAccessMode.ReadWrite))
						{
							ms.Position = 0;
							await ms.CopyToAsync(archiveStream);
							await ms.FlushAsync();
							archiveStream.SetLength(archiveStream.Position);
						}
					}
				}
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}

		public override IAsyncAction DeleteAsync() => DeleteAsync(StorageDeleteOption.Default);
		public override IAsyncAction DeleteAsync(StorageDeleteOption option)
		{
			return AsyncInfo.Run((cancellationToken) => SafetyExtensions.WrapAsync(async () =>
			{
				if (Path == containerPath)
				{
					if (backingFile is not null)
					{
						await backingFile.DeleteAsync();
					}
					else if (option == StorageDeleteOption.PermanentDelete)
					{
						PInvoke.DeleteFileFromApp(Path);
					}
					else
					{
						throw new NotSupportedException("Moving to recycle bin is not supported.");
					}
				}
				else
				{
					var index = await FetchZipIndex();
					if (index < 0)
					{
						return;
					}
					using (var ms = new MemoryStream())
					{
						using (var archiveStream = await OpenZipFileAsync(FileAccessMode.Read))
						{
							SevenZipCompressor compressor = new SevenZipCompressor() { CompressionMode = CompressionMode.Append };
							compressor.SetFormatFromExistingArchive(archiveStream);
							await compressor.ModifyArchiveAsync(archiveStream, new Dictionary<int, string>() { { index, null } }, Credentials.Password, ms);
						}
						using (var archiveStream = await OpenZipFileAsync(FileAccessMode.ReadWrite))
						{
							ms.Position = 0;
							await ms.CopyToAsync(archiveStream);
							await ms.FlushAsync();
							archiveStream.SetLength(archiveStream.Position);
						}
					}
				}
			}, ((IPasswordProtectedItem)this).RetryWithCredentialsAsync));
		}

		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
			=> Task.FromResult<StorageItemThumbnail>(null).AsAsyncOperation();

		private static bool CheckAccess(string path)
		{
			try
			{
				var hFile = Win32Helper.OpenFileForRead(path);
				if (hFile.IsInvalid)
				{
					return false;
				}
				using (SevenZipExtractor zipFile = new SevenZipExtractor(new FileStream(hFile, FileAccess.Read)))
				{
					//zipFile.IsStreamOwner = true;
					return zipFile.ArchiveFileData is not null;
				}
			}
			catch (SevenZipOpenFailedException ex)
			{
				return ex.Result == OperationResult.WrongPassword;
			}
			catch
			{
				return false;
			}
		}

		private async Task<int> FetchZipIndex()
		{
			using (SevenZipExtractor zipFile = await OpenZipFileAsync())
			{
				if (zipFile is null || zipFile.ArchiveFileData is null)
				{
					return -1;
				}
				//zipFile.IsStreamOwner = true;
				var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(containerPath, x.FileName) == Path);
				if (entry.FileName is not null)
				{
					return entry.Index;
				}
				return -1;
			}
		}

		private async Task<BaseBasicProperties> GetBasicProperties()
		{
			using SevenZipExtractor zipFile = await OpenZipFileAsync();
			if (zipFile is null || zipFile.ArchiveFileData is null)
			{
				return null;
			}

			//zipFile.IsStreamOwner = true;
			var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(containerPath, x.FileName) == Path);

			return entry.FileName is null
				? new BaseBasicProperties()
				: new ZipFileBasicProperties(entry);
		}

		private IAsyncOperation<SevenZipExtractor> OpenZipFileAsync()
		{
			return AsyncInfo.Run<SevenZipExtractor>(async (cancellationToken) =>
			{
				var zipFile = await OpenZipFileAsync(FileAccessMode.Read);
				return zipFile is not null ? new SevenZipExtractor(zipFile, Credentials.Password) : null;
			});
		}

		private IAsyncOperation<Stream> OpenZipFileAsync(FileAccessMode accessMode)
		{
			return AsyncInfo.Run<Stream>(async (cancellationToken) =>
			{
				bool readWrite = accessMode == FileAccessMode.ReadWrite;
				if (backingFile is not null)
				{
					return (await backingFile.OpenAsync(accessMode)).AsStream();
				}
				else
				{
					var hFile = Win32Helper.OpenFileForRead(containerPath, readWrite);
					if (hFile.IsInvalid)
					{
						return null;
					}
					return new FileStream(hFile, readWrite ? FileAccess.ReadWrite : FileAccess.Read);
				}
			});
		}

		private StreamedFileDataRequestedHandler ZipDataStreamingHandler(string name)
		{
			return async request =>
			{
				try
				{
					using SevenZipExtractor zipFile = await OpenZipFileAsync();
					if (zipFile is null || zipFile.ArchiveFileData is null)
					{
						request.FailAndClose(StreamedFileFailureMode.CurrentlyUnavailable);
						return;
					}
					//zipFile.IsStreamOwner = true;
					var entry = zipFile.ArchiveFileData.FirstOrDefault(x => System.IO.Path.Combine(containerPath, x.FileName) == name);
					if (entry.FileName is null)
					{
						request.FailAndClose(StreamedFileFailureMode.CurrentlyUnavailable);
					}
					else
					{
						using (var outStream = request.AsStreamForWrite())
						{
							await zipFile.ExtractFileAsync(entry.Index, outStream);
						}
						request.Dispose();
					}
				}
				catch
				{
					request.FailAndClose(StreamedFileFailureMode.Failed);
				}
			};
		}

		private sealed class ZipFileBasicProperties : BaseBasicProperties
		{
			private ArchiveFileInfo entry;

			public ZipFileBasicProperties(ArchiveFileInfo entry) => this.entry = entry;

			public override DateTimeOffset DateModified => entry.LastWriteTime == DateTime.MinValue ? DateTimeOffset.MinValue : entry.LastWriteTime;

			public override DateTimeOffset DateCreated => entry.CreationTime == DateTime.MinValue ? DateTimeOffset.MinValue : entry.CreationTime;

			public override ulong Size => entry.Size;
		}
	}
}
