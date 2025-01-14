// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using static Files.App.Helpers.Win32Helper;
using static Files.App.Helpers.Win32PInvoke;

namespace Files.App.Utils.Serialization.Implementation
{
	internal sealed class DefaultSettingsSerializer : ISettingsSerializer
	{
		private string? _filePath;

		public bool CreateFile(string path)
		{
			PInvoke.CreateDirectoryFromApp(Path.GetDirectoryName(path), null);

			var hFile = CreateFileFromApp(path, (uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ, FILE_SHARE_READ, IntPtr.Zero, OPEN_ALWAYS, (uint)File_Attributes.BackupSemantics, IntPtr.Zero);
			if (hFile.IsHandleInvalid())
			{
				return false;
			}

			Win32PInvoke.CloseHandle(hFile);

			_filePath = path;
			return true;
		}

		/// <summary>
		/// Reads a file to a string
		/// </summary>
		/// <returns>A string value or string.Empty if nothing is present in the file</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public string ReadFromFile()
		{
			_ = _filePath ?? throw new ArgumentNullException(nameof(_filePath));

			return ReadStringFromFile(_filePath);
		}

		public bool WriteToFile(string? text)
		{
			_ = _filePath ?? throw new ArgumentNullException(nameof(_filePath));

			return WriteStringToFile(_filePath, text);
		}
	}
}
