// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.Core.Storage.Extensions
{
	public static partial class StorageExtensions
	{
		/// <inheritdoc cref="IFileExtended.OpenStreamAsync(FileAccess, FileShare, CancellationToken)"/>
		public static async Task<Stream> OpenStreamAsync(this IFile file, FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
		{
			if (file is IFileExtended fileExtended)
				return await fileExtended.OpenStreamAsync(access, share, cancellationToken);

			// TODO: Check if the file inherits from ILockableStorable and ensure a disposable handle to it via Stream bridge
			return await file.OpenStreamAsync(access, cancellationToken);
		}

		/// <returns>If successful, returns a <see cref="Stream"/>, otherwise null.</returns>
		/// <inheritdoc cref="IFile.OpenStreamAsync"/>
		public static async Task<Stream?> TryOpenStreamAsync(this IFile file, FileAccess access, CancellationToken cancellationToken = default)
		{
			try
			{
				return await file.OpenStreamAsync(access, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <returns>If successful, returns a <see cref="Stream"/>, otherwise null.</returns>
		/// <inheritdoc cref="IFile.OpenStreamAsync"/>
		public static async Task<Stream?> TryOpenStreamAsync(this IFile file, FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
		{
			try
			{
				return await OpenStreamAsync(file, access, share, cancellationToken);
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Copies contents of <paramref name="source"/> to <paramref name="destination"/> overwriting existing data.
		/// </summary>
		/// <param name="source">The source file to copy from.</param>
		/// <param name="destination">The destination file to copy to.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		public static async Task CopyContentsToAsync(this IFile source, IFile destination, CancellationToken cancellationToken = default)
		{
			await using var sourceStream = await source.OpenStreamAsync(FileAccess.Read, cancellationToken);
			await using var destinationStream = await destination.OpenStreamAsync(FileAccess.Read, cancellationToken);
			await sourceStream.CopyToAsync(destinationStream, cancellationToken);
		}
	}
}
