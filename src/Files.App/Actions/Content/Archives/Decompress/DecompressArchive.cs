// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Files.Shared.Helpers;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Text;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Actions
{
	internal sealed class DecompressArchive : BaseDecompressArchiveAction
	{
		public override string Label
			=> "ExtractFiles".GetLocalizedResource();

		public override string Description
			=> "DecompressArchiveDescription".GetLocalizedResource();

		public override HotKey HotKey
			=> new(Keys.E, KeyModifiers.Ctrl);

		public DecompressArchive()
		{
		}

		public override async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return;

			BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(context.SelectedItem?.ItemPath ?? string.Empty);

			if (archive?.Path is null)
				return;

			var isArchiveEncrypted = await FilesystemTasks.Wrap(() => StorageArchiveService.IsEncryptedAsync(archive.Path));
			var password = string.Empty;

			DecompressArchiveDialog decompressArchiveDialog = new();
			DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
			{
				IsArchiveEncrypted = isArchiveEncrypted,
				ShowPathSelection = true
			};
			decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				decompressArchiveDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
			if (option != ContentDialogResult.Primary)
				return;

			if (isArchiveEncrypted && decompressArchiveViewModel.Password is not null)
				password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);

			// Check if archive still exists
			if (!StorageHelpers.Exists(archive.Path))
				return;

			BaseStorageFolder destinationFolder = decompressArchiveViewModel.DestinationFolder;
			string destinationFolderPath = decompressArchiveViewModel.DestinationFolderPath;

			if (destinationFolder is null)
			{
				BaseStorageFolder parentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(Path.GetDirectoryName(archive.Path) ?? string.Empty);
				destinationFolder = await FilesystemTasks.Wrap(() => parentFolder.CreateFolderAsync(Path.GetFileName(destinationFolderPath), CreationCollisionOption.GenerateUniqueName).AsTask());
			}

			// Operate decompress
			var result = await FilesystemTasks.Wrap(() =>
				StorageArchiveService.DecompressAsync(archive?.Path ?? string.Empty, destinationFolder?.Path ?? string.Empty, password));

			if (decompressArchiveViewModel.OpenDestinationFolderOnCompletion)
				await NavigationHelpers.OpenPath(destinationFolderPath, context.ShellPage, FilesystemItemType.Directory);
		}

		protected override bool CanDecompressInsideArchive()
		{
			return
				context.PageType == ContentPageTypes.ZipFolder &&
				!context.HasSelection &&
				context.Folder is not null &&
				FileExtensionHelpers.IsZipFile(Path.GetExtension(context.Folder.ItemPath));
		}
	}
}
