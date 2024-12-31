using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Windows.Win32.System.Console;
using static Windows.Win32.PInvoke;

namespace Files.App.Utils.Terminal.ConPTY
{
	/// <summary>
	/// Utility functions around the new Pseudo Console APIs
	/// </summary>
	internal sealed class PseudoConsole : IDisposable
	{
		public static readonly nint PseudoConsoleThreadAttribute = (nint)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE;

		public SafeHandle Handle { get; }

		private PseudoConsole(SafeHandle handle)
		{
			Handle = handle;
		}

		internal static PseudoConsole Create(SafeFileHandle inputReadSide, SafeFileHandle outputWriteSide, int width, int height)
		{
			var createResult = CreatePseudoConsole(
				new COORD { X = (short)width, Y = (short)height },
				inputReadSide, outputWriteSide,
				0, out var hPC);
			if (createResult != 0)
			{
				throw new InvalidOperationException("Could not create pseudo console. Error Code " + createResult);
			}
			return new PseudoConsole(hPC);
		}

		internal void Resize(int width, int height)
		{
			ResizePseudoConsole(Handle, new COORD { X = (short)width, Y = (short)height });
		}

		public void Dispose()
		{
			ClosePseudoConsole((HPCON)Handle.DangerousGetHandle());
		}
	}
}
