// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class OpenTerminalFromSidebarAction : OpenTerminalAction
	{
		private ISidebarContext SidebarContext { get; } = Ioc.Default.GetRequiredService<ISidebarContext>();

		public override string Label
			=> "OpenTerminal".GetLocalizedResource();

		public override string Description
			=> "OpenTerminalDescription".GetLocalizedResource();

		public override bool IsExecutable =>
			SidebarContext.IsItemRightClicked &&
			SidebarContext.RightClickedItem is not null &&
			SidebarContext.RightClickedItem.MenuOptions.ShowShellItems &&
			!SidebarContext.RightClickedItem.MenuOptions.ShowEmptyRecycleBin;

		public override bool IsAccessibleGlobally
			=> false;

		public override HotKey HotKey
			=> HotKey.None;

		protected override string[] GetPaths()
		{
			if (SidebarContext.IsItemRightClicked && SidebarContext.RightClickedItem is not null)
				return [SidebarContext.RightClickedItem.Path];

			return [];
		}
	}
}
