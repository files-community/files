// Copyright (c) Files Community
// Licensed under the MIT License.

using System.ComponentModel;

namespace Files.App.Data.Contracts
{
	public interface IInfoPaneSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets a value indicating if the preview pane is enabled.
		/// </summary>
		bool IsInfoPaneEnabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the height of the pane in a horizontal layout.
		/// </summary>
		double HorizontalSizePx { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the width of the pane in a vertical layout.
		/// </summary>
		double VerticalSizePx { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the preview pane media volume.
		/// </summary>
		double MediaVolume { get; set; }

		/// <summary>
		/// Gets or sets a value indicating the selected tab in the Info Pane.
		/// </summary>
		InfoPaneTabs SelectedTab { get; set; }
	}
}
