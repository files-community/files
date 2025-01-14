// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides manager of recent files and folders of File Explorer on Windows.
	/// </summary>
	public interface IWindowsRecentItemsService
	{
		/// <summary>
		/// Gets recent files of File Explorer.
		/// </summary>
		IReadOnlyList<RecentItem> RecentFiles { get; }

		/// <summary>
		/// Gets recent folders of File Explorer.
		/// </summary>
		IReadOnlyList<RecentItem> RecentFolders { get; }

		/// <summary>
		/// Gets invoked when recent files of File Explorer have changed.
		/// </summary>
		event EventHandler<NotifyCollectionChangedEventArgs>? RecentFilesChanged;

		/// <summary>
		/// Gets invoked when recent folders of File Explorer have changed.
		/// </summary>
		event EventHandler<NotifyCollectionChangedEventArgs>? RecentFoldersChanged;

		/// <summary>
		/// Updates recent files of File Explorer.
		/// </summary>
		Task<bool> UpdateRecentFilesAsync();

		/// <summary>
		/// Updates recent folders of File Explorer.
		/// </summary>
		Task<bool> UpdateRecentFoldersAsync();

		/// <summary>
		/// Adds a recent file for File Explorer.
		/// </summary>
		bool Add(string path);

		/// <summary>
		/// Removes a recent folder for File Explorer.
		/// </summary>
		bool Remove(RecentItem item);

		/// <summary>
		/// Clears recent files and folders of File Explorer.
		/// </summary>
		bool Clear();
	}
}
