// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.Core.Storage.Storables
{
	/// <summary>
	/// Represents a file that provides additional options for its manipulation.
	/// </summary>
	public interface IFileExtended : IFile
	{
		/// <param name="share">The file sharing flags that specify access other processes have to the file.</param>
		/// <inheritdoc cref="IFile.OpenStreamAsync"/>
		Task<Stream> OpenStreamAsync(FileAccess access, FileShare share, CancellationToken cancellationToken = default);
	}
}
