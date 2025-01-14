// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class ToggleInfoPaneAction : ObservableObject, IToggleAction
	{
		private readonly InfoPaneViewModel viewModel;

		public string Label
			=> "ToggleInfoPane".GetLocalizedResource();

		public string Description
			=> "ToggleInfoPaneDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.PanelRight");

		public HotKey HotKey
			=> new(Keys.I, KeyModifiers.CtrlAlt);

		public bool IsOn
			=> viewModel.IsEnabled;

		public ToggleInfoPaneAction()
		{
			viewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
			viewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			viewModel.IsEnabled = !IsOn;

			return Task.CompletedTask;
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(InfoPaneViewModel.IsEnabled))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
