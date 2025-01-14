// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;

namespace Files.App.ViewModels.Dialogs
{
	public sealed class BulkRenameDialogViewModel : ObservableObject
	{
		private IContentPageContext context { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		// Properties

		public bool IsNameValid =>
			FilesystemHelpers.IsValidForFilename(_FileName) && !_FileName.Contains(".");

		public bool ShowNameWarning =>
			!string.IsNullOrEmpty(_FileName) && !IsNameValid;

		private string _FileName = string.Empty;
		public string FileName
		{
			get => _FileName;
			set
			{
				if (SetProperty(ref _FileName, value))
				{
					OnPropertyChanged(nameof(IsNameValid));
					OnPropertyChanged(nameof(ShowNameWarning));
				}
			}
		}

		// Commands

		public IAsyncRelayCommand CommitRenameCommand { get; private set; }

		public BulkRenameDialogViewModel()
		{
			CommitRenameCommand = new AsyncRelayCommand(DoCommitRenameAsync);
		}

		private async Task DoCommitRenameAsync()
		{
			if (context.ShellPage is null)
				return;

			foreach (ListedItem item in context.SelectedItems)
			{
				var itemType = item.PrimaryItemAttribute == StorageItemTypes.Folder ? FilesystemItemType.Directory : FilesystemItemType.File;
				await context.ShellPage.FilesystemHelpers.RenameAsync(
					StorageHelpers.FromPathAndType(item.ItemPath, itemType),
					FileName + item.FileExtension,
					NameCollisionOption.GenerateUniqueName,
					true,
					false
				);
			};
		}

	}
}