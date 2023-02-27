using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Filesystem.Security;
using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Linq;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI;

namespace Files.App.Views.Properties
{
	public sealed partial class SecurityAdvancedPage : Page
	{
		public SecurityAdvancedPage()
		{
			InitializeComponent();

			_isWinUI3 = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8);

			App.AppSettings.ThemeModeChanged += AppSettings_ThemeModeChanged;

			var flowDirectionSetting = new ResourceManager().CreateResourceContext().QualifierValues["LayoutDirection"];
			if (flowDirectionSetting == "RTL")
				FlowDirection = FlowDirection.RightToLeft;
		}

		#region Fields and Properties
		public string WindowTitle
			=> string.Format("SecurityAdvancedPermissionsTitle".GetLocalizedResource(), ViewModel?.Item.Name);

		public SecurityViewModel? ViewModel { get; set; }

		public AppWindow? appWindow;

		private readonly bool _isWinUI3;
		#endregion

		#region Methods
		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var args = (PropertiesPageNavigationArguments)e.Parameter;

			if (args.Item is ListedItem listedItem)
				ViewModel = new SecurityViewModel(listedItem);
			else if (args.Item is DriveItem driveitem)
				ViewModel = new SecurityViewModel(driveitem);

			base.OnNavigatedTo(e);
		}

		private async void SecurityAdvancedPage_Loaded(object sender, RoutedEventArgs e)
		{
			if (_isWinUI3 && appWindow is not null)
			{
				appWindow.Destroying += AppWindow_Destroying;

				// Update theme
				await App.Window.DispatcherQueue.EnqueueAsync(() => App.AppSettings.UpdateThemeElements.Execute(null));
			}
		}

		private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ViewModel is null)
				return;

			ViewModel.SelectedAdvancedAccessControlEntries =
				((ListView)sender).SelectedItems.Cast<AccessControlEntryAdvanced>().ToList();

			if (e.AddedItems is not null)
			{
				foreach (var item in e.AddedItems)
				{
					((AccessControlEntryAdvanced)item).IsSelected = true;
				}
			}
			if (e.RemovedItems is not null)
			{
				foreach (var item in e.RemovedItems)
				{
					((AccessControlEntryAdvanced)item).IsSelected = false;
				}
			}
		}

		private void AppWindow_Destroying(AppWindow sender, object args)
		{
			App.AppSettings.ThemeModeChanged -= AppSettings_ThemeModeChanged;
			sender.Destroying -= AppWindow_Destroying;
		}

		private async void AppSettings_ThemeModeChanged(object sender, EventArgs e)
		{
			var selectedTheme = ThemeHelper.RootTheme;

			await DispatcherQueue.EnqueueAsync(() =>
			{
				((Frame)Parent).RequestedTheme = selectedTheme;

				if (_isWinUI3 && appWindow is not null)
				{
					switch (selectedTheme)
					{
						case ElementTheme.Default:
							appWindow.TitleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
							appWindow.TitleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
							break;
						case ElementTheme.Light:
							appWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
							appWindow.TitleBar.ButtonForegroundColor = Colors.Black;
							break;
						case ElementTheme.Dark:
							appWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
							appWindow.TitleBar.ButtonForegroundColor = Colors.White;
							break;
					}
				}
			});
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			if (_isWinUI3 && appWindow is not null)
			{
				ViewModel?.SetAccessControlList();
				appWindow.Destroy();
			}
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (_isWinUI3 && appWindow is not null)
				appWindow.Destroy();
		}

		private void SecurityAdvancedPage_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key.Equals(VirtualKey.Escape) && _isWinUI3 && appWindow is not null)
				appWindow.Destroy();
		}
		#endregion

		public class PropertiesPageNavigationArguments
		{
			public object? Item { get; set; }
		}
	}
}
