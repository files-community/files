// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SevenZip;
using System.IO;
using System.Text;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.Win32.Storage.FileSystem;

namespace Files.App.ViewModels.Settings
{
	public sealed class AdvancedViewModel : ObservableObject
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();

		private readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

		public ICommand SetAsDefaultExplorerCommand { get; }
		public ICommand SetAsOpenFileDialogCommand { get; }
		public ICommand ExportSettingsCommand { get; }
		public ICommand ImportSettingsCommand { get; }
		public ICommand OpenSettingsJsonCommand { get; }
		public AsyncRelayCommand OpenFilesOnWindowsStartupCommand { get; }


		public AdvancedViewModel()
		{
			IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
			IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();

			SetAsDefaultExplorerCommand = new AsyncRelayCommand(SetAsDefaultExplorerAsync);
			SetAsOpenFileDialogCommand = new AsyncRelayCommand(SetAsOpenFileDialogAsync);
			ExportSettingsCommand = new AsyncRelayCommand(ExportSettingsAsync);
			ImportSettingsCommand = new AsyncRelayCommand(ImportSettingsAsync);
			OpenSettingsJsonCommand = new AsyncRelayCommand(OpenSettingsJsonAsync);
			OpenFilesOnWindowsStartupCommand = new AsyncRelayCommand(OpenFilesOnWindowsStartupAsync);

			_ = DetectOpenFilesAtStartupAsync();
		}

		private async Task OpenSettingsJsonAsync()
		{
			await SafetyExtensions.IgnoreExceptions(async () =>
			{
				var settingsJsonFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/settings/user_settings.json"));
				if (!await Launcher.LaunchFileAsync(settingsJsonFile))
					await ContextMenu.InvokeVerb("open", settingsJsonFile.Path);
			});
		}

		private async Task SetAsDefaultExplorerAsync()
		{
			// Make sure IsSetAsDefaultFileManager is updated
			await Task.Yield();

			if (IsSetAsDefaultFileManager == DetectIsSetAsDefaultFileManager())
				return;

			var destFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FilesOpenDialog");
			Directory.CreateDirectory(destFolder);

			foreach (var file in Directory.GetFiles(Path.Combine(Package.Current.InstalledLocation.Path, "Files.App", "Assets", "FilesOpenDialog")))
			{
				if (!SafetyExtensions.IgnoreExceptions(() => File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true), App.Logger))
				{
					// Error copying files
					await DetectResult();
					return;
				}
			}

			var dataPath = Environment.ExpandEnvironmentVariables("%LocalAppData%\\Files");
			if (IsSetAsDefaultFileManager)
			{
				if (!await Win32Helper.RunPowershellCommandAsync($"-command \"New-Item -Force -Path '{dataPath}' -ItemType Directory; Copy-Item -Filter *.* -Path '{destFolder}\\*' -Recurse -Force -Destination '{dataPath}'\"", PowerShellExecutionOptions.Hidden))
				{
					// Error copying files
					await DetectResult();
					return;
				}
			}
			else
			{
				await Win32Helper.RunPowershellCommandAsync($"-command \"Remove-Item -Path '{dataPath}' -Recurse -Force\"", PowerShellExecutionOptions.Hidden);
			}

			try
			{
				using var regProcess = Process.Start(new ProcessStartInfo("regedit.exe", @$"/s ""{Path.Combine(destFolder, IsSetAsDefaultFileManager ? "SetFilesAsDefault.reg" : "UnsetFilesAsDefault.reg")}""") { UseShellExecute = true, Verb = "runas" });
				if (regProcess is not null)
					await regProcess.WaitForExitAsync();
			}
			catch
			{
				// Canceled UAC
			}

