// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Core.Storage.Storables;
using System;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Represents a storage device watcher
	/// </summary>
	public interface IStorageDeviceWatcher
	{
		/// <summary>
		/// Fires when a new device is detected by the storage device watcher
		/// </summary>
		event EventHandler<ILocatableFolder> DeviceAdded;

		/// <summary>
		/// Fires when a device removal is detected by the storage device watcher
		/// </summary>
		event EventHandler<string> DeviceRemoved;

		/// <summary>
		/// Fires when the storage device watcher completes an enumeration
		/// </summary>
		event EventHandler EnumerationCompleted;

		/// <summary>
		/// Fires when a device modification is detected by the storage device watcher
		/// </summary>
		event EventHandler<string> DeviceModified;

		/// <summary>
		/// Represents whether the storage device watcher should be started
		/// </summary>
		bool CanBeStarted { get; }

		/// <summary>
		/// Starts the storage device watcher
		/// </summary>
		void Start();

		/// <summary>
		/// Stops the storage device watcher
		/// </summary>
		void Stop();
	}
}
