// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents contract for compatibility mode service for Windows.
	/// </summary>
	public interface IWindowsCompatibilityService
	{
		/// <summary>
		/// Gets compatibility options for path.
		/// </summary>
		/// <param name="filePath">The path to get options.</param>
		/// <returns>Returns an instance of<see cref="WindowsCompatibilityOptions"/> contains options for the path.</returns>
		public WindowsCompatibilityOptions GetCompatibilityOptionsForPath(string filePath);

		/// <summary>
		/// Sets compatibility options for path.
		/// </summary>
		/// <param name="filePath">The path to set options.</param>
		/// <param name="options">The options to set.</param>
		/// <returns>Returns true if succeed; otherwise, false.</returns>
		public bool SetCompatibilityOptionsForPath(string filePath, WindowsCompatibilityOptions options);
	}
}
