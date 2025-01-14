// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Windows.Storage;

namespace Files.App.Storage.Storables
{
	/// <inheritdoc cref="IStorable"/>
	[Obsolete("Use the new WindowsStorable")]
	public abstract class WindowsStorableLegacy<TStorage> : ILocatableStorable, INestedStorable
		where TStorage : class, IStorageItem
	{
		private string? _computedId;
		internal readonly TStorage storage;

		/// <inheritdoc/>
		public string Path { get; protected internal set; }

		/// <inheritdoc/>
		public string Name { get; protected internal set; }

		/// <inheritdoc/>
		public virtual string Id => _computedId ??= ChecksumHelpers.CalculateChecksumForPath(Path);

		protected internal WindowsStorableLegacy(TStorage storage)
		{
			this.storage = storage;
			Path = storage.Path;
			Name = storage.Name;
		}

		/// <inheritdoc/>
		public abstract Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default);
	}
}
