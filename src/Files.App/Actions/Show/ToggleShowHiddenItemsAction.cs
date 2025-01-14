// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class ToggleShowHiddenItemsAction : ObservableObject, IToggleAction
	{
		private readonly IFoldersSettingsService settings;

		public string Label
			=> "ShowHiddenItems".GetLocalizedResource();

		public string Description
			=> "ToggleShowHiddenItemsDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.H, KeyModifiers.Ctrl);

		public bool IsOn
			=> settings.ShowHiddenItems;

		public ToggleShowHiddenItemsAction()
		{
			settings = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

			settings.PropertyChanged += Settings_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			settings.ShowHiddenItems = !settings.ShowHiddenItems;

			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.ShowHiddenItems))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
