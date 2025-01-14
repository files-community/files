// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI.Controls;
using Files.App.ViewModels.Previews;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace Files.App.UserControls.FilePreviews
{
	public sealed partial class MarkdownPreview : UserControl
	{
		private MarkdownPreviewViewModel ViewModel { get; set; }

		public MarkdownPreview(MarkdownPreviewViewModel model)
		{
			ViewModel = model;
			InitializeComponent();
		}

		private async void PreviewMarkdownTextBlock_LinkClicked(object sender, LinkClickedEventArgs e)
		{
			if (Uri.TryCreate(e.Link, UriKind.Absolute, out Uri? link))
				await Launcher.LaunchUriAsync(link);
		}
	}
}