			await DetectResult();
		}

		private Task DetectResult()
		{
			IsSetAsDefaultFileManager = DetectIsSetAsDefaultFileManager();
			if (!IsSetAsDefaultFileManager)
			{
				IsSetAsOpenFileDialog = false;
				return SetAsOpenFileDialogAsync();
			}

			return Task.CompletedTask;
		}

		private async Task SetAsOpenFileDialogAsync()
		{
			// Make sure IsSetAsDefaultFileManager is updated
			await Task.Yield();
			if (IsSetAsOpenFileDialog == DetectIsSetAsOpenFileDialog())
				return;

			var destFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FilesOpenDialog");
			Directory.CreateDirectory(destFolder);
			foreach (var file in Directory.GetFiles(Path.Combine(Package.Current.InstalledLocation.Path, "Files.App", "Assets", "FilesOpenDialog")))
			{
				if (!SafetyExtensions.IgnoreExceptions(() => File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true), App.Logger))
				{
					// Error copying files
					goto DetectResult;
				}
			}

			try
			{
				using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.OpenDialog32.dll")}"""))
					await regProc.WaitForExitAsync();
				using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.OpenDialog64.dll")}"""))
					await regProc.WaitForExitAsync();
				using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.OpenDialogARM64.dll")}"""))
					await regProc.WaitForExitAsync();
				using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.SaveDialog32.dll")}"""))
					await regProc.WaitForExitAsync();
				using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.SaveDialog64.dll")}"""))
					await regProc.WaitForExitAsync();
				using (var regProc = Process.Start("regsvr32.exe", @$"/s /n {(!IsSetAsOpenFileDialog ? "/u" : "")} /i:user ""{Path.Combine(destFolder, "Files.App.SaveDialogARM64.dll")}"""))
					await regProc.WaitForExitAsync();
			}
			catch
			{
			}

		DetectResult:
			IsSetAsOpenFileDialog = DetectIsSetAsOpenFileDialog();
		}

		private async Task ImportSettingsAsync()
		{
			string[] extensions = ["ZipFileCapitalized".GetLocalizedResource(), "*.zip"];
			bool result = CommonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, false, extensions, Environment.SpecialFolder.Desktop, out var filePath);
			if (!result)
				return;

			try
			{
				var file = await StorageHelpers.ToStorageItem<BaseStorageFile>(filePath);

				var zipFolder = await ZipStorageFolder.FromStorageFileAsync(file);
				if (zipFolder is null)
					return;

				var localFolderPath = ApplicationData.Current.LocalFolder.Path;
				var settingsFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(localFolderPath, Constants.LocalSettings.SettingsFolderName));

				// Import user settings
				var userSettingsFile = await zipFolder.GetFileAsync(Constants.LocalSettings.UserSettingsFileName);
				string importSettings = await userSettingsFile.ReadTextAsync();
				UserSettingsService.ImportSettings(importSettings);

				// Import file tags list and DB
				var fileTagsList = await zipFolder.GetFileAsync(Constants.LocalSettings.FileTagSettingsFileName);
				string importTags = await fileTagsList.ReadTextAsync();
				fileTagsSettingsService.ImportSettings(importTags);
				var fileTagsDB = await zipFolder.GetFileAsync(Constants.LocalSettings.FileTagSettingsDatabaseFileName);
				string importTagsDB = await fileTagsDB.ReadTextAsync();
				var tagDbInstance = FileTagsHelper.GetDbInstance();
				tagDbInstance.Import(importTagsDB);

				// Import layout preferences and DB
				var layoutPrefsDB = await zipFolder.GetFileAsync(Constants.LocalSettings.UserSettingsDatabaseFileName);
				string importPrefsDB = await layoutPrefsDB.ReadTextAsync();
				var layoutDbInstance = LayoutPreferencesManager.GetDatabaseManagerInstance();
				layoutDbInstance.Import(importPrefsDB);
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Error importing settings");
				UIHelpers.CloseAllDialogs();
				await DialogDisplayHelper.ShowDialogAsync("SettingsImportErrorTitle".GetLocalizedResource(), "SettingsImportErrorDescription".GetLocalizedResource());
			}
		}

		private async Task ExportSettingsAsync()
		{
			string[] extensions = ["ZipFileCapitalized".GetLocalizedResource(), "*.zip" ];
			bool result = CommonDialogService.Open_FileSaveDialog(MainWindow.Instance.WindowHandle, false, extensions, Environment.SpecialFolder.Desktop, out var filePath);
			if (!result)
				return;

			if (!filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
				filePath += ".zip";

			try
			{
				var handle = Win32PInvoke.CreateFileFromAppW(
					filePath,
					(uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE),
					Win32PInvoke.FILE_SHARE_READ | Win32PInvoke.FILE_SHARE_WRITE,
					nint.Zero,
					Win32PInvoke.CREATE_NEW,
					0,
					nint.Zero);

				Win32PInvoke.CloseHandle(handle);

				var file = await StorageHelpers.ToStorageItem<BaseStorageFile>(filePath);

				await ZipStorageFolder.InitArchive(file, OutArchiveFormat.Zip);

				var zipFolder = (ZipStorageFolder)await ZipStorageFolder.FromStorageFileAsync(file);
				if (zipFolder is null)
					return;

				var localFolderPath = ApplicationData.Current.LocalFolder.Path;

				// Export user settings
				var exportSettings = UTF8Encoding.UTF8.GetBytes((string)UserSettingsService.ExportSettings());
				await zipFolder.CreateFileAsync(new MemoryStream(exportSettings), Constants.LocalSettings.UserSettingsFileName, CreationCollisionOption.ReplaceExisting);

				// Export file tags list and DB
				var exportTags = UTF8Encoding.UTF8.GetBytes((string)fileTagsSettingsService.ExportSettings());
				await zipFolder.CreateFileAsync(new MemoryStream(exportTags), Constants.LocalSettings.FileTagSettingsFileName, CreationCollisionOption.ReplaceExisting);
				var tagDbInstance = FileTagsHelper.GetDbInstance();
				byte[] exportTagsDB = UTF8Encoding.UTF8.GetBytes(tagDbInstance.Export());
				await zipFolder.CreateFileAsync(new MemoryStream(exportTagsDB), Constants.LocalSettings.FileTagSettingsDatabaseFileName, CreationCollisionOption.ReplaceExisting);

				// Export layout preferences DB
				var layoutDbInstance = LayoutPreferencesManager.GetDatabaseManagerInstance();
				byte[] exportPrefsDB = UTF8Encoding.UTF8.GetBytes(layoutDbInstance.Export());
				await zipFolder.CreateFileAsync(new MemoryStream(exportPrefsDB), Constants.LocalSettings.UserSettingsDatabaseFileName, CreationCollisionOption.ReplaceExisting);
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Error exporting settings");
			}
		}

		private bool DetectIsSetAsDefaultFileManager()
		{
			using var subkey = Registry.ClassesRoot.OpenSubKey(@"Folder\shell\open\command");
			var command = (string?)subkey?.GetValue(string.Empty);

			return !string.IsNullOrEmpty(command) && command.Contains("Files.App.Launcher.exe");
		}

		private bool DetectIsSetAsOpenFileDialog()
		{
			using var subkeyOpen = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7}");
			using var subkeySave = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\CLSID\{C0B4E2F3-BA21-4773-8DBA-335EC946EB8B}");

			var isSetAsOpenDialog = subkeyOpen?.GetValue(string.Empty) as string == "FilesOpenDialog class";
			var isSetAsSaveDialog = subkeySave?.GetValue(string.Empty) as string == "FilesSaveDialog class";

			return isSetAsOpenDialog || isSetAsSaveDialog;
		}

		private bool isSetAsDefaultFileManager;
		public bool IsSetAsDefaultFileManager
		{
			get => isSetAsDefaultFileManager;
			set => SetProperty(ref isSetAsDefaultFileManager, value);
		}

		private bool isSetAsOpenFileDialog;
		public bool IsSetAsOpenFileDialog
		{
			get => isSetAsOpenFileDialog;
			set => SetProperty(ref isSetAsOpenFileDialog, value);
		}

		public bool IsAppEnvironmentDev
		{
			get => AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev;
		}

		private FileSavePicker InitializeWithWindow(FileSavePicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, MainWindow.Instance.WindowHandle);

			return obj;
		}

		private FileOpenPicker InitializeWithWindow(FileOpenPicker obj)
		{
			WinRT.Interop.InitializeWithWindow.Initialize(obj, MainWindow.Instance.WindowHandle);

			return obj;
		}

		private bool openOnWindowsStartup;
		public bool OpenOnWindowsStartup
		{
			get => openOnWindowsStartup;
			set => SetProperty(ref openOnWindowsStartup, value);
		}

		private bool canOpenOnWindowsStartup;
		public bool CanOpenOnWindowsStartup
		{
			get => canOpenOnWindowsStartup;
			set => SetProperty(ref canOpenOnWindowsStartup, value);
		}

		public bool LeaveAppRunning
		{
			get => UserSettingsService.GeneralSettingsService.LeaveAppRunning;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.LeaveAppRunning)
				{
					UserSettingsService.GeneralSettingsService.LeaveAppRunning = value;

					OnPropertyChanged();
				}
			}
		}
		
		public bool ShowSystemTrayIcon
		{
			get => UserSettingsService.GeneralSettingsService.ShowSystemTrayIcon;
			set
			{
				if (value != UserSettingsService.GeneralSettingsService.ShowSystemTrayIcon)
				{
					UserSettingsService.GeneralSettingsService.ShowSystemTrayIcon = value;

					OnPropertyChanged();
				}
			}
		}

		// TODO remove when feature is marked as stable
		public bool ShowFlattenOptions
		{
			get => UserSettingsService.GeneralSettingsService.ShowFlattenOptions;
			set
			{
				if (value == UserSettingsService.GeneralSettingsService.ShowFlattenOptions)
					return;

				UserSettingsService.GeneralSettingsService.ShowFlattenOptions = value;
				OnPropertyChanged();
			}
		}

		public async Task OpenFilesOnWindowsStartupAsync()
		{
			var stateMode = await ReadState();

			bool state = stateMode switch
			{
				StartupTaskState.Enabled => true,
				StartupTaskState.EnabledByPolicy => true,
				StartupTaskState.DisabledByPolicy => false,
				StartupTaskState.DisabledByUser => false,
				_ => false,
			};

			if (state != OpenOnWindowsStartup)
			{
				StartupTask startupTask = await StartupTask.GetAsync("3AA55462-A5FA-4933-88C4-712D0B6CDEBB");
				if (OpenOnWindowsStartup)
					await startupTask.RequestEnableAsync();
				else
					startupTask.Disable();
				await DetectOpenFilesAtStartupAsync();
			}
		}

		public async Task DetectOpenFilesAtStartupAsync()
		{
			var stateMode = await ReadState();

			switch (stateMode)
			{
				case StartupTaskState.Disabled:
					CanOpenOnWindowsStartup = true;
					OpenOnWindowsStartup = false;
					break;
				case StartupTaskState.Enabled:
					CanOpenOnWindowsStartup = true;
					OpenOnWindowsStartup = true;
					break;
				case StartupTaskState.DisabledByPolicy:
					CanOpenOnWindowsStartup = false;
					OpenOnWindowsStartup = false;
					break;
				case StartupTaskState.DisabledByUser:
					CanOpenOnWindowsStartup = false;
					OpenOnWindowsStartup = false;
					break;
				case StartupTaskState.EnabledByPolicy:
					CanOpenOnWindowsStartup = false;
					OpenOnWindowsStartup = true;
					break;
			}
		}

		public async Task<StartupTaskState> ReadState()
		{
			var state = await StartupTask.GetAsync("3AA55462-A5FA-4933-88C4-712D0B6CDEBB");
			return state.State;
		}
	}
}
