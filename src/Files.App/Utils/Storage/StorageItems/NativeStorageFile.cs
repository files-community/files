// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.Win32;
using IO = System.IO;

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Shortcuts and alternate data stream.
	/// Uses *FromApp methods for file operations
	/// </summary>
	public sealed class NativeStorageFile : BaseStorageFile
	{
		public override string Path { get; }
		public override string Name { get; }
		public override string DisplayName => Name;
		public override string ContentType => "application/octet-stream";
		public override string FileType => IO.Path.GetExtension(Name);
		public override string FolderRelativeId => $"0\\{Name}";

		public bool IsShortcut => FileExtensionHelpers.IsShortcutOrUrlFile(FileType);
		public bool IsAlternateStream => System.Text.RegularExpressions.Regex.IsMatch(Path, @"\w:\w");

		public override string DisplayType
		{
			get
			{
				var itemType = "File".GetLocalizedResource();

				if (Name.Contains('.', StringComparison.Ordinal))
					itemType = IO.Path.GetExtension(Name).Trim('.') + " " + itemType;

				return itemType;
			}
		}

		public override DateTimeOffset DateCreated { get; }
		public override Windows.Storage.FileAttributes Attributes { get; } = Windows.Storage.FileAttributes.Normal;
		public override IStorageItemExtraProperties Properties => new BaseBasicStorageItemExtraProperties(this);

		public NativeStorageFile(string path, string name, DateTimeOffset dateCreated)
		{
			Path = path;
			Name = name;
			DateCreated = dateCreated;
		}

		public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace)
			=> throw new NotSupportedException();

		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder)
			=> CopyAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);

		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> CopyAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);

		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
		{
			return AsyncInfo.Run<BaseStorageFile>(async (cancellationToken) =>
			{
				if (string.IsNullOrEmpty(destinationFolder.Path))
				{
					throw new NotSupportedException();
				}
				var destination = IO.Path.Combine(destinationFolder.Path, desiredNewName);
				var destFile = new NativeStorageFile(destination, desiredNewName, DateTime.Now);
				if (!IsAlternateStream)
				{
					if (!await Task.Run(() => PInvoke.CopyFileFromApp(Path, destination, option != NameCollisionOption.ReplaceExisting)))
					{
						throw new Win32Exception(Marshal.GetLastWin32Error());
					}
				}
				else
				{
					destFile.CreateFile();
					await using (var inStream = await this.OpenStreamForReadAsync())
					await using (var outStream = await destFile.OpenStreamForWriteAsync())
					{
						await inStream.CopyToAsync(outStream);
						await outStream.FlushAsync();
					}
				}
				return destFile;
			});
		}

		private void CreateFile()
		{
			using var hFile = Win32Helper.CreateFileForWrite(Path, false);
			if (hFile.IsInvalid)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}
		}

		public override IAsyncAction DeleteAsync()
		{
			return AsyncInfo.Run(_ =>
			{
				if (!PInvoke.DeleteFileFromApp(Path))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error());
				}

				return Task.CompletedTask;
			});
		}

		public override IAsyncAction DeleteAsync(StorageDeleteOption option)
		{
			if (option == StorageDeleteOption.PermanentDelete)
			{
				return DeleteAsync();
			}
			throw new NotSupportedException();
		}

		public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync()
		{
			return Task.FromResult(new BaseBasicProperties()).AsAsyncOperation();
		}

		public override IAsyncOperation<BaseStorageFolder> GetParentAsync()
			=> throw new NotSupportedException();

		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
			=> throw new NotSupportedException();

		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
			=> throw new NotSupportedException();

		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
			=> throw new NotSupportedException();

		public static IAsyncOperation<BaseStorageFile> FromPathAsync(string path)
		{
			if (IsNativePath(path) && CheckAccess(path))
			{
				var name = IO.Path.GetFileName(path);
				return Task.FromResult((BaseStorageFile)new NativeStorageFile(path, name[(name.LastIndexOf(":") + 1)..], DateTime.Now)).AsAsyncOperation();
			}
			return Task.FromResult<BaseStorageFile>(null).AsAsyncOperation();
		}

		private static bool CheckAccess(string path)
		{
			using var hFile = Win32Helper.OpenFileForRead(path);
			return !hFile.IsInvalid;
		}

		private static bool IsNativePath(string path)
		{
			var isShortcut = FileExtensionHelpers.IsShortcutOrUrlFile(path);
			var isAlternateStream = RegexHelpers.AlternateStream().IsMatch(path);
			return isShortcut || isAlternateStream;
		}

		public override bool IsEqual(IStorageItem item) => item?.Path == Path;
		public override bool IsOfType(StorageItemTypes type) => type is StorageItemTypes.File;

		public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace)
			=> throw new NotSupportedException();

		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder)
			=> MoveAsync(destinationFolder, Name, NameCollisionOption.FailIfExists);

		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName)
			=> MoveAsync(destinationFolder, desiredNewName, NameCollisionOption.FailIfExists);

		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				if (string.IsNullOrEmpty(destinationFolder.Path))
				{
					throw new NotSupportedException();
				}
				var destination = IO.Path.Combine(destinationFolder.Path, desiredNewName);
				if (!IsAlternateStream)
				{
					if (!await Task.Run(() => PInvoke.MoveFileFromApp(Path, destination)))
					{
						throw new Win32Exception(Marshal.GetLastWin32Error());
					}
				}
				else
				{
					await CopyAsync(destinationFolder, desiredNewName, option);
					await DeleteAsync();
				}
			});
		}

		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode)
		{
			var hFile = Win32Helper.OpenFileForRead(Path, accessMode == FileAccessMode.ReadWrite);
			return Task.FromResult(new FileStream(hFile, accessMode == FileAccessMode.ReadWrite ? FileAccess.ReadWrite : FileAccess.Read).AsRandomAccessStream()).AsAsyncOperation();
		}

		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options) => OpenAsync(accessMode);

		public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync()
		{
			return AsyncInfo.Run<IRandomAccessStreamWithContentType>(async (cancellationToken) =>
			{
				return new StreamWithContentType(await OpenAsync(FileAccessMode.Read));
			});
		}

		public override IAsyncOperation<IInputStream> OpenSequentialReadAsync()
		{
			var hFile = Win32Helper.OpenFileForRead(Path);
			return Task.FromResult(new FileStream(hFile, FileAccess.Read).AsInputStream()).AsAsyncOperation();
		}

		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync()
			=> throw new NotSupportedException();

		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options)
			=> throw new NotSupportedException();

		public override IAsyncAction RenameAsync(string desiredName)
			=> RenameAsync(desiredName, NameCollisionOption.FailIfExists);

		public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				string destination = IO.Path.Combine(IO.Path.GetDirectoryName(Path), desiredName);
				var destFile = new NativeStorageFile(destination, desiredName, DateTime.Now);
				if (!IsAlternateStream)
				{
					if (!await Task.Run(() => PInvoke.MoveFileFromApp(Path, destination)))
					{
						throw new Win32Exception(Marshal.GetLastWin32Error());
					}
				}
				else
				{
					destFile.CreateFile();
					await using (var inStream = await this.OpenStreamForReadAsync())
					await using (var outStream = await destFile.OpenStreamForWriteAsync())
					{
						await inStream.CopyToAsync(outStream);
						await outStream.FlushAsync();
					}
					await DeleteAsync();
				}
			});
		}

		public override IAsyncOperation<StorageFile> ToStorageFileAsync()
			=> throw new NotSupportedException();
	}
}
