// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Services.Settings
{
	internal sealed class ActionsSettingsService : BaseObservableJsonSettings, IActionsSettingsService
	{
		/// <inheritdoc/>
		public List<ActionWithParameterItem>? ActionsV2
		{
			get => Get<List<ActionWithParameterItem>>(null);
			set => Set(value);
		}

		public ActionsSettingsService(ISettingsSharingContext settingsSharingContext)
		{
			// Register root
			RegisterSettingsContext(settingsSharingContext);
		}

		protected override void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			base.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
