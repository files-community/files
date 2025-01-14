// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Foundation.Metadata;
using Windows.System;

namespace Files.App.Utils.Storage
{
	internal sealed class StorageSenseHelper
	{
		public static async Task OpenStorageSenseAsync(string path)
		{
			if (!path.StartsWith(Constants.UserEnvironmentPaths.SystemDrivePath, StringComparison.OrdinalIgnoreCase)
				&& ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				LaunchHelper.LaunchSettings("page=SettingsPageStorageSenseStorageOverview&target=SystemSettings_StorageSense_VolumeListLink");
			}
			else
			{
				await Launcher.LaunchUriAsync(new Uri("ms-settings:storagesense"));
			}
		}
	}
}
