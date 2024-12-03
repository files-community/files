// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.Dialogs;
using Files.App.ViewModels.Dialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Files.App.Dialogs
{
	public sealed partial class CreateShortcutDialog : ContentDialog, IDialog<CreateShortcutDialogViewModel>, IRealTimeControl
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public CreateShortcutDialogViewModel ViewModel
		{
			get => (CreateShortcutDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public CreateShortcutDialog()
		{
			InitializeComponent();
			InitializeContentLayout();
			this.Closing += CreateShortcutDialog_Closing;

			InvalidPathWarning.SetBinding(TeachingTip.TargetProperty, new Binding()
			{
				Source = ShortcutTarget
			});
		}

		private void CreateShortcutDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			this.Closing -= CreateShortcutDialog_Closing;
			InvalidPathWarning.IsOpen = false;
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await base.ShowAsync();
		}
	}
}
