// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal class FormatDriveAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		private readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

		public string Label
			=> Strings.FormatDriveText.GetLocalizedResource();

		public string Description
			=> Strings.FormatDriveDescription.GetLocalizedResource();

		public virtual bool IsExecutable =>
			context.HasItem &&
			!context.HasSelection &&
			drivesViewModel.Drives
				.Cast<DriveItem>()
				.FirstOrDefault(x => string.Equals(x.Path, context.Folder?.ItemPath)) is DriveItem driveItem &&
				!(driveItem.Type == DriveType.Network || string.Equals(context.Folder?.ItemPath, $@"{Constants.UserEnvironmentPaths.SystemDrivePath}\", StringComparison.OrdinalIgnoreCase));

		public virtual bool IsAccessibleGlobally
			=> true;

		public FormatDriveAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public virtual Task ExecuteAsync(object? parameter = null)
		{
			return Win32Helper.OpenFormatDriveDialog(context.Folder?.ItemPath ?? string.Empty);
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
