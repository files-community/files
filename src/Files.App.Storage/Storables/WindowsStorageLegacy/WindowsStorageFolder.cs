// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Windows.Storage;

namespace Files.App.Storage.Storables
{
	/// <inheritdoc cref="IFolder"/>
	[Obsolete("Use the new WindowsStorable")]
	public sealed class WindowsStorageFolderLegacy : WindowsStorableLegacy<StorageFolder>, ILocatableFolder, IFolderExtended, INestedFolder, IDirectCopy, IDirectMove
	{
		// TODO: Implement IMutableFolder

		public WindowsStorageFolderLegacy(StorageFolder storage)
			: base(storage)
		{
		}

		/// <inheritdoc/>
		public async Task<INestedFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default)
		{
			var file = await storage.GetFileAsync(fileName).AsTask(cancellationToken);
			return new WindowsStorageFileLegacy(file);
		}

		/// <inheritdoc/>
		public async Task<INestedFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default)
		{
			var folder = await storage.GetFolderAsync(folderName).AsTask(cancellationToken);
			return new WindowsStorageFolderLegacy(folder);
		}

		/// <inheritdoc/>
		public async IAsyncEnumerable<INestedStorable> GetItemsAsync(StorableKind kind = StorableKind.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			switch (kind)
			{
				case StorableKind.Files:
				{
					var files = await storage.GetFilesAsync().AsTask(cancellationToken);
					foreach (var item in files)
					{
						yield return new WindowsStorageFileLegacy(item);
					}

					break;
				}

				case StorableKind.Folders:
				{
					var folders = await storage.GetFoldersAsync().AsTask(cancellationToken);
					foreach (var item in folders)
					{
						yield return new WindowsStorageFolderLegacy(item);
					}

					break;
				}

				case StorableKind.All:
				{
					var items = await storage.GetItemsAsync().AsTask(cancellationToken);
					foreach (var item in items)
					{
						if (item is StorageFile storageFile)
							yield return new WindowsStorageFileLegacy(storageFile);

						if (item is StorageFolder storageFolder)
							yield return new WindowsStorageFolderLegacy(storageFolder);
					}

					break;
				}

				default:
					yield break;
			}
		}

		/// <inheritdoc/>
		public Task DeleteAsync(INestedStorable item, bool permanently = default, CancellationToken cancellationToken = default)
		{
			return item switch
			{
				WindowsStorableLegacy<StorageFile> storageFile => storageFile.storage
					.DeleteAsync(GetWindowsStorageDeleteOption(permanently))
					.AsTask(cancellationToken),

				WindowsStorableLegacy<StorageFolder> storageFolder => storageFolder.storage
					.DeleteAsync(GetWindowsStorageDeleteOption(permanently))
					.AsTask(cancellationToken),

				_ => throw new NotImplementedException()
			};
		}

		/// <inheritdoc/>
		public async Task<INestedStorable> CreateCopyOfAsync(INestedStorable itemToCopy, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			if (itemToCopy is WindowsStorableLegacy<StorageFile> sourceFile)
			{
				var copiedFile = await sourceFile.storage.CopyAsync(storage, itemToCopy.Name, GetWindowsNameCollisionOption(overwrite)).AsTask(cancellationToken);
				return new WindowsStorageFileLegacy(copiedFile);
			}

			throw new ArgumentException($"Could not copy type {itemToCopy.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<INestedStorable> MoveFromAsync(INestedStorable itemToMove, IModifiableFolder source, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			if (itemToMove is WindowsStorableLegacy<StorageFile> sourceFile)
			{
				await sourceFile.storage.MoveAsync(storage, itemToMove.Name, GetWindowsNameCollisionOption(overwrite)).AsTask(cancellationToken);
				return new WindowsStorageFileLegacy(sourceFile.storage);
			}

			throw new ArgumentException($"Could not copy type {itemToMove.GetType()}");
		}

		/// <inheritdoc/>
		public async Task<INestedFile> CreateFileAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			var file = await storage.CreateFileAsync(desiredName, GetWindowsCreationCollisionOption(overwrite)).AsTask(cancellationToken);
			return new WindowsStorageFileLegacy(file);
		}

		/// <inheritdoc/>
		public async Task<INestedFolder> CreateFolderAsync(string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			var folder = await storage.CreateFolderAsync(desiredName, GetWindowsCreationCollisionOption(overwrite)).AsTask(cancellationToken);
			return new WindowsStorageFolderLegacy(folder);
		}

		/// <inheritdoc/>
		public override async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			var parentFolder = await storage.GetParentAsync().AsTask(cancellationToken);
			return new WindowsStorageFolderLegacy(parentFolder);
		}

		private static StorageDeleteOption GetWindowsStorageDeleteOption(bool permanently)
		{
			return permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default;
		}

		private static NameCollisionOption GetWindowsNameCollisionOption(bool overwrite)
		{
			return overwrite ? NameCollisionOption.ReplaceExisting : NameCollisionOption.GenerateUniqueName;
		}

		private static CreationCollisionOption GetWindowsCreationCollisionOption(bool overwrite)
		{
			return overwrite ? CreationCollisionOption.ReplaceExisting : CreationCollisionOption.OpenIfExists;
		}
	}
}
