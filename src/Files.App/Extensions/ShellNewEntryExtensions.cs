// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils;
using Files.App.Helpers;
using Files.App.Utils.Shell;
using Files.Shared;
using Files.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Extensions
{
	public static class ShellNewEntryExtensions
	{
		public static async Task<List<ShellNewEntry>> GetNewContextMenuEntries()
		{
			var shellEntryList = new List<ShellNewEntry>();

			var entries = await SafetyExtensions.IgnoreExceptions(() => ShellNewMenuHelper.GetNewContextMenuEntries(), App.Logger);
			if (entries is not null)
			{
				shellEntryList.AddRange(entries);
			}

			return shellEntryList;
		}

		public static async Task<ShellNewEntry?> GetNewContextMenuEntryForType(string extension)
		{
			return await SafetyExtensions.IgnoreExceptions(() => ShellNewMenuHelper.GetNewContextMenuEntryForType(extension), App.Logger);
		}

		public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, string filePath, IShellPage associatedInstance)
		{
			var parentFolder = await associatedInstance.ShellViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(filePath));
			if (parentFolder)
			{
				return await Create(shellEntry, parentFolder, filePath);
			}

			return new FilesystemResult<BaseStorageFile>(null, parentFolder.ErrorCode);
		}

		public static async Task<FilesystemResult<BaseStorageFile>> Create(this ShellNewEntry shellEntry, BaseStorageFolder parentFolder, string filePath)
		{
			FilesystemResult<BaseStorageFile> createdFile = null;
			var fileName = Path.GetFileName(filePath);

			if (shellEntry.Template is null)
			{
				createdFile = await FilesystemTasks.Wrap(() => parentFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName).AsTask());
			}
			else
			{
				createdFile = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFileFromPathAsync(shellEntry.Template))
					.OnSuccess(t => t.CopyAsync(parentFolder, fileName, NameCollisionOption.GenerateUniqueName).AsTask());
			}

			if (createdFile && shellEntry.Data is not null)
			{
				// Calls unsupported OpenTransactedWriteAsync
				//await FileIO.WriteBytesAsync(createdFile.Result, shellEntry.Data);

				await createdFile.Result.WriteBytesAsync(shellEntry.Data);
			}

			return createdFile;
		}
	}
}
