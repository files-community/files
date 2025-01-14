// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class LaunchPreviewPopupAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		private readonly IPreviewPopupService previewPopupService;

		public string Label
			=> "LaunchPreviewPopup".GetLocalizedResource();

		public string Description
			=> "LaunchPreviewPopupDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Space);

		public bool IsExecutable =>
			context.SelectedItems.Count == 1 &&
			(!context.ShellPage?.ToolbarViewModel?.IsEditModeEnabled ?? false) &&
			(!context.ShellPage?.SlimContentPage?.IsRenamingItem ?? false);

		public LaunchPreviewPopupAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
			previewPopupService = Ioc.Default.GetRequiredService<IPreviewPopupService>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var provider = await previewPopupService.GetProviderAsync();
			if (provider is null)
				return;

			var itemPath = context.SelectedItem?.ItemPath;
			if (itemPath is not null)
				await provider.TogglePreviewPopupAsync(itemPath);
		}

		private async Task SwitchPopupPreviewAsync()
		{
			if (IsExecutable)
			{
				var provider = await previewPopupService.GetProviderAsync();
				if (provider is null)
					return;

				var itemPath = context.SelectedItem?.ItemPath;
				if (itemPath is not null)
					await provider.SwitchPreviewAsync(itemPath);
			}
		}

		public async void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					await SwitchPopupPreviewAsync();
					break;
			}
		}
	}
}
