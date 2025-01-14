// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class AddBranchDialog : ContentDialog, IDialog<AddBranchDialogViewModel>
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public AddBranchDialogViewModel ViewModel
		{
			get => (AddBranchDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public AddBranchDialog()
		{
			InitializeComponent();
		}

		public new async Task<DialogResult> ShowAsync() => (DialogResult)await base.ShowAsync();

		private void ContentDialog_Closing(ContentDialog _, ContentDialogClosingEventArgs e)
		{
			InvalidNameWarning.IsOpen = false;
			Closing -= ContentDialog_Closing;
		}
	}
}
