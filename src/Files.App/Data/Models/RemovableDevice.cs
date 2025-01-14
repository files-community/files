// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using static Files.App.Helpers.Win32PInvoke;

namespace Files.App.Data.Models
{
	public sealed class RemovableDevice
	{
		private nint handle;
		private char driveLetter;

		public RemovableDevice(string letter)
		{
			driveLetter = letter[0];

			string filename = @"\\.\" + driveLetter + ":";

			handle = CreateFileFromAppW(filename,
				(uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE),
				FILE_SHARE_READ | FILE_SHARE_WRITE,
				nint.Zero, OPEN_EXISTING, 0, nint.Zero);
		}

		public async Task<bool> EjectAsync()
		{
			bool result = false;

			if (handle.ToInt32() == INVALID_HANDLE_VALUE)
			{
				Debug.WriteLine($"Unable to open drive {driveLetter}");
				return false;
			}

			if (await LockVolumeAsync() && DismountVolume())
			{
				PreventRemovalOfVolume(false);
				result = AutoEjectVolume();
			}

			CloseVolume();

			return result;
		}

		private async Task<bool> LockVolumeAsync()
		{
			bool result = false;

			for (int i = 0; i < 5; i++)
			{
				if (DeviceIoControl(handle, PInvoke.FSCTL_LOCK_VOLUME, nint.Zero, 0, nint.Zero, 0, out _, nint.Zero))
				{
					Debug.WriteLine("Lock successful!");
					result = true;

					break;
				}
				else
				{
					Debug.WriteLine($"Can't lock device, attempt {i + 1}, trying again... ");
				}

				await Task.Delay(500);
			}

			return result;
		}

		private bool DismountVolume()
		{
			return DeviceIoControl(handle, PInvoke.FSCTL_DISMOUNT_VOLUME, nint.Zero, 0, nint.Zero, 0, out _, nint.Zero);
		}

		private bool PreventRemovalOfVolume(bool prevent)
		{
			byte[] buf = [prevent ? (byte)1 : (byte)0];
			return DeviceIoControl(handle, IOCTL_STORAGE_MEDIA_REMOVAL, buf, 1, nint.Zero, 0, out _, nint.Zero);
		}

		private bool AutoEjectVolume()
		{
			return DeviceIoControl(handle, IOCTL_STORAGE_EJECT_MEDIA, nint.Zero, 0, nint.Zero, 0, out _, nint.Zero);
		}

		private bool CloseVolume()
		{
			return CloseHandle(handle);
		}
	}
}
