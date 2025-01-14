// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Cloud;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides an utility for generic cloud detection.
	/// </summary>
	public sealed class GenericCloudDetector : AbstractCloudDetector
	{
		protected override async IAsyncEnumerable<ICloudProvider> GetProviders()
		{
			foreach (var provider in await CloudDrivesDetector.DetectCloudDrives())
			{
				yield return provider;
			}
		}
	}
}
