// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides service for manipulating shell wallpapers on Windows.
	/// </summary>
	public interface IWindowsWallpaperService
	{
		/// <summary>
		/// Sets desktop wallpaper using the specified image path.
		/// </summary>
		/// <param name="szPath">The image path to assign as the desktop wallpaper.</param>
		/// <exception cref="System.Runtime.InteropServices.COMException">Thrown if any of COM methods failed to process successfully.</exception>
		void SetDesktopWallpaper(string szPath);

		/// <summary>
		/// Sets desktop slideshow using the specified image paths.
		/// </summary>
		/// <param name="aszPaths">The image paths to use to set as slideshow.</param>
		/// <exception cref="System.Runtime.InteropServices.COMException">Thrown if any of COM methods failed to process successfully.</exception>
		void SetDesktopSlideshow(string[] aszPaths);

		/// <summary>
		/// Gets lock screen wallpaper using the specified image path.
		/// </summary>
		/// <param name="szPath">The image path to use to set as lock screen wallpaper.</param>
		/// <exception cref="System.Runtime.InteropServices.COMException">Thrown if any of COM methods failed to process successfully.</exception>
		Task SetLockScreenWallpaper(string szPath);
	}
}
