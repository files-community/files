// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Files.App.Actions
{
	internal sealed class EnterCompactOverlayAction : ObservableObject, IAction
	{
		private readonly IWindowContext windowContext;

		public string Label
			=> "EnterCompactOverlay".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.CompactOverlay");

		public HotKey HotKey
			=> new(Keys.Up, KeyModifiers.CtrlAlt);

		public string Description
			=> "EnterCompactOverlayDescription".GetLocalizedResource();

		public bool IsExecutable
			=> !windowContext.IsCompactOverlay;

		public EnterCompactOverlayAction()
		{
			windowContext = Ioc.Default.GetRequiredService<IWindowContext>();

			windowContext.PropertyChanged += WindowContext_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var appWindow = MainWindow.Instance.AppWindow;
			appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
			appWindow.Resize(new SizeInt32(400, 350));

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
