// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;

namespace Files.App.Views.Properties
{
	public sealed partial class ShortcutPage : BasePropertiesPage
	{
		public ShortcutPage()
		{
			InitializeComponent();
		}

		public override async Task<bool> SaveChangesAsync()
		{
			var shortcutItem = BaseProperties switch
			{
				FileProperties properties => properties.Item,
				FolderProperties properties => properties.Item,
				_ => null
			} as ShortcutItem;

			if (shortcutItem is null)
				return true;

			ViewModel.RunAsAdmin = ViewModel.RunAsAdminEditedValue;
			ViewModel.ShortcutItemPath = ViewModel.ShortcutItemPathEditedValue;
			ViewModel.ShortcutItemWorkingDir = ViewModel.ShortcutItemWorkingDirEditedValue;
			ViewModel.ShortcutItemArguments = ViewModel.ShortcutItemArgumentsEditedValue;
			ViewModel.ShowWindowCommand = ViewModel.ShowWindowCommandEditedValue;

			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
				UIFilesystemHelpers.UpdateShortcutItemProperties(shortcutItem,
				ViewModel.ShortcutItemPath,
				ViewModel.ShortcutItemArguments,
				ViewModel.ShortcutItemWorkingDir,
				ViewModel.RunAsAdmin,
				ViewModel.ShowWindowCommand)
			);

			return true;
		}

		public override void Dispose()
		{
		}
	}
}
