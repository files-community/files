// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using Files.Shared.Helpers;

namespace Files.App.ViewModels.Dialogs
{
	public sealed class CreateShortcutDialogViewModel : ObservableObject
	{
		// User's working directory
		public readonly string WorkingDirectory;

		// Placeholder text of destination path textbox
		public readonly string DestinationPlaceholder = $@"{Constants.UserEnvironmentPaths.SystemDrivePath}\Users\";

		// Tells whether destination path exists
		public bool DestinationPathExists { get; set; }

		// Tells whether the shortcut has been created
		public bool ShortcutCreatedSuccessfully { get; private set; }

		// Shortcut name with extension
		public string ShortcutCompleteName { get; private set; } = string.Empty;

		// Full path of the destination item
		public string FullPath { get; private set; }

		// Arguments to be passed to the destination item if it's an executable
		public string Arguments { get; private set; }

		// Previous path of the destination item
		private string _previousShortcutTargetPath;

		private string _shortcutName;
		public string ShortcutName
		{
			get => _shortcutName; 
			set
			{
				if (SetProperty(ref _shortcutName, value))
				{
					OnPropertyChanged(nameof(ShowNameWarningTip));
					OnPropertyChanged(nameof(IsShortcutValid));
				}
			}
		}

		// Destination of the shortcut chosen by the user (can be a path, a command or a URL)
		private string _shortcutTarget;
		public string ShortcutTarget
		{
			get => _shortcutTarget;
			set
			{
				if (!SetProperty(ref _shortcutTarget, value))
					return;

				OnPropertyChanged(nameof(ShowWarningTip));
				if (string.IsNullOrWhiteSpace(ShortcutTarget))
				{
					DestinationPathExists = false;
					IsLocationValid = false;
					_previousShortcutTargetPath = string.Empty;
					return;
				}
				try
				{
					var trimmed = ShortcutTarget.Trim();
					// If the text starts with '"', try to parse the quoted part as path, and the rest as arguments
					if (trimmed.StartsWith('"'))
					{
						var endQuoteIndex = trimmed.IndexOf('"', 1);
						if (endQuoteIndex == -1)
						{
							DestinationPathExists = false;
							IsLocationValid = false;
							_previousShortcutTargetPath = string.Empty;
							return;
						}

						var quoted = trimmed[1..endQuoteIndex];

						if (quoted == _previousShortcutTargetPath)
						{
							Arguments = !Directory.Exists(FullPath) ? trimmed[(endQuoteIndex + 1)..] : string.Empty;
							return;
						}

						if (IsValidAbsolutePath(quoted))
						{
							DestinationPathExists = true;
							IsLocationValid = true;
							FullPath = Path.GetFullPath(quoted);
							Arguments = !Directory.Exists(FullPath) ? trimmed[(endQuoteIndex + 1)..] : string.Empty;
							_previousShortcutTargetPath = quoted;
							return;
						}

						// If the quoted part is a valid filename, try to find it in the PATH
						if (quoted == Path.GetFileName(quoted)
							&& quoted.IndexOfAny(Path.GetInvalidFileNameChars()) == -1
							&& PathHelpers.TryGetFullPath(quoted, out var fullPath))
						{
							DestinationPathExists = true;
							IsLocationValid = true;
							FullPath = fullPath;
							Arguments = trimmed[(endQuoteIndex + 1)..];
							_previousShortcutTargetPath = quoted;
							return;
						}

						var uri = new Uri(quoted);
						DestinationPathExists = false;
						IsLocationValid = uri.IsWellFormedOriginalString();
						FullPath = quoted;
						Arguments = string.Empty;
						_previousShortcutTargetPath = string.Empty;
					}
					else
					{
						var filePath = trimmed.Split(' ')[0];

						if (filePath == _previousShortcutTargetPath)
						{
							Arguments = !Directory.Exists(FullPath) ? trimmed.Split(' ')[1..].Aggregate(string.Empty, (current, arg) => current + arg + " ") : string.Empty;
							return;
						}

						if (IsValidAbsolutePath(filePath))
						{
							DestinationPathExists = true;
							IsLocationValid = true;
							FullPath = Path.GetFullPath(filePath);
							Arguments = !Directory.Exists(FullPath) ? trimmed.Split(' ')[1..].Aggregate(string.Empty, (current, arg) => current + arg + " ") : string.Empty;
							_previousShortcutTargetPath = filePath;
							return;
						}

						// Try to parse the whole text as path
						if (IsValidAbsolutePath(trimmed))
						{
							DestinationPathExists = true;
							IsLocationValid = true;
							FullPath = Path.GetFullPath(trimmed);
							Arguments = string.Empty;
							_previousShortcutTargetPath = string.Empty;
							return;
						}

						if (filePath == Path.GetFileName(filePath)
							&& filePath.IndexOfAny(Path.GetInvalidFileNameChars()) == -1
							&& PathHelpers.TryGetFullPath(filePath, out var fullPath))
						{
							DestinationPathExists = true;
							IsLocationValid = true;
							FullPath = fullPath;
							Arguments = trimmed.Split(' ')[1..].Aggregate(string.Empty, (current, arg) => current + arg + " ");
							_previousShortcutTargetPath = filePath;
							return;
						}

						var uri = new Uri(trimmed);
						DestinationPathExists = false;
						IsLocationValid = uri.IsWellFormedOriginalString();
						FullPath = trimmed;
						Arguments = string.Empty;
						_previousShortcutTargetPath = string.Empty;
					}

				}
				catch (Exception)
				{
					DestinationPathExists = false;
					IsLocationValid = false;
					FullPath = string.Empty;
					Arguments = string.Empty;
					_previousShortcutTargetPath = string.Empty;
				}
				finally
				{
					AutoFillName();
				}
			}
		}

