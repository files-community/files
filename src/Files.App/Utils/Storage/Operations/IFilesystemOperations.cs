// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Represents an interface for file system operations.
	/// </summary>
	/// <remarks>
	/// Each operation returns <see cref="Task{IStorageHistory}"/> and the <see cref="IStorageHistory"/> is not saved automatically.
	/// </remarks>
	public interface IFilesystemOperations : IDisposable
	{
		/// <summary>
		/// Creates an item from <paramref name="source"/>
		/// </summary>
		/// <param name="source">FullPath to the item</param>
		/// <param name="process">Progress of the operation</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The created item fullPath (as <see cref="PathWithType"/>)
		/// <br/>
		/// Destination: null
		/// </returns>
		Task<(IStorageHistory, IStorageItem)> CreateAsync(
			IStorageItemWithPath source,
			IProgress<StatusCenterItemProgressModel> process,
			CancellationToken cancellationToken,
			bool asAdmin = false);

		Task<IStorageHistory> CreateShortcutItemsAsync(
			IList<IStorageItemWithPath> source,
			IList<string> destination,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken);

		/// <summary>
		/// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source item to be copied</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="collision">The item naming collision</param>
		/// <param name="progress">Progress of the operation</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The <paramref name="source"/> item fullPath (as <see cref="PathWithType"/>)
		/// <br/>
		/// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> was copied
		/// </returns>
		Task<IStorageHistory> CopyAsync(
			IStorageItem source,
			string destination,
			NameCollisionOption collision,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken);

		/// <summary>
		/// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source item to be copied</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="collision">The item naming collision</param>
		/// <param name="progress">Progress of the operation</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The <paramref name="source"/> item fullPath (as <see cref="PathWithType"/>)
		/// <br/>
		/// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> was copied
		/// </returns>
		Task<IStorageHistory> CopyAsync(
			IStorageItemWithPath source,
			string destination,
			NameCollisionOption collision,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken);

		/// <summary>
		/// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		Task<IStorageHistory> CopyItemsAsync(
			IList<IStorageItem> source,
			IList<string> destination,
			IList<FileNameConflictResolveOptionType> collisions,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken);

		/// <summary>
		/// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		Task<IStorageHistory> CopyItemsAsync(
			IList<IStorageItemWithPath> source,
			IList<string> destination,
			IList<FileNameConflictResolveOptionType> collisions,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken,
			bool asAdmin = false);

		/// <summary>
		/// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source item to be moved</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="collision">The item naming collision</param>
		/// <param name="progress">Progress of the operation</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The source item fullPath (as <see cref="PathWithType"/>)
		/// <br/>
		/// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> was moved
		/// </returns>
		Task<IStorageHistory> MoveAsync(
			IStorageItem source,
			string destination,
			NameCollisionOption collision,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken);

		/// <summary>
		/// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source item to be moved</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="collision">The item naming collision</param>
		/// <param name="progress">Progress of the operation</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The source item fullPath (as <see cref="PathWithType"/>)
		/// <br/>
		/// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> was moved
		/// </returns>
		Task<IStorageHistory> MoveAsync(
			IStorageItemWithPath source,
			string destination,
			NameCollisionOption collision,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken);

		/// <summary>
		/// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		Task<IStorageHistory> MoveItemsAsync(
			IList<IStorageItem> source,
			IList<string> destination,
			IList<FileNameConflictResolveOptionType> collisions,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken);

		/// <summary>
		/// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		Task<IStorageHistory> MoveItemsAsync(
			IList<IStorageItemWithPath> source,
			IList<string> destination,
			IList<FileNameConflictResolveOptionType> collisions,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken,
			bool asAdmin = false);

		/// <summary>
		/// Deletes <paramref name="source"/>
		/// </summary>
		/// <param name="source">The source to delete</param>
		/// <param name="progress">Progress of the operation</param>
		/// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The deleted item fullPath (as <see cref="PathWithType"/>)
		/// <br/>
		/// Destination:
		/// <br/>
		/// Returns null if <paramref name="permanently"/> was true
		/// <br/>
		/// If <paramref name="permanently"/> was false, returns path to recycled item
		/// </returns>
		Task<IStorageHistory> DeleteAsync(
			IStorageItem source,
			IProgress<StatusCenterItemProgressModel> progress,
			bool permanently,
			CancellationToken cancellationToken);

		/// <summary>
		/// Deletes <paramref name="source"/>
		/// </summary>
		/// <param name="source">The source to delete</param>
		/// <param name="progress">Progress of the operation</param>
		/// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The deleted item fullPath (as <see cref="PathWithType"/>)
		/// <br/>
		/// Destination:
		/// <br/>
		/// Returns null if <paramref name="permanently"/> was true
		/// <br/>
		/// If <paramref name="permanently"/> was false, returns path to recycled item
		/// </returns>
		Task<IStorageHistory> DeleteAsync(
			IStorageItemWithPath source,
			IProgress<StatusCenterItemProgressModel> progress,
			bool permanently,
			CancellationToken cancellationToken);

		/// <summary>
		/// Deletes provided <paramref name="source"/>
		/// </summary>
		Task<IStorageHistory> DeleteItemsAsync(
			IList<IStorageItem> source,
			IProgress<StatusCenterItemProgressModel> progress,
			bool permanently,
			CancellationToken cancellationToken);

		/// <summary>
		/// Deletes provided <paramref name="source"/>
		/// </summary>
		Task<IStorageHistory> DeleteItemsAsync(
			IList<IStorageItemWithPath> source,
			IProgress<StatusCenterItemProgressModel> progress,
			bool permanently,
			CancellationToken cancellationToken,
			bool asAdmin = false);

		/// <summary>
		/// Renames <paramref name="source"/> with <paramref name="newName"/>
		/// </summary>
		/// <param name="source">The item to rename</param>
		/// <param name="newName">Desired new name</param>
		/// <param name="collision">Determines what to do if item already exists</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The original item fullPath (as <see cref="PathWithType"/>)
		/// <br/>
		/// Destination: The renamed item fullPath (as <see cref="PathWithType"/>)
		/// </returns>
		Task<IStorageHistory> RenameAsync(
			IStorageItem source,
			string newName,
			NameCollisionOption collision,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken);

		/// <summary>
		/// Renames <paramref name="source"/> fullPath with <paramref name="newName"/>
		/// </summary>
		/// <param name="source">The item to rename</param>
		/// <param name="newName">Desired new name</param>
		/// <param name="collision">Determines what to do if item already exists</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The original item fullPath (as <see cref="PathWithType"/>)
		/// <br/>
		/// Destination: The renamed item fullPath (as <see cref="PathWithType"/>)
		/// </returns>
		Task<IStorageHistory> RenameAsync(
			IStorageItemWithPath source,
			string newName,
			NameCollisionOption collision,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken,
			bool asAdmin = false);

		/// <summary>
		/// Restores <paramref name="source"/> from the RecycleBin to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source Recycle Bin item path</param>
		/// <param name="destination">The destination fullPath to restore to</param>
		/// <param name="progress">Progress of the operation</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The trash item fullPath
		/// <br/>
		/// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> has been restored
		/// </returns>
		Task<IStorageHistory> RestoreItemsFromTrashAsync(
			IList<IStorageItem> source,
			IList<string> destination,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken);

		/// <summary>
		/// Restores <paramref name="source"/> from the RecycleBin to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source Recycle Bin item path</param>
		/// <param name="destination">The destination fullPath to restore to</param>
		/// <param name="progress">Progress of the operation</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The trash item fullPath
		/// <br/>
		/// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> has been restored
		/// </returns>
		Task<IStorageHistory> RestoreFromTrashAsync(
			IStorageItem source,
			string destination,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken);

		/// <summary>
		/// Restores <paramref name="source"/> from the RecycleBin to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source Recycle Bin item path</param>
		/// <param name="destination">The destination fullPath to restore to</param>
		/// <param name="progress">Progress of the operation</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The trash item fullPath
		/// <br/>
		/// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> has been restored
		/// </returns>
		Task<IStorageHistory> RestoreItemsFromTrashAsync(
			IList<IStorageItemWithPath> source,
			IList<string> destination,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken,
			bool asAdmin = false);

		/// <summary>
		/// Restores <paramref name="source"/> from the RecycleBin to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source Recycle Bin item path</param>
		/// <param name="destination">The destination fullPath to restore to</param>
		/// <param name="progress">Progress of the operation</param>
		/// <param name="cancellationToken">Can be cancelled with <see cref="CancellationToken"/></param>
		/// <returns><see cref="IStorageHistory"/> where:
		/// <br/>
		/// Source: The trash item fullPath
		/// <br/>
		/// Destination: The <paramref name="destination"/> item fullPath (as <see cref="PathWithType"/>) the <paramref name="source"/> has been restored
		/// </returns>
		Task<IStorageHistory> RestoreFromTrashAsync(
			IStorageItemWithPath source,
			string destination,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken);
	}
}
