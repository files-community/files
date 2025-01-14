// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Management.Infrastructure;

namespace Files.App.Utils.Storage
{
	public sealed class DeviceManager
	{
		private static readonly Lazy<DeviceManager> lazy = new(() => new DeviceManager());

		private ManagementEventWatcher? insertWatcher, removeWatcher, modifyWatcher;

		public event EventHandler<DeviceEventArgs>? DeviceAdded;
		public event EventHandler<DeviceEventArgs>? DeviceRemoved;
		public event EventHandler<DeviceEventArgs>? DeviceInserted;
		public event EventHandler<DeviceEventArgs>? DeviceEjected;

		public static DeviceManager Default
		{
			get
			{
				return lazy.Value;
			}
		}

		private DeviceManager()
		{
			Initialize();
		}

		private void Initialize()
		{
			WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'");
			insertWatcher = new ManagementEventWatcher(insertQuery);
			insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
			insertWatcher.Start();

			WqlEventQuery modifyQuery = new WqlEventQuery("SELECT * FROM __InstanceModificationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk' and TargetInstance.DriveType = 5");
			modifyWatcher = new ManagementEventWatcher(modifyQuery);
			modifyWatcher.EventArrived += new EventArrivedEventHandler(DeviceModifiedEvent);
			modifyWatcher.Start();

			WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'");
			removeWatcher = new ManagementEventWatcher(removeQuery);
			removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
			removeWatcher.Start();
		}

		private void DeviceModifiedEvent(object sender, EventArrivedEventArgs e)
		{
			CimInstance obj = (CimInstance)e.NewEvent.Instance.CimInstanceProperties["TargetInstance"].Value;
			var deviceName = (string)obj.CimInstanceProperties["Name"]?.Value;
			var deviceId = (string)obj.CimInstanceProperties["DeviceID"]?.Value;
			var volumeName = (string)obj.CimInstanceProperties["VolumeName"]?.Value;
			var eventType = volumeName is not null ? DeviceEvent.Inserted : DeviceEvent.Ejected;
			System.Diagnostics.Debug.WriteLine($"Drive modify event: {deviceName}, {deviceId}, {eventType}");
			if (eventType == DeviceEvent.Inserted)
			{
				DeviceInserted?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
			}
			else
			{
				DeviceEjected?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
			}
		}

		private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
		{
			CimInstance obj = (CimInstance)e.NewEvent.Instance.CimInstanceProperties["TargetInstance"].Value;
			var deviceName = (string)obj.CimInstanceProperties["Name"].Value;
			var deviceId = (string)obj.CimInstanceProperties["DeviceID"].Value;
			System.Diagnostics.Debug.WriteLine($"Drive removed event: {deviceName}, {deviceId}");
			DeviceRemoved?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
		}

		private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
		{
			CimInstance obj = (CimInstance)e.NewEvent.Instance.CimInstanceProperties["TargetInstance"].Value;
			var deviceName = (string)obj.CimInstanceProperties["Name"].Value;
			var deviceId = (string)obj.CimInstanceProperties["DeviceID"].Value;
			System.Diagnostics.Debug.WriteLine($"Drive added event: {deviceName}, {deviceId}");
			DeviceAdded?.Invoke(sender, new DeviceEventArgs(deviceName, deviceId));
		}

		private void Unregister()
		{
			insertWatcher?.Dispose();
			removeWatcher?.Dispose();
			modifyWatcher?.Dispose();
			insertWatcher = null;
			removeWatcher = null;
			modifyWatcher = null;
		}

		~DeviceManager()
		{
			Unregister();
		}
	}
}