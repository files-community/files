// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.UITests.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UITests
{
	public sealed partial class MainWindow : Window
	{
		private static MainWindow? _Instance;
		public static MainWindow Instance => _Instance ??= new();

		private MainWindow()
		{
			InitializeComponent();

			ExtendsContentIntoTitleBar = true;

			MainFrame.Navigate(typeof(MainPage));
		}

		private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
		{
			if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
				return;

			switch (tag)
			{
				case "ThemedIconPage":
					MainFrame.Navigate(typeof(ThemedIconPage));
					break;
				case "ToolbarPage":
					MainFrame.Navigate(typeof(ToolbarPage));
					break;
				case "StorageControlsPage":
					MainFrame.Navigate(typeof(StorageControlsPage));
					break;
			}
		}

		private void ThemeModeSelectorCombBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is not ComboBox comboBox ||
				Content is not FrameworkElement element)
				return;

			switch (comboBox.SelectedIndex)
			{
				case 0:
					element.RequestedTheme = ElementTheme.Default;
					break;
				case 1:
					element.RequestedTheme = ElementTheme.Light;
					break;
				case 2:
					element.RequestedTheme = ElementTheme.Dark;
					break;
			}
		}
	}
}
