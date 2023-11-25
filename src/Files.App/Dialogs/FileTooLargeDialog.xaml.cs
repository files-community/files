// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class FileTooLargeDialog : ContentDialog, IDialog<FileTooLargeDialogViewModel>
	{
		public IAppThemeModeService AppThemeModeService { get; } = Ioc.Default.GetRequiredService<IAppThemeModeService>();

		public FileTooLargeDialogViewModel ViewModel { get; set; }

		private ElementTheme ThemeMode
			=> (ElementTheme)AppThemeModeService.ThemeMode;

		public FileTooLargeDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await SetContentDialogRoot(this).TryShowAsync();
		}

		// WINUI3
		private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			return contentDialog;
		}
	}
}
