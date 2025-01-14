// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using System.Security;

namespace Files.App.Helpers
{
	internal static class SoftwareHelpers
	{
		private const string UninstallRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
		private const string VsRegistryKey = @"SOFTWARE\Microsoft\VisualStudio";

		private const string VsCodeName = "Microsoft Visual Studio Code";


		public static bool IsVSCodeInstalled()
		{
			try
			{
				return
					ContainsName(Registry.CurrentUser.OpenSubKey(UninstallRegistryKey), VsCodeName) ||
					ContainsName(Registry.LocalMachine.OpenSubKey(UninstallRegistryKey), VsCodeName);
			}
			catch (SecurityException)
			{
				// Handle edge case where OpenSubKey results in SecurityException
				return false;
			}
		}

		public static bool IsVSInstalled()
		{
			try
			{
				var key = Registry.LocalMachine.OpenSubKey(VsRegistryKey);
				if (key is null)
					return false;

				key.Close();

				return true;
			}
			catch (SecurityException)
			{
				// Handle edge case where OpenSubKey results in SecurityException
				return false;
			}
		}

		private static bool ContainsName(RegistryKey? key, string find)
		{
			if (key is null)
				return false;

			try
			{
				foreach (var subKey in key.GetSubKeyNames().Select(key.OpenSubKey))
				{
					var displayName = subKey?.GetValue("DisplayName") as string;
					if (!string.IsNullOrWhiteSpace(displayName) && displayName.StartsWith(find))
					{
						key.Close();

						return true;
					}
				}

				key.Close();

				return false;
			}
			catch (SecurityException)
			{
				// Handle edge case where OpenSubKey results in SecurityException
				return false;
			}
		}
	}
}
