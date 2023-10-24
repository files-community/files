using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Files.App.Terminal.Native.ConsoleApi;

namespace Files.App.Terminal
{
	/// <summary>
	/// The UI of the terminal. It's just a normal console window, but we're managing the input/output.
	/// In a "real" project this could be some other UI.
	/// </summary>
	internal sealed class Terminal
	{
		private SafeFileHandle _consoleInputPipeWriteHandle;
		private FileStream _consoleInputWriter;
		private PseudoConsole _pseudoConsole;

		/// <summary>
		/// A stream of VT-100-enabled output from the console.
		/// </summary>
		public FileStream ConsoleOutStream { get; private set; }

		/// <summary>
		/// Fired once the console has been hooked up and is ready to receive input.
		/// </summary>
		public event EventHandler OutputReady;

		public Terminal()
		{
			// By default, UI applications don't have a console associated with them.
			// So first, we check to see if this process has a console.
			if (GetConsoleWindow() == IntPtr.Zero)
			{
				// If it doesn't ask Windows to allocate one to it for us.
				bool createConsoleSuccess = AllocConsole();
				if (!createConsoleSuccess)
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not allocate console for this process.");
				}
			}

			var windowHandle = GetConsoleWindow();
			User32.ShowWindow(windowHandle, ShowWindowCommand.SW_HIDE);

			EnableVirtualTerminalSequenceProcessing();
		}

		/// <summary>
		/// Newer versions of the windows console support interpreting virtual terminal sequences, we just have to opt-in
		/// </summary>
		private static void EnableVirtualTerminalSequenceProcessing()
		{
			//var hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
			SafeFileHandle hStdOut = GetConsoleScreenBuffer();
			if (!GetConsoleMode(hStdOut, out uint outConsoleMode))
			{
				throw new InvalidOperationException("Could not get console mode");
			}

			outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
			if (!SetConsoleMode(hStdOut, outConsoleMode))
			{
				throw new InvalidOperationException("Could not enable virtual terminal processing");
			}
		}

		/// <summary>
		/// Start the pseudoconsole and run the process as shown in 
		/// https://docs.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session#creating-the-pseudoconsole
		/// </summary>
		/// <param name="command">the command to run, e.g. cmd.exe</param>
		public void Start(string command, string directory, int consoleWidth = 80, int consoleHeight = 30)
		{
			using var inputPipe = new PseudoConsolePipe();
			using var outputPipe = new PseudoConsolePipe();
			_pseudoConsole = PseudoConsole.Create(inputPipe.ReadSide, outputPipe.WriteSide, consoleWidth, consoleHeight);
			using var process = ProcessFactory.Start(command, directory, PseudoConsole.PseudoConsoleThreadAttribute, _pseudoConsole.Handle);

			// copy all pseudoconsole output to a FileStream and expose it to the rest of the app
			ConsoleOutStream = new FileStream(outputPipe.ReadSide, FileAccess.Read);
			OutputReady.Invoke(this, EventArgs.Empty);

			// Store input pipe handle, and a writer for later reuse
			_consoleInputPipeWriteHandle = inputPipe.WriteSide;
			_consoleInputWriter = new FileStream(_consoleInputPipeWriteHandle, FileAccess.Write);

			// free resources in case the console is ungracefully closed (e.g. by the 'x' in the window titlebar)
			OnClose(() => DisposeResources(process, _pseudoConsole, outputPipe, inputPipe, _consoleInputWriter));

			WaitForExit(process).WaitOne(Timeout.Infinite);
		}

		public void Resize(int width, int height)
		{
			_pseudoConsole?.Resize(width, height);
		}

		/// <summary>
		/// Sends the given string to the anonymous pipe that writes to the active pseudoconsole.
		/// </summary>
		public void WriteToPseudoConsole(byte[] data)
		{
			_consoleInputWriter?.Write(data, 0, data.Length);
			_consoleInputWriter?.Flush();
		}

		/// <summary>
		/// Get an AutoResetEvent that signals when the process exits
		/// </summary>
		private static AutoResetEvent WaitForExit(Process process) =>
			new AutoResetEvent(false)
			{
				SafeWaitHandle = new SafeWaitHandle(process.ProcessInfo.hProcess, ownsHandle: false)
			};

		/// <summary>
		/// Set a callback for when the terminal is closed (e.g. via the "X" window decoration button).
		/// Intended for resource cleanup logic.
		/// </summary>
		private static void OnClose(Action handler)
		{
			SetConsoleCtrlHandler(eventType =>
			{
				if(eventType == CtrlTypes.CTRL_CLOSE_EVENT)
				{
					handler();
				}
				return false;
			}, true);
		}

		/// <summary>
		/// A helper method that opens a handle on the console's screen buffer, which will allow us to get its output,
		/// even if STDOUT has been redirected (which Visual Studio does by default).
		/// </summary>
		/// <returns>A file handle to the console's screen buffer.</returns>
		/// <remarks>This is described in more detail here: https://docs.microsoft.com/en-us/windows/console/console-handles </remarks>
		private static SafeFileHandle GetConsoleScreenBuffer()
		{
			var file = Kernel32.CreateFile(
				"CONOUT$",
				Kernel32.FileAccess.GENERIC_WRITE | Kernel32.FileAccess.GENERIC_READ,
				FileShare.Write,
				null,
				FileMode.Open,
				FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL,
				IntPtr.Zero);

			if (file == new IntPtr(-1))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not get console screen buffer.");
			}

			return new SafeFileHandle(file.ReleaseOwnership(), true);
		}

		private void DisposeResources(params IDisposable[] disposables)
		{
			foreach (var disposable in disposables)
			{
				disposable.Dispose();
			}
		}
	}
}
