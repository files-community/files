// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Storage;

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Provides group of shell file system operation for given page instance.
	/// </summary>
	public sealed class ShellFilesystemOperations : IFilesystemOperations
	{
		private readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();

		private IShellPage _associatedInstance;

		private FilesystemOperations _filesystemOperations;

		public ShellFilesystemOperations(IShellPage associatedInstance)
		{
			_associatedInstance = associatedInstance;
			_filesystemOperations = new FilesystemOperations(associatedInstance);
		}

		public Task<IStorageHistory> CopyAsync(IStorageItem source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return CopyAsync(source.FromStorageItem(), destination, collision, progress, cancellationToken);
		}

		public Task<IStorageHistory> CopyAsync(IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return CopyItemsAsync(source.CreateList(), destination.CreateList(), collision.ConvertBack().CreateList(), progress, cancellationToken);
		}

		public async Task<IStorageHistory> CopyItemsAsync(IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return await CopyItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, cancellationToken);
		}

		public async Task<IStorageHistory> CopyItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken, bool asAdmin = false)
		{
			if (source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x.Path) || ZipStorageFolder.IsZipPath(x.Path, false))
				|| destination.Any(x => string.IsNullOrWhiteSpace(x) || x.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x) || ZipStorageFolder.IsZipPath(x, false)))
			{
				// Fallback to built-in file operations
				return await _filesystemOperations.CopyItemsAsync(source, destination, collisions, progress, cancellationToken);
			}

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				true,
				FileSystemStatusCode.InProgress,
				source.Count);

			fsProgress.Report();

			var sourceNoSkip = source.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
			var destinationNoSkip = destination.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
			var collisionsNoSkip = collisions.Where(c => c != FileNameConflictResolveOptionType.Skip);

			var operationID = Guid.NewGuid().ToString();

			await using var r = cancellationToken.Register(CancelOperation, operationID, false);

			var sourceReplace = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
			var destinationReplace = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
			var sourceRename = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
			var destinationRename = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);

			var result = (FilesystemResult)true;
			var copyResult = new ShellOperationResult();

			if (sourceRename.Any())
			{
				var resultItem = await FileOperationsHelpers.CopyItemAsync(sourceRename.Select(s => s.Path).ToArray(), destinationRename.ToArray(), false, MainWindow.Instance.WindowHandle.ToInt64(), asAdmin, progress, operationID);

				result &= (FilesystemResult)resultItem.Item1;

				copyResult.Items.AddRange(resultItem.Item2?.Final ?? Enumerable.Empty<ShellOperationItemResult>());
			}

			if (sourceReplace.Any())
			{
				var resultItem = await FileOperationsHelpers.CopyItemAsync(sourceReplace.Select(s => s.Path).ToArray(), destinationReplace.ToArray(), true, MainWindow.Instance.WindowHandle.ToInt64(), asAdmin, progress, operationID);

				result &= (FilesystemResult)resultItem.Item1;

				copyResult.Items.AddRange(resultItem.Item2?.Final ?? Enumerable.Empty<ShellOperationItemResult>());
			}

			result &= (FilesystemResult)copyResult.Items.All(x => x.Succeeded);

			if (result)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Success);
				var copiedSources = copyResult.Items.Where(x => x.Succeeded && x.Destination is not null && x.Source != x.Destination);
				if (copiedSources.Any())
				{
					var sourceMatch = await copiedSources.Select(x => sourceRename
						.SingleOrDefault(s => s.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

					return new StorageHistory(
						FileOperationType.Copy,
						sourceMatch,
						await copiedSources.Zip(sourceMatch, (rSrc, oSrc) => new { rSrc, oSrc })
							.Select(item => StorageHelpers.FromPathAndType(item.rSrc.Destination, item.oSrc.ItemType)).ToListAsync());
				}

				// Cannot undo overwrite operation
				return null;
			}
			else
			{
				if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
				{
					if (!asAdmin && await RequestAdminOperation())
						return await CopyItemsAsync(source, destination, collisions, progress, cancellationToken, true);
				}
				else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse))
				{
					var failedSources = copyResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse);
					var filePath = failedSources.Select(x => x.HResult == CopyEngineResult.COPYENGINE_E_SHARING_VIOLATION_SRC ? x.Source : x.Destination);
					var lockingProcess = WhoIsLocking(filePath);

					switch (await GetFileInUseDialog(filePath, lockingProcess))
					{
						case DialogResult.Primary:
							var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
							var sourceMatch = await failedSources.Select(x => copyZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

							return await CopyItemsAsync(
								await sourceMatch.Select(x => x.src).ToListAsync(),
								await sourceMatch.Select(x => x.dest).ToListAsync(),
								await sourceMatch.Select(x => x.coll).ToListAsync(), progress, cancellationToken);
					}
				}
				else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong))
				{
					// Retry with the StorageFile API
					var failedSources = copyResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong);
					var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
					var sourceMatch = await failedSources.Select(x => copyZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

					return await _filesystemOperations.CopyItemsAsync(
						await sourceMatch.Select(x => x.src).ToListAsync(),
						await sourceMatch.Select(x => x.dest).ToListAsync(),
						await sourceMatch.Select(x => x.coll).ToListAsync(), progress, cancellationToken);
				}
				else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NotFound))
				{
					await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
				}
				else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.AlreadyExists))
				{
					await DialogDisplayHelper.ShowDialogAsync("ItemAlreadyExistsDialogTitle".GetLocalizedResource(), "ItemAlreadyExistsDialogContent".GetLocalizedResource());
				}
				else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.PropertyLoss))
				{
					var failedSources = copyResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.PropertyLoss);
					var filePath = failedSources.Select(x => x.Source);

					switch (await GetFileListDialog(filePath, "FilePropertiesCannotBeCopied".GetLocalizedResource(), "CopyFileWithoutProperties".GetLocalizedResource(), "OK".GetLocalizedResource(), "Cancel".GetLocalizedResource()))
					{
						case DialogResult.Primary:
							var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
							var sourceMatch = await failedSources.Select(x => copyZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

							return await CopyItemsAsync(
								await sourceMatch.Select(x => x.src).ToListAsync(),
								await sourceMatch.Select(x => x.dest).ToListAsync(),
								// Force collision option to "replace" to accept copying with property loss
								// Ok since property loss error is raised after checking if the destination already exists
								await sourceMatch.Select(x => FileNameConflictResolveOptionType.ReplaceExisting).ToListAsync(), progress, cancellationToken);
					}
				}
				else if (copyResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.FileTooLarge))
				{
					var failingItems = copyResult.Items
						.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.FileTooLarge)
						.Select(item => item.Source);

					await Ioc.Default.GetRequiredService<IDialogService>().ShowDialogAsync(new FileTooLargeDialogViewModel(failingItems));
				}
				// ADS
				else if (copyResult.Items.All(x => x.HResult == -1))
				{
					// Retry with the StorageFile API
					var failedSources = copyResult.Items.Where(x => !x.Succeeded);
					var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
					var sourceMatch = await failedSources.Select(x => copyZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

					return await _filesystemOperations.CopyItemsAsync(
						await sourceMatch.Select(x => x.src).ToListAsync(),
						await sourceMatch.Select(x => x.dest).ToListAsync(),
						await sourceMatch.Select(x => x.coll).ToListAsync(), progress, cancellationToken);
				}

				fsProgress.ReportStatus(CopyEngineResult.Convert(copyResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));

				return null;
			}
		}

		public async Task<(IStorageHistory, IStorageItem)> CreateAsync(IStorageItemWithPath source, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken, bool asAdmin = false)
		{
			if (string.IsNullOrWhiteSpace(source.Path) || source.Path.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(source.Path) || ZipStorageFolder.IsZipPath(source.Path, false))
			{
				// Fallback to built-in file operations
				return await _filesystemOperations.CreateAsync(source, progress, cancellationToken);
			}

			StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress, 1);
			fsProgress.Report();

			var createResult = new ShellOperationResult();
			(var success, var response) = (false, new ShellOperationResult());

			switch (source.ItemType)
			{
				case FilesystemItemType.File:
					{
						var newEntryInfo = await ShellNewEntryExtensions.GetNewContextMenuEntryForType(Path.GetExtension(source.Path));
						if (newEntryInfo?.Command is not null)
						{
							var args = CommandLineParser.SplitArguments(newEntryInfo.Command);
							if (args.Any())
							{
								if (await LaunchHelper.LaunchAppAsync(
										args[0].Replace("\"", "", StringComparison.Ordinal),
										string.Join(' ', args.Skip(1)).Replace("%1", source.Path),
										PathNormalization.GetParentDir(source.Path)))
								{
									success = true;
								}
							}
						}
						else
						{
							(success, response) = await FileOperationsHelpers.CreateItemAsync(source.Path, "CreateFile", MainWindow.Instance.WindowHandle.ToInt64(), asAdmin, newEntryInfo?.Template, newEntryInfo?.Data);
						}

						break;
					}
				case FilesystemItemType.Directory:
					{
						(success, response) = await FileOperationsHelpers.CreateItemAsync(source.Path, "CreateFolder", MainWindow.Instance.WindowHandle.ToInt64(), asAdmin);
						break;
					}
			}

			var result = (FilesystemResult)success;
			var shellOpResult = response;
			createResult.Items.AddRange(shellOpResult?.Final ?? Enumerable.Empty<ShellOperationItemResult>());

			result &= (FilesystemResult)createResult.Items.All(x => x.Succeeded);

			if (result)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Success);

				var createdSources = createResult.Items.Where(x => x.Succeeded && x.Destination is not null && x.Source != x.Destination);
				if (createdSources.Any())
				{
					var item = StorageHelpers.FromPathAndType(createdSources.Single().Destination, source.ItemType);
					var storageItem = await item.ToStorageItem();

					return (new StorageHistory(FileOperationType.CreateNew, item.CreateList(), null), storageItem);
				}

				return (null, null);
			}
			else
			{
				if (createResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
				{
					if (!asAdmin && await RequestAdminOperation())
						return await CreateAsync(source, progress, cancellationToken, true);
				}
				else if (createResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong))
				{
					// Retry with the StorageFile API
					return await _filesystemOperations.CreateAsync(source, progress, cancellationToken);
				}
				else if (createResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NotFound))
				{
					await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
				}
				else if (createResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.AlreadyExists))
				{
					await DialogDisplayHelper.ShowDialogAsync("ItemAlreadyExistsDialogTitle".GetLocalizedResource(), "ItemAlreadyExistsDialogContent".GetLocalizedResource());
				}

				fsProgress.ReportStatus(CopyEngineResult.Convert(createResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));

				return (null, null);
			}
		}

		public async Task<IStorageHistory> CreateShortcutItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			var createdSources = new List<IStorageItemWithPath>();
			var createdDestination = new List<IStorageItemWithPath>();

			StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
			fsProgress.Report();

			var items = source.Zip(destination, (src, dest) => new { src, dest }).Where(x => !string.IsNullOrEmpty(x.src.Path) && !string.IsNullOrEmpty(x.dest));
			foreach (var item in items)
			{
				var result = await FileOperationsHelpers.CreateOrUpdateLinkAsync(item.dest, item.src.Path);

				if (!result)
					result = await UIFilesystemHelpers.HandleShortcutCannotBeCreated(Path.GetFileName(item.dest), item.src.Path);

				if (result)
				{
					createdSources.Add(item.src);
					createdDestination.Add(StorageHelpers.FromPathAndType(item.dest, FilesystemItemType.File));
				}

				fsProgress.AddProcessedItemsCount(1);
				fsProgress.Report();
			}

			fsProgress.ReportStatus(createdSources.Count == source.Count ? FileSystemStatusCode.Success : FileSystemStatusCode.Generic);

			return new StorageHistory(FileOperationType.CreateLink, createdSources, createdDestination);
		}

		public Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
		{
			return DeleteAsync(source.FromStorageItem(), progress, permanently, cancellationToken);
		}

		public Task<IStorageHistory> DeleteAsync(IStorageItemWithPath source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
		{
			return DeleteItemsAsync(source.CreateList(), progress, permanently, cancellationToken);
		}

		public async Task<IStorageHistory> DeleteItemsAsync(IList<IStorageItem> source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
		{
			return await DeleteItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), progress, permanently, cancellationToken);
		}

		public async Task<IStorageHistory> DeleteItemsAsync(IList<IStorageItemWithPath> source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken, bool asAdmin = false)
		{
			if (source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x.Path)))
			{
				// Fallback to built-in file operations
				return await _filesystemOperations.DeleteItemsAsync(source, progress, permanently, cancellationToken);
			}

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				true,
				FileSystemStatusCode.InProgress,
				source.Count);

			fsProgress.Report();

			var deleteFilePaths = source.Select(s => s.Path).Distinct();
			var deleteFromRecycleBin = source.Any() && StorageTrashBinService.IsUnderTrashBin(source.ElementAt(0).Path);

			permanently |= deleteFromRecycleBin;

			if (deleteFromRecycleBin)
			{
				// Recycle bin also stores a file starting with $I for each item
				deleteFilePaths = deleteFilePaths.Concat(source.Select(x => Path.Combine(Path.GetDirectoryName(x.Path), Path.GetFileName(x.Path).Replace("$R", "$I", StringComparison.Ordinal)))).Distinct();
			}

			var operationID = Guid.NewGuid().ToString();
			await using var r = cancellationToken.Register(CancelOperation, operationID, false);

			var (success, response) = await FileOperationsHelpers.DeleteItemAsync(deleteFilePaths.ToArray(), permanently, MainWindow.Instance.WindowHandle.ToInt64(), asAdmin, progress, operationID);

			var result = (FilesystemResult)success;
			var deleteResult = new ShellOperationResult();
			deleteResult.Items.AddRange(response?.Final ?? Enumerable.Empty<ShellOperationItemResult>());

			result &= (FilesystemResult)deleteResult.Items.All(x => x.Succeeded);

			if (result)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Success);

				foreach (var item in deleteResult.Items)
				{
					await _associatedInstance.ShellViewModel.RemoveFileOrFolderAsync(item.Source);
				}

				var recycledSources = deleteResult.Items.Where(x => x.Succeeded && x.Destination is not null && x.Source != x.Destination);
				if (recycledSources.Any())
				{
					var sourceMatch = await recycledSources.Select(x => source.DistinctBy(x => x.Path)
						.SingleOrDefault(s => s.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

					return new StorageHistory(
						FileOperationType.Recycle,
						sourceMatch,
						await recycledSources.Zip(sourceMatch, (rSrc, oSrc) => new { rSrc, oSrc })
							.Select(item => StorageHelpers.FromPathAndType(item.rSrc.Destination, item.oSrc.ItemType)).ToListAsync());
				}

				return new StorageHistory(FileOperationType.Delete, source, null);
			}
			else
			{
				if (deleteResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
				{
					if (!asAdmin && await RequestAdminOperation())
						return await DeleteItemsAsync(source, progress, permanently, cancellationToken, true);
				}
				else if (deleteResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse))
				{
					var failedSources = deleteResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse);
					var filePath = failedSources.Select(x => x.Source); // When deleting only source can be in use but shell returns COPYENGINE_E_SHARING_VIOLATION_DEST for folders
					var lockingProcess = WhoIsLocking(filePath);

					switch (await GetFileInUseDialog(filePath, lockingProcess))
					{
						case DialogResult.Primary:
							return await DeleteItemsAsync(await failedSources.Select(x => source.DistinctBy(x => x.Path).SingleOrDefault(s => s.Path == x.Source)).Where(x => x is not null).ToListAsync(), progress, permanently, cancellationToken);
					}
				}
				else if (deleteResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong))
				{
					// Abort, path is too long for recycle bin
				}
				else if (deleteResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NotFound))
				{
					await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
				}
				else if (deleteResult.Items.All(x => x.HResult == -1) && permanently) // ADS
				{
					// Retry with StorageFile API
					var failedSources = deleteResult.Items.Where(x => !x.Succeeded);
					var sourceMatch = await failedSources.Select(x => source.DistinctBy(x => x.Path).SingleOrDefault(s => s.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

					return await _filesystemOperations.DeleteItemsAsync(sourceMatch, progress, permanently, cancellationToken);
				}

				fsProgress.ReportStatus(CopyEngineResult.Convert(deleteResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));

				return null;
			}
		}

		public Task<IStorageHistory> MoveAsync(IStorageItem source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return MoveAsync(source.FromStorageItem(), destination, collision, progress, cancellationToken);
		}

		public Task<IStorageHistory> MoveAsync(IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return MoveItemsAsync(source.CreateList(), destination.CreateList(), collision.ConvertBack().CreateList(), progress, cancellationToken);
		}

		public async Task<IStorageHistory> MoveItemsAsync(IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return await MoveItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, cancellationToken);
		}

		public async Task<IStorageHistory> MoveItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken, bool asAdmin = false)
		{
			if (source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal)) || destination.Any(x => string.IsNullOrWhiteSpace(x) || x.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x) || ZipStorageFolder.IsZipPath(x, false)))
			{
				// Fallback to built-in file operations
				return await _filesystemOperations.MoveItemsAsync(source, destination, collisions, progress, cancellationToken);
			}

			StatusCenterItemProgressModel fsProgress = new(
				progress,
				true,
				FileSystemStatusCode.InProgress,
				source.Count);

			fsProgress.Report();

			var sourceNoSkip = source.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
			var destinationNoSkip = destination.Zip(collisions, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.Skip).Select(item => item.src);
			var collisionsNoSkip = collisions.Where(c => c != FileNameConflictResolveOptionType.Skip);
			var operationID = Guid.NewGuid().ToString();

			await using var r = cancellationToken.Register(CancelOperation, operationID, false);

			var sourceReplace = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
			var destinationReplace = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll == FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
			var sourceRename = sourceNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
			var destinationRename = destinationNoSkip.Zip(collisionsNoSkip, (src, coll) => new { src, coll }).Where(item => item.coll != FileNameConflictResolveOptionType.ReplaceExisting).Select(item => item.src);
			var result = (FilesystemResult)true;
			var moveResult = new ShellOperationResult();

			if (sourceRename.Any())
			{
				var (status, response) = await FileOperationsHelpers.MoveItemAsync(sourceRename.Select(s => s.Path).ToArray(), destinationRename.ToArray(), false, MainWindow.Instance.WindowHandle.ToInt64(), asAdmin, progress, operationID);

				result &= (FilesystemResult)status;
				moveResult.Items.AddRange(response?.Final ?? Enumerable.Empty<ShellOperationItemResult>());
			}

			if (sourceReplace.Any())
			{
				var (status, response) = await FileOperationsHelpers.MoveItemAsync(sourceReplace.Select(s => s.Path).ToArray(), destinationReplace.ToArray(), true, MainWindow.Instance.WindowHandle.ToInt64(), asAdmin, progress, operationID);

				result &= (FilesystemResult)status;
				moveResult.Items.AddRange(response?.Final ?? Enumerable.Empty<ShellOperationItemResult>());
			}

			result &= (FilesystemResult)moveResult.Items.All(x => x.Succeeded);

			if (result)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Success);

				var movedSources = moveResult.Items.Where(x => x.Succeeded && x.Destination is not null && x.Source != x.Destination);
				if (movedSources.Any())
				{
					var sourceMatch = await movedSources.Select(x => sourceRename
						.SingleOrDefault(s => s.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

					return new StorageHistory(FileOperationType.Move,
						sourceMatch,
						await movedSources.Zip(sourceMatch, (rSrc, oSrc) => new { rSrc, oSrc })
							.Select(item => StorageHelpers.FromPathAndType(item.rSrc.Destination, item.oSrc.ItemType)).ToListAsync());
				}

				// Cannot undo overwrite operation
				return null;
			}
			else
			{
				fsProgress.ReportStatus(CopyEngineResult.Convert(moveResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));
				if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
				{
					if (!asAdmin && await RequestAdminOperation())
						return await MoveItemsAsync(source, destination, collisions, progress, cancellationToken, true);
				}
				else if (source.Zip(destination, (src, dest) => (src, dest)).FirstOrDefault(x => x.src.ItemType == FilesystemItemType.Directory && PathNormalization.GetParentDir(x.dest).IsSubPathOf(x.src.Path)) is (IStorageItemWithPath, string) subtree)
				{
					var destName = subtree.dest.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
					var srcName = subtree.src.Path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();

					await DialogDisplayHelper.ShowDialogAsync("ErrorDialogThisActionCannotBeDone".GetLocalizedResource(), $"{"ErrorDialogTheDestinationFolder".GetLocalizedResource()} ({destName}) {"ErrorDialogIsASubfolder".GetLocalizedResource()} ({srcName})");
				}
				else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse))
				{
					var failedSources = moveResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse);
					var filePath = failedSources.Select(x => x.HResult == CopyEngineResult.COPYENGINE_E_SHARING_VIOLATION_SRC ? x.Source : x.Destination);
					var lockingProcess = WhoIsLocking(filePath);

					switch (await GetFileInUseDialog(filePath, lockingProcess))
					{
						case DialogResult.Primary:
							var moveZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
							var sourceMatch = await failedSources.Select(x => moveZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

							return await MoveItemsAsync(
								await sourceMatch.Select(x => x.src).ToListAsync(),
								await sourceMatch.Select(x => x.dest).ToListAsync(),
								await sourceMatch.Select(x => x.coll).ToListAsync(), progress, cancellationToken);
					}
				}
				else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong))
				{
					// Retry with the StorageFile API
					var failedSources = moveResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong);
					var moveZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
					var sourceMatch = await failedSources.Select(x => moveZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

					return await _filesystemOperations.MoveItemsAsync(
						await sourceMatch.Select(x => x.src).ToListAsync(),
						await sourceMatch.Select(x => x.dest).ToListAsync(),
						await sourceMatch.Select(x => x.coll).ToListAsync(), progress, cancellationToken);
				}
				else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NotFound))
				{
					await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
				}
				else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.AlreadyExists))
				{
					await DialogDisplayHelper.ShowDialogAsync("ItemAlreadyExistsDialogTitle".GetLocalizedResource(), "ItemAlreadyExistsDialogContent".GetLocalizedResource());
				}
				else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.PropertyLoss))
				{
					var failedSources = moveResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.PropertyLoss);
					var filePath = failedSources.Select(x => x.Source);
					switch (await GetFileListDialog(filePath, "FilePropertiesCannotBeMoved".GetLocalizedResource(), "MoveFileWithoutProperties".GetLocalizedResource(), "OK".GetLocalizedResource(), "Cancel".GetLocalizedResource()))
					{
						case DialogResult.Primary:
							var copyZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
							var sourceMatch = await failedSources.Select(x => copyZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();
							return await CopyItemsAsync(
								await sourceMatch.Select(x => x.src).ToListAsync(),
								await sourceMatch.Select(x => x.dest).ToListAsync(),
								// Force collision option to "replace" to accept moving with property loss
								// Ok since property loss error is raised after checking if the destination already exists
								await sourceMatch.Select(x => FileNameConflictResolveOptionType.ReplaceExisting).ToListAsync(), progress, cancellationToken);
					}
				}
				else if (moveResult.Items.All(x => x.HResult == -1)) // ADS
				{
					// Retry with the StorageFile API
					var failedSources = moveResult.Items.Where(x => !x.Succeeded);
					var moveZip = sourceNoSkip.Zip(destinationNoSkip, (src, dest) => new { src, dest }).Zip(collisionsNoSkip, (z1, coll) => new { z1.src, z1.dest, coll });
					var sourceMatch = await failedSources.Select(x => moveZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

					return await _filesystemOperations.MoveItemsAsync(
						await sourceMatch.Select(x => x.src).ToListAsync(),
						await sourceMatch.Select(x => x.dest).ToListAsync(),
						await sourceMatch.Select(x => x.coll).ToListAsync(), progress, cancellationToken);
				}

				return null;
			}
		}

		public Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return RenameAsync(StorageHelpers.FromStorageItem(source), newName, collision, progress, cancellationToken);
		}

		public async Task<IStorageHistory> RenameAsync(IStorageItemWithPath source, string newName, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken, bool asAdmin = false)
		{
			if (string.IsNullOrWhiteSpace(source.Path) || source.Path.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(source.Path) || ZipStorageFolder.IsZipPath(source.Path, false))
			{
				// Fallback to built-in file operations
				return await _filesystemOperations.RenameAsync(source, newName, collision, progress, cancellationToken);
			}

			StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
			fsProgress.Report();

			var renameResult = new ShellOperationResult();
			var (status, response) = await FileOperationsHelpers.RenameItemAsync(source.Path, newName, collision == NameCollisionOption.ReplaceExisting, MainWindow.Instance.WindowHandle.ToInt64(), asAdmin);
			var result = (FilesystemResult)status;

			renameResult.Items.AddRange(response?.Final ?? Enumerable.Empty<ShellOperationItemResult>());

			result &= (FilesystemResult)renameResult.Items.All(x => x.Succeeded);

			if (result)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Success);

				var renamedSources = renameResult.Items.Where(x => x.Succeeded && x.Destination is not null && x.Source != x.Destination)
					.Where(x => x.Source == source.Path);
				if (renamedSources.Any())
				{
					return new StorageHistory(FileOperationType.Rename, source,
						StorageHelpers.FromPathAndType(renamedSources.Single().Destination, source.ItemType));
				}

				// Cannot undo overwrite operation
				return null;
			}
			else
			{
				if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
				{
					if (!asAdmin && await RequestAdminOperation())
					{
						var res = await RenameAsync(source, newName, collision, progress, cancellationToken, true);
						if (res is null)
						{
							await DynamicDialogFactory
								.GetFor_RenameRequiresHigherPermissions(source.Path)
								.TryShowAsync();
						}

						return res;
					}
				}
				else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse))
				{
					var failedSources = renameResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse);
					var filePath = failedSources.Select(x => x.HResult == CopyEngineResult.COPYENGINE_E_SHARING_VIOLATION_SRC ? x.Source : x.Destination);
					var lockingProcess = WhoIsLocking(filePath);

					switch (await GetFileInUseDialog(filePath, lockingProcess))
					{
						case DialogResult.Primary:
							return await RenameAsync(source, newName, collision, progress, cancellationToken);
					}
				}
				else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong))
				{
					// Retry with the StorageFile API
					return await _filesystemOperations.RenameAsync(source, newName, collision, progress, cancellationToken);
				}
				else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NotFound))
				{
					await DialogDisplayHelper.ShowDialogAsync("RenameError/ItemDeleted/Title".GetLocalizedResource(), "RenameError/ItemDeleted/Text".GetLocalizedResource());
				}
				else if (renameResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.AlreadyExists))
				{
					await DialogDisplayHelper.ShowDialogAsync("ItemAlreadyExistsDialogTitle".GetLocalizedResource(), "ItemAlreadyExistsDialogContent".GetLocalizedResource());
				}
				// ADS
				else if (renameResult.Items.All(x => x.HResult == -1))
				{
					// Retry with StorageFile API
					return await _filesystemOperations.RenameAsync(source, newName, collision, progress, cancellationToken);
				}

				fsProgress.ReportStatus(CopyEngineResult.Convert(renameResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));

				return null;
			}
		}

		public Task<IStorageHistory> RestoreFromTrashAsync(IStorageItem source, string destination, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return RestoreFromTrashAsync(source.FromStorageItem(), destination, progress, cancellationToken);
		}

		public Task<IStorageHistory> RestoreFromTrashAsync(IStorageItemWithPath source, string destination, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return RestoreItemsFromTrashAsync(source.CreateList(), destination.CreateList(), progress, cancellationToken);
		}

		public async Task<IStorageHistory> RestoreItemsFromTrashAsync(IList<IStorageItem> source, IList<string> destination, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return await RestoreItemsFromTrashAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, progress, cancellationToken);
		}

		public async Task<IStorageHistory> RestoreItemsFromTrashAsync(IList<IStorageItemWithPath> source, IList<string> destination, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken, bool asAdmin = false)
		{
			if (source.Any(x => string.IsNullOrWhiteSpace(x.Path) || x.Path.StartsWith(@"\\?\", StringComparison.Ordinal)) || destination.Any(x => string.IsNullOrWhiteSpace(x) || x.StartsWith(@"\\?\", StringComparison.Ordinal) || FtpHelpers.IsFtpPath(x) || ZipStorageFolder.IsZipPath(x, false)))
			{
				// Fallback to built-in file operations
				return await _filesystemOperations.RestoreItemsFromTrashAsync(source, destination, progress, cancellationToken);
			}

			StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
			fsProgress.Report();
			var operationID = Guid.NewGuid().ToString();
			await using var r = cancellationToken.Register(CancelOperation, operationID, false);

			var moveResult = new ShellOperationResult();
			var (status, response) = await FileOperationsHelpers.MoveItemAsync(source.Select(s => s.Path).ToArray(), [.. destination], false, MainWindow.Instance.WindowHandle.ToInt64(), asAdmin, progress, operationID);

			var result = (FilesystemResult)status;
			moveResult.Items.AddRange(response?.Final ?? Enumerable.Empty<ShellOperationItemResult>());

			result &= (FilesystemResult)moveResult.Items.All(x => x.Succeeded);

			if (result)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Success);

				var movedSources = moveResult.Items.Where(x => x.Succeeded && x.Destination is not null && x.Source != x.Destination);
				if (movedSources.Any())
				{
					var sourceMatch = await movedSources.Select(x => source
						.SingleOrDefault(s => s.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

					// Recycle bin also stores a file starting with $I for each item
					await DeleteItemsAsync(await movedSources.Zip(sourceMatch, (rSrc, oSrc) => new { rSrc, oSrc })
						.Select(src => StorageHelpers.FromPathAndType(
							Path.Combine(Path.GetDirectoryName(src.rSrc.Source), Path.GetFileName(src.rSrc.Source).Replace("$R", "$I", StringComparison.Ordinal)),
							src.oSrc.ItemType)).ToListAsync(), null, true, cancellationToken);

					return new StorageHistory(FileOperationType.Restore,
						sourceMatch,
						await movedSources.Zip(sourceMatch, (rSrc, oSrc) => new { rSrc, oSrc })
							.Select(item => StorageHelpers.FromPathAndType(item.rSrc.Destination, item.oSrc.ItemType)).ToListAsync());
				}
				// Cannot undo overwrite operation
				return null;
			}
			else
			{
				if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.Unauthorized))
				{
					if (!asAdmin && await RequestAdminOperation())
						return await RestoreItemsFromTrashAsync(source, destination, progress, cancellationToken, true);
				}
				else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse))
				{
					var failedSources = moveResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.InUse);
					var filePath = failedSources.Select(x => x.HResult == CopyEngineResult.COPYENGINE_E_SHARING_VIOLATION_SRC ? x.Source : x.Destination);
					var lockingProcess = WhoIsLocking(filePath);

					switch (await GetFileInUseDialog(filePath, lockingProcess))
					{
						case DialogResult.Primary:
							var moveZip = source.Zip(destination, (src, dest) => new { src, dest });
							var sourceMatch = await failedSources.Select(x => moveZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

							return await RestoreItemsFromTrashAsync(
								await sourceMatch.Select(x => x.src).ToListAsync(),
								await sourceMatch.Select(x => x.dest).ToListAsync(), progress, cancellationToken);
					}
				}
				else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong))
				{
					// Retry with StorageFile API
					var failedSources = moveResult.Items.Where(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NameTooLong);
					var moveZip = source.Zip(destination, (src, dest) => new { src, dest });
					var sourceMatch = await failedSources.Select(x => moveZip.SingleOrDefault(s => s.src.Path.Equals(x.Source, StringComparison.OrdinalIgnoreCase))).Where(x => x is not null).ToListAsync();

					return await _filesystemOperations.RestoreItemsFromTrashAsync(
						await sourceMatch.Select(x => x.src).ToListAsync(),
						await sourceMatch.Select(x => x.dest).ToListAsync(), progress, cancellationToken);
				}
				else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.NotFound))
				{
					await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
				}
				else if (moveResult.Items.Any(x => CopyEngineResult.Convert(x.HResult) == FileSystemStatusCode.AlreadyExists))
				{
					await DialogDisplayHelper.ShowDialogAsync("ItemAlreadyExistsDialogTitle".GetLocalizedResource(), "ItemAlreadyExistsDialogContent".GetLocalizedResource());
				}

				fsProgress.ReportStatus(CopyEngineResult.Convert(moveResult.Items.FirstOrDefault(x => !x.Succeeded)?.HResult));

				return null;
			}
		}

		private void CancelOperation(object operationID)
		{
			FileOperationsHelpers.TryCancelOperation((string)operationID);
		}

		private async Task<bool> RequestAdminOperation()
		{
			var dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			return await dialogService.ShowDialogAsync(new ElevateConfirmDialogViewModel()) == DialogResult.Primary;
		}

		private Task<DialogResult> GetFileInUseDialog(IEnumerable<string> source, IEnumerable<Win32Process> lockingProcess = null)
		{
			var titleText = "FileInUseDialog/Title".GetLocalizedResource();
			var subtitleText = lockingProcess.IsEmpty()
				? "FileInUseDialog/Text".GetLocalizedResource()
				: string.Format("FileInUseByDialog/Text".GetLocalizedResource(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})")));

			return GetFileListDialog(source, titleText, subtitleText, "Retry".GetLocalizedResource(), "Cancel".GetLocalizedResource());
		}

		private async Task<DialogResult> GetFileListDialog(IEnumerable<string> source, string titleText, string descriptionText = null, string primaryButtonText = null, string secondaryButtonText = null)
		{
			var incomingItems = new List<BaseFileSystemDialogItemViewModel>();
			List<ShellFileItem> binItems = null;
			foreach (var src in source)
			{
				if (StorageTrashBinService.IsUnderTrashBin(src))
				{
					binItems ??= await StorageTrashBinService.GetAllRecycleBinFoldersAsync();

					// Might still be null because we're deserializing the list from Json
					if (!binItems.IsEmpty())
					{
						// Get original file name
						var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == src);

						incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src, DisplayName = matchingItem?.FileName });
					}
				}
				else
				{
					incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src });
				}
			}

			var dialogViewModel = FileSystemDialogViewModel.GetDialogViewModel(
				incomingItems, titleText, descriptionText, primaryButtonText, secondaryButtonText);

			var dialogService = Ioc.Default.GetRequiredService<IDialogService>();

			return await dialogService.ShowDialogAsync(dialogViewModel);
		}

		private IEnumerable<Win32Process> WhoIsLocking(IEnumerable<string> filesToCheck)
		{
			return FileOperationsHelpers.CheckFileInUse(filesToCheck.ToArray());
		}

		public void Dispose()
		{
			_filesystemOperations?.Dispose();

			_filesystemOperations = null;
			_associatedInstance = null;
		}
	}
}
