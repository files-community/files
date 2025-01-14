// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Data.Contracts
{
	public interface IAppearanceSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		#region Internal Settings

		/// <summary>
		/// Gets or sets a value indicating the width of the sidebar pane when open.
		/// </summary>
		double SidebarWidth { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if the sidebar pane should be open or closed.
		/// </summary>
		bool IsSidebarOpen { get; set; }

		#endregion

		/// <summary>
		/// Gets or sets a value for the app theme mode.
		/// </summary>
		string AppThemeMode { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme background color.
		/// </summary>
		String AppThemeBackgroundColor { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme address bar background color.
		/// </summary>
		String AppThemeAddressBarBackgroundColor { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme toolbar background color.
		/// </summary>
		String AppThemeToolbarBackgroundColor { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme sidebar background color.
		/// </summary>
		String AppThemeSidebarBackgroundColor { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme file area background color.
		/// </summary>
		String AppThemeFileAreaBackgroundColor { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme file area background color for the inactive pane.
		/// </summary>
		String AppThemeFileAreaSecondaryBackgroundColor { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme info pane background color.
		/// </summary>
		String AppThemeInfoPaneBackgroundColor { get; set; }

		/// <summary>
		/// Gets or sets a value for the app theme font family.
		/// </summary>
		String AppThemeFontFamily { get; set; }

		/// <summary>
		/// Gets or sets a value for the theme system backdrop.
		/// </summary>
		BackdropMaterialType AppThemeBackdropMaterial { get; set; }

		/// <summary>
		/// Gets or sets a value for the app background image source
		/// </summary>
		string AppThemeBackgroundImageSource { get; set; }

		/// <summary>
		/// Gets or sets a value for the app background image fit.
		/// </summary>
		Stretch AppThemeBackgroundImageFit { get; set; }

		/// <summary>
		/// Gets or sets a value for the app background image opacity.
		/// </summary>
		float AppThemeBackgroundImageOpacity { get; set; }

		/// <summary>
		/// Gets or sets a value for the app background image Vertical Alignment.
		/// </summary>
		VerticalAlignment AppThemeBackgroundImageVerticalAlignment { get; set; }

		/// <summary>
		/// Gets or sets a value for the app background image Horizontal Alignment.
		/// </summary>
		HorizontalAlignment AppThemeBackgroundImageHorizontalAlignment { get; set; }

		/// <summary>
		/// Gets or sets a value whether the toolbar should be displayed.
		/// </summary>
		bool ShowToolbar { get; set; }
		
		/// <summary>
		/// Gets or sets a value whether the tab actions button should be displayed.
		/// </summary>
		bool ShowTabActions { get; set; }

		/// <summary>
		/// Gets or sets a value whether the home button should be displayed.
		/// </summary>
		bool ShowHomeButton { get; set; }

		/// <summary>
		/// Gets or sets a value whether the shelf pane toggle button should be displayed.
		/// </summary>
		bool ShowShelfPaneToggleButton{ get; set; }
	}
}
