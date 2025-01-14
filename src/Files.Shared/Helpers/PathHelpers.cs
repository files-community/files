// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;

namespace Files.Shared.Helpers
{
	public static class PathHelpers
	{
		public static string Combine(string folder, string name)
		{
			if (string.IsNullOrEmpty(folder))
				return name;

			return folder.Contains('/') ? Path.Combine(folder, name).Replace('\\', '/') : Path.Combine(folder, name);
		}

		public static string FormatName(string path)
		{
			string fileName;
			string rootPath = Path.GetPathRoot(path) ?? string.Empty;

			if (rootPath == path && path.StartsWith(@"\\"))
			{
				// Network Share path
				fileName = path.Substring(path.LastIndexOf(@"\", StringComparison.Ordinal) + 1);
			}
			else if (rootPath == path)
			{
				// Drive path
				fileName = path;
			}
			else
			{
				// Standard file name
				fileName = Path.GetFileName(path);
			}

			// Check for link file name
			if (FileExtensionHelpers.IsShortcutOrUrlFile(fileName))
				fileName = fileName.Remove(fileName.Length - 4);

			return fileName;
		}

		/// <summary>
		/// Determines whether the <paramref name="path"/> points to any special system folders.
		/// </summary>
		/// <remarks>
		///	The term "Special folder" refers to any folders that may be natively supported by a certain platform (e.g. Libraries).
		/// </remarks>
		/// <param name="path">The path to a folder to check.</param>
		/// <returns>If the path points to a special folder, returns true; otherwise false.</returns>
		public static bool IsSpecialFolder(string path)
		{
			foreach (Environment.SpecialFolder specialFolder in Enum.GetValues(typeof(Environment.SpecialFolder)))
			{
				var specialFolderPath = Environment.GetFolderPath(specialFolder);
				if (string.Equals(specialFolderPath, path, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}

		public static bool TryGetFullPath(string commandName, out string fullPath)
		{
			fullPath = string.Empty;
			try
			{
				var p = new Process();
				p.StartInfo = new ProcessStartInfo
				{
					UseShellExecute = false,
					CreateNoWindow = true,
					FileName = "where.exe",
					Arguments = commandName,
					RedirectStandardOutput = true
				};
				p.Start();
				var output = p.StandardOutput.ReadToEnd();
				p.WaitForExit(1000);


				if (p.ExitCode != 0)
					return false;

				// Return the first one with valid executable extension, in case there is a match with no extension
				foreach (var line in output.Split(Environment.NewLine))
				{
					if (FileExtensionHelpers.IsExecutableFile(line))
					{
						fullPath = line;
						return true;
					}
				}
				return false;
			}
			catch (Exception)
			{
				return false;
			}

		}
	}
}
