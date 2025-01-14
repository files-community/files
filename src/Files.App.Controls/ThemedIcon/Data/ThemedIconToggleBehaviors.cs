// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	/// <summary>
	/// Defines IconTypes for <see cref="ThemedIcon"/>.
	/// </summary>
	public enum ToggleBehaviors
	{
		/// <summary>
		/// Auto enables the ThemedIcon to listen to owner control states.
		/// </summary>
		Auto,

		/// <summary>
		/// On will always use the ThemedIcon's Toggle state
		/// </summary>
		On,

		/// <summary>
		/// Off will not use the ThemedIcon's Toggle state
		/// </summary>
		Off,
	}
}
