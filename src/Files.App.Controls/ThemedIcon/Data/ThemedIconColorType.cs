// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	/// <summary>
	/// Defines the IconColorTypes for <see cref="ThemedIcon"/> which sets the visual state
	/// to use the correct brush values which match system signal colors.
	/// </summary>
	public enum ThemedIconColorType
	{
		None,

		/// <summary>
		/// Icon color type of <see cref="ThemedIcon"/> is Normal. Default Value.
		/// </summary>
		Normal,

		/// <summary>
		/// Icon color type of <see cref="ThemedIcon"/> is Critical.
		/// </summary>
		Critical,

		/// <summary>
		/// Icon color type of <see cref="ThemedIcon"/> is Caution.
		/// </summary>
		Caution,

		/// <summary>
		/// Icon color type of <see cref="ThemedIcon"/> is Success.
		/// </summary>
		Success,

		/// <summary>
		/// Icon color type of <see cref="ThemedIcon"/> is Neutral.
		/// </summary>
		Neutral,

		/// <summary>
		/// Icon color type of <see cref="ThemedIcon"/> is Accent.
		/// </summary>
		Accent,

		/// <summary>
		/// Icon color type of <see cref="ThemedIcon"/> is Custom. Used in combination
		/// with the IconColor and Foreground brushes.
		/// </summary>
		Custom
	}
}
