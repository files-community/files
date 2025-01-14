// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Drawing;

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Contains methods related to modifying various app theme resources
	/// </summary>
	public interface IResourcesService
	{
		/// <summary>
		/// Applies updated resource styles
		/// </summary>
		void ApplyResources();

		/// <summary>
		/// Overrides the XAML resource for App.Theme.BackgroundBrush
		/// </summary>
		/// <param name="appThemeBackgroundColor"></param>
		void SetAppThemeBackgroundColor(Color appThemeBackgroundColor);

		/// <summary>
		/// Overrides the XAML resource for App.Theme.AddressBar.BackgroundBrush
		/// </summary>
		/// <param name="appThemeAddressBarBackgroundColor"></param>
		void SetAppThemeAddressBarBackgroundColor(Color appThemeAddressBarBackgroundColor);
		
		/// <summary>
		/// Overrides the XAML resource for App.Theme.Toolbar.BackgroundBrush
		/// </summary>
		/// <param name="appThemeToolbarBackgroundColor"></param>
		void SetAppThemeToolbarBackgroundColor(Color appThemeToolbarBackgroundColor);

		/// <summary>
		/// Overrides the XAML resource for App.Theme.Sidebar.BackgroundBrush
		/// </summary>
		/// <param name="appThemeSidebarBackgroundColor"></param>
		void SetAppThemeSidebarBackgroundColor(Color appThemeSidebarBackgroundColor);

		/// <summary>
		/// Overrides the XAML resource for App.Theme.FileArea.BackgroundBrush
		/// </summary>
		/// <param name="appThemeFileAreaBackgroundColor"></param>
		void SetAppThemeFileAreaBackgroundColor(Color appThemeFileAreaBackgroundColor);

		/// <summary>
		/// Overrides the XAML resource for App.Theme.FileArea.SecondaryBackgroundBrush
		/// </summary>
		/// <param name="appThemeFileAreaSecondaryBackgroundColor"></param>
		void SetAppThemeFileAreaSecondaryBackgroundColor(Color appThemeFileAreaSecondaryBackgroundColor);

		/// <summary>
		/// Overrides the XAML resource for App.Theme.InfoPane.BackgroundBrush
		/// </summary>
		/// <param name="appThemeInfoPaneBackgroundColor"></param>
		void SetAppThemeInfoPaneBackgroundColor(Color appThemeInfoPaneBackgroundColor);

		/// <summary>
		/// Overrides the XAML resource for ContentControlThemeFontFamily
		/// </summary>
		/// <param name="contentControlThemeFontFamily"></param>
		void SetAppThemeFontFamily(string contentControlThemeFontFamily);
	}
}
