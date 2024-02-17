﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class EjectDriveAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		private DrivesViewModel DrivesViewModel { get; } = Ioc.Default.GetRequiredService<DrivesViewModel>();

		public string Label
			=> "EjectDrive".GetLocalizedResource();

		public string Description
			=> "EjectDrive".GetLocalizedResource();

		public bool IsExecutable
			=> GetIsExecutable();

		public EjectDriveAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var result = await DriveHelpers.EjectDeviceAsync(HomePageContext.RightClickedItem?.Path ?? string.Empty);

			if (HomePageContext.RightClickedItem is WidgetDriveCardItem driveCardItem &&
				driveCardItem.Item is DriveItem driveItem)
				await UIHelpers.ShowDeviceEjectResultAsync(driveItem.Type, result);
		}

		private bool GetIsExecutable()
		{
			var executableInHomePage =
				HomePageContext.IsAnyItemRightClicked &&
				HomePageContext.RightClickedItem is WidgetDriveCardItem driveCardItem &&
				driveCardItem.Item is DriveItem driveItem &&
				driveItem.MenuOptions.ShowEjectDevice;

			return executableInHomePage;
		}

		public void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasItem))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
