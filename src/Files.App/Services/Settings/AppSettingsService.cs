// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services.Settings
{
	internal sealed class AppSettingsService : BaseObservableJsonSettings, IAppSettingsService
	{
		public AppSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Initialize settings
			RegisterSettingsContext(settingsSharingContext);
		}

		public bool ShowStatusCenterTeachingTip
		{
			get => Get(true);
			set => Set(value);
		}

		public bool ShowBackgroundRunningNotification
		{
			get => Get(true);
			set => Set(value);
		}

		public bool RestoreTabsOnStartup
		{
			get => Get(false);
			set => Set(value);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
