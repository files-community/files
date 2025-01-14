// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides a service to get or set a friendly name of a storage object.
	/// </summary>
	internal interface IStorageCacheService
	{
		/// <summary>
		/// Gets friendly name of the specified storage object.
		/// </summary>
		/// <param name="path">Path indicates the storage object to get its friendly name.</param>
		/// <param name="cancellationToken">An instance of CancellationToken.</param>
		/// <returns></returns>
		public ValueTask<string> GetDisplayName(string path, CancellationToken cancellationToken);

		/// <summary>
		/// Adds friendly name of the specified storage object.
		/// </summary>
		/// <remarks>
		/// If <paramref name="displayName"/> is null, removes.
		/// </remarks>
		/// <param name="path">Path to get its friendly name.</param>
		/// <param name="displayName">Friendly name for the storage object specified by the path.</param>
		/// <returns></returns>
		public ValueTask AddDisplayName(string path, string? displayName);
	}
}
