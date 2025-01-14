// Copyright (c) 2023 Files Community
// Licensed under the MIT License.

namespace Files.Core.Storage.Contracts
{
	internal interface IDeviceWatcher
	{
		/// <summary>
		/// Gets the value that indicates whether the watcher can be started.
		/// </summary>
		bool CanBeStarted { get; }

		/// <summary>
		/// Gets invoked when an item addition is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? ItemAdded;

		/// <summary>
		/// Gets invoked when an item removal is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? ItemDeleted;

		/// <summary>
		/// Gets invoked when an item changing is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? ItemChanged;

		/// <summary>
		/// Gets invoked when an item inserting is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? ItemInserted;

		/// <summary>
		/// Gets invoked when an item ejection is detected by the watcher
		/// </summary>
		event EventHandler<DeviceEventArgs>? ItemEjected;

		/// <summary>
		/// Gets invoked when item enumeration completed.
		/// </summary>
		event EventHandler? EnumerationCompleted;
	}
}
