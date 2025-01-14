// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class ToggleDotFilesSettingAction : ObservableObject, IToggleAction
	{
		private readonly IFoldersSettingsService FoldersSettingsService;

		public string Label
			=> "ShowDotFiles".GetLocalizedResource();

		public string Description
			=> "ToggleDotFilesSettingDescription".GetLocalizedResource();

		public bool IsOn
			=> FoldersSettingsService.ShowDotFiles;

		public ToggleDotFilesSettingAction()
		{
			FoldersSettingsService = Ioc.Default.GetRequiredService<IFoldersSettingsService>();

			FoldersSettingsService.PropertyChanged += Settings_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			FoldersSettingsService.ShowDotFiles = !FoldersSettingsService.ShowDotFiles;

			return Task.CompletedTask;
		}

		private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.ShowDotFiles))
				OnPropertyChanged(nameof(IsOn));
		}
	}
}
