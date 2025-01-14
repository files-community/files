// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services
{
	/// <inheritdoc cref="IWindowsSecurityService"/>
	public sealed class WindowsSecurityService : IWindowsSecurityService
	{
		/// <inheritdoc/>
		public unsafe bool IsAppElevated()
		{
			var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
			var principal = new System.Security.Principal.WindowsPrincipal(identity);
			return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
		}

		/// <inheritdoc/>
		public unsafe bool CanDragAndDrop()
		{
			return !IsAppElevated();
		}

		/// <inheritdoc/>
		public bool IsElevationRequired(string path)
		{
			if (string.IsNullOrEmpty(path))
				return false;

			return Win32PInvoke.IsElevationRequired(path);
		}
	}
}
