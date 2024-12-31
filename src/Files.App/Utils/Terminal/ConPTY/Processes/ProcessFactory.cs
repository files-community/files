using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.System.Threading;

namespace Files.App.Utils.Terminal.ConPTY
{
	/// <summary>
	/// Support for starting and configuring processes.
	/// </summary>
	/// <remarks>
	/// Possible to replace with managed code? The key is being able to provide the PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE attribute
	/// </remarks>
	static class ProcessFactory
	{
		/// <summary>
		/// Start and configure a process. The return value represents the process and should be disposed.
		/// </summary>
		internal static Process Start(string command, string directory, nint attributes, nint hPC)
		{
			var startupInfo = ConfigureProcessThread(hPC, attributes);
			var processInfo = RunProcess(ref startupInfo, command, directory);
			return new Process(startupInfo, processInfo);
		}

		private static STARTUPINFOEXW ConfigureProcessThread(nint hPC, nint attributes)
		{
			// this method implements the behavior described in https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#preparing-for-creation-of-the-child-process
			unsafe
			{
				var lpSize = nuint.Zero;
				var success = PInvoke.InitializeProcThreadAttributeList(
					lpAttributeList: new(null),
					dwAttributeCount: 1,
					lpSize: ref lpSize
				);
				if (success || lpSize == nuint.Zero) // we're not expecting `success` here, we just want to get the calculated lpSize
				{
					throw new InvalidOperationException("Could not calculate the number of bytes for the attribute list. " + Marshal.GetLastWin32Error());
				}

				var startupInfo = new STARTUPINFOEXW();
				startupInfo.StartupInfo.cb = (uint)Marshal.SizeOf<STARTUPINFOEXW>();
				startupInfo.lpAttributeList = new((void*)Marshal.AllocHGlobal((int)lpSize));

				success = PInvoke.InitializeProcThreadAttributeList(
					lpAttributeList: startupInfo.lpAttributeList,
					dwAttributeCount: 1,
					lpSize: ref lpSize
				);
				if (!success)
				{
					throw new InvalidOperationException("Could not set up attribute list. " + Marshal.GetLastWin32Error());
				}

				success = PInvoke.UpdateProcThreadAttribute(
					lpAttributeList: startupInfo.lpAttributeList,
					dwFlags: 0,
					Attribute: (nuint)attributes,
					lpValue: (void*)hPC,
					cbSize: (nuint)nint.Size,
					lpPreviousValue: null,
					lpReturnSize: (nuint*)null
				);
				if (!success)
				{
					throw new InvalidOperationException("Could not set pseudoconsole thread attribute. " + Marshal.GetLastWin32Error());
				}

				return startupInfo;
			}
		}

		private static PROCESS_INFORMATION RunProcess(ref STARTUPINFOEXW sInfoEx, string commandLine, string directory)
		{
			unsafe
			{
				var success = false;

				int securityAttributeSize = Marshal.SizeOf<SECURITY_ATTRIBUTES>();
				var pSec = new SECURITY_ATTRIBUTES { nLength = (uint)securityAttributeSize };
				var tSec = new SECURITY_ATTRIBUTES { nLength = (uint)securityAttributeSize };

				PROCESS_INFORMATION lpProcessInformation;

				fixed (STARTUPINFOEXW* lpStartupInfo = &sInfoEx)
				{
					fixed (char* lpCurrentDirectory = directory)
					{
						fixed (char* lpCommandLine = commandLine)
						{
							success = PInvoke.CreateProcess(
								lpApplicationName: new PCWSTR(null),
								lpCommandLine: lpCommandLine,
								lpProcessAttributes: &pSec,
								lpThreadAttributes: &tSec,
								bInheritHandles: false,
								dwCreationFlags: PROCESS_CREATION_FLAGS.EXTENDED_STARTUPINFO_PRESENT,
								lpEnvironment: null,
								lpCurrentDirectory: lpCurrentDirectory,
								lpStartupInfo: (STARTUPINFOW*)lpStartupInfo,
								lpProcessInformation: &lpProcessInformation
							);
						}
					}
				}

				if (!success)
				{
					throw new InvalidOperationException("Could not create process. " + Marshal.GetLastWin32Error());
				}

				return lpProcessInformation;
			}
		}
	}
}
