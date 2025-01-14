// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;
using IO = System.IO;

namespace Files.App.Utils
{
	public sealed class StorageFileWithPath : IStorageItemWithPath
	{
		public string Path { get; }
		public string Name => Item?.Name ?? IO.Path.GetFileName(Path);

		IStorageItem IStorageItemWithPath.Item => Item;
		public BaseStorageFile Item { get; }

		public FilesystemItemType ItemType => FilesystemItemType.File;

		public StorageFileWithPath(BaseStorageFile file)
			: this(file, file.Path) { }
		public StorageFileWithPath(BaseStorageFile file, string path)
			=> (Item, Path) = (file, path);
	}
}
