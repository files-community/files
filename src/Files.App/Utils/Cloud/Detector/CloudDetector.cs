// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Cloud;

namespace Files.App.Utils.Cloud
{
	/// <summary>
	/// Provides an utility for cloud detection.
	/// </summary>
	public sealed class CloudDetector : ICloudDetector
	{
		public async Task<IEnumerable<ICloudProvider>> DetectCloudProvidersAsync()
		{
			var tasks = new List<Task<IEnumerable<ICloudProvider>>>();

			foreach (var detector in EnumerateDetectors())
				tasks.Add(detector.DetectCloudProvidersAsync());

			await Task.WhenAll(tasks);

			return tasks
				.SelectMany(task => task.Result)
				.OrderBy(task => task.ID.ToString())
				.ThenBy(task => task.Name)
				.Distinct();
		}

		private static IEnumerable<ICloudDetector> EnumerateDetectors()
		{
			yield return new GoogleDriveCloudDetector();
			yield return new DropBoxCloudDetector();
			yield return new BoxCloudDetector();
			yield return new GenericCloudDetector();
			yield return new SynologyDriveCloudDetector();
			yield return new LucidLinkCloudDetector();
		}
	}
}
