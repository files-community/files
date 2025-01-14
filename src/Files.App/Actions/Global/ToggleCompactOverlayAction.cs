// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Files.App.Actions
{
	internal sealed class ToggleCompactOverlayAction : ObservableObject, IToggleAction
	{
		private readonly IWindowContext windowContext;

		public string Label
			=> "ToggleCompactOverlay".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.F12);

		public string Description
			=> "ToggleCompactOverlayDescription".GetLocalizedResource();

		public bool IsOn
			=> windowContext.IsCompactOverlay;

		public ToggleCompactOverlayAction()
		{
			windowContext = Ioc.Default.GetRequiredService<IWindowContext>();

			windowContext.PropertyChanged += WindowContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var appWindow = MainWindow.Instance.AppWindow;

			if (windowContext.IsCompactOverlay)
			{
				appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
			}
			else
			{
				appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
				appWindow.Resize(new SizeInt32(400, 350));
			}

			return Task.CompletedTask;
		}

		private void WindowContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IWindowContext.IsCompactOverlay):
					OnPropertyChanged(nameof(IsOn));
					break;
			}
		}
	}
}
