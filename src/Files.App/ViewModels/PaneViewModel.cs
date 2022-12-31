using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.Backend.Services.Settings;
using Files.Shared.Enums;
using System;
using System.ComponentModel;

namespace Files.App.ViewModels
{
	public interface IPaneViewModel : INotifyPropertyChanged, IDisposable
	{
		bool HasContent { get; }

		bool IsPreviewSelected { get; set; }
	}

	public class PaneViewModel : ObservableObject, IPaneViewModel
	{
		private readonly IPreviewPaneSettingsService settings = Ioc.Default.GetRequiredService<IPreviewPaneSettingsService>();

		private PaneContents content = PaneContents.None;

		public bool HasContent => content is not PaneContents.None;

		public bool IsPreviewSelected
		{
			get => content is PaneContents.Preview;
			set => SetState(value, PaneContents.Preview);
		}

		public PaneViewModel()
		{
			settings.PropertyChanged += Settings_PropertyChanged;
			content = settings.Content;
		}

		public void Dispose() => settings.PropertyChanged -= Settings_PropertyChanged;

		private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IPreviewPaneSettingsService.Content))
			{
				var newContent = settings.Content;
				if (content != newContent)
				{
					content = newContent;

					OnPropertyChanged(nameof(HasContent));
					OnPropertyChanged(nameof(IsPreviewSelected));
				}
			}
		}

		private void SetState(bool state, PaneContents field)
		{
			if (state && content != field)
			{
				settings.Content = field;
			}
			else if (!state && content == field)
			{
				settings.Content = PaneContents.None;
			}
		}
	}
}
