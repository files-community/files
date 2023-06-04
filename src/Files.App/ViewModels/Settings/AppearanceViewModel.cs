// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.Helpers;
using Files.App.ServicesImplementation.Settings;
using Files.Backend.Services;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;

namespace Files.App.ViewModels.Settings
{
	public class AppearanceViewModel : ObservableObject
	{
		private readonly IUserSettingsService UserSettingsService;
		private readonly IResourcesService ResourcesService;

		public List<string> Themes { get; private set; }
		public Dictionary<SystemBackdropType, string> SystemBackdropTypes { get; private set; } = new();

		public ObservableCollection<AppThemeResourceItem> AppThemeResources { get; }

		public AppearanceViewModel(IUserSettingsService userSettingsService, IResourcesService resourcesService)
		{
			UserSettingsService = userSettingsService;
			ResourcesService = resourcesService;

			Themes = new List<string>()
			{
				"Default".GetLocalizedResource(),
				"LightTheme".GetLocalizedResource(),
				"DarkTheme".GetLocalizedResource()
			};

			// TODO: Re-add Solid and regular Mica when theming is revamped
			//SystemBackdropTypes.Add(SystemBackdropType.Solid, "Solid".GetLocalizedResource());

			if (DesktopAcrylicController.IsSupported())
				SystemBackdropTypes.Add(SystemBackdropType.Acrylic, "Acrylic".GetLocalizedResource());

			if (MicaController.IsSupported())
			{
				//SystemBackdropTypes.Add(SystemBackdropType.Mica, "Mica".GetLocalizedResource());
				SystemBackdropTypes.Add(SystemBackdropType.MicaAlt, "MicaAlt".GetLocalizedResource());
			}
			selectedSystemBackdrop = SystemBackdropTypes[UserSettingsService.AppearanceSettingsService.AppThemeSystemBackdrop];

			AppThemeResources = AppThemeResourceFactory.AppThemeResources;

			UpdateSelectedResource();
		}

		/// <summary>
		/// Selects the AppThemeResource corresponding to the current settings
		/// </summary>
		private void UpdateSelectedResource()
		{
			var themeBackgroundColor = AppThemeBackgroundColor;

			// Add color to the collection if it's not already there
			if (!AppThemeResources.Any(p => p.BackgroundColor == themeBackgroundColor))
			{
				// Remove current value before adding a new one
				if (AppThemeResources.Last().Name == "Custom")
					AppThemeResources.Remove(AppThemeResources.Last());

				var appThemeBackgroundColor = new AppThemeResourceItem
				{
					BackgroundColor = themeBackgroundColor,
					Name = "Custom"
				};

				AppThemeResources.Add(appThemeBackgroundColor);
			}

			SelectedAppThemeResources = AppThemeResources
				.Where(p => p.BackgroundColor == themeBackgroundColor)
				.FirstOrDefault() ?? AppThemeResources[0];
		}

		private AppThemeResourceItem selectedAppThemeResources;
		public AppThemeResourceItem SelectedAppThemeResources
		{
			get => selectedAppThemeResources;
			set
			{
				if (value is not null && SetProperty(ref selectedAppThemeResources, value))
				{
					AppThemeBackgroundColor = SelectedAppThemeResources.BackgroundColor;
					OnPropertyChanged(nameof(selectedAppThemeResources));
				}
			}
		}

		private int selectedThemeIndex = (int)Enum.Parse(typeof(ElementTheme), ThemeHelper.RootTheme.ToString());
		public int SelectedThemeIndex
		{
			get => selectedThemeIndex;
			set
			{
				if (SetProperty(ref selectedThemeIndex, value))
				{
					ThemeHelper.RootTheme = (ElementTheme)value;
					OnPropertyChanged(nameof(SelectedElementTheme));
				}
			}
		}

		public ElementTheme SelectedElementTheme
		{
			get => (ElementTheme)selectedThemeIndex;
		}

		public bool UseCompactStyles
		{
			get => UserSettingsService.AppearanceSettingsService.UseCompactStyles;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.UseCompactStyles)
				{
					UserSettingsService.AppearanceSettingsService.UseCompactStyles = value;

					// Apply the updated compact spacing resource
					ResourcesService.SetCompactSpacing(UseCompactStyles);
					ResourcesService.ApplyResources();

					OnPropertyChanged();
				}
			}
		}

		public string AppThemeBackgroundColor
		{
			get => UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor;
			set
			{
				if (value != UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor)
				{
					UserSettingsService.AppearanceSettingsService.AppThemeBackgroundColor = value;

					// Apply the updated background resource
					ResourcesService.SetAppThemeBackgroundColor(ColorHelper.ToColor(value).FromWindowsColor());
					ResourcesService.ApplyResources();

					OnPropertyChanged();
				}
			}
		}

		private string selectedSystemBackdrop;
		public string SelectedSystemBackdrop
		{
			get => selectedSystemBackdrop;
			set
			{
				if(SetProperty(ref selectedSystemBackdrop, value))
				{
					UserSettingsService.AppearanceSettingsService.AppThemeSystemBackdrop = SystemBackdropTypes.First(e => e.Value == value).Key;
				}
			}
		}
		
	}
}
