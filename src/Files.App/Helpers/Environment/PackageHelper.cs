// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.System;

namespace Files.App.Helpers
{
	public static class PackageHelper
	{
		private static readonly Uri dummyUri = new Uri("mailto:dummy@dummy.com");

		/// <summary>
		/// Check if target <paramref name="packageName"/> is installed on this device.
		/// </summary>
		/// <param name="packageName">Package name in format: "949FFEAB.Email.cz_refxrrjvvv3cw"</param>
		/// <returns>True is app is installed on this device, false otherwise.</returns>
		public static async Task<bool> IsAppInstalledAsync(string packageName)
		{
			try
			{
				bool appInstalled;
				LaunchQuerySupportStatus result = await Launcher.QueryUriSupportAsync(dummyUri, LaunchQuerySupportType.Uri, packageName);
				switch (result)
				{
					case LaunchQuerySupportStatus.Available:
					case LaunchQuerySupportStatus.NotSupported:
						appInstalled = true;
						break;
					//case LaunchQuerySupportStatus.AppNotInstalled:
					//case LaunchQuerySupportStatus.AppUnavailable:
					//case LaunchQuerySupportStatus.Unknown:
					default:
						appInstalled = false;
						break;
				}

				Debug.WriteLine($"App {packageName}, query status: {result}, installed: {appInstalled}");
				return appInstalled;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error checking if app {packageName} is installed. Error: {ex}");
				return false;
			}
		}
	}
}