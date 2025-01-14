// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.Storage.StorageEnumeration
{
	/// <summary>
	/// Enumerates storage objects of a given directory.
	/// <remarks>
	/// This interface can be implemented to provide complex enumeration of directories as well as being a substitute for built-in <see cref="IFolder"/> enumeration.</remarks>
	/// </summary>
	public interface IStorageEnumerator : IDisposable
	{
		/// <summary>
		/// Gets the folder where enumeration takes place.
		/// </summary>
		IFolder SourceFolder { get; }

		/// <summary>
		/// Enumerates the <see cref="SourceFolder"/> for files.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="IFile"/> of all files discovered by the enumerator.</returns>
		IAsyncEnumerable<IFile> EnumerateFilesAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Enumerates the <see cref="SourceFolder"/> for folders.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="IFolder"/> of all folders discovered by the enumerator.</returns>
		IAsyncEnumerable<IFolder> EnumerateFoldersAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Enumerates the <see cref="SourceFolder"/> for items.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="IStorable"/> of all items discovered by the enumerator.</returns>
		IAsyncEnumerable<IStorable> EnumerateStorageAsync(CancellationToken cancellationToken = default);
	}
}
