// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls.Widgets
{
	/// <summary>
	/// Represents group of control displays a list of network <see cref="WidgetDriveCardItem"/>.
	/// </summary>
	public sealed partial class NetworkLocationsWidget : UserControl
	{
		public NetworkLocationsWidgetViewModel ViewModel { get; set; } = Ioc.Default.GetRequiredService<NetworkLocationsWidgetViewModel>();

		public NetworkLocationsWidget()
		{
			InitializeComponent();
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button ||
				button.Tag.ToString() is not string path)
				return;

			await ViewModel.NavigateToPath(path);
		}

		private async void Button_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (!e.GetCurrentPoint(null).Properties.IsMiddleButtonPressed ||
				sender is not Button button ||
				button.Tag.ToString() is not string path)
				return;

			if (await DriveHelpers.CheckEmptyDrive(path))
				return;

			await NavigationHelpers.OpenPathInNewTab(path);
		}

		private void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			ViewModel.BuildItemContextMenu(sender, e);
		}

		private void NoNetworkLocationsInfoBarButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.DisableWidget();
		}
	}
}
