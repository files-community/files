// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Storage;

namespace Files.App.Storage.Storables
{
	/// <inheritdoc cref="IFile"/>
	[Obsolete("Use the new WindowsStorable")]
	public sealed class WindowsStorageFileLegacy : WindowsStorableLegacy<StorageFile>, ILocatableFile, IModifiableFile, IFileExtended, INestedFile
	{
		public WindowsStorageFileLegacy(StorageFile storage)
			: base(storage)
		{
		}

		/// <inheritdoc/>
		public Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default)
		{
			return OpenStreamAsync(access, FileShare.None, cancellationToken);
		}

		/// <inheritdoc/>
		public async Task<Stream> OpenStreamAsync(FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
		{
			var fileAccessMode = GetFileAccessMode(access);
			var storageOpenOptions = GetStorageOpenOptions(share);

			var winrtStreamTask = storage.OpenAsync(fileAccessMode, storageOpenOptions).AsTask(cancellationToken);
			var winrtStream = await winrtStreamTask;

			return winrtStream.AsStream();
		}

		/// <inheritdoc/>
		public override async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			var parentFolderTask = storage.GetParentAsync().AsTask(cancellationToken);
			var parentFolder = await parentFolderTask;

			return new WindowsStorageFolderLegacy(parentFolder);
		}

		private static FileAccessMode GetFileAccessMode(FileAccess access)
		{
			return access switch
			{
				FileAccess.Read => FileAccessMode.Read,
				FileAccess.Write => FileAccessMode.ReadWrite,
				FileAccess.ReadWrite => FileAccessMode.ReadWrite,
				_ => throw new ArgumentOutOfRangeException(nameof(access))
			};
		}

		private static StorageOpenOptions GetStorageOpenOptions(FileShare share)
		{
			return share switch
			{
				FileShare.Read => StorageOpenOptions.AllowOnlyReaders,
				FileShare.Write => StorageOpenOptions.AllowReadersAndWriters,
				FileShare.ReadWrite => StorageOpenOptions.AllowReadersAndWriters,
				FileShare.Inheritable => StorageOpenOptions.None,
				FileShare.Delete => StorageOpenOptions.None,
				FileShare.None => StorageOpenOptions.None,
				_ => throw new ArgumentOutOfRangeException(nameof(share))
			};
		}
	}
}
