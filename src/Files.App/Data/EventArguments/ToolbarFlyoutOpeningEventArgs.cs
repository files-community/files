// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.EventArguments
{
	public sealed class ToolbarFlyoutOpeningEventArgs
	{
		public MenuFlyout OpeningFlyout { get; }

		public ToolbarFlyoutOpeningEventArgs(MenuFlyout openingFlyout)
		{
			OpeningFlyout = openingFlyout;
		}
	}
}
