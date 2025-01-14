// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices.WindowsRuntime;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.Win32.UI.WindowsAndMessaging;
using IO = System.IO;

namespace Files.App.Utils.Storage
{
	public sealed class ShortcutStorageFile : ShellStorageFile, IShortcutStorageItem
	{
		public string TargetPath { get; }
		public string Arguments { get; }
		public string WorkingDirectory { get; }
		public bool RunAsAdmin { get; }
		public SHOW_WINDOW_CMD ShowWindowCommand { get; set; }

		public ShortcutStorageFile(ShellLinkItem item) : base(item)
		{
			TargetPath = item.TargetPath;
			Arguments = item.Arguments;
			WorkingDirectory = item.WorkingDirectory;
			RunAsAdmin = item.RunAsAdmin;
			ShowWindowCommand = item.ShowWindowCommand;
		}
	}

	public sealed class BinStorageFile : ShellStorageFile, IBinStorageItem
	{
		public string OriginalPath { get; }
		public DateTimeOffset DateDeleted { get; }

		public BinStorageFile(ShellFileItem item) : base(item)
		{
			OriginalPath = item.FilePath;
			DateDeleted = item.RecycleDate;
		}
	}

	public class ShellStorageFile : BaseStorageFile
	{
		public override string Path { get; }
		public override string Name { get; }
		public override string DisplayName => Name;
		public override string ContentType => "application/octet-stream";
		public override string FileType => IO.Path.GetExtension(Name);
		public override string FolderRelativeId => $"0\\{Name}";

		public override string DisplayType { get; }

		public override DateTimeOffset DateCreated { get; }

		public override FileAttributes Attributes => FileAttributes.Normal | FileAttributes.ReadOnly;

		private IStorageItemExtraProperties properties;
		public override IStorageItemExtraProperties Properties => properties ??= new BaseBasicStorageItemExtraProperties(this);

		public ShellStorageFile(ShellFileItem item)
		{
			Name = item.FileName;
			Path = item.RecyclePath; // True path on disk
			DateCreated = item.CreatedDate;
			DisplayType = item.FileType;
		}

		public override IAsyncOperation<StorageFile> ToStorageFileAsync() => throw new NotSupportedException();

		public static ShellStorageFile FromShellItem(ShellFileItem item)
		{
			if (item.RecyclePath.Contains("$Recycle.Bin", StringComparison.OrdinalIgnoreCase))
				return new BinStorageFile(item);

			if (item is ShellLinkItem linkItem)
				return new ShortcutStorageFile(linkItem);

			return new ShellStorageFile(item);
		}

		public static IAsyncOperation<BaseStorageFile> FromPathAsync(string path)
		{
			if (ShellStorageFolder.IsShellPath(path) && GetFile(path) is ShellFileItem file)
				return Task.FromResult<BaseStorageFile>(FromShellItem(file)).AsAsyncOperation();
			return Task.FromResult<BaseStorageFile>(null).AsAsyncOperation();
		}

		private static ShellFileItem? GetFile(string path)
		{
			try
			{
				using var shellItem = ShellFolderExtensions.GetShellItemFromPathOrPIDL(path);
				return ShellFolderExtensions.GetShellFileItem(shellItem);
			}
			catch
			{
				// Can happen when dealing with recent items or when browsing shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}
				return default;
			}
		}

		public override bool IsEqual(IStorageItem item) => item?.Path == Path;
		public override bool IsOfType(StorageItemTypes type) => type is StorageItemTypes.File;

		public override IAsyncOperation<BaseStorageFolder> GetParentAsync() => throw new NotSupportedException();
		public override IAsyncOperation<BaseBasicProperties> GetBasicPropertiesAsync() => GetBasicProperties().AsAsyncOperation();

		public override IAsyncAction CopyAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();
		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder) => throw new NotSupportedException();
		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName) => throw new NotSupportedException();
		public override IAsyncOperation<BaseStorageFile> CopyAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option) => throw new NotSupportedException();

		public override IAsyncAction DeleteAsync() => throw new NotSupportedException();
		public override IAsyncAction DeleteAsync(StorageDeleteOption option) => throw new NotSupportedException();

		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				if (ShellStorageFolder.IsShellPath(Path))
				{
					return null;
				}
				var zipFile = await StorageFile.GetFileFromPathAsync(Path);
				return await zipFile.GetThumbnailAsync(mode);
			});
		}
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				if (ShellStorageFolder.IsShellPath(Path))
				{
					return null;
				}
				var zipFile = await StorageFile.GetFileFromPathAsync(Path);
				return await zipFile.GetThumbnailAsync(mode, requestedSize);
			});
		}
		public override IAsyncOperation<StorageItemThumbnail> GetThumbnailAsync(ThumbnailMode mode, uint requestedSize, ThumbnailOptions options)
		{
			return AsyncInfo.Run(async (cancellationToken) =>
			{
				if (ShellStorageFolder.IsShellPath(Path))
				{
					return null;
				}
				var zipFile = await StorageFile.GetFileFromPathAsync(Path);
				return await zipFile.GetThumbnailAsync(mode, requestedSize, options);
			});
		}

		public override IAsyncAction MoveAndReplaceAsync(IStorageFile fileToReplace) => throw new NotSupportedException();
		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder) => throw new NotSupportedException();
		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName) => throw new NotSupportedException();
		public override IAsyncAction MoveAsync(IStorageFolder destinationFolder, string desiredNewName, NameCollisionOption option) => throw new NotSupportedException();

		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode) => throw new NotSupportedException();
		public override IAsyncOperation<IRandomAccessStream> OpenAsync(FileAccessMode accessMode, StorageOpenOptions options) => throw new NotSupportedException();
		public override IAsyncOperation<IRandomAccessStreamWithContentType> OpenReadAsync() => throw new NotSupportedException();
		public override IAsyncOperation<IInputStream> OpenSequentialReadAsync() => throw new NotSupportedException();

		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync() => throw new NotSupportedException();
		public override IAsyncOperation<StorageStreamTransaction> OpenTransactedWriteAsync(StorageOpenOptions options) => throw new NotSupportedException();

		public override IAsyncAction RenameAsync(string desiredName) => throw new NotSupportedException();
		public override IAsyncAction RenameAsync(string desiredName, NameCollisionOption option) => throw new NotSupportedException();

		private Task<BaseBasicProperties> GetBasicProperties()
		{
			if (GetFile(Path) is ShellFileItem file)
			{
				return Task.FromResult<BaseBasicProperties>(new ShellFileBasicProperties(file));
			}
			return Task.FromResult(new BaseBasicProperties());
		}

		private sealed class ShellFileBasicProperties : BaseBasicProperties
		{
			private readonly ShellFileItem file;

			public ShellFileBasicProperties(ShellFileItem folder) => file = folder;

			public override ulong Size => file.FileSizeBytes;

			public override DateTimeOffset DateCreated => file.CreatedDate;
			public override DateTimeOffset DateModified => file.ModifiedDate;
		}
	}
}
