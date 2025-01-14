// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.System;

namespace Files.App.Actions
{
	internal sealed class OpenHelpAction : IAction
	{
		public string Label
			=> "Help".GetLocalizedResource();

		public string Description
			=> "OpenHelpDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.F1);

		public Task ExecuteAsync(object? parameter = null)
		{
			var url = new Uri(Constants.ExternalUrl.DocumentationUrl);
			return Launcher.LaunchUriAsync(url).AsTask();
		}
	}
}
