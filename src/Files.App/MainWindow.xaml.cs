// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using WinUIEx;
using IO = System.IO;

namespace Files.App
{
	public sealed partial class MainWindow : WindowEx
	{
		private static MainWindow? _Instance;
		public static MainWindow Instance => _Instance ??= new();

		public IntPtr WindowHandle { get; }

		private MainWindow()
		{
			WindowHandle = this.GetWindowHandle();

			InitializeComponent();

			EnsureEarlyWindow();
		}

		private void EnsureEarlyWindow()
		{
			// Set PersistenceId
			PersistenceId = "FilesMainWindow";

			// Set minimum sizes
			MinHeight = 416;
			MinWidth = 516;

			AppWindow.Title = "Files";
			AppWindow.SetIcon(AppLifecycleHelper.AppIconPath);
			AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

			// Workaround for full screen window messing up the taskbar
			// https://github.com/microsoft/microsoft-ui-xaml/issues/8431
			// This property should only be set if the "Automatically hide the taskbar" in Windows 11,
			// or "Automatically hide the taskbar in desktop mode" in Windows 10 is enabled.
			// Setting this property when the setting is disabled will result in the taskbar overlapping the application
			if (AppLifecycleHelper.IsAutoHideTaskbarEnabled()) 
				Win32PInvoke.SetPropW(WindowHandle, "NonRudeHWND", new IntPtr(1));
		}

		public void ShowSplashScreen()
		{
			var rootFrame = EnsureWindowIsInitialized();

			rootFrame?.Navigate(typeof(SplashScreenPage));
		}

		public async Task InitializeApplicationAsync(object activatedEventArgs)
		{
			var rootFrame = EnsureWindowIsInitialized();

			if (rootFrame is null)
				return;

			// Set system backdrop
			SystemBackdrop = new AppSystemBackdrop();

			switch (activatedEventArgs)
			{
				case ILaunchActivatedEventArgs launchArgs:
					if (launchArgs.Arguments is not null &&
						(CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith($"files.exe", StringComparison.OrdinalIgnoreCase)
						|| CommandLineParser.SplitArguments(launchArgs.Arguments, true)[0].EndsWith($"files", StringComparison.OrdinalIgnoreCase)))
					{
						// WINUI3: When launching from commandline the argument is not ICommandLineActivatedEventArgs (#10370)
						var ppm = CommandLineParser.ParseUntrustedCommands(launchArgs.Arguments);
						if (ppm.IsEmpty())
							rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
						else
							await InitializeFromCmdLineArgsAsync(rootFrame, ppm);
					}
					else if (rootFrame.Content is null || rootFrame.Content is SplashScreenPage || !MainPageViewModel.AppInstances.Any())
					{
						// When the navigation stack isn't restored navigate to the first page,
						// configuring the new page by passing required information as a navigation parameter
						rootFrame.Navigate(typeof(MainPage), launchArgs.Arguments, new SuppressNavigationTransitionInfo());
					}
					else if (!(string.IsNullOrEmpty(launchArgs.Arguments) && MainPageViewModel.AppInstances.Count > 0))
					{
						// Bring to foreground (#14730)
						Win32Helper.BringToForegroundEx(new(WindowHandle));

						await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), launchArgs.Arguments, true);
					}
					else
					{
						rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
					}
					break;

				case IProtocolActivatedEventArgs eventArgs:
					if (eventArgs.Uri.AbsoluteUri == "files-uwp:")
					{
						rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());

