// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	public static class WidgetsHelpers
	{
		public static bool TryGetWidget<TWidget>(HomeViewModel widgetsViewModel) where TWidget : IWidgetViewModel, new()
		{
			bool canAddWidget = widgetsViewModel.CanAddWidget(typeof(TWidget).Name);
			bool isWidgetSettingEnabled = TryGetIsWidgetSettingEnabled<TWidget>();

			if (canAddWidget && isWidgetSettingEnabled)
			{
				return true;
			}
			// The widgets exists but the setting has been disabled for it
			else if (!canAddWidget && !isWidgetSettingEnabled)
			{
				// Remove the widget
				widgetsViewModel.RemoveWidget<TWidget>();
				return false;
			}
			else if (!isWidgetSettingEnabled)
			{
				return false;
			}

			return true;
		}

		public static bool TryGetIsWidgetSettingEnabled<TWidget>() where TWidget : IWidgetViewModel
		{
			IGeneralSettingsService generalSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

			if (typeof(TWidget) == typeof(QuickAccessWidgetViewModel))
			{
				return generalSettingsService.ShowQuickAccessWidget;
			}
			if (typeof(TWidget) == typeof(DrivesWidgetViewModel))
			{
				return generalSettingsService.ShowDrivesWidget;
			}
			if (typeof(TWidget) == typeof(NetworkLocationsWidgetViewModel))
			{
				return generalSettingsService.ShowNetworkLocationsWidget;
			}
			if (typeof(TWidget) == typeof(FileTagsWidgetViewModel))
			{
				return generalSettingsService.ShowFileTagsWidget;
			}
			if (typeof(TWidget) == typeof(RecentFilesWidgetViewModel))
			{
				return generalSettingsService.ShowRecentFilesWidget;
			}

			return false;
		}
	}
}
