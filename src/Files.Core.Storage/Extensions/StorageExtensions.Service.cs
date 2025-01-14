// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.Storage.Extensions
{
	public static partial class StorageExtensions
	{
		/// <summary>
		/// Checks whether the directory exists at a given path and tries to retrieve the folder, otherwise tries to retrieve the file.
		/// </summary>
		/// <param name="storageService">The service.</param>
		/// <param name="id">The unique ID of the storable to retrieve.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful, value is <see cref="IStorable"/> that represents the item, otherwise null.</returns>
		public static async Task<IStorable?> TryGetStorableAsync(this IStorageService storageService, string id, CancellationToken cancellationToken = default)
		{
			return (IStorable?)await storageService.TryGetFolderAsync(id, cancellationToken) ?? await storageService.TryGetFileAsync(id, cancellationToken);
		}

		/// <summary>
		/// Tries to retrieve a folder associated with <paramref name="id"/>.
		/// </summary>
		/// <param name="storageService">The service.</param>
		/// <param name="id">The unique ID of the folder to retrieve.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful, value is <see cref="IFolder"/> that represents the folder, otherwise null.</returns>
		public static async Task<IFolder?> TryGetFolderAsync(this IStorageService storageService, string id, CancellationToken cancellationToken = default)
		{
			try
			{
				return await storageService.GetFolderAsync(id, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Tries to retrieve a file associated with <paramref name="id"/>.
		/// </summary>
		/// <param name="storageService">The service.</param>
		/// <param name="id">The unique ID of the file to retrieve.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful, value is <see cref="IFile"/> that represents the file, otherwise null.</returns>
		public static async Task<IFile?> TryGetFileAsync(this IStorageService storageService, string id, CancellationToken cancellationToken = default)
		{
			try
			{
				return await storageService.GetFileAsync(id, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}
