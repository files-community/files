// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Core.Storage.Storables;

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents a service used to manage file tags.
	/// </summary>
	public interface IFileTagsService
	{
		/// <summary>
		/// Checks if file tags are supported by the platform.
		/// </summary>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. Value is true if file tags are supported, otherwise false.</returns>
		Task<bool> IsSupportedAsync();

		/// <summary>
		/// Sets or updates tag values for a given <paramref name="storable"/>.
		/// </summary>
		/// <param name="storable">The storable object to update tags for.</param>
		/// <param name="tagUids">The tag UIDs to set.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation. If successful, returns true, otherwise false.</returns>
		Task<bool> SetFileTagAsync(ILocatableStorable storable, string[] tagUids, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all tags which are used to tag files and folders from the database.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="string"/> that represents all tags.</returns>
		IAsyncEnumerable<TagViewModel> GetTagsAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all items that are tagged with a specific <paramref name="tag"/>.
		/// </summary>
		/// <param name="tagUid">The UID of a tag.</param>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="TaggedItemModel"/>.</returns>
		IAsyncEnumerable<TaggedItemModel> GetItemsForTagAsync(string tagUid, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets all file/folder tags from the database.
		/// </summary>
		/// <param name="cancellationToken">A <see cref="CancellationToken"/> that cancels this action.</param>
		/// <returns>Returns an async operation represented by <see cref="IAsyncEnumerable{T}"/> of type <see cref="TaggedItemModel"/> of tags saved in the database.</returns>
		IAsyncEnumerable<TaggedItemModel> GetAllFileTagsAsync(CancellationToken cancellationToken = default);
	}
}
