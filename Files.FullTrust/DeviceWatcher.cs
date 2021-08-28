using Files.Common;
using System.IO.Pipes;
using System.Management;
using Windows.Foundation.Collections;

namespace FilesFullTrust
{
    public class DeviceWatcher : IDisposable
    {
        private ManagementEventWatcher insertWatcher, removeWatcher, modifyWatcher;
        private NamedPipeServerStream connection;

        private const string WpdGuid = "{6ac27878-a6fa-4155-ba85-f98f491d4f33}";

        public DeviceWatcher(NamedPipeServerStream connection)
        {
            this.connection = connection;
        }

        public void Start()
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

        private async void DeviceModifiedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject obj = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            var deviceName = (string)obj.Properties["Name"].Value;
            var deviceId = (string)obj.Properties["DeviceID"].Value;
            var volumeName = (string)obj.Properties["VolumeName"].Value;
            var eventType = volumeName != null ? DeviceEvent.Inserted : DeviceEvent.Ejected;
            System.Diagnostics.Debug.WriteLine($"Drive modify event: {deviceName}, {deviceId}, {eventType}");
            await SendEvent(deviceName, deviceId, eventType);
        }

        private async void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject obj = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            var deviceName = (string)obj.Properties["Name"].Value;
            var deviceId = (string)obj.Properties["DeviceID"].Value;
            System.Diagnostics.Debug.WriteLine($"Drive removed event: {deviceName}, {deviceId}");
            await SendEvent(deviceName, deviceId, DeviceEvent.Removed);
        }

        private async void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject obj = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            var deviceName = (string)obj.Properties["Name"].Value;
            var deviceId = (string)obj.Properties["DeviceID"].Value;
            System.Diagnostics.Debug.WriteLine($"Drive added event: {deviceName}, {deviceId}");
            await SendEvent(deviceName, deviceId, DeviceEvent.Added);
        }

        private async Task SendEvent(string deviceName, string deviceId, DeviceEvent eventType)
        {
            if (connection?.IsConnected ?? false)
            {
                await Win32API.SendMessageAsync(connection, new ValueSet()
                {
                    { "DeviceID", deviceId },
                    { "EventType", (int)eventType }
                });
            }
        }

        public void Dispose()
        {
            insertWatcher?.Dispose();
            removeWatcher?.Dispose();
            modifyWatcher?.Dispose();
            insertWatcher = null;
            removeWatcher = null;
            modifyWatcher = null;
        }
    }
}