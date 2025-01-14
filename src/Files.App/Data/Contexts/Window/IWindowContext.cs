// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contexts
{
	public interface IWindowContext : INotifyPropertyChanged
	{
		bool IsCompactOverlay { get; }

		/// <inheritdoc cref="IWindowsSecurityService.IsAppElevated"/>
		bool IsRunningAsAdmin { get; }

		/// <inheritdoc cref="IWindowsSecurityService.CanDragAndDrop"/>
		bool CanDragAndDrop { get; }
	}
}
