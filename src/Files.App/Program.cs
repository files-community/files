// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System.IO;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using static Files.App.Helpers.Win32PInvoke;

namespace Files.App
{
	/// <summary>
	/// Represents the base entry point of the Files app.
	/// </summary>
	/// <remarks>
	/// Gets called at the first time when the app launched or activated.
	/// </remarks>
	internal sealed class Program
	{
		private const uint CWMO_DEFAULT = 0;
		private const uint INFINITE = 0xFFFFFFFF;

		public static Semaphore? Pool { get; set; }

		static Program()
		{
			var pool = new Semaphore(0, 1, $"Files-{AppLifecycleHelper.AppEnvironment}-Instance", out var isNew);

			if (!isNew)
			{
				// Resume cached instance
				pool.Release();

				// Redirect to the main process
				var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
				var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
				RedirectActivationTo(instance, AppInstance.GetCurrent().GetActivatedEventArgs());

				// Kill the current process
				Environment.Exit(0);
			}

			pool.Dispose();
		}

		/// <summary>
		/// Initializes the process; the entry point of the process.
		/// </summary>
		/// <remarks>
		/// <see cref="Main"/> cannot be declared to be async because this prevents Narrator from reading XAML elements in a WinUI app.
		/// </remarks>
		[STAThread]
		private static void Main()
		{
			WinRT.ComWrappersSupport.InitializeComWrappers();

			// We are about to do the first WinRT server call, in case the WinRT server is hanging
			// we need to kill the server if there is no other Files instances already running

			static bool ProcessPathPredicate(Process p)
			{
				try
				{
					return p.MainModule?.FileName
						.StartsWith(Windows.ApplicationModel.Package.Current.EffectivePath, StringComparison.OrdinalIgnoreCase) ?? false;
				}
				catch
				{
					return false;
				}
			}

			var processes = Process.GetProcessesByName("Files")
				.Where(ProcessPathPredicate)
				.Where(p => p.Id != Environment.ProcessId);

			if (!processes.Any())
			{
				foreach (var process in Process.GetProcessesByName("Files.App.Server").Where(ProcessPathPredicate))
				{
					try
					{
						process.Kill();
					}
					catch
					{
						// ignore any exceptions
					}
					finally
					{
						process.Dispose();
					}
				}
			}

			// NOTE:
			//  This has been commentted out since out-of-proc WinRT server seems not to support elevetion.
			//  For more info, see the GitHub issue (#15384).
			// Now we can do the first WinRT server call
			//Server.AppInstanceMonitor.StartMonitor(Environment.ProcessId);

			var OpenTabInExistingInstance = ApplicationData.Current.LocalSettings.Values.Get("OpenTabInExistingInstance", true);
			var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
			var commandLineArgs = GetCommandLineArgs(activatedArgs);

			if (commandLineArgs is not null)
			{
				var parsedCommands = CommandLineParser.ParseUntrustedCommands(commandLineArgs);

				if (parsedCommands is not null)
				{
					foreach (var command in parsedCommands)
					{
						switch (command.Type)
						{
							case ParsedCommandType.ExplorerShellCommand:
								if (!Constants.UserEnvironmentPaths.ShellPlaces.ContainsKey(command.Payload.ToUpperInvariant()))
								{
									OpenShellCommandInExplorer(command.Payload, Environment.ProcessId);
									return;
								}
								break;

							default:
								break;
						}
					}
				}

				// Always open a new instance for OpenDialog, never open new instance for "-Tag" command
				if (parsedCommands is null || !parsedCommands.Any(x => x.Type == ParsedCommandType.OutputPath) &&
					(OpenTabInExistingInstance || parsedCommands.Any(x => x.Type == ParsedCommandType.TagFiles)))
				{
					var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
					var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());

					if (!instance.IsCurrent)
					{
						RedirectActivationTo(instance, activatedArgs);
						return;
					}
				}
			}
			else if (activatedArgs.Data is ILaunchActivatedEventArgs tileArgs)
			{
				if (tileArgs.Arguments is not null &&
					FileExtensionHelpers.IsExecutableFile(tileArgs.Arguments))
				{
					if (File.Exists(tileArgs.Arguments))
					{
						OpenFileFromTile(tileArgs.Arguments);
						return;
					}
				}
			}

