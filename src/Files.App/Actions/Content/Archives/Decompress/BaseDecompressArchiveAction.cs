// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;
using SevenZip;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Actions
{
	internal abstract class BaseDecompressArchiveAction : BaseUIAction, IAction
	{
		protected readonly IContentPageContext context;
		protected IStorageArchiveService StorageArchiveService { get; } = Ioc.Default.GetRequiredService<IStorageArchiveService>();

		public abstract string Label { get; }

		public abstract string Description { get; }

		public virtual HotKey HotKey
			=> HotKey.None;

		public override bool IsExecutable =>
			(IsContextPageTypeAdaptedToCommand() &&
			StorageArchiveService.CanDecompress(context.SelectedItems) ||
			CanDecompressInsideArchive()) &&
			UIHelpers.CanShowDialog;

		public BaseDecompressArchiveAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public abstract Task ExecuteAsync(object? parameter = null);

		protected bool IsContextPageTypeAdaptedToCommand()
		{
			return
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.ZipFolder &&
				context.PageType != ContentPageTypes.None;
		}

		protected async Task DecompressArchiveHereAsync(bool smart = false)
		{
			if (context.SelectedItems.Count is 0)
				return;

			foreach (var selectedItem in context.SelectedItems)
			{
				var password = string.Empty;
				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(selectedItem.ItemPath);
				BaseStorageFolder currentFolder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(context.ShellPage?.ShellViewModel.CurrentFolder?.ItemPath ?? string.Empty);

				if (archive?.Path is null)
					return;

				if (await FilesystemTasks.Wrap(() => StorageArchiveService.IsEncryptedAsync(archive.Path)))
				{
					DecompressArchiveDialog decompressArchiveDialog = new();
					DecompressArchiveDialogViewModel decompressArchiveViewModel = new(archive)
					{
						IsArchiveEncrypted = true,
						ShowPathSelection = false
					};

					decompressArchiveDialog.ViewModel = decompressArchiveViewModel;

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						decompressArchiveDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					ContentDialogResult option = await decompressArchiveDialog.TryShowAsync();
					if (option != ContentDialogResult.Primary)
						return;

					if (decompressArchiveViewModel.Password is not null)
						password = Encoding.UTF8.GetString(decompressArchiveViewModel.Password);
				}

				BaseStorageFolder? destinationFolder = null;

				var isMultipleItems = await FilesystemTasks.Wrap(async () =>
				{
					using SevenZipExtractor? zipFile = await StorageArchiveService.GetSevenZipExtractorAsync(archive.Path);
					if (zipFile is null)
						return true;

					return zipFile.ArchiveFileData.Select(file =>
					{
						var pathCharIndex = file.FileName.IndexOfAny(['/', '\\']);
						if (pathCharIndex == -1)
							return file.FileName;
						else
							return file.FileName.Substring(0, pathCharIndex);
					})
					.Distinct().Count() > 1;
				});

				if (smart && currentFolder is not null && isMultipleItems)
				{
					destinationFolder =
						await FilesystemTasks.Wrap(() =>
							currentFolder.CreateFolderAsync(
								SystemIO.Path.GetFileNameWithoutExtension(archive.Path),
								CreationCollisionOption.GenerateUniqueName).AsTask());
				}
				else
				{
					destinationFolder = currentFolder;
				}

				// Operate decompress
				var result = await FilesystemTasks.Wrap(() =>
					StorageArchiveService.DecompressAsync(selectedItem.ItemPath, destinationFolder?.Path ?? string.Empty, password));
			}
		}

		protected virtual bool CanDecompressInsideArchive()
		{
			return false;
		}

		protected virtual void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
