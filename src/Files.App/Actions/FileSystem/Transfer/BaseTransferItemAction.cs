// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

namespace Files.App.Actions
{
	internal abstract class BaseTransferItemAction : ObservableObject
	{
		protected readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected readonly StatusCenterViewModel StatusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public bool IsExecutable
			=> ContentPageContext.HasSelection;

		public BaseTransferItemAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
		}

		public async Task ExecuteTransferAsync(DataPackageOperation type = DataPackageOperation.Copy)
		{
			if (ContentPageContext.ShellPage?.SlimContentPage is null ||
				ContentPageContext.ShellPage.SlimContentPage.IsItemSelected is false)
				return;

			// Reset cut mode
			ContentPageContext.ShellPage.SlimContentPage.ItemManipulationModel.RefreshItemsOpacity();

			ConcurrentBag<IStorageItem> items = [];
			var itemsCount = ContentPageContext.SelectedItems.Count;
			var statusCenterItem = itemsCount > 50 ? StatusCenterHelper.AddCard_Prepare() : null;
			var dataPackage = new DataPackage() { RequestedOperation = type };

			try
			{
				// Update the status to in-progress
				if (statusCenterItem is not null)
				{
					statusCenterItem.Progress.EnumerationCompleted = true;
					statusCenterItem.Progress.ItemsCount = items.Count;
					statusCenterItem.Progress.ReportStatus(FileSystemStatusCode.InProgress);
				}

				await ContentPageContext.SelectedItems.ToList().ParallelForEachAsync(async listedItem =>
				{
					// Update the status to increase processed count by one
					if (statusCenterItem is not null)
					{
						statusCenterItem.Progress.AddProcessedItemsCount(1);
						statusCenterItem.Progress.Report();
					}

					if (listedItem is FtpItem ftpItem)
					{
						// Don't dim selected items here since FTP doesn't support cut
						if (ftpItem.PrimaryItemAttribute is StorageItemTypes.File or StorageItemTypes.Folder)
							items.Add(await ftpItem.ToStorageItem());
					}
					else
					{
						if (type is DataPackageOperation.Move)
						{
							// Dim opacities accordingly
							await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
							{
								listedItem.Opacity = Constants.UI.DimItemOpacity;
							});
						}

						FilesystemResult? result =
							listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem is ZipItem
								? await ContentPageContext.ShellPage.ShellViewModel.GetFileFromPathAsync(listedItem.ItemPath).OnSuccess(t => items.Add(t))
								: await ContentPageContext.ShellPage.ShellViewModel.GetFolderFromPathAsync(listedItem.ItemPath).OnSuccess(t => items.Add(t));

						if (!result)
							throw new IOException($"Failed to process {listedItem.ItemPath} in cutting/copying to the clipboard.", (int)result.ErrorCode);
					}
				},
				10,
				statusCenterItem?.CancellationToken ?? default);

				var standardObjectsOnly = items.All(x => x is StorageFile or StorageFolder or SystemStorageFile or SystemStorageFolder);
				if (standardObjectsOnly)
					items = new ConcurrentBag<IStorageItem>(await items.ToStandardStorageItemsAsync());

				if (items.IsEmpty)
					return;

				dataPackage.Properties.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
				dataPackage.SetStorageItems(items, false);

				Clipboard.SetContent(dataPackage);
			}
			catch (Exception ex)
			{
				dataPackage = default;

				if (ex is not IOException)
					App.Logger.LogWarning(ex, "Failed to process cutting/copying due to an unknown error.");

				if ((FileSystemStatusCode)ex.HResult is FileSystemStatusCode.Unauthorized)
				{
					string[] filePaths = ContentPageContext.SelectedItems.Select(x => x.ItemPath).ToArray();
					await FileOperationsHelpers.SetClipboard(filePaths, type);

					return;
				}

				// Reset cut mode
				ContentPageContext.ShellPage.SlimContentPage.ItemManipulationModel.RefreshItemsOpacity();

				return;
			}
			finally
			{
				if (statusCenterItem is not null)
					StatusCenterViewModel.RemoveItem(statusCenterItem);
			}
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
