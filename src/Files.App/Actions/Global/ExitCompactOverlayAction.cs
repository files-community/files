﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Windowing;

namespace Files.App.Actions
{
	internal class ExitCompactOverlayAction : ObservableObject, IAction
	{
		private IWindowContext WindowContext { get; } = Ioc.Default.GetRequiredService<IWindowContext>();

		public string Label
			=> "ExitCompactOverlay".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconExitCompactOverlay");

		public HotKey HotKey
			=> new(Keys.Down, KeyModifiers.MenuCtrl);

		public string Description
			=> "ExitCompactOverlayDescription".GetLocalizedResource();

		public bool IsExecutable
			=> WindowContext.IsCompactOverlay;

		public ExitCompactOverlayAction()
		{
			WindowContext.PropertyChanged += WindowContext_PropertyChanged;
		}

		public Task ExecuteAsync()
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
