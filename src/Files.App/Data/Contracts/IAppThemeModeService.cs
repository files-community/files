// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace Files.App.Data.Contracts
{
	public interface IAppThemeModeService
	{
		/// <summary>
		/// Gets invoked when application theme mode is changed.
		/// </summary>
		public event EventHandler? AppThemeModeChanged;

		/// <summary>
		/// Gets or sets application theme mode.
		/// </summary>
		public ElementTheme AppThemeMode { get; set; }

		/// <summary>
		/// Gets the default accent fill color for the current theme mode.
		/// </summary>
		public Color DefaultAccentColor { get; }

		/// <summary>
		/// Refreshes the application theme mode only for the main window.
		/// </summary>
		/// <remarks>
		/// This is a workaround for <a href="https://github.com/microsoft/microsoft-ui-xaml/issues/4651">a WinUI bug</a>.
		/// </remarks>
		public void ApplyResources();

		/// <summary>
		/// Sets application theme mode to the main window or a specific window.
		/// </summary>
		/// <param name="window">A window instance to set application theme mode.</param>
		/// <param name="titleBar">A window's title bar instance to adjust contrast of action buttons.</param>
		/// <param name="rootTheme">A theme mode to set.</param>
		/// <param name="callThemeModeChangedEvent">Whether the method should notify the theme mode changed. If this called for a specific window, doesn't have to call.</param>
		public void SetAppThemeMode(Window? window = null, AppWindowTitleBar? titleBar = null, ElementTheme? rootTheme = null, bool callThemeModeChangedEvent = true);
	}
}
