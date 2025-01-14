// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Files.App.Services;
using Files.App.Services.Settings;
using System;

namespace Files.App.Helpers
{
	public static class AppThemeResourcesHelper
	{
		/// <summary>
		/// Loads the resource styles from settings
		/// </summary>
		public static void LoadAppResources(this IResourcesService service, IAppearanceSettingsService appearance)
		{
			var appThemeBackgroundColor = appearance.AppThemeBackgroundColor;
			var appThemeAddressBarBackgroundColor = appearance.AppThemeAddressBarBackgroundColor;
			var appThemeToolbarBackgroundColor = appearance.AppThemeToolbarBackgroundColor;
			var appThemeSidebarBackgroundColor = appearance.AppThemeSidebarBackgroundColor;
			var appThemeFileAreaBackgroundColor = appearance.AppThemeFileAreaBackgroundColor;
			var appThemeFileAreaSecondaryBackgroundColor = appearance.AppThemeFileAreaSecondaryBackgroundColor;
			var appThemeInfoPaneBackgroundColor = appearance.AppThemeInfoPaneBackgroundColor;
			var appThemeFontFamily = appearance.AppThemeFontFamily;

			try
			{
				service.SetAppThemeBackgroundColor(ColorHelper.ToColor(appThemeBackgroundColor).FromWindowsColor());
			}
			catch
			{
				appearance.AppThemeBackgroundColor = "#00000000"; // reset to default
				service.SetAppThemeBackgroundColor(ColorHelper.ToColor("#00000000").FromWindowsColor());
			}

			if (!string.IsNullOrWhiteSpace(appThemeAddressBarBackgroundColor))
			{
				try
				{
					service.SetAppThemeAddressBarBackgroundColor(ColorHelper.ToColor(appThemeAddressBarBackgroundColor).FromWindowsColor());
				}
				catch
				{
					appearance.AppThemeAddressBarBackgroundColor = ""; // reset to default
				}
			}

			if (!string.IsNullOrWhiteSpace(appThemeToolbarBackgroundColor))
			{
				try
				{
					service.SetAppThemeToolbarBackgroundColor(ColorHelper.ToColor(appThemeToolbarBackgroundColor).FromWindowsColor());
				}
				catch
				{
					appearance.AppThemeAddressBarBackgroundColor = ""; //reset to default
				}
			}

			if (!string.IsNullOrWhiteSpace(appThemeSidebarBackgroundColor))
			{
				try
				{
					service.SetAppThemeSidebarBackgroundColor(ColorHelper.ToColor(appThemeSidebarBackgroundColor).FromWindowsColor());
				}
				catch
				{
					appearance.AppThemeSidebarBackgroundColor = ""; //reset to default
				}
			}

			if (!string.IsNullOrWhiteSpace(appThemeFileAreaBackgroundColor))
			{
				try
				{
					service.SetAppThemeFileAreaBackgroundColor(ColorHelper.ToColor(appThemeFileAreaBackgroundColor).FromWindowsColor());
				}
				catch
				{
					appearance.AppThemeFileAreaBackgroundColor = ""; //reset to default
				}
			}

			if (!string.IsNullOrWhiteSpace(appThemeFileAreaSecondaryBackgroundColor))
			{
				try
				{
					service.SetAppThemeFileAreaSecondaryBackgroundColor(ColorHelper.ToColor(appThemeFileAreaSecondaryBackgroundColor).FromWindowsColor());
				}
				catch
				{
					appearance.AppThemeFileAreaSecondaryBackgroundColor = ""; //reset to default
				}
			}

			if (!string.IsNullOrWhiteSpace(appThemeInfoPaneBackgroundColor))
			{
				try
				{
					service.SetAppThemeInfoPaneBackgroundColor(ColorHelper.ToColor(appThemeInfoPaneBackgroundColor).FromWindowsColor());
				}
				catch
				{
					appearance.AppThemeInfoPaneBackgroundColor = ""; //reset to default
				}
			}

			if (appThemeFontFamily != Constants.Appearance.StandardFont)
				service.SetAppThemeFontFamily(appThemeFontFamily);

			service.ApplyResources();
		}
	}
}
