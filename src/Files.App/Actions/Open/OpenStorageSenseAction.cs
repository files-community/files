// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal class OpenStorageSenseAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly DrivesViewModel drivesViewModel = Ioc.Default.GetRequiredService<DrivesViewModel>();

		public virtual string Label
			=> Strings.Cleanup.GetLocalizedResource();

		public virtual string Description
			=> Strings.OpenStorageSenseDescription.GetLocalizedResource();

		public virtual bool IsExecutable =>
			context.HasItem &&
			!context.HasSelection &&
			drivesViewModel.Drives
				.Cast<DriveItem>()
				.FirstOrDefault(x => string.Equals(x.Path, context.Folder?.ItemPath)) is DriveItem driveItem &&
				driveItem.Type != DriveType.Network;

		public virtual bool IsAccessibleGlobally
			=> true;

		public OpenStorageSenseAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public virtual Task ExecuteAsync(object? parameter = null)
		{
			return StorageSenseHelper.OpenStorageSenseAsync(context.Folder?.ItemPath ?? string.Empty);
		}

		public void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
