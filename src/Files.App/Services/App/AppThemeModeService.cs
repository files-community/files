// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Runtime.InteropServices;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Files.App.Services
{
	public class AppThemeModeService : IAppThemeModeService
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private UISettings UISettings { get; } = new();

		/// <inheritdoc/>
		public ElementTheme AppThemeMode
		{
			get
			{
				var theme = UserSettingsService.AppearanceSettingsService.AppThemeMode;

				return EnumExtensions.GetEnum<ElementTheme>(theme);
			}
			set
			{
				UserSettingsService.AppearanceSettingsService.AppThemeMode = value.ToString();

				SetAppThemeMode();
			}
		}

		/// <inheritdoc/>
		public Color DefaultAccentColor
		{
			get => AppThemeMode switch
			{
				// these values are from the definition of AccentFillColorDefaultBrush in generic.xaml
				// will have to be updated if WinUI changes in the future
				ElementTheme.Light => (Color)(App.Current.Resources["SystemAccentColorDark1"]),
				ElementTheme.Dark => (Color)(App.Current.Resources["SystemAccentColorLight2"]),
				ElementTheme.Default or _ => (App.Current.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush)!.Color
			};
		}

		/// <inheritdoc/>
		public event EventHandler? AppThemeModeChanged;

		/// <summary>
		/// Initializes an instance of <see cref="AppThemeModeService"/>.
		/// </summary>
		public AppThemeModeService()
		{
			// Set the desired theme based on what is set in the application settings
			SetAppThemeMode();

			// Registering to color changes, so that we can notice when changed system theme mode
			UISettings.ColorValuesChanged += UISettings_ColorValuesChanged;
		}

		/// <inheritdoc/>
		public void ApplyResources()
		{
			// Toggle between the themes to force reload the resource styles
			SetAppThemeMode(null, null, ElementTheme.Dark);
			SetAppThemeMode(null, null, ElementTheme.Light);

			// Restore the theme to the correct one
			SetAppThemeMode();
		}

		/// <inheritdoc/>
		public void SetAppThemeMode(Window? window = null, AppWindowTitleBar? titleBar = null, ElementTheme? rootTheme = null, bool callThemeModeChangedEvent = true)
		{
			try
			{
				window ??= MainWindow.Instance;
				titleBar ??= MainWindow.Instance.AppWindow?.TitleBar;
				rootTheme ??= AppThemeMode;

				if (window.Content is FrameworkElement rootElement)
					rootElement.RequestedTheme = (ElementTheme)rootTheme;

				if (titleBar is not null)
				{
					titleBar.ButtonBackgroundColor = Colors.Transparent;
					titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

					switch (rootTheme)
					{
						case ElementTheme.Default:
							titleBar.ButtonHoverBackgroundColor = (Color)Application.Current.Resources["SystemBaseLowColor"];
							titleBar.ButtonForegroundColor = (Color)Application.Current.Resources["SystemBaseHighColor"];
							break;
						case ElementTheme.Light:
							titleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 0, 0, 0);
							titleBar.ButtonForegroundColor = Colors.Black;
							break;
						case ElementTheme.Dark:
							titleBar.ButtonHoverBackgroundColor = Color.FromArgb(51, 255, 255, 255);
							titleBar.ButtonForegroundColor = Colors.White;
							break;
					}
				}

				if (callThemeModeChangedEvent)
					AppThemeModeChanged?.Invoke(null, EventArgs.Empty);
			}
			catch (COMException ex)
			{
				App.Logger.LogInformation(ex, "Failed to change theme mode of the app.");
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, "Failed to change theme mode of the app.");
			}
		}

		private async void UISettings_ColorValuesChanged(UISettings sender, object args)
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				SetAppThemeMode();
			});
		}
	}
}
