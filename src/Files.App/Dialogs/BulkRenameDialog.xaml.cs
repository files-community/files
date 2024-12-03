// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Dialogs
{
	public sealed partial class BulkRenameDialog : ContentDialog, IDialog<BulkRenameDialogViewModel>, IRealTimeControl
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public BulkRenameDialogViewModel ViewModel
		{
			get => (BulkRenameDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public BulkRenameDialog()
		{
			InitializeComponent();
			InitializeContentLayout();
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}
	}
}
