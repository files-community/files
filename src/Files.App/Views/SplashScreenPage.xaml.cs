// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Views
{
	/// <summary>
	/// Display the app splash screen.
	/// </summary>
	public sealed partial class SplashScreenPage : Page
	{
		private string BranchLabel =>
			AppLifecycleHelper.AppEnvironment switch
			{
				AppEnvironment.Dev => "Dev",
				AppEnvironment.SideloadPreview or AppEnvironment.StorePreview => "Preview",
				_ => string.Empty,
			};

		public SplashScreenPage()
		{
			InitializeComponent();
		}

		private void Image_ImageOpened(object sender, RoutedEventArgs e)
		{
			App.SplashScreenLoadingTCS?.TrySetResult();
		}

		private void Image_ImageFailed(object sender, RoutedEventArgs e)
		{
			App.SplashScreenLoadingTCS?.TrySetResult();
		}
	}
}
