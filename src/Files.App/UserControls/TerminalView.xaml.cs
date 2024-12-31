using CommunityToolkit.WinUI;
using Files.App.Utils.Terminal;
using Files.App.Utils.Terminal.ConPTY;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.UserControls
{
	/// <summary>
	/// Code modified from https://github.com/felixse/FluentTerminal
	/// </summary>
	public sealed partial class TerminalView : UserControl, IxtermEventListener, IDisposable
	{
		// Members related to initialization
		private readonly TaskCompletionSource<object> _tcsConnected = new TaskCompletionSource<object>();
		private readonly TaskCompletionSource<object> _tcsNavigationCompleted = new TaskCompletionSource<object>();

		IContentPageContext _context = Ioc.Default.GetRequiredService<IContentPageContext>();
		MainPageViewModel _mainPageModel = Ioc.Default.GetRequiredService<MainPageViewModel>();

		#region Resize handling

		// Members related to resize handling
		private static readonly TimeSpan ResizeDelay = TimeSpan.FromMilliseconds(60);
		private readonly object _resizeLock = new object();
		private TerminalSize _requestedSize;
		private TerminalSize _setSize;
		private DateTime _resizeScheduleTime;
		private Task _resizeTask;
		private MemoryStream _outputBlockedBuffer;

		// Must be called from a code locked with _resizeLock
		private void ScheduleResize(TerminalSize size, bool scheduleIfEqual)
		{
			if (!scheduleIfEqual && size.EquivalentTo(_requestedSize))
			{
				return;
			}

			_requestedSize = size;
			_resizeScheduleTime = DateTime.UtcNow.Add(ResizeDelay);

			if (_resizeTask == null)
			{
				_resizeTask = ResizeTask();
			}
		}

		private async Task ResizeTask()
		{
			while (true)
			{
				TimeSpan delay;
				TerminalSize size = null;

				lock (_resizeLock)
				{
					if (_requestedSize?.EquivalentTo(_setSize) ?? true)
					{
						// Resize finished. Unblock output and exit.

						if (_outputBlockedBuffer != null)
						{
							OnOutput?.Invoke(this, _outputBlockedBuffer.ToArray());

							_outputBlockedBuffer.Dispose();
							_outputBlockedBuffer = null;
						}

						_resizeTask = null;

						break;
					}

					delay = _resizeScheduleTime.Subtract(DateTime.UtcNow);

					// To avoid sleeping for only few milliseconds we're introducing a threshold of 10 milliseconds
					if (delay.TotalMilliseconds < 10)
					{
						_setSize = size = _requestedSize;

						if (_outputBlockedBuffer == null)
						{
							_outputBlockedBuffer = new MemoryStream();
						}
					}
				}

				if (size == null)
				{
					await Task.Delay(delay).ConfigureAwait(false);
				}
				else
				{
					_terminal.Resize(_requestedSize.Columns, _requestedSize.Rows);
				}
			}
		}

		#endregion Resize handling

		public event EventHandler<object> OnOutput;
		public event EventHandler<string> OnPaste;
		public event EventHandler<string> OnSessionRestart;

		public WebView2 WebView => WebViewControl;

		private Terminal _terminal;
		private BufferedReader _reader;
		private ShellProfile _profile;
		private string _id;

		public TerminalView(ShellProfile profile, string id) : this()
			=> (_profile, _id) = (profile, id);

		public TerminalView()
		{
			InitializeComponent();
		}

		private async void WebViewControl_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			if (WebViewControl.Source is not null)
				return;

			var envOptions = new CoreWebView2EnvironmentOptions()
			{
				// TODO: switch to "ScrollBarStyle" when available
				// https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2environmentoptions.scrollbarstyle?view=webview2-dotnet-1.0.2470-prerelease
				AdditionalBrowserArguments = "--enable-features=OverlayScrollbar,OverlayScrollbarWinStyle,OverlayScrollbarWinStyleAnimation"
			};
			var environment = await CoreWebView2Environment.CreateWithOptionsAsync("", "", envOptions);
			await WebViewControl.EnsureCoreWebView2Async(environment);
			//WebViewControl.CoreWebView2.OpenDevToolsWindow();
			WebViewControl.NavigationCompleted += WebViewControl_NavigationCompleted;
			WebViewControl.CoreWebView2.SetVirtualHostNameToFolderMapping(
				"terminal.files",
				Path.Combine(Package.Current.InstalledLocation.Path, "Utils", "Terminal", "UI"),
				CoreWebView2HostResourceAccessKind.DenyCors);
			WebViewControl.Source = new Uri("http://terminal.files/index.html");

			// Waiting for WebView.NavigationCompleted event to happen
			await _tcsNavigationCompleted.Task;

			var provider = new DefaultValueProvider();
			var options = provider.GetDefaultTerminalOptions();
			var keyBindings = provider.GetCommandKeyBindings();
			var theme = provider.GetPreInstalledThemes().First(x => x.Id == _profile.TerminalThemeId);

			WebViewControl.CoreWebView2.Profile.PreferredColorScheme = (ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark) ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
			RootGrid.Background = new SolidColorBrush(ColorHelpers.FromHex(theme.Colors.Background));

			var size = await CreateXtermViewAsync(options, theme.Colors,
				keyBindings.Values.SelectMany(k => k)).ConfigureAwait(false);

			// Waiting for IxtermEventListener.OnInitialized() call to happen
			await _tcsConnected.Task;

			lock (_resizeLock)
			{
				// Check to see if some resizing has happened meanwhile
				if (_requestedSize != null)
				{
					size = _requestedSize;
				}
				else
				{
					_requestedSize = size;
				}
			}

			StartShellProcess(size, _profile);

			lock (_resizeLock)
			{
				// Check to see if some resizing has happened meanwhile
				if (!size.EquivalentTo(_requestedSize))
				{
					ScheduleResize(_requestedSize, true);
				}
				else
				{
					_setSize = size;
				}
			}
		}

		private Task<TerminalSize> CreateXtermViewAsync(TerminalOptions options, TerminalColors theme, IEnumerable<KeyBinding> keyBindings)
		{
			var serializerSettings = new JsonSerializerOptions();
			serializerSettings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			var serializedOptions = JsonSerializer.Serialize(options, serializerSettings);
			var serializedTheme = JsonSerializer.Serialize(theme, serializerSettings);
			var serializedKeyBindings = JsonSerializer.Serialize(keyBindings, serializerSettings);
			return ExecuteScriptAsync(
					$"createTerminal('{serializedOptions}', '{serializedTheme}', '{serializedKeyBindings}')")
				.ContinueWith(t => JsonSerializer.Deserialize<TerminalSize>(t.Result)!);
		}

		public Task<string> SerializeXtermStateAsync()
		{
			return ExecuteScriptAsync(@"serializeTerminal()");
		}

		private async void WebViewControl_NavigationCompleted(WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
		{
			var _terminalBridge = new TerminalBridge(this);
			await WebViewControl.AddWebAllowedObject("terminalBridge", _terminalBridge);
			_tcsNavigationCompleted.TrySetResult(null);
		}

		void IxtermEventListener.OnKeyboardCommand(string command)
		{
			if (Enum.TryParse(command, true, out Command commandValue))
			{
				ProcessKeyboardCommand(commandValue.ToString());
			}
			else if (Guid.TryParse(command, out Guid shellProfileId))
			{
				ProcessKeyboardCommand(shellProfileId.ToString());
			}
		}

		void IxtermEventListener.OnMouseClick(MouseButton mouseButton, int x, int y, bool hasSelection, string hoveredUri)
		{
			var action = MouseAction.None;

			switch (mouseButton)
			{
				case MouseButton.Middle:
					action = MouseAction.None;
					break;
				case MouseButton.Right:
					action = MouseAction.CopySelectionOrPaste;
					break;
			}

			if (action == MouseAction.ContextMenu)
			{
			}
			else if (action == MouseAction.Paste)
			{
				((IxtermEventListener)this).OnKeyboardCommand(nameof(Command.Paste));
			}
			else if (action == MouseAction.CopySelectionOrPaste)
			{
				if (hasSelection)
				{
					((IxtermEventListener)this).OnKeyboardCommand(nameof(Command.Copy));
				}
				else
				{
					((IxtermEventListener)this).OnKeyboardCommand(nameof(Command.Paste));
				}
			}
		}

		async void IxtermEventListener.OnSelectionChanged(string selection)
		{
			if (!string.IsNullOrEmpty(selection) && false)
			{
				CopyTextToClipbpard(selection);
				await ExecuteScriptAsync("term.clearSelection()").ConfigureAwait(false);
			}
		}

		void IxtermEventListener.OnTerminalResized(int columns, int rows)
		{
			var size = new TerminalSize { Columns = columns, Rows = rows };

			lock (_resizeLock)
			{
				if (_setSize == null)
				{
					// Initialization not finished yet
					_requestedSize = size;
				}
				else
				{
					ScheduleResize(size, false);
				}
			}
		}

		void IxtermEventListener.OnInitialized()
		{
			_tcsConnected.TrySetResult(null);
		}

		void IxtermEventListener.OnTitleChanged(string title)
		{
		}

		void IxtermEventListener.OnError(string error)
		{
			App.Logger.LogWarning(error);
		}

		private void OutputReceivedCallback(byte[] e)
		{
			OnOutput?.Invoke(this, e);
		}

		public void OnInput(byte[] data)
		{
			_terminal.WriteToPseudoConsole(data);
		}

		private void CopyTextToClipbpard(string content)
		{
			SafetyExtensions.IgnoreExceptions(() =>
			{
				DataPackage data = new();
				data.SetText(content);

				Clipboard.SetContent(data);
				Clipboard.Flush();
			});
		}

		private async Task<string> ExecuteScriptAsync(string script)
		{
			try
			{
				var scriptTask = DispatcherQueue.EnqueueAsync(() => WebViewControl.InvokeScriptAsync("eval", new[] { script }))
					.ConfigureAwait(false);

				return await scriptTask;
			}
			catch (Exception e)
			{
			}

			return string.Empty;
		}

		public void Dispose()
		{
			WebViewControl.Close();
			_outputBlockedBuffer?.Dispose();
			_reader?.Dispose();
			_terminal?.Dispose();
		}

		public void Paste(string text) => OnPaste?.Invoke(this, text);

		public async Task<string?> GetTerminalFolder()
		{
			var tcs = new TaskCompletionSource<string>();
			EventHandler<object> getResponse = (s, e) =>
			{
				var pwd = Encoding.UTF8.GetString((byte[])e);
				var match = Regex.Match(pwd, @"[a-zA-Z]:\\(((?![<>:""\r/\\|?*]).)+((?<![ .])\\)?)*");
				if (match.Success)
					tcs.TrySetResult(match.Value);
			};
			OnOutput += getResponse;
			if (_profile.Location.Contains("wsl.exe"))
				_terminal.WriteToPseudoConsole(Encoding.UTF8.GetBytes($"wslpath -w \"$(pwd)\"\r"));
			else
				_terminal.WriteToPseudoConsole(Encoding.UTF8.GetBytes($"cd .\r"));
			var pwd = await tcs.Task.WithTimeoutAsync(TimeSpan.FromSeconds(1));
			OnOutput -= getResponse;
			return pwd;
		}

		public void SetTerminalFolder(string folder)
		{
			if (_profile.Location.Contains("wsl.exe"))
				_terminal.WriteToPseudoConsole(Encoding.UTF8.GetBytes($"cd \"$(wslpath \"{folder}\")\"\r"));
			else
				_terminal.WriteToPseudoConsole(Encoding.UTF8.GetBytes($"cd \"{folder}\"\r"));
		}

		private void StartShellProcess(TerminalSize size, ShellProfile profile)
		{
			var ShellExecutableName = Path.GetFileNameWithoutExtension(profile.Location);
			var cwd = _context.Folder?.ItemPath ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			var args = !string.IsNullOrWhiteSpace(profile.Location)
				? $"\"{profile.Location}\" {profile.Arguments}"
				: profile.Arguments;

			_terminal = new Terminal();
			_terminal.OutputReady += (s, e) =>
			{
				_reader = new BufferedReader(_terminal.ConsoleOutStream, OutputReceivedCallback, true);
			};
			_terminal.Exited += (s, e) =>
			{
				DispatcherQueue.EnqueueAsync(() =>
				{
					_mainPageModel.TerminalCloseCommand.Execute(_id);
				});
			};

			Task.Factory.StartNew(() => _terminal.Start(args, cwd, size.Columns, size.Rows));
		}

		private async void ProcessKeyboardCommand(string e)
		{
			switch (e)
			{
				case nameof(Command.Copy):
					{
						var selection = await ExecuteScriptAsync("term.getSelection()").ConfigureAwait(false);
						CopyTextToClipbpard(selection);
						return;
					}
				case nameof(Command.Paste):
					{
						Task<string> GetTextAsync()
						{
							var content = Clipboard.GetContent();
							if (content.Contains(StandardDataFormats.Text))
								return content.GetTextAsync().AsTask();
							// Otherwise return a new task that just sends an empty string.
							return Task.FromResult(string.Empty);
						}
						var content = await GetTextAsync().ConfigureAwait(false);
						if (content != null)
							Paste(content);
						return;
					}
				default:
					{
						return;
					}
			}
		}

		private void TerminalView_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
		}

		private async void TerminalView_ActualThemeChanged(Microsoft.UI.Xaml.FrameworkElement sender, object args)
		{
			if (!_tcsConnected.Task.IsCompleted)
				return;

			var serializerSettings = new JsonSerializerOptions();
			serializerSettings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			var theme = new DefaultValueProvider().GetPreInstalledThemes().First(x => x.Id == _profile.TerminalThemeId);

			WebViewControl.CoreWebView2.Profile.PreferredColorScheme = (ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark) ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
			RootGrid.Background = new SolidColorBrush(ColorHelpers.FromHex(theme.Colors.Background));

			var serializedTheme = JsonSerializer.Serialize(theme.Colors, serializerSettings);
			await ExecuteScriptAsync($"changeTheme('{serializedTheme}')");
		}
	}
}