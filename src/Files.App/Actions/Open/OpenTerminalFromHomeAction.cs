// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class OpenTerminalFromHomeAction : OpenTerminalAction
	{
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();

		public override string Label
			=> "OpenTerminal".GetLocalizedResource();

		public override string Description
			=> "OpenTerminalDescription".GetLocalizedResource();

		public override bool IsExecutable =>
			HomePageContext.IsAnyItemRightClicked &&
			HomePageContext.RightClickedItem is not null &&
			(HomePageContext.RightClickedItem is WidgetFileTagCardItem fileTagItem
				? fileTagItem.IsFolder
				: true) &&
			HomePageContext.RightClickedItem.Path is not null &&
			HomePageContext.RightClickedItem.Path != Constants.UserEnvironmentPaths.RecycleBinPath;

		public override bool IsAccessibleGlobally
			=> false;

		public override HotKey HotKey
			=> HotKey.None;

		protected override string[] GetPaths()
		{
			if (HomePageContext.IsAnyItemRightClicked && HomePageContext.RightClickedItem?.Path is not null)
				return [HomePageContext.RightClickedItem.Path];

			return [];
		}
	}
}
