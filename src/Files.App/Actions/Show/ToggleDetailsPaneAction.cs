// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class ToggleDetailsPaneAction : ObservableObject, IAction
	{
		private readonly InfoPaneViewModel infoPaneViewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
		private readonly IInfoPaneSettingsService infoPaneSettingsService = Ioc.Default.GetRequiredService<IInfoPaneSettingsService>();

		public string Label
			=> Strings.ToggleDetailsPane.GetLocalizedResource();

		public string Description
			=> Strings.ToggleDetailsPaneDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.PanelRight");

		public bool IsAccessibleGlobally
			=> false;

		public bool IsExecutable
			=> infoPaneViewModel.IsEnabled;

		public ToggleDetailsPaneAction()
		{
			infoPaneViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			infoPaneSettingsService.SelectedTab = InfoPaneTabs.Details;

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(InfoPaneViewModel.IsEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
