// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;

namespace Files.App.Actions
{
	internal sealed class OpenReleaseNotesAction : ObservableObject, IAction
	{
		private readonly IDialogService DialogService = Ioc.Default.GetRequiredService<IDialogService>();
		private readonly IUpdateService UpdateService = Ioc.Default.GetRequiredService<IUpdateService>();
		public string Label
			=> Strings.WhatsNew.GetLocalizedResource();

		public string Description
			=> Strings.WhatsNewDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.AppUpdatedBox");

		public bool IsExecutable
			=> UpdateService.AreReleaseNotesAvailable;

		public OpenReleaseNotesAction()
		{
			UpdateService.PropertyChanged += UpdateService_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var viewModel = new ReleaseNotesDialogViewModel(Constants.ExternalUrl.ReleaseNotesUrl);
			var dialog = DialogService.GetDialog(viewModel);

			return dialog.TryShowAsync();
		}

		private void UpdateService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IUpdateService.AreReleaseNotesAvailable):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
