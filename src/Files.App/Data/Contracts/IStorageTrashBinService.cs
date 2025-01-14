// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides service for Recycle Bin on Windows.
	/// </summary>
	public interface IStorageTrashBinService
	{
		/// <summary>
		/// Gets the watcher of Recycle Bin folder.
		/// </summary>
		RecycleBinWatcher Watcher { get; }

		/// <summary>
		/// Gets all Recycle Bin shell folders.
		/// </summary>
		/// <returns>A collection of Recycle Bin shell folders.</returns>
		Task<List<ShellFileItem>> GetAllRecycleBinFoldersAsync();

		/// <summary>
		/// Gets the info of Recycle Bin.
		/// </summary>
		/// <param name="drive">The drive letter, Recycle Bin of which you want.</param>
		/// <returns></returns>
		(bool HasRecycleBin, long NumItems, long BinSize) QueryRecycleBin(string drive = "");

		/// <summary>
		/// Gets the used size of Recycle Bin.
		/// </summary>
		/// <returns></returns>
		ulong GetSize();

		/// <summary>
		/// Gets the value that indicates whether Recycle Bin folder has item(s).
		/// </summary>
		/// <returns></returns>
		bool HasItems();

		/// <summary>
		/// Gets the file or folder specified is already moved to Recycle Bin.
		/// </summary>
		/// <param name="path">The path that indicates to a file or folder.</param>
		/// <returns>True if the file or path is recycled; otherwise, false.</returns>
		bool IsUnderTrashBin(string? path);

		/// <summary>
		/// Gets the file or folder specified can be moved to Recycle Bin.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		Task<bool> CanGoTrashBin(string? path);

		/// <summary>
		/// Deletes files and folders in Recycle Bin permanently.
		/// </summary>
		/// <returns>True if succeeded; otherwise, false</returns>
		bool EmptyTrashBin();

		/// <summary>
		/// Restores files and folders in Recycle Bin to original paths.
		/// </summary>
		/// <returns>True if succeeded; otherwise, false</returns>
		Task<bool> RestoreAllTrashesAsync();
	}
}
