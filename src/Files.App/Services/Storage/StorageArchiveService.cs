// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using SevenZip;
using System.IO;
using Windows.Storage;
using Windows.Win32;

namespace Files.App.Services
{
	/// <inheritdoc cref="IStorageArchiveService"/>
	public class StorageArchiveService : IStorageArchiveService
	{
		private StatusCenterViewModel StatusCenterViewModel { get; } = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
		private IThreadingService ThreadingService { get; } = Ioc.Default.GetRequiredService<IThreadingService>();

		/// <inheritdoc/>
		public bool CanCompress(IReadOnlyList<ListedItem> items)
		{
			return
				CanDecompress(items) is false ||
				items.Count > 1;
		}

		/// <inheritdoc/>
		public bool CanDecompress(IReadOnlyList<ListedItem> items)
		{
			return
				items.Any() &&
				(items.All(x => x.IsArchive) ||
				items.All(x =>
					x.PrimaryItemAttribute == StorageItemTypes.File &&
					FileExtensionHelpers.IsZipFile(x.FileExtension)));
		}

		/// <inheritdoc/>
		public async Task<bool> CompressAsync(ICompressArchiveModel compressionModel)
		{
			var archivePath = compressionModel.GetArchivePath();

			int index = 1;

			while (SystemIO.File.Exists(archivePath) || SystemIO.Directory.Exists(archivePath))
				archivePath = compressionModel.GetArchivePath($" ({++index})");

			compressionModel.ArchivePath = archivePath;

			var banner = StatusCenterHelper.AddCard_Compress(
				compressionModel.Sources,
				archivePath.CreateEnumerable(),
				ReturnResult.InProgress,
				compressionModel.Sources.Count());

			compressionModel.Progress = banner.ProgressEventSource;
			compressionModel.CancellationToken = banner.CancellationToken;

			bool isSuccess = await compressionModel.RunCreationAsync();

			StatusCenterViewModel.RemoveItem(banner);

			if (isSuccess)
			{
				StatusCenterHelper.AddCard_Compress(
					compressionModel.Sources,
					archivePath.CreateEnumerable(),
					ReturnResult.Success,
					compressionModel.Sources.Count());
			}
			else
			{
				PInvoke.DeleteFileFromApp(archivePath);

				StatusCenterHelper.AddCard_Compress(
					compressionModel.Sources,
					archivePath.CreateEnumerable(),
					compressionModel.CancellationToken.IsCancellationRequested
						? ReturnResult.Cancelled
						: ReturnResult.Failed,
					compressionModel.Sources.Count());
			}

			return isSuccess;
		}

		/// <inheritdoc/>
		public async Task<bool> DecompressAsync(string archiveFilePath, string destinationFolderPath, string password = "")
		{
			if (string.IsNullOrEmpty(archiveFilePath) ||
				string.IsNullOrEmpty(destinationFolderPath))
				return false;

			using var zipFile = await GetSevenZipExtractorAsync(archiveFilePath, password);
			if (zipFile is null)
				return false;

			// Initialize a new in-progress status card
			var statusCard = StatusCenterHelper.AddCard_Decompress(
				archiveFilePath.CreateEnumerable(),
				destinationFolderPath.CreateEnumerable(),
				ReturnResult.InProgress);

			// Check if the decompress operation canceled
			if (statusCard.CancellationToken.IsCancellationRequested)
				return false;

			StatusCenterItemProgressModel fsProgress = new(
				statusCard.ProgressEventSource,
				enumerationCompleted: true,
				FileSystemStatusCode.InProgress,
				zipFile.ArchiveFileData.Count(x => !x.IsDirectory));

			fsProgress.TotalSize = zipFile.ArchiveFileData.Select(x => (long)x.Size).Sum();
			fsProgress.Report();

			bool isSuccess = false;

			try
			{
				// TODO: Get this method return result
				await zipFile.ExtractArchiveAsync(destinationFolderPath);

				isSuccess = true;
			}
			catch
			{
				isSuccess = false;
			}
			finally
			{
				// Remove the in-progress status card
				StatusCenterViewModel.RemoveItem(statusCard);

				if (isSuccess)
				{
					// Successful
					StatusCenterHelper.AddCard_Decompress(
						archiveFilePath.CreateEnumerable(),
						destinationFolderPath.CreateEnumerable(),
						ReturnResult.Success);
				}
				else
				{
					// Error
					StatusCenterHelper.AddCard_Decompress(
						archiveFilePath.CreateEnumerable(),
						destinationFolderPath.CreateEnumerable(),
						statusCard.CancellationToken.IsCancellationRequested
							? ReturnResult.Cancelled
							: ReturnResult.Failed);
				}
			}

			zipFile.Extracting += (s, e) =>
			{
				if (fsProgress.TotalSize > 0)
					fsProgress.Report(e.BytesProcessed / (double)fsProgress.TotalSize * 100);
			};

			zipFile.FileExtractionStarted += (s, e) =>
			{
				if (statusCard.CancellationToken.IsCancellationRequested)
					e.Cancel = true;

				if (!e.FileInfo.IsDirectory)
				{
					ThreadingService.ExecuteOnUiThreadAsync(() =>
					{
						fsProgress.FileName = e.FileInfo.FileName;
						fsProgress.Report();
					});
				}
			};

			zipFile.FileExtractionFinished += (s, e) =>
			{
				if (!e.FileInfo.IsDirectory)
				{
					fsProgress.AddProcessedItemsCount(1);
					fsProgress.Report();
				}
			};

			return isSuccess;
		}

		/// <inheritdoc/>
		public string GenerateArchiveNameFromItems(IReadOnlyList<ListedItem> items)
		{
			if (!items.Any())
				return string.Empty;

			return
				SystemIO.Path.GetFileName(
					items.Count is 1
						? items[0].ItemPath
						: SystemIO.Path.GetDirectoryName(items[0].ItemPath))
					?? string.Empty;
		}

		/// <inheritdoc/>
		public async Task<bool> IsEncryptedAsync(string archiveFilePath)
		{
			using SevenZipExtractor? zipFile = await GetSevenZipExtractorAsync(archiveFilePath);
			if (zipFile is null)
				return true;

			return zipFile.ArchiveFileData.Any(file => file.Encrypted || file.Method.Contains("Crypto") || file.Method.Contains("AES"));
		}

		/// <inheritdoc/>
		public async Task<SevenZipExtractor?> GetSevenZipExtractorAsync(string archiveFilePath, string password = "")
		{
			return await FilesystemTasks.Wrap(async () =>
			{
				BaseStorageFile archive = await StorageHelpers.ToStorageItem<BaseStorageFile>(archiveFilePath);

				var extractor = new SevenZipExtractor(await archive.OpenStreamForReadAsync(), password);

				// Force to load archive (1665013614u)
				return extractor?.ArchiveFileData is null ? null : extractor;
			});
		}
	}
}
