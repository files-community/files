// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace Files.App.Utils.Storage
{
	public interface IFilesystemHelpers : IDisposable
	{
		/// <summary>
		/// Creates an item from <paramref name="source"/>
		/// </summary>
		/// <param name="source">FullPath to the item</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<(ReturnResult, IStorageItem?)> CreateAsync(IStorageItemWithPath source, bool registerHistory);

		#region Delete

		/// <summary>
		/// Deletes provided <paramref name="source"/>
		/// </summary>
		/// <param name="source">The <paramref name="source"/> to delete</param>
		/// <param name="showDialog">Determines whether to show delete confirmation dialog</param>
		/// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> DeleteItemsAsync(IEnumerable<IStorageItem> source, DeleteConfirmationPolicies showDialog, bool permanently, bool registerHistory);

		/// <summary>
		/// Deletes provided <paramref name="source"/>
		/// </summary>
		/// <param name="source">The <paramref name="source"/> to delete</param>
		/// <param name="showDialog">Determines whether to show delete confirmation dialog</param>
		/// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> DeleteItemAsync(IStorageItem source, DeleteConfirmationPolicies showDialog, bool permanently, bool registerHistory);

		/// <summary>
		/// Deletes provided <paramref name="source"/>
		/// </summary>
		/// <param name="source">The <paramref name="source"/> to delete</param>
		/// <param name="showDialog">Determines whether to show delete confirmation dialog</param>
		/// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> DeleteItemsAsync(IEnumerable<IStorageItemWithPath> source, DeleteConfirmationPolicies showDialog, bool permanently, bool registerHistory);

		/// <summary>
		/// Deletes provided <paramref name="source"/>
		/// </summary>
		/// <param name="source">The <paramref name="source"/> to delete</param>
		/// <param name="showDialog">Determines whether to show delete confirmation dialog</param>
		/// <param name="permanently">Determines whether <paramref name="source"/> is be deleted permanently</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> DeleteItemAsync(IStorageItemWithPath source, DeleteConfirmationPolicies showDialog, bool permanently, bool registerHistory);

		#endregion Delete

		#region Restore

		/// <summary>
		/// Restores <paramref name="source"/> from the RecycleBin to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source Recycle Bin item path</param>
		/// <param name="destination">The destination fullPath to restore to</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> RestoreItemFromTrashAsync(IStorageItem source, string destination, bool registerHistory);

		/// <summary>
		/// Restores <paramref name="source"/> from the RecycleBin to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source Recycle Bin item path</param>
		/// <param name="destination">The destination fullPath to restore to</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> RestoreItemsFromTrashAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool registerHistory);

		/// <summary>
		/// Restores <paramref name="source"/> from the RecycleBin to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source Recycle Bin item path</param>
		/// <param name="destination">The destination fullPath to restore to</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> RestoreItemFromTrashAsync(IStorageItemWithPath source, string destination, bool registerHistory);

		/// <summary>
		/// Restores <paramref name="source"/> from the RecycleBin to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source Recycle Bin item path</param>
		/// <param name="destination">The destination fullPath to restore to</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> RestoreItemsFromTrashAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool registerHistory);

		#endregion Restore

		/// <summary>
		/// Performs relevant operation based on <paramref name="operation"/>
		/// </summary>
		/// <param name="operation">The operation</param>
		/// <param name="packageView">The package view data</param>
		/// <param name="destination">Destination directory to perform the operation
		/// <param name="showDialog">Determines whether to show dialog</param>
		/// <br/>
		/// <br/>
		/// Note:
		/// <br/>
		/// The <paramref name="destination"/> is NOT fullPath</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> PerformOperationTypeAsync(DataPackageOperation operation, DataPackageView packageView, string destination, bool showDialog, bool registerHistory, bool isDestinationExecutable = false, bool isDestinationScript = false);

		#region Copy

		/// <summary>
		/// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source items to be copied</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="showDialog">Determines whether to show copy dialog</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> CopyItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool showDialog, bool registerHistory);

		/// <summary>
		/// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source item to be copied</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="showDialog">Determines whether to show copy dialog</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> CopyItemAsync(IStorageItem source, string destination, bool showDialog, bool registerHistory);

		/// <summary>
		/// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source items to be copied</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="showDialog">Determines whether to show copy dialog</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> CopyItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool showDialog, bool registerHistory);

		/// <summary>
		/// Copies <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source item to be copied</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="showDialog">Determines whether to show copy dialog</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> CopyItemAsync(IStorageItemWithPath source, string destination, bool showDialog, bool registerHistory);

		/// <summary>
		/// Copies items from clipboard to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="packageView">Clipboard data</param>
		/// <param name="destination">Destination directory to perform the operation
		/// <param name="showDialog">Determines whether to show copy dialog</param>
		/// <br/>
		/// <br/>
		/// Note:
		/// <br/>
		/// The <paramref name="destination"/> is NOT fullPath</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> CopyItemsFromClipboard(DataPackageView packageView, string destination, bool showDialog, bool registerHistory);

		Task<ReturnResult> RecycleItemsFromClipboard(DataPackageView packageView, string destination, DeleteConfirmationPolicies showDialog, bool registerHistory);

		Task<ReturnResult> CreateShortcutFromClipboard(DataPackageView packageView, string destination, bool showDialog, bool registerHistory);

		#endregion Copy

		#region Move

		/// <summary>
		/// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source items to be moved</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="showDialog">Determines whether to show move dialog</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> MoveItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool showDialog, bool registerHistory);

		/// <summary>
		/// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source to move</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="showDialog">Determines whether to show move dialog</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> MoveItemAsync(IStorageItem source, string destination, bool showDialog, bool registerHistory);

		/// <summary>
		/// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source items to be moved</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="showDialog">Determines whether to show move dialog</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> MoveItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool showDialog, bool registerHistory);

		/// <summary>
		/// Moves <paramref name="source"/> to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="source">The source to move</param>
		/// <param name="destination">The destination fullPath</param>
		/// <param name="showDialog">Determines whether to show move dialog</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> MoveItemAsync(IStorageItemWithPath source, string destination, bool showDialog, bool registerHistory);

		/// <summary>
		/// Moves items from clipboard to <paramref name="destination"/> fullPath
		/// </summary>
		/// <param name="packageView">Clipboard data</param>
		/// <param name="destination">Destination directory to perform the operation
		/// <param name="showDialog">Determines whether to show move dialog</param>
		/// <br/>
		/// <br/>
		/// Note:
		/// <br/>
		/// The <paramref name="destination"/> is NOT fullPath</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> MoveItemsFromClipboard(DataPackageView packageView, string destination, bool showDialog, bool registerHistory);

		#endregion Move

		/// <summary>
		/// Renames <paramref name="source"/> with <paramref name="newName"/>
		/// </summary>
		/// <param name="source">The item to rename</param>
		/// <param name="newName">Desired new name</param>
		/// <param name="collision">Determines what to do if item already exists</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <param name="showExtensionDialog">Determines wheteher the Extension Modified Dialog is shown</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, bool registerHistory, bool showExtensionDialog = true);

		/// <summary>
		/// Renames <paramref name="source"/> fullPath with <paramref name="newName"/>
		/// </summary>
		/// <param name="source">The item to rename</param>
		/// <param name="newName">Desired new name</param>
		/// <param name="collision">Determines what to do if item already exists</param>
		/// <param name="registerHistory">Determines whether <see cref="IStorageHistory"/> is saved</param>
		/// <param name="showExtensionDialog">Determines wheteher the Extension Modified Dialog is shown</param>
		/// <returns><see cref="ReturnResult"/> of performed operation</returns>
		Task<ReturnResult> RenameAsync(IStorageItemWithPath source, string newName, NameCollisionOption collision, bool registerHistory, bool showExtensionDialog = true);
	}
}