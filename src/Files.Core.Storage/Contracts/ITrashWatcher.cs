// Copyright (c) 2023 Files Community
// Licensed under the MIT License.

namespace Files.Core.Storage.Contracts
{
	public interface ITrashWatcher : IWatcher
	{
		/// <summary>
		/// Gets invoked when an item addition is detected by the watcher
		/// </summary>
		event EventHandler<SystemIO.FileSystemEventArgs>? ItemAdded;

		/// <summary>
		/// Gets invoked when an item removal is detected by the watcher
		/// </summary>
		event EventHandler<SystemIO.FileSystemEventArgs>? ItemDeleted;

		/// <summary>
		/// Gets invoked when an item changing is detected by the watcher
		/// </summary>
		event EventHandler<SystemIO.FileSystemEventArgs>? ItemChanged;

		/// <summary>
		/// Gets invoked when an item renaming is detected by the watcher
		/// </summary>
		event EventHandler<SystemIO.FileSystemEventArgs>? ItemRenamed;

		/// <summary>
		/// Gets invoked when an refresh request is detected by the watcher
		/// </summary>
		event EventHandler<SystemIO.FileSystemEventArgs>? RefreshRequested;
	}
}
