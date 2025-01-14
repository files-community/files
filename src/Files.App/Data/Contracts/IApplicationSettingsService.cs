// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IApplicationSettingsService : IBaseSettingsService
	{
		/// <summary>
		/// Gets or sets a value indicating whether or not the user clicked to review the app.
		/// </summary>
		bool ClickedToReviewApp { get; set; }
		
		/// <summary>
		/// Gets or sets a value indicating whether or not to display a prompt when running the app as administrator.
		/// </summary>
		bool ShowRunningAsAdminPrompt { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to display a prompt when creating an alternate data stream.
		/// </summary>
		bool ShowDataStreamsAreHiddenPrompt { get; set; }

	}
}
