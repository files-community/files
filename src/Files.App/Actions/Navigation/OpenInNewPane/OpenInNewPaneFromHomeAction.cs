// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class OpenInNewPaneFromHomeAction : BaseOpenInNewPaneAction
	{
		public override bool IsExecutable =>
			HomePageContext.IsAnyItemRightClicked &&
			HomePageContext.RightClickedItem is not null &&
			(HomePageContext.RightClickedItem is WidgetFileTagCardItem fileTagItem
				? fileTagItem.IsFolder
				: true);

		public override bool IsAccessibleGlobally
			=> false;

		public override async Task ExecuteAsync(object? parameter = null)
		{
			if (HomePageContext.RightClickedItem is null)
				return;

			if (await DriveHelpers.CheckEmptyDrive(HomePageContext.RightClickedItem!.Path))
				return;

			ContentPageContext.ShellPage!.PaneHolder?.OpenSecondaryPane(HomePageContext.RightClickedItem!.Path ?? string.Empty);
		}

		protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IHomePageContext.IsAnyItemRightClicked):
				case nameof(IHomePageContext.RightClickedItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
