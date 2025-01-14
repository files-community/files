// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class OpenStorageSenseFromSidebarAction : OpenStorageSenseAction
	{
		private ISidebarContext SidebarContext { get; } = Ioc.Default.GetRequiredService<ISidebarContext>();
		private readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

		public override bool IsExecutable =>
			SidebarContext.IsItemRightClicked &&
			SidebarContext.RightClickedItem is not null &&
			SidebarContext.RightClickedItem.Path is not null &&
			drivesViewModel.Drives
				.Cast<DriveItem>()
				.FirstOrDefault(x => string.Equals(x.Path, SidebarContext.RightClickedItem.Path)) is DriveItem driveItem &&
				driveItem.Type != DriveType.Network;

		public override bool IsAccessibleGlobally
			=> false;

		public override Task ExecuteAsync(object? parameter = null)
		{
			return StorageSenseHelper.OpenStorageSenseAsync(SidebarContext?.RightClickedItem?.Path ?? string.Empty);
		}
	}
}