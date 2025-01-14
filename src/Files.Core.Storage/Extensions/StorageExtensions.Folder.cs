// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace Files.Core.Storage.Extensions
{
	public static partial class StorageExtensions
	{
		#region Without Result

		/// <returns>If file was found, returns the requested <see cref="IFile"/>, otherwise null.</returns>
		/// <inheritdoc cref="IFolderExtended.GetFileAsync"/>
		public static async Task<IFile?> TryGetFileAsync(this IFolder folder, string fileName, CancellationToken cancellationToken = default)
		{
			try
			{
				if (folder is IFolderExtended folderExtended)
					return await folderExtended.GetFileAsync(fileName, cancellationToken);

				await foreach (var item in folder.GetFilesAsync(cancellationToken))
				{
					if (item.Name == fileName)
						return item;
				}

				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <returns>If folder was found, returns the requested <see cref="IFolder"/>, otherwise null.</returns>
		/// <inheritdoc cref="IFolderExtended.GetFileAsync"/>
		public static async Task<IFolder?> TryGetFolderAsync(this IFolder folder, string folderName, CancellationToken cancellationToken = default)
		{
			try
			{
				if (folder is IFolderExtended folderExtended)
					return await folderExtended.GetFolderAsync(folderName, cancellationToken);

				await foreach (var item in folder.GetFoldersAsync(cancellationToken))
				{
					if (item.Name == folderName)
						return item;
				}

				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <returns>If file was created, returns the requested <see cref="IFile"/>, otherwise null.</returns>
		/// <inheritdoc cref="IModifiableFolder.CreateFileAsync"/>
		public static async Task<IFile?> TryCreateFileAsync(this IModifiableFolder folder, string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			try
			{
				return await folder.CreateFileAsync(desiredName, overwrite, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <returns>If folder was created, returns the requested <see cref="IFolder"/>, otherwise null.</returns>
		/// <inheritdoc cref="IModifiableFolder.CreateFolderAsync"/>
		public static async Task<IFolder?> TryCreateFolderAsync(this IModifiableFolder folder, string desiredName, bool overwrite = default, CancellationToken cancellationToken = default)
		{
			try
			{
				return await folder.CreateFolderAsync(desiredName, overwrite, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}

		#endregion

		#region Other

		/// <summary>
		/// Gets all files contained within <paramref name="folder"/>.
		/// </summary>
		/// <param name="folder">The folder to enumerate.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="IFile"/> of files in the directory.</returns>
		public static async IAsyncEnumerable<IFile> GetFilesAsync(this IFolder folder, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in folder.GetItemsAsync(StorableKind.Files, cancellationToken))
			{
				if (item is IFile fileItem)
					yield return fileItem;
			}
		}

		/// <summary>
		/// Gets all folders contained within <paramref name="folder"/>.
		/// </summary>
		/// <param name="folder">The folder to enumerate.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="IFolder"/> of folders in the directory.</returns>
		public static async IAsyncEnumerable<IFolder> GetFoldersAsync(this IFolder folder, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await foreach (var item in folder.GetItemsAsync(StorableKind.Files, cancellationToken))
			{
				if (item is IFolder folderItem)
					yield return folderItem;
			}
		}

		#endregion
	}
}
