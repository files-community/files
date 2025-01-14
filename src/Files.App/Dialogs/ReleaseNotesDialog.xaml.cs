// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using Windows.System;
using Windows.UI.WebUI;

namespace Files.App.Dialogs
{
	public sealed partial class ReleaseNotesDialog : ContentDialog, IDialog<ReleaseNotesDialogViewModel>
	{
		private FrameworkElement RootAppElement
			=> (FrameworkElement)MainWindow.Instance.Content;

		public ReleaseNotesDialogViewModel ViewModel
		{
			get => (ReleaseNotesDialogViewModel)DataContext;
			set => DataContext = value;
		}

		public ReleaseNotesDialog()
		{
			InitializeComponent();

			MainWindow.Instance.SizeChanged += Current_SizeChanged;
			UpdateDialogLayout();
		}

		private void UpdateDialogLayout()
		{
			var maxHeight = MainWindow.Instance.Bounds.Height - 70;
			var maxWidth = MainWindow.Instance.Bounds.Width;
			ContainerGrid.Height = maxHeight > 740 ? 740 : maxHeight;
			ContainerGrid.Width = maxWidth > 740 ? 740 : maxWidth;
		}

		private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			UpdateDialogLayout();
		}

		private void ContentDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
		{
			MainWindow.Instance.SizeChanged -= Current_SizeChanged;
		}

		public new async Task<DialogResult> ShowAsync()
		{
			return (DialogResult)await SetContentDialogRoot(this).TryShowAsync();
		}

		private void CloseDialogButton_Click(object sender, RoutedEventArgs e)
		{
			Hide();
		}

		// WINUI3
		private ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			return contentDialog;
		}

		private async void BlogPostWebView_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs args)
		{
			BlogPostWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
			BlogPostWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
			BlogPostWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
			BlogPostWebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;

			var script = @"
				document.addEventListener('click', function(event) {
					var target = event.target;
					while (target && target.tagName !== 'A') {
						target = target.parentElement;
					}
					if (target && target.href) {
						event.preventDefault();
						window.chrome.webview.postMessage(target.href);
					}
				});
			";

			await sender.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
			sender.WebMessageReceived += WebView_WebMessageReceived;
		}

		private async void WebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
		{
			// Open link in web browser
			if (Uri.TryCreate(args.TryGetWebMessageAsString(), UriKind.Absolute, out Uri? uri))
				await Launcher.LaunchUriAsync(uri);

			// Navigate back to blog post
			if (sender.CoreWebView2.CanGoBack)
				sender.CoreWebView2.GoBack(); 
		}

	}
}
