using Microsoft.UI.Xaml.Controls;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;

namespace Files.App.Utils.Terminal
{
	/// <summary>
	/// Code modified from https://github.com/felixse/FluentTerminal
	/// </summary>
	[AllowForWeb]
	public sealed class TerminalBridge
	{
		private IxtermEventListener _terminalEventListener;

		public TerminalBridge(IxtermEventListener terminalEventListener)
		{
			_terminalEventListener = terminalEventListener;
			_terminalEventListener.OnOutput += OnOutput;
			_terminalEventListener.OnPaste += OnPaste;
			_terminalEventListener.OnSessionRestart += OnSessionRestart;
		}

		private async void OnPaste(object sender, string e)
		{
			await _terminalEventListener.WebView.InvokeScriptAsync("onPaste", new[] { e })
				.ConfigureAwait(false);
		}

		private async void OnOutput(object sender, object e)
		{
			var arg = Encoding.UTF8.GetString((byte[])e);
			await _terminalEventListener.WebView.InvokeScriptAsync("onOutput", new[] { arg })
				.ConfigureAwait(false);
		}

		private async void OnSessionRestart(object sender, string e)
		{
			await _terminalEventListener.WebView.InvokeScriptAsync("onSessionRestart", new[] { e })
				.ConfigureAwait(false);
		}

		public void InputReceived(string message)
		{
			_terminalEventListener?.OnInput(Encoding.UTF8.GetBytes(message));
		}

		public void BinaryReceived(string binary)
		{
			_terminalEventListener?.OnInput(Encoding.UTF8.GetBytes(binary));
		}

		public void Initialized()
		{
			_terminalEventListener.OnInitialized();
		}

		public void DisposalPrepare()
		{
			_terminalEventListener.OnOutput -= OnOutput;
			_terminalEventListener.OnPaste -= OnPaste;
			_terminalEventListener = null;
		}

		public void NotifySizeChanged(long columns, long rows)
		{
			_terminalEventListener?.OnTerminalResized((int)columns, (int)rows);
		}

		public void NotifyTitleChanged(string title)
		{
			_terminalEventListener?.OnTitleChanged(title);
		}

		public void InvokeCommand(string command)
		{
			_terminalEventListener?.OnKeyboardCommand(command);
		}

		public void NotifyRightClick(long x, long y, bool hasSelection, string hoveredUri)
		{
			_terminalEventListener?.OnMouseClick(MouseButton.Right, (int)x, (int)y, hasSelection, hoveredUri);
		}

		public void NotifyMiddleClick(long x, long y, bool hasSelection, string hoveredUri)
		{
			_terminalEventListener?.OnMouseClick(MouseButton.Middle, (int)x, (int)y, hasSelection, hoveredUri);
		}

		public void NotifySelectionChanged(string selection)
		{
			_terminalEventListener?.OnSelectionChanged(selection);
		}

		public void ReportError(string error)
		{
			_terminalEventListener?.OnError(error);
		}
	}

	public interface IxtermEventListener
	{
		void OnTerminalResized(int columns, int rows);

		void OnTitleChanged(string title);

		void OnKeyboardCommand(string command);

		void OnMouseClick(MouseButton mouseButton, int x, int y, bool hasSelection, string hoveredUri);

		void OnSelectionChanged(string selection);

		void OnError(string error);

		void OnInput([ReadOnlyArray] byte[] data);

		void OnInitialized();

		event EventHandler<object> OnOutput;
		event EventHandler<string> OnPaste;
		event EventHandler<string> OnSessionRestart;

		public WebView2 WebView { get; }
	}
}