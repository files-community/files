// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsCompatibilityService"/>
	public sealed class WindowsCompatibilityService : IWindowsCompatibilityService
	{
		private readonly string _registrySubPath = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers";

		/// <inheritdoc/>
		public WindowsCompatibilityOptions GetCompatibilityOptionsForPath(string filePath)
		{
			try
			{
				// Get the key
				using var compatKey = Registry.CurrentUser.OpenSubKey(_registrySubPath);
				if (compatKey is null)
					return new();

				// Get the value for the specified path
				var stringOptions = (string?)compatKey.GetValue(filePath, null);

				return WindowsCompatibilityOptions.FromString(stringOptions);
			}
			catch (Exception)
			{
				return new();
			}
		}

		/// <inheritdoc/>
		public bool SetCompatibilityOptionsForPath(string filePath, WindowsCompatibilityOptions options)
		{
			var stringOptions = options.ToString();

			// Remove old one if new one is valid
			if (string.IsNullOrEmpty(stringOptions) || stringOptions == "~")
			{
				return Win32Helper.RunPowershellCommand(
					@$"Remove-ItemProperty -Path 'HKCU:\{_registrySubPath}' -Name '{filePath}' | Out-Null",
					PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
			}

			// Set the new one
			return Win32Helper.RunPowershellCommand(
				@$"New-ItemProperty -Path 'HKCU:\{_registrySubPath}' -Name '{filePath}' -Value '{options}' -PropertyType String -Force | Out-Null",
				PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
		}
	}
}
