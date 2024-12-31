using Microsoft.Win32.SafeHandles;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32.UI.WindowsAndMessaging;
using Windows.Win32.System.Console;
using Windows.Win32.Storage.FileSystem;
using static Windows.Win32.PInvoke;

namespace Files.App.Utils.Terminal.ConPTY
{
	/// <summary>
	/// The UI of the terminal. It's just a normal console window, but we're managing the input/output.
	/// In a "real" project this could be some other UI.
	/// </summary>
	internal sealed class Terminal : IDisposable
	{
		private SafeFileHandle _consoleInputPipeWriteHandle;
		private FileStream _consoleInputWriter;
		private PseudoConsolePipe _inputPipe;
		private PseudoConsolePipe _outputPipe;
		private PseudoConsole _pseudoConsole;
		private Process _process;

		/// <summary>
		/// A stream of VT-100-enabled output from the console.
		/// </summary>
		public FileStream ConsoleOutStream { get; private set; }

		/// <summary>
		/// Fired once the console has been hooked up and is ready to receive input.
		/// </summary>
		public event EventHandler OutputReady;
		public event EventHandler Exited;

		public Terminal()
		{
			// By default, UI applications don't have a console associated with them.
			// So first, we check to see if this process has a console.
			if (GetConsoleWindow() == nint.Zero)
			{
				// If it doesn't ask Windows to allocate one to it for us.
				bool createConsoleSuccess = AllocConsole();
				if (!createConsoleSuccess)
				{
					throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not allocate console for this process.");
				}
			}

			var windowHandle = GetConsoleWindow();
			ShowWindow(windowHandle, SHOW_WINDOW_CMD.SW_HIDE);

			EnableVirtualTerminalSequenceProcessing();
		}

		~Terminal()
		{
			Dispose(false);
		}

		/// <summary>
		/// Newer versions of the windows console support interpreting virtual terminal sequences, we just have to opt-in
		/// </summary>
		private static void EnableVirtualTerminalSequenceProcessing()
		{
			//var hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
			SafeFileHandle hStdOut = GetConsoleScreenBuffer();
			if (!GetConsoleMode(hStdOut, out var outConsoleMode))
			{
				throw new InvalidOperationException("Could not get console mode");
			}

			outConsoleMode |= CONSOLE_MODE.ENABLE_VIRTUAL_TERMINAL_PROCESSING | CONSOLE_MODE.DISABLE_NEWLINE_AUTO_RETURN;
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
			_inputPipe = new PseudoConsolePipe();
			_outputPipe = new PseudoConsolePipe();
			_pseudoConsole = PseudoConsole.Create(_inputPipe.ReadSide, _outputPipe.WriteSide, consoleWidth, consoleHeight);
			_process = ProcessFactory.Start(command, directory, PseudoConsole.PseudoConsoleThreadAttribute, _pseudoConsole.Handle.DangerousGetHandle());

			// copy all pseudoconsole output to a FileStream and expose it to the rest of the app
			ConsoleOutStream = new FileStream(_outputPipe.ReadSide, FileAccess.Read);
			OutputReady.Invoke(this, EventArgs.Empty);

			// Store input pipe handle, and a writer for later reuse
			_consoleInputPipeWriteHandle = _inputPipe.WriteSide;
			_consoleInputWriter = new FileStream(_consoleInputPipeWriteHandle, FileAccess.Write);

			WaitForExit(_process).WaitOne(Timeout.Infinite);

			Exited?.Invoke(this, EventArgs.Empty);
		}

		public void Resize(int width, int height)
		{
			if (alreadyDisposed)
				return;
			_pseudoConsole?.Resize(width, height);
		}

		/// <summary>
		/// Sends the given string to the anonymous pipe that writes to the active pseudoconsole.
		/// </summary>
		public void WriteToPseudoConsole(byte[] data)
		{
			if (alreadyDisposed)
				return;
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
		/// A helper method that opens a handle on the console's screen buffer, which will allow us to get its output,
		/// even if STDOUT has been redirected (which Visual Studio does by default).
		/// </summary>
		/// <returns>A file handle to the console's screen buffer.</returns>
		/// <remarks>This is described in more detail here: https://docs.microsoft.com/en-us/windows/console/console-handles </remarks>
		private static SafeFileHandle GetConsoleScreenBuffer()
		{
			var file = CreateFile(
				"CONOUT$",
				(uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE | FILE_ACCESS_RIGHTS.FILE_GENERIC_READ),
				FILE_SHARE_MODE.FILE_SHARE_WRITE,
				null,
				FILE_CREATION_DISPOSITION.OPEN_EXISTING,
				FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
				new SafeFileHandle());

			if (file.IsInvalid)
			{
				throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not get console screen buffer.");
			}

			return file;
		}

		#region IDisposable Support

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(true);
		}

		private bool alreadyDisposed = false;

		public void Dispose(bool disposeManaged)
		{
			if (alreadyDisposed)
			{
				return;
			}

			alreadyDisposed = true;

			if (disposeManaged)
			{
				ConsoleOutStream.Dispose();
				// Dispose pseudo console before _consoleInputWriter to avoid
				// hanging on call of ClosePseudoConsole
				_pseudoConsole?.Dispose();
				_consoleInputWriter?.Dispose();
				_process?.Dispose();
				_outputPipe?.Dispose();
				_inputPipe?.Dispose();
			}
		}

		#endregion
	}
}
