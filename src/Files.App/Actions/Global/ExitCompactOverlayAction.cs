// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Windowing;

namespace Files.App.Actions
{
	internal sealed class ExitCompactOverlayAction : ObservableObject, IAction
	{
		private readonly IWindowContext windowContext;

		public string Label
			=> "ExitCompactOverlay".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.CompactOverlayExit");

		public HotKey HotKey
			=> new(Keys.Down, KeyModifiers.CtrlAlt);

		public string Description
			=> "ExitCompactOverlayDescription".GetLocalizedResource();

		public bool IsExecutable
			=> windowContext.IsCompactOverlay;

		public ExitCompactOverlayAction()
		{
			windowContext = Ioc.Default.GetRequiredService<IWindowContext>();

			windowContext.PropertyChanged += WindowContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var appWindow = MainWindow.Instance.AppWindow;
			appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);

			return Task.CompletedTask;
		}

		private void WindowContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IWindowContext.IsCompactOverlay):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
