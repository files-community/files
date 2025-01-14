// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;

namespace Files.App.Actions
{
	internal sealed class OpenSettingsAction : BaseUIAction, IAction
	{
		private readonly IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		private readonly SettingsDialogViewModel viewModel = new();

		public string Label
			=> "Settings".GetLocalizedResource();

		public string Description
			=> "OpenSettingsDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.OemComma, KeyModifiers.Ctrl);

		public Task ExecuteAsync(object? parameter = null)
		{
			var dialog = dialogService.GetDialog(viewModel);
			if (parameter is not null && parameter is SettingsNavigationParams navParams)
				((SettingsDialog)dialog).NavigateTo(navParams);

			return dialog.TryShowAsync();
		}
	}
}
