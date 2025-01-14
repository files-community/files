// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;

namespace Files.App.Storage.Storables
{
	/// <inheritdoc cref="IStorable"/>
	[Obsolete("Use the new WindowsStorable")]
	public abstract class NativeStorableLegacy<TStorage> : ILocatableStorable, INestedStorable
		where TStorage : FileSystemInfo
	{
		protected readonly TStorage storage;

		/// <inheritdoc/>
		public string Path { get; protected set; }

		/// <inheritdoc/>
		public string Name { get; protected set; }

		/// <inheritdoc/>
		public virtual string Id { get; }

		protected NativeStorableLegacy(TStorage storage, string? name = null)
		{
			this.storage = storage;
			Path = storage.FullName;
			Name = name ?? storage.Name;
			Id = storage.FullName;
		}

		/// <inheritdoc/>
		public virtual Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
		{
			var parent = Directory.GetParent(Path);
			if (parent is null)
				return Task.FromResult<IFolder?>(null);

			return Task.FromResult<IFolder?>(new NativeFolderLegacy(parent));
		}

		/// <summary>
		/// Formats a given <paramref name="path"/>.
		/// </summary>
		/// <param name="path">The path to format.</param>
		/// <returns>A formatted path.</returns>
		protected static string FormatPath(string path)
		{
			path = path.Replace("file:///", string.Empty);

			if ('/' != System.IO.Path.DirectorySeparatorChar)
				return path.Replace('/', System.IO.Path.DirectorySeparatorChar);

			if ('\\' != System.IO.Path.DirectorySeparatorChar)
				return path.Replace('\\', System.IO.Path.DirectorySeparatorChar);

			return path;
		}
	}
}