		// Tells if the selected destination is valid (Path exists or URL is well-formed). Used to enable primary button
		private bool _isLocationValid;
		public bool IsLocationValid
		{
			get => _isLocationValid;
			set
			{
				if (SetProperty(ref _isLocationValid, value))
				{ 
					OnPropertyChanged(nameof(ShowWarningTip));
					OnPropertyChanged(nameof(IsShortcutValid));
				}
			}
		}

		public bool ShowWarningTip => !string.IsNullOrEmpty(ShortcutTarget) && !_isLocationValid;

		public bool ShowNameWarningTip => !string.IsNullOrEmpty(_shortcutTarget) && !FilesystemHelpers.IsValidForFilename(_shortcutName);

		public bool IsShortcutValid => _isLocationValid && !ShowNameWarningTip && !string.IsNullOrEmpty(_shortcutTarget);

		// Command invoked when the user clicks the 'Browse' button
		public ICommand SelectDestinationCommand { get; private set; }

		// Command invoked when the user clicks primary button
		public ICommand PrimaryButtonCommand { get; private set; }

		public CreateShortcutDialogViewModel(string workingDirectory)
		{
			WorkingDirectory = workingDirectory;
			_shortcutTarget = string.Empty;

			SelectDestinationCommand = new AsyncRelayCommand(SelectDestination);
			PrimaryButtonCommand = new AsyncRelayCommand(CreateShortcutAsync);
		}

		private bool IsValidAbsolutePath(string path)
		{
			return Path.Exists(path) && Path.IsPathFullyQualified(path) && path != Path.GetPathRoot(path);
		}

		private Task SelectDestination()
		{
			Win32PInvoke.BROWSEINFO bi = new Win32PInvoke.BROWSEINFO();
			bi.ulFlags = 0x00004000;
			bi.lpszTitle = "Select a folder";
			nint pidl = Win32PInvoke.SHBrowseForFolder(ref bi);
			if (pidl != nint.Zero)
			{
				StringBuilder path = new StringBuilder(260);
				if (Win32PInvoke.SHGetPathFromIDList(pidl, path))
				{
					ShortcutTarget = path.ToString();
				}
				Marshal.FreeCoTaskMem(pidl);
			}

			return Task.CompletedTask;
		}

		private async Task CreateShortcutAsync()
		{
			var extension = DestinationPathExists ? ".lnk" : ".url";

			var shortcutName = FilesystemHelpers.GetShortcutNamingPreference(_shortcutName);
			ShortcutCompleteName = shortcutName + extension;
			var filePath = Path.Combine(WorkingDirectory, ShortcutCompleteName);

			int fileNumber = 1;
			while (Path.Exists(filePath))
			{
				ShortcutCompleteName = shortcutName + $" ({++fileNumber})" + extension;
				filePath = Path.Combine(WorkingDirectory, ShortcutCompleteName);
			}

			ShortcutCreatedSuccessfully = await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, FullPath, Arguments);
		}

		private void AutoFillName()
		{
			if (DestinationPathExists)
			{
				var destinationName = Path.GetFileName(FullPath);
				if (DestinationPathExists)
				{
					destinationName = Path.GetFileName(FullPath);

					if (string.IsNullOrEmpty(FullPath))
					{

						var destinationPath = FullPath.Replace('/', '\\');

						if (destinationPath.EndsWith('\\'))
							destinationPath = destinationPath.Substring(0, destinationPath.Length - 1);

						destinationName = destinationPath.Substring(destinationPath.LastIndexOf('\\') + 1);
					}
				}
				ShortcutName = destinationName;
			}
			else if (!string.IsNullOrEmpty(FullPath))
			{
				var uri = new Uri(FullPath);
				ShortcutName = uri.Host;
			}
		}
	}
}