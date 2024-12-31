using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.System.Threading;

namespace Files.App.Utils.Terminal.ConPTY
{
	/// <summary>
	/// Represents an instance of a process.
	/// </summary>
	internal sealed class Process : IDisposable
	{
		public Process(STARTUPINFOEXW startupInfo, PROCESS_INFORMATION processInfo)
		{
			StartupInfo = startupInfo;
			ProcessInfo = processInfo;
		}

		public STARTUPINFOEXW StartupInfo { get; }
		public PROCESS_INFORMATION ProcessInfo { get; }

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects).
				}

				// dispose unmanaged state

				// Free the attribute list
				unsafe
				{
					if ((void*)StartupInfo.lpAttributeList != null)
					{
						PInvoke.DeleteProcThreadAttributeList(StartupInfo.lpAttributeList);
						Marshal.FreeHGlobal((nint)(void*)StartupInfo.lpAttributeList);
					}
				}

				// Close process and thread handles
				if (ProcessInfo.hProcess != nint.Zero)
				{
					PInvoke.CloseHandle(ProcessInfo.hProcess);
				}
				if (ProcessInfo.hThread != nint.Zero)
				{
					PInvoke.CloseHandle(ProcessInfo.hThread);
				}

				disposedValue = true;
			}
		}

		~Process()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// use the following line if the finalizer is overridden above.
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}
