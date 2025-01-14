// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Linq;

namespace Files.Shared.Helpers
{
	/// <summary>
	/// Provides static extension for path extension.
	/// </summary>
	public static class FileExtensionHelpers
	{
		/// <summary>
		/// Check if the file extension matches one of the specified extensions.
		/// </summary>
		/// <param name="filePathToCheck">Path or name or extension of the file to check.</param>
		/// <param name="extensions">List of the extensions to check.</param>
		/// <returns><c>true</c> if the filePathToCheck has one of the specified extensions; otherwise, <c>false</c>.</returns>
		public static bool HasExtension(string? filePathToCheck, params string[] extensions)
		{
			if (string.IsNullOrWhiteSpace(filePathToCheck))
				return false;

			return extensions.Any(ext => filePathToCheck.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Check if the file extension is an image file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is an image; otherwise, <c>false</c>.</returns>
		public static bool IsImageFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".png", ".bmp", ".jpg", ".jpeg", ".jfif", ".gif", ".tiff", ".tif", ".webp");
		}

		/// <summary>
		/// Checks if the file can be set as wallpaper.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is an image; otherwise, <c>false</c>.</returns>
		public static bool IsCompatibleToSetAsWindowsWallpaper(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".png", ".bmp", ".jpg", ".jpeg", ".jfif", ".gif", ".tiff", ".tif");
		}

		/// <summary>
		/// Check if the file extension is an audio file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is an audio file; otherwise, <c>false</c>.</returns>
		public static bool IsAudioFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".mp3", ".m4a", ".wav", ".wma", ".aac", ".adt", ".adts", ".cda", ".flac");
		}
		
		/// <summary>
		/// Check if the file extension is a video file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a video file; otherwise, <c>false</c>.</returns>
		public static bool IsVideoFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".mp4", ".webm", ".ogg", ".mov", ".qt", ".m4v", ".mp4v", ".3g2", ".3gp2", ".3gp", ".3gpp", ".mkv");
		}

		/// <summary>
		/// Check if the file extension is a PowerShell script.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a PowerShell script; otherwise, <c>false</c>.</returns>
		public static bool IsPowerShellFile(string fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".ps1");
		}

		/// <summary>
		/// Check if the file extension is a Batch file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a Batch file; otherwise, <c>false</c>.</returns>
		public static bool IsBatchFile(string fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".bat");
		}

		/// <summary>
		/// Check if the file extension is a zip file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a zip bundle file; otherwise, <c>false</c>.</returns>
		public static bool IsZipFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".zip", ".msix", ".appx", ".msixbundle", ".appxbundle", ".7z", ".rar", ".tar", ".mcpack", ".mcworld", ".mrpack", ".jar", ".gz", ".lzh");
		}

		public static bool IsBrowsableZipFile(string? filePath, out string? ext)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				ext = null;

				return false;
			}

			// Only extensions we want to browse
			ext = new[] { ".zip", ".7z", ".rar", ".tar", ".gz", ".lzh", ".mrpack", ".jar" }
				.FirstOrDefault(x => filePath.Contains(x, StringComparison.OrdinalIgnoreCase));

			return ext is not null;
		}

		/// <summary>
		/// Check if the file extension is a driver inf file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is an inf file; otherwise <c>false</c>.</returns>
		public static bool IsInfFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".inf");
		}

		/// <summary>
		/// Check if the file extension is a font file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a font file; otherwise <c>false</c>.</returns>
		/// <remarks>Font file types are; fon, otf, ttc, ttf</remarks>
		public static bool IsFontFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".fon", ".otf", ".ttc", ".ttf");
		}

		/// <summary>
		/// Check if the file path is a shortcut file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the filePathToCheck is a shortcut file; otherwise, <c>false</c>.</returns>
		/// <remarks>Shortcut file type is .lnk</remarks>
		public static bool IsShortcutFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".lnk");
		}

		/// <summary>
		/// Check if the file path is a web link file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the filePathToCheck is a web link file; otherwise, <c>false</c>.</returns>
		/// <remarks>Web link file type is .url</remarks>
		public static bool IsWebLinkFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".url");
		}

		public static bool IsShortcutOrUrlFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".lnk", ".url");
		}

		/// <summary>
		/// Check if the file path is an executable file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the filePathToCheck is an executable file; otherwise, <c>false</c>.</returns>
		/// /// <remarks>Executable file types are; exe, bat, cmd</remarks>
		public static bool IsExecutableFile(string? filePathToCheck, bool exeOnly = false)
		{
			return
				exeOnly
					? HasExtension(filePathToCheck, ".exe")
					: HasExtension(filePathToCheck, ".exe", ".bat", ".cmd", ".ahk");
		}

		/// <summary>
		/// Check if the file path is an Auto Hot Key file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the filePathToCheck is an Auto Hot Key file; otherwise, <c>false</c>.</returns>
		public static bool IsAhkFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".ahk");
		}

		/// <summary>
		/// Check if the file path is a cmd file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the filePathToCheck is a cmd file; otherwise, <c>false</c>.</returns>
		public static bool IsCmdFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".cmd");
		}

		/// <summary>
		/// Check if the file path is an msi installer file.
		/// </summary>
		/// <param name="filePathToCheck">The file path to check.</param>
		/// <returns><c>true</c> if the filePathToCheck is an msi installer file; otherwise, <c>false</c>.</returns>
		public static bool IsMsiFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".msi");
		}

		/// <summary>
		/// Check if the file extension is a vhd disk file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a vhd disk file; otherwise, <c>false</c>.</returns>
		/// <remarks>Vhd disk file types are; vhd, vhdx</remarks>
		public static bool IsVhdFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".vhd", ".vhdx");
		}
		
		/// <summary>
		/// Check if the file extension is a screen saver file.
		/// </summary>
		/// <param name="fileExtensionToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the fileExtensionToCheck is a screen saver file; otherwise, <c>false</c>.</returns>
		/// <remarks>Screen saver file types are; scr</remarks>
		public static bool IsScreenSaverFile(string? fileExtensionToCheck)
		{
			return HasExtension(fileExtensionToCheck, ".scr");
		}

		/// <summary>
		/// Check if the file extension is a media (audio/video) file.
		/// </summary>
		/// <param name="filePathToCheck">The file extension to check.</param>
		/// <returns><c>true</c> if the filePathToCheck is a media file; otherwise, <c>false</c>.</returns>
		public static bool IsMediaFile(string? filePathToCheck)
		{
			return HasExtension(
				filePathToCheck, ".mp4", ".m4v", ".mp4v", ".3g2", ".3gp2", ".3gp", ".3gpp",
				".mpg", ".mp2", ".mpeg", ".mpe", ".mpv", ".mkv", ".ogg", ".avi", ".wmv", ".mov", ".qt");
		}

		/// <summary>
		/// Check if the file extension is a certificate file.
		/// </summary>
		/// <param name="filePathToCheck"></param>
		/// <returns><c>true</c> if the filePathToCheck is a certificate file; otherwise, <c>false</c>.</returns>
		public static bool IsCertificateFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".cer", ".crt", ".der", ".pfx");
		}

		/// <summary>
		/// Check if the file extension is a Script file.
		/// </summary>
		/// <param name="filePathToCheck"></param>
		/// <returns><c>true</c> if the filePathToCheck is a script file; otherwise, <c>false</c>.</returns>
		public static bool IsScriptFile(string? filePathToCheck)
		{
			return HasExtension(filePathToCheck, ".py", ".ahk");
		}

	}
}
