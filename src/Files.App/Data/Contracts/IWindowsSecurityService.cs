// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides service for security APIs on Windows.
	/// </summary>
	public interface IWindowsSecurityService
	{
		/// <summary>
		/// Gets a value that indicates whether the application is elevated.
		/// </summary>
		/// <returns>Returns true if the application is elevated; otherwise, false.</returns>
		bool IsAppElevated();

		/// <summary>
		/// Gets a value that indicates whether the application can drag &amp; drop.
		/// </summary>
		/// <remarks>
		/// Drag &amp; drop onto an elevated app is not allowed (just crashes) due to UIPI.
		/// <br/>
		/// <br/>
		/// For more info, visit:
		/// <br/>
		/// <a href="https://github.com/files-community/Files/issues/12390"/>
		/// <br/>
		/// <a href="https://github.com/microsoft/terminal/issues/12017#issuecomment-1004129669"/>
		/// </remarks>
		/// <returns>Returns true if the application can drag &amp; drop; otherwise, false.</returns>
		bool CanDragAndDrop();

		/// <summary>
		/// Gets a value that indicates whether the application needs to be elevated for some operations.
		/// </summary>
		/// <param name="path"></param>
		/// <returns>True if the application needs to be elevated for some operations; otherwise, false.</returns>
		bool IsElevationRequired(string path);
	}
}