			if (OpenTabInExistingInstance && commandLineArgs is null)
			{
				if (activatedArgs.Data is ILaunchActivatedEventArgs launchArgs)
				{
					var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
					var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
					if (!instance.IsCurrent && !string.IsNullOrWhiteSpace(launchArgs.Arguments))
					{
						RedirectActivationTo(instance, activatedArgs);
						return;
					}
				}
				else if (activatedArgs.Data is IProtocolActivatedEventArgs protocolArgs)
				{
					var parsedArgs = protocolArgs.Uri.Query.TrimStart('?').Split('=');
					if (parsedArgs.Length == 1)
					{
						var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
						var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
						if (!instance.IsCurrent)
						{
							RedirectActivationTo(instance, activatedArgs);
							return;
						}
					}
				}
				else if (activatedArgs.Data is IFileActivatedEventArgs)
				{
					var activePid = ApplicationData.Current.LocalSettings.Values.Get("INSTANCE_ACTIVE", -1);
					var instance = AppInstance.FindOrRegisterForKey(activePid.ToString());
					if (!instance.IsCurrent)
					{
						RedirectActivationTo(instance, activatedArgs);
						return;
					}
				}
			}

			var currentInstance = AppInstance.FindOrRegisterForKey((-Environment.ProcessId).ToString());
			if (currentInstance.IsCurrent)
				currentInstance.Activated += OnActivated;

			ApplicationData.Current.LocalSettings.Values["INSTANCE_ACTIVE"] = -Environment.ProcessId;

			Application.Start((p) =>
			{
				var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
				SynchronizationContext.SetSynchronizationContext(context);

				_ = new App();
			});
		}

		/// <summary>
		/// Gets command line args from AppActivationArguments
		/// Command line args can be ILaunchActivatedEventArgs, ICommandLineActivatedEventArgs or IProtocolActivatedEventArgs
		/// </summary>
		private static string? GetCommandLineArgs(AppActivationArguments activatedArgs)
		{
			// WINUI3: When launching from commandline the argument is not ICommandLineActivatedEventArgs (#10370)
			var cmdLaunchArgs = activatedArgs.Data is ILaunchActivatedEventArgs launchArgs &&
				launchArgs.Arguments is not null &&
				CommandLineParser.SplitArguments(launchArgs.Arguments, true).FirstOrDefault() is string arg0 &&
				(arg0.EndsWith($"files-dev.exe", StringComparison.OrdinalIgnoreCase) ||
				arg0.EndsWith($"files-dev", StringComparison.OrdinalIgnoreCase)) ? launchArgs.Arguments : null;
			var cmdProtocolArgs = activatedArgs.Data is IProtocolActivatedEventArgs protocolArgs &&
				protocolArgs.Uri.Query.TrimStart('?').Split('=') is string[] parsedArgs &&
				parsedArgs.Length == 2 && parsedArgs[0] == "cmd" ? Uri.UnescapeDataString(parsedArgs[1]) : null;
			var cmdLineArgs = activatedArgs.Data is ICommandLineActivatedEventArgs cmdArgs ? cmdArgs.Operation.Arguments : null;

			return cmdLaunchArgs ?? cmdProtocolArgs ?? cmdLineArgs;
		}

		/// <summary>
		/// Gets invoked when the application is activated.
		/// </summary>
		private static async void OnActivated(object? sender, AppActivationArguments args)
		{
			// WINUI3: Verify if needed or OnLaunched is called
			if (App.Current is App thisApp)
				await thisApp.OnActivatedAsync(args);
		}

		/// <summary>
		/// Redirects the activation to the main process.
		/// </summary>
		/// <remarks>
		/// Redirects on another thread and uses a non-blocking wait method to wait for the redirection to complete.
		/// </remarks>
		public static void RedirectActivationTo(AppInstance keyInstance, AppActivationArguments args)
		{
			IntPtr eventHandle = CreateEvent(IntPtr.Zero, true, false, null);

			Task.Run(() =>
			{
				keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
				SetEvent(eventHandle);
			});

			_ = CoWaitForMultipleObjects(
				CWMO_DEFAULT,
				INFINITE,
				1,
				[eventHandle],
				out uint handleIndex);
		}

		public static void OpenShellCommandInExplorer(string shellCommand, int pid)
		{
			Win32Helper.OpenFolderInExistingShellWindow(shellCommand);
		}

		public static void OpenFileFromTile(string filePath)
		{
			IntPtr eventHandle = CreateEvent(IntPtr.Zero, true, false, null);

			Task.Run(() =>
			{
				LaunchHelper.LaunchAppAsync(filePath, null, null).Wait();
				SetEvent(eventHandle);
			});

			_ = CoWaitForMultipleObjects(
				CWMO_DEFAULT,
				INFINITE,
				1,
				[eventHandle],
				out uint handleIndex);
		}
	}
}
