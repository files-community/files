// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace Files.App.Actions
{
	internal abstract class BaseSetAsAction : ObservableObject, IAction
	{
		protected readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected readonly IWindowsWallpaperService WindowsWallpaperService = Ioc.Default.GetRequiredService<IWindowsWallpaperService>();

		public abstract string Label { get; }

		public abstract string Description { get; }

		public abstract RichGlyph Glyph { get; }

		public virtual bool IsExecutable =>
			ContentPageContext.ShellPage is not null &&
			ContentPageContext.PageType != ContentPageTypes.RecycleBin &&
			ContentPageContext.PageType != ContentPageTypes.ZipFolder &&
			(ContentPageContext.ShellPage?.SlimContentPage?.SelectedItemsPropertiesViewModel?.IsCompatibleToSetAsWindowsWallpaper ?? false);

		public BaseSetAsAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public abstract Task ExecuteAsync(object? parameter = null);

		protected async void ShowErrorDialog(string message)
		{
			var errorDialog = new ContentDialog()
			{
				Title = "FailedToSetBackground".GetLocalizedResource(),
				Content = message,
				PrimaryButtonText = "OK".GetLocalizedResource(),
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				errorDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			await errorDialog.TryShowAsync();
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
				case nameof(IContentPageContext.SelectedItem):
				case nameof(IContentPageContext.SelectedItems):
					{
						if (ContentPageContext.ShellPage is not null && ContentPageContext.ShellPage.SlimContentPage is not null)
						{
							var viewModel = ContentPageContext.ShellPage.SlimContentPage.SelectedItemsPropertiesViewModel;
							var extensions = ContentPageContext.SelectedItems.Select(selectedItem => selectedItem.FileExtension).Distinct().ToList();

							viewModel.CheckAllFileExtensions(extensions);
						}

						OnPropertyChanged(nameof(IsExecutable));
						break;
					}
			}
		}
	}
}
