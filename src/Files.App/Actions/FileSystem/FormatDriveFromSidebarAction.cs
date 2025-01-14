// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class FormatDriveFromSidebarAction : FormatDriveAction
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
				!(driveItem.Type == DriveType.Network || string.Equals(SidebarContext.RightClickedItem.Path, $@"{Constants.UserEnvironmentPaths.SystemDrivePath}\", StringComparison.OrdinalIgnoreCase));

		public override bool IsAccessibleGlobally
			=> false;

		public override Task ExecuteAsync(object? parameter = null)
		{
			return Win32Helper.OpenFormatDriveDialog(SidebarContext?.RightClickedItem?.Path ?? string.Empty);
		}
	}
}