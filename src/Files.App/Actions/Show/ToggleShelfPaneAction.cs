// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class ToggleShelfPaneAction : ObservableObject, IToggleAction
	{
		private readonly IGeneralSettingsService generalSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		public string Label
			=> Strings.ToggleShelfPane.GetLocalizedResource();

		public string Description
			=> Strings.ToggleShelfPaneDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Shelf");

		// TODO Remove IsAccessibleGlobally when shelf feature is ready
		public bool IsAccessibleGlobally
			=> AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev;

		// TODO Remove IsExecutable when shelf feature is ready
		public bool IsExecutable
			=> AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev;
		
		public bool IsOn
			=> generalSettingsService.ShowShelfPane;

		public ToggleShelfPaneAction()
		{
			generalSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			generalSettingsService.ShowShelfPane = !IsOn;

			return Task.CompletedTask;
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(GeneralSettingsService.ShowShelfPane))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
