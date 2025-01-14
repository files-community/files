// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.Win32;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents view model of <see cref="Views.Settings.AboutPage"/>.
	/// </summary>
	public sealed class AboutViewModel : ObservableObject
	{
		// Dependency injections

		private IGeneralSettingsService GeneralSettingsService { get; } = Ioc.Default.GetRequiredService<IGeneralSettingsService>();


		// Properties

		public string Version
			=> string.Format($"{"SettingsAboutVersionTitle".GetLocalizedResource()} {AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Build}.{AppVersion.Revision}");

		public string AppName
			=> Package.Current.DisplayName;

		public PackageVersion AppVersion
			=> Package.Current.Id.Version;

		public ObservableCollection<OpenSourceLibraryItem> OpenSourceLibraries { get; }

		// Commands

		public ICommand CopyAppVersionCommand { get; }
		public ICommand CopyWindowsVersionCommand { get; }
		public ICommand CopyUserIDCommand { get; }
		public ICommand SupportUsCommand { get; }
		public ICommand OpenLogLocationCommand { get; }
		public ICommand OpenDocumentationCommand { get; }
		public ICommand OpenDiscordCommand { get; }
		public ICommand SubmitFeatureRequestCommand { get; }
		public ICommand SubmitBugReportCommand { get; }
		public ICommand OpenGitHubRepoCommand { get; }
		public ICommand OpenPrivacyPolicyCommand { get; }
		public ICommand OpenCrowdinCommand { get; }

		// Constructor

		/// <summary>
		/// Initializes an instance of <see cref="AboutViewModel"/> class.
		/// </summary>
		public AboutViewModel()
		{
			OpenSourceLibraries =
			[
				new ("https://github.com/omar/ByteSize", "ByteSize"),
				new ("https://github.com/CommunityToolkit/dotnet", "CommunityToolkit.Mvvm"),
				new ("https://github.com/DiscUtils/DiscUtils", "DiscUtils.Udf"),
				new ("https://github.com/robinrodricks/FluentFTP", "FluentFTP"),
				new ("https://github.com/libgit2/libgit2sharp", "libgit2sharp"),
				new ("https://github.com/jeffijoe/messageformat.net", "MessageFormat"),
				new ("https://github.com/dotnet/efcore", "EF Core for SQLite"),
				new ("https://github.com/dotnet/runtime", "Microsoft.Extensions"),
				new ("https://github.com/files-community/SevenZipSharp", "SevenZipSharp"),
				new ("https://sourceforge.net/projects/sevenzip", "7zip"),
				new ("https://github.com/ericsink/SQLitePCL.raw", "PCL for SQLite"),
				new ("https://github.com/microsoft/WindowsAppSDK", "WindowsAppSDK"),
				new ("https://github.com/microsoft/microsoft-ui-xaml", "WinUI 3"),
				new ("https://github.com/microsoft/Win2D", "Win2D"),
				new ("https://github.com/CommunityToolkit/WindowsCommunityToolkit", "Windows Community Toolkit 7.x"),
				new ("https://github.com/mono/taglib-sharp", "TagLibSharp"),
				new ("https://github.com/Tulpep/Active-Directory-Object-Picker", "ActiveDirectoryObjectPicker"),
				new ("https://github.com/PowerShell/MMI", "MMI"),
				new ("https://github.com/microsoft/CsWin32", "CsWin32"),
				new ("https://github.com/microsoft/CsWinRT", "CsWinRT"),
			];

			CopyAppVersionCommand = new RelayCommand(CopyAppVersion);
			CopyWindowsVersionCommand = new RelayCommand(CopyWindowsVersion);
			CopyUserIDCommand = new RelayCommand(CopyUserID);
			SupportUsCommand = new AsyncRelayCommand(SupportUs);
			OpenDocumentationCommand = new AsyncRelayCommand(DoOpenDocumentation);
			OpenDiscordCommand = new AsyncRelayCommand(DoOpenDiscord);
			SubmitFeatureRequestCommand = new AsyncRelayCommand(DoSubmitFeatureRequest);
			SubmitBugReportCommand = new AsyncRelayCommand(DoSubmitBugReport);
			OpenGitHubRepoCommand = new AsyncRelayCommand(DoOpenGitHubRepo);
			OpenPrivacyPolicyCommand = new AsyncRelayCommand(DoOpenPrivacyPolicy);
			OpenLogLocationCommand = new AsyncRelayCommand(OpenLogLocation);
			OpenCrowdinCommand = new AsyncRelayCommand(DoOpenCrowdin);
		}

		// Methods

		private async Task<bool> OpenLogLocation()
		{
			await Launcher.LaunchFolderAsync(ApplicationData.Current.LocalFolder).AsTask();

			// TODO: Move this to an application service
			// Detect if Files is set as the default file manager
			using var subkey = Registry.ClassesRoot.OpenSubKey(@"Folder\shell\open\command");
			var command = (string?)subkey?.GetValue(string.Empty);

			// Close the settings dialog if Files is the deault file manager
			if (!string.IsNullOrEmpty(command) && command.Contains("Files.App.Launcher.exe"))
				UIHelpers.CloseAllDialogs();

			return true;
		}

		public Task DoOpenDocumentation()
		{
			return Launcher.LaunchUriAsync(new Uri(Constants.ExternalUrl.DocumentationUrl)).AsTask();
		}

		public Task DoOpenDiscord()
		{
			return Launcher.LaunchUriAsync(new Uri(Constants.ExternalUrl.DiscordUrl)).AsTask();
		}

		public Task DoSubmitFeatureRequest()
		{
			return Launcher.LaunchUriAsync(new Uri($"{Constants.ExternalUrl.FeatureRequestUrl}&{GetVersionsQueryString()}")).AsTask();
		}

		public Task DoSubmitBugReport()
		{
			return Launcher.LaunchUriAsync(new Uri($"{Constants.ExternalUrl.BugReportUrl}&{GetVersionsQueryString()}")).AsTask();
		}

		public Task DoOpenGitHubRepo()
		{
			return Launcher.LaunchUriAsync(new Uri(Constants.ExternalUrl.GitHubRepoUrl)).AsTask();
		}

		public Task DoOpenPrivacyPolicy()
		{
			return Launcher.LaunchUriAsync(new Uri(Constants.ExternalUrl.PrivacyPolicyUrl)).AsTask();
		}

		public Task DoOpenCrowdin()
		{
			return Launcher.LaunchUriAsync(new Uri(Constants.ExternalUrl.CrowdinUrl)).AsTask();
		}

		public void CopyAppVersion()
		{
			SafetyExtensions.IgnoreExceptions(() =>
			{
				DataPackage dataPackage = new DataPackage();
				dataPackage.RequestedOperation = DataPackageOperation.Copy;
				dataPackage.SetText(GetAppVersion());
				Clipboard.SetContent(dataPackage);
			});
		}

		public void CopyWindowsVersion()
		{
			SafetyExtensions.IgnoreExceptions(() =>
			{
				DataPackage dataPackage = new DataPackage();
				dataPackage.RequestedOperation = DataPackageOperation.Copy;
				dataPackage.SetText(GetWindowsVersion());
				Clipboard.SetContent(dataPackage);
			});
		}
		
		public void CopyUserID()
		{
			SafetyExtensions.IgnoreExceptions(() =>
			{
				DataPackage dataPackage = new DataPackage();
				dataPackage.RequestedOperation = DataPackageOperation.Copy;
				dataPackage.SetText(GetUserID());
				Clipboard.SetContent(dataPackage);
			});
		}

		public Task SupportUs()
		{
			return Launcher.LaunchUriAsync(new Uri(Constants.ExternalUrl.SupportUsUrl)).AsTask();
		}

		public string GetAppVersion()
		{
			return string.Format($"{AppVersion.Major}.{AppVersion.Minor}.{AppVersion.Build}.{AppVersion.Revision}");
		}

		public string GetWindowsVersion()
		{
			return SystemInformation.Instance.OperatingSystemVersion.ToString();
		}
		
		public string GetUserID()
		{
			return GeneralSettingsService.UserId;
		}

		public string GetVersionsQueryString()
		{
			var query = System.Web.HttpUtility.ParseQueryString(string.Empty);
			query["files_version"] = GetAppVersion();
			query["windows_version"] = GetWindowsVersion();
			query["user_id"] = GetUserID();
			return query.ToString() ?? string.Empty;
		}
	}
}
