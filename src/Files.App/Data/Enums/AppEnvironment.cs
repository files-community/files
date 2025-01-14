// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Defines constants that specify application distribution type.
	/// </summary>
	/// <remarks>
	/// Those type names are mostly corresponded to build configurations.
	/// </remarks>
	public enum AppEnvironment
	{
		/// <summary>
		/// Application distribution type is Dev.
		/// </summary>
		Dev,

		/// <summary>
		/// Application distribution type is Sideload Stable.
		/// </summary>
		SideloadStable,

		/// <summary>
		/// Application distribution type is Store Stable.
		/// </summary>
		StoreStable,

		/// <summary>
		/// Application distribution type is Sideload Preview.
		/// </summary>
		SideloadPreview,

		/// <summary>
		/// Application distribution type is Store Preview.
		/// </summary>
		StorePreview
	}
}
