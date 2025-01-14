// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using static Files.App.Helpers.Win32Helper;

namespace Files.App.Utils.Storage
{
	public static class FolderHelpers
	{
		public static bool CheckFolderAccessWithWin32(string path)
		{
			IntPtr hFileTsk = Win32PInvoke.FindFirstFileExFromApp($"{path}{Path.DirectorySeparatorChar}*.*", Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic,
				out Win32PInvoke.WIN32_FIND_DATA _, Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH);
			if (hFileTsk.ToInt64() != -1)
			{
				Win32PInvoke.FindClose(hFileTsk);
				return true;
			}
			return false;
		}

		public static async Task<bool> CheckBitlockerStatusAsync(BaseStorageFolder rootFolder, string path)
		{
			if (rootFolder?.Properties is null)
			{
				return false;
			}
			if (Path.IsPathRooted(path) && Path.GetPathRoot(path) == path)
			{
				IDictionary<string, object> extraProperties =
					await rootFolder.Properties.RetrievePropertiesAsync(["System.Volume.BitLockerProtection"]);
				return (int?)extraProperties["System.Volume.BitLockerProtection"] == 6; // Drive is bitlocker protected and locked
			}
			return false;
		}

		/// <summary>
		/// This function is used to determine whether or not a folder has any contents.
		/// </summary>
		/// <param name="targetPath">The path to the target folder</param>
		///
		public static bool CheckForFilesFolders(string targetPath)
		{
			IntPtr hFile = Win32PInvoke.FindFirstFileExFromApp($"{targetPath}{Path.DirectorySeparatorChar}*.*", Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic,
				out Win32PInvoke.WIN32_FIND_DATA _, Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH);
			Win32PInvoke.FindNextFile(hFile, out _);
			var result = Win32PInvoke.FindNextFile(hFile, out _);
			Win32PInvoke.FindClose(hFile);
			return result;
		}
	}
}
