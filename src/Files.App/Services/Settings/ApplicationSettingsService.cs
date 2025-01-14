// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Utils.Serialization;
using Files.App.Services.Settings;

namespace Files.App.Services.Settings
{
	internal sealed class ApplicationSettingsService : BaseObservableJsonSettings, IApplicationSettingsService
	{
		public bool ClickedToReviewApp
		{
			get => Get(false);
			set => Set(value);
		}
		
		public bool ShowRunningAsAdminPrompt
		{
			get => Get(true);
			set => Set(value);
		}
		
		public bool ShowDataStreamsAreHiddenPrompt
		{
			get => Get(true);
			set => Set(value);
		}

		public ApplicationSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			RegisterSettingsContext(settingsSharingContext);
		}
	}
}
