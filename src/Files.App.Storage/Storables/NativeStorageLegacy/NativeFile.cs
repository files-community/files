// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.App.Storage.Storables
{
	/// <inheritdoc cref="IFile"/>
	[Obsolete("Use the new WindowsStorable")]
	public class NativeFileLegacy : NativeStorableLegacy<FileInfo>, ILocatableFile, IModifiableFile, IFileExtended, INestedFile
	{
		public NativeFileLegacy(FileInfo fileInfo, string? name = null)
			: base(fileInfo, name)
		{
		}

		public NativeFileLegacy(string path, string? name = null)
			: this(new FileInfo(path), name)
		{
		}

		/// <inheritdoc/>
		public virtual Task<Stream> OpenStreamAsync(FileAccess access, CancellationToken cancellationToken = default)
		{
			return OpenStreamAsync(access, FileShare.None, cancellationToken);
		}

		/// <inheritdoc/>
		public virtual Task<Stream> OpenStreamAsync(FileAccess access, FileShare share = FileShare.None, CancellationToken cancellationToken = default)
		{
			var stream = File.Open(Path, FileMode.Open, access, share);
			return Task.FromResult<Stream>(stream);
		}
	}
}
