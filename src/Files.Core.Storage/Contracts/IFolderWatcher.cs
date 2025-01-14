// Copyright (c) 2023 Files Community
// Licensed under the MIT License.

using Files.Core.Storage.Storables;
using System.Collections.Specialized;

namespace Files.Core.Storage.Contracts
{
	public interface IFolderWatcher : INotifyCollectionChanged
	{
		/// <summary>
		/// Gets the folder being watched for changes.
		/// </summary>
		IMutableFolder TargetFolder { get; }

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
	}
}
