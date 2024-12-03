// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Text;

namespace Files.App.Dialogs
{
	public sealed partial class DecompressArchiveDialog : ContentDialog, IRealTimeControl
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public DecompressArchiveDialogViewModel ViewModel
		{
			get => (DecompressArchiveDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public DecompressArchiveDialog()
		{
			InitializeComponent();
			InitializeContentLayout();
		}

		private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
		{
			if (ViewModel.IsArchiveEncrypted)
				ViewModel.PrimaryButtonClickCommand.Execute(new DisposableArray(Encoding.UTF8.GetBytes(Password.Password)));
		}
	}
}
