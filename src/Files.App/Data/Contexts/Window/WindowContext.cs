// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Windowing;

namespace Files.App.Data.Contexts
{
	/// <inheritdoc cref="IWindowContext"/>
	internal sealed class WindowContext : ObservableObject, IWindowContext
	{
		private IWindowsSecurityService WindowsSecurityService = Ioc.Default.GetRequiredService<IWindowsSecurityService>();

		private bool isCompactOverlay;
		/// <inheritdoc/>
		public bool IsCompactOverlay => isCompactOverlay;

		/// <inheritdoc/>
		public bool IsRunningAsAdmin { get; private set; }

		/// <inheritdoc/>
		public bool CanDragAndDrop { get; private set; }

		public WindowContext()
		{
			IsRunningAsAdmin = WindowsSecurityService.IsAppElevated();
			CanDragAndDrop = WindowsSecurityService.CanDragAndDrop();

			MainWindow.Instance.AppWindow.Changed += AppWindow_Changed;
		}

		private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
		{
			if (args.DidPresenterChange)
			{
				SetProperty(
					ref isCompactOverlay,
					sender.Presenter.Kind is AppWindowPresenterKind.CompactOverlay,
					nameof(IsCompactOverlay));
			}
		}
	}
}