						if (MainPageViewModel.AppInstances.Count > 0)
						{
							// Bring to foreground (#14730)
							Win32Helper.BringToForegroundEx(new(WindowHandle));
						}
					}
					else
					{
						var parsedArgs = eventArgs.Uri.Query.TrimStart('?').Split('=');
						var unescapedValue = Uri.UnescapeDataString(parsedArgs[1]);
						var folder = (StorageFolder)await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(unescapedValue).AsTask());
						if (folder is not null && !string.IsNullOrEmpty(folder.Path))
						{
							// Convert short name to long name (#6190)
							unescapedValue = folder.Path;
						}
						switch (parsedArgs[0])
						{
							case "tab":
								rootFrame.Navigate(typeof(MainPage),
									new MainPageNavigationArguments() { Parameter = TabBarItemParameter.Deserialize(unescapedValue), IgnoreStartupSettings = true },
									new SuppressNavigationTransitionInfo());
								break;

							case "folder":
								rootFrame.Navigate(typeof(MainPage),
									new MainPageNavigationArguments() { Parameter = unescapedValue, IgnoreStartupSettings = true },
									new SuppressNavigationTransitionInfo());
								break;

							case "cmd":
								var ppm = CommandLineParser.ParseUntrustedCommands(unescapedValue);
								if (ppm.IsEmpty())
									rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
								else
									await InitializeFromCmdLineArgsAsync(rootFrame, ppm);
								break;
							default:
								rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
								break;
						}
					}
					break;

				case ICommandLineActivatedEventArgs cmdLineArgs:
					var operation = cmdLineArgs.Operation;
					var cmdLineString = operation.Arguments;
					var activationPath = operation.CurrentDirectoryPath;

					var parsedCommands = CommandLineParser.ParseUntrustedCommands(cmdLineString);
					if (parsedCommands is not null && parsedCommands.Count > 0)
					{
						await InitializeFromCmdLineArgsAsync(rootFrame, parsedCommands, activationPath);
					}
					else
					{
						rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
					}
					break;

				case IFileActivatedEventArgs fileArgs:
					var index = 0;
					if (rootFrame.Content is null || rootFrame.Content is SplashScreenPage || !MainPageViewModel.AppInstances.Any())
					{
						// When the navigation stack isn't restored navigate to the first page,
						// configuring the new page by passing required information as a navigation parameter
						rootFrame.Navigate(typeof(MainPage), fileArgs.Files.First().Path, new SuppressNavigationTransitionInfo());
						index = 1;
					}
					else
					{
						// Bring to foreground (#14730)
						Win32Helper.BringToForegroundEx(new(WindowHandle));
					}

					for (; index < fileArgs.Files.Count; index++)
					{
						await NavigationHelpers.AddNewTabByPathAsync(typeof(ShellPanesPage), fileArgs.Files[index].Path, true);
					}
					break;

				case IStartupTaskActivatedEventArgs startupArgs:
					// Just launch the app with no arguments
					rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
					break;

				default:
					// Just launch the app with no arguments
					rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
					break;
			}

			if (!AppWindow.IsVisible)
			{
				// When resuming the cached instance
				AppWindow.Show();
				Activate();
			}

			if (Windows.Win32.PInvoke.IsIconic(new(WindowHandle)))
				Instance.Restore(); // Restore window if minimized
		}

		private Frame? EnsureWindowIsInitialized()
		{
			try
			{
				// NOTE:
				//  Do not repeat app initialization when the Window already has content,
				//  just ensure that the window is active
				if (Instance.Content is not Frame rootFrame)
				{
					// Create a Frame to act as the navigation context and navigate to the first page
					rootFrame = new() { CacheSize = 1 };
					rootFrame.NavigationFailed += OnNavigationFailed;

					// Place the frame in the current Window
					Instance.Content = rootFrame;
				}

				return rootFrame;
			}
			catch (COMException)
			{
				return null;
			}
		}

		/// <summary>
		/// Invoked when Navigation to a certain page fails
		/// </summary>
		/// <param name="sender">The Frame which failed navigation</param>
		/// <param name="e">Details about the navigation failure</param>
		private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
			=> throw new Exception("Failed to load Page " + e.SourcePageType.FullName);

		private async Task InitializeFromCmdLineArgsAsync(Frame rootFrame, ParsedCommands parsedCommands, string activationPath = "")
		{
			async Task PerformNavigationAsync(string payload, string selectItem = null)
			{
				if (!string.IsNullOrEmpty(payload))
				{
					payload = Constants.UserEnvironmentPaths.ShellPlaces.Get(payload.ToUpperInvariant(), payload);
					var folder = (StorageFolder)await FilesystemTasks.Wrap(() => StorageFolder.GetFolderFromPathAsync(payload).AsTask());
					if (folder is not null && !string.IsNullOrEmpty(folder.Path))
						payload = folder.Path; // Convert short name to long name (#6190)
				}

				var generalSettingsService = Ioc.Default.GetService<IGeneralSettingsService>();

				double boundsWidth = 0;
				try
				{
					boundsWidth = Bounds.Width;
				}
				catch (Exception ex)
				{
					// Handle exception in case WinUI Windows is closed
					// (see https://github.com/files-community/Files/issues/15599)

					App.Logger.LogWarning(ex, ex.Message);
					return;
				}

				var paneNavigationArgs = new PaneNavigationArguments
				{
					LeftPaneNavPathParam = payload,
					LeftPaneSelectItemParam = selectItem,
					RightPaneNavPathParam = boundsWidth > Constants.UI.MultiplePaneWidthThreshold && (generalSettingsService?.AlwaysOpenDualPaneInNewTab ?? false) ? "Home" : null,
				};

				if (rootFrame.Content is MainPage && MainPageViewModel.AppInstances.Any())
				{
					// Bring to foreground (#14730)
					Win32Helper.BringToForegroundEx(new(WindowHandle));

					var existingTabIndex = MainPageViewModel.AppInstances
						.Select((tabItem, idx) => new { tabItem, idx })
						.FirstOrDefault(x => x.tabItem.NavigationParameter.NavigationParameter is PaneNavigationArguments paneArgs &&
							(paneNavigationArgs.LeftPaneNavPathParam == paneArgs.LeftPaneNavPathParam || 
							paneNavigationArgs.LeftPaneNavPathParam == paneArgs.RightPaneNavPathParam))?.idx ?? -1;

					if (existingTabIndex >= 0)
						App.AppModel.TabStripSelectedIndex = existingTabIndex;
					else
						await NavigationHelpers.AddNewTabByParamAsync(typeof(ShellPanesPage), paneNavigationArgs);
				}
				else
					rootFrame.Navigate(typeof(MainPage), paneNavigationArgs, new SuppressNavigationTransitionInfo());
			}
			foreach (var command in parsedCommands)
			{
				switch (command.Type)
				{
					case ParsedCommandType.OpenDirectory:
					case ParsedCommandType.OpenPath:
					case ParsedCommandType.ExplorerShellCommand:
						var selectItemCommand = parsedCommands.FirstOrDefault(x => x.Type == ParsedCommandType.SelectItem);
						await PerformNavigationAsync(command.Payload, selectItemCommand?.Payload);
						break;

					case ParsedCommandType.SelectItem:
						if (IO.Path.IsPathRooted(command.Payload))
							await PerformNavigationAsync(IO.Path.GetDirectoryName(command.Payload), IO.Path.GetFileName(command.Payload));
						break;

					case ParsedCommandType.TagFiles:
						var tagService = Ioc.Default.GetService<IFileTagsSettingsService>();
						var tag = tagService.GetTagsByName(command.Payload).FirstOrDefault();
						foreach (var file in command.Args.Skip(1))
						{
							var fileFRN = await FilesystemTasks.Wrap(() => StorageHelpers.ToStorageItem<IStorageItem>(file))
								.OnSuccess(item => FileTagsHelper.GetFileFRN(item));
							if (fileFRN is not null)
							{
								var tagUid = tag is not null ? new[] { tag.Uid } : [];
								var dbInstance = FileTagsHelper.GetDbInstance();
								dbInstance.SetTags(file, fileFRN, tagUid);
								FileTagsHelper.WriteFileTag(file, tagUid);
							}
						}
						break;

					case ParsedCommandType.Unknown:
						if (command.Payload.Equals("."))
						{
							await PerformNavigationAsync(activationPath);
						}
						else
						{
							if (!string.IsNullOrEmpty(command.Payload))
							{
								var target = IO.Path.GetFullPath(IO.Path.Combine(activationPath, command.Payload));
								await PerformNavigationAsync(target);
							}
							else
							{
								await PerformNavigationAsync(null);
							}
						}
						break;

					case ParsedCommandType.OutputPath:
						App.OutputPath = command.Payload;
						break;
				}
			}
		}
	}
}
