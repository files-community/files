// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.Storage.Storables
{
	/// <summary>
	/// Extends existing <see cref="IFolder"/> interface with additional functionality.
	/// </summary>
	public interface IFolderExtended : IFolder
	{
		/// <summary>
		/// Gets a file in the current directory by name.
		/// </summary>
		/// <param name="fileName">The name of the file.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. Value is <see cref="IFile"/>, otherwise an exception is thrown.</returns>
		Task<INestedFile> GetFileAsync(string fileName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets a folder in the current directory by name.
		/// </summary>
		/// <param name="folderName">The name of the folder.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If folder is found and access is granted, returns <see cref="IFolder"/> otherwise null.</returns>
		Task<INestedFolder> GetFolderAsync(string folderName, CancellationToken cancellationToken = default);
	}
}
