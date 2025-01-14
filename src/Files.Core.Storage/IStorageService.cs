// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Core.Storage.Storables;
using System.Threading;
using System.Threading.Tasks;

namespace Files.Core.Storage
{
	/// <summary>
	/// Provides an abstract layer for accessing the file system.
	/// </summary>
	public interface IStorageService
	{
		/// <summary>
		/// Gets the file with associated <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The unique ID of the file to retrieve.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If file is found and access is granted, returns <see cref="IFile"/>, otherwise throws an exception.</returns>
		Task<IFile> GetFileAsync(string id, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the folder with associated <paramref name="id"/>.
		/// </summary>
		/// <param name="id">The unique ID of the folder to retrieve.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If folder is found and access is granted, returns <see cref="IFolder"/>, otherwise throws an exception.</returns>
		Task<IFolder> GetFolderAsync(string id, CancellationToken cancellationToken = default);
	}
}
