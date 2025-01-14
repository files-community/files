// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Core.Storage;
using Files.Core.Storage.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using FileAttributes = System.IO.FileAttributes;

namespace Files.App.Utils.Storage
{
	public sealed class FilesystemHelpers : IFilesystemHelpers
	{
		private readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();
		private readonly static StatusCenterViewModel _statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		private IShellPage associatedInstance;
		private readonly IWindowsJumpListService jumpListService;
		private ShellFilesystemOperations filesystemOperations;

		private ItemManipulationModel? itemManipulationModel => associatedInstance.SlimContentPage?.ItemManipulationModel;

		private readonly CancellationToken cancellationToken;
		private static char[] RestrictedCharacters
		{
			get
			{
				var userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
				return userSettingsService.FoldersSettingsService.AreAlternateStreamsVisible
					? ['\\', '/', '*', '?', '"', '<', '>', '|'] // Allow ":" char
					: ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];
			}
		}

		private static readonly string[] RestrictedFileNames =
		[
				"CON", "PRN", "AUX",
				"NUL", "COM1", "COM2",
				"COM3", "COM4", "COM5",
				"COM6", "COM7", "COM8",
				"COM9", "LPT1", "LPT2",
				"LPT3", "LPT4", "LPT5",
				"LPT6", "LPT7", "LPT8", "LPT9"
		];

		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		public FilesystemHelpers(IShellPage associatedInstance, CancellationToken cancellationToken)
		{
			this.associatedInstance = associatedInstance;
			this.cancellationToken = cancellationToken;
			jumpListService = Ioc.Default.GetRequiredService<IWindowsJumpListService>();
			filesystemOperations = new ShellFilesystemOperations(this.associatedInstance);
		}
		public async Task<(ReturnResult, IStorageItem?)> CreateAsync(IStorageItemWithPath source, bool registerHistory)
		{
			var returnStatus = ReturnResult.InProgress;
			var progress = new Progress<StatusCenterItemProgressModel>();
			progress.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

			if (!IsValidForFilename(source.Name))
			{
				await DialogDisplayHelper.ShowDialogAsync(
					"ErrorDialogThisActionCannotBeDone".GetLocalizedResource(),
					"ErrorDialogNameNotAllowed".GetLocalizedResource());
				return (ReturnResult.Failed, null);
			}

			var result = await filesystemOperations.CreateAsync(source, progress, cancellationToken);

			if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
			{
				App.HistoryWrapper.AddHistory(result.Item1);
			}

			await Task.Yield();
			return (returnStatus, result.Item2);
		}

		public async Task<ReturnResult> DeleteItemsAsync(IEnumerable<IStorageItemWithPath> source, DeleteConfirmationPolicies showDialog, bool permanently, bool registerHistory)
		{
			source = await source.ToListAsync();

			var returnStatus = ReturnResult.InProgress;

			var deleteFromRecycleBin = source.Select(item => item.Path).Any(StorageTrashBinService.IsUnderTrashBin);
			var canBeSentToBin = !deleteFromRecycleBin && await StorageTrashBinService.CanGoTrashBin(source.FirstOrDefault()?.Path);

			if (showDialog is DeleteConfirmationPolicies.Always ||
				showDialog is DeleteConfirmationPolicies.PermanentOnly &&
				(permanently || !canBeSentToBin))
			{
				var incomingItems = new List<BaseFileSystemDialogItemViewModel>();
				List<ShellFileItem>? binItems = null;

				foreach (var src in source)
				{
					if (StorageTrashBinService.IsUnderTrashBin(src.Path))
					{
						binItems ??= await StorageTrashBinService.GetAllRecycleBinFoldersAsync();

						// Might still be null because we're deserializing the list from Json
						if (!binItems.IsEmpty())
						{
							// Get original file name
							var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == src.Path);
							incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src.Path, DisplayName = matchingItem?.FileName ?? src.Name });
						}
					}
					else
					{
						incomingItems.Add(new FileSystemDialogDefaultItemViewModel() { SourcePath = src.Path });
					}
				}

				var dialogViewModel = FileSystemDialogViewModel.GetDialogViewModel(
					new() { IsInDeleteMode = true },
					(canBeSentToBin ? permanently : true, canBeSentToBin),
					FilesystemOperationType.Delete,
					incomingItems,
					[]);

				var dialogService = Ioc.Default.GetRequiredService<IDialogService>();

				// Return if the result isn't delete
				if (await dialogService.ShowDialogAsync(dialogViewModel) != DialogResult.Primary)
					return ReturnResult.Cancelled;

				// Delete selected items if the result is Yes
				permanently = dialogViewModel.DeletePermanently;
			}
			else
			{
				// Delete permanently if recycle bin is not supported
				permanently |= !canBeSentToBin;
			}

			// Add an in-progress card in the StatusCenter
			var banner = permanently
				? StatusCenterHelper.AddCard_Delete(returnStatus, source)
				: StatusCenterHelper.AddCard_Recycle(returnStatus, source);

			banner.ProgressEventSource.ProgressChanged += (s, e)
				=> returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

			var token = banner.CancellationToken;

			var sw = new Stopwatch();
			sw.Start();

			IStorageHistory history = await filesystemOperations.DeleteItemsAsync((IList<IStorageItemWithPath>)source, banner.ProgressEventSource, permanently, token);
			banner.Progress.ReportStatus(FileSystemStatusCode.Success);
			await Task.Yield();

			if (!permanently && registerHistory)
				App.HistoryWrapper.AddHistory(history);

			// Execute removal tasks concurrently in background
			var sourcePaths = source.Select(x => x.Path);
			_ = Task.WhenAll(sourcePaths.Select(jumpListService.RemoveFolderAsync));

			var itemsCount = banner.TotalItemsCount;

			// Remove the in-progress card from the StatusCenter
			_statusCenterViewModel.RemoveItem(banner);

			sw.Stop();

			// Add a complete card in the StatusCenter
			_ = permanently
				? StatusCenterHelper.AddCard_Delete(token.IsCancellationRequested ? ReturnResult.Cancelled : returnStatus, source, itemsCount)
				: StatusCenterHelper.AddCard_Recycle(token.IsCancellationRequested ? ReturnResult.Cancelled : returnStatus, source, itemsCount);

			return returnStatus;
		}

		public Task<ReturnResult> DeleteItemAsync(IStorageItemWithPath source, DeleteConfirmationPolicies showDialog, bool permanently, bool registerHistory)
			=> DeleteItemsAsync(source.CreateEnumerable(), showDialog, permanently, registerHistory);

		public Task<ReturnResult> DeleteItemsAsync(IEnumerable<IStorageItem> source, DeleteConfirmationPolicies showDialog, bool permanently, bool registerHistory)
			=> DeleteItemsAsync(source.Select((item) => item.FromStorageItem()), showDialog, permanently, registerHistory);

		public Task<ReturnResult> DeleteItemAsync(IStorageItem source, DeleteConfirmationPolicies showDialog, bool permanently, bool registerHistory)
			=> DeleteItemAsync(source.FromStorageItem(), showDialog, permanently, registerHistory);

		public Task<ReturnResult> RestoreItemFromTrashAsync(IStorageItem source, string destination, bool registerHistory)
			=> RestoreItemFromTrashAsync(source.FromStorageItem(), destination, registerHistory);

		public Task<ReturnResult> RestoreItemsFromTrashAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool registerHistory)
			=> RestoreItemsFromTrashAsync(source.Select((item) => item.FromStorageItem()), destination, registerHistory);

		public Task<ReturnResult> RestoreItemFromTrashAsync(IStorageItemWithPath source, string destination, bool registerHistory)
			=> RestoreItemsFromTrashAsync(source.CreateEnumerable(), destination.CreateEnumerable(), registerHistory);

		public async Task<ReturnResult> RestoreItemsFromTrashAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool registerHistory)
		{
			source = await source.ToListAsync();
			destination = await destination.ToListAsync();

			var returnStatus = ReturnResult.InProgress;
			var progress = new Progress<StatusCenterItemProgressModel>();
			progress.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

			var sw = new Stopwatch();
			sw.Start();

			IStorageHistory history = await filesystemOperations.RestoreItemsFromTrashAsync((IList<IStorageItemWithPath>)source, (IList<string>)destination, progress, cancellationToken);
			await Task.Yield();

			if (registerHistory && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
			{
				App.HistoryWrapper.AddHistory(history);
			}
			int itemsMoved = history?.Source.Count ?? 0;

			sw.Stop();

			return returnStatus;
		}

		public async Task<ReturnResult> PerformOperationTypeAsync(
			DataPackageOperation operation,
			DataPackageView packageView,
			string destination,
			bool showDialog,
			bool registerHistory,
			bool isTargetExecutable = false,
			bool isTargetScriptFile = false)
		{
			try
			{
				if (destination is null)
				{
					return default;
				}
				if (destination.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
				{
					return await RecycleItemsFromClipboard(packageView, destination, UserSettingsService.FoldersSettingsService.DeleteConfirmationPolicy, registerHistory);
				}
				else if (operation.HasFlag(DataPackageOperation.Move))
				{
					return await MoveItemsFromClipboard(packageView, destination, showDialog, registerHistory);
				}
				else if (operation.HasFlag(DataPackageOperation.Copy))
				{
					return await CopyItemsFromClipboard(packageView, destination, showDialog, registerHistory);
				}
				else if (operation.HasFlag(DataPackageOperation.Link))
				{
					// Open with piggybacks off of the link operation, since there isn't one for it
					if (isTargetExecutable || isTargetScriptFile)
					{
						var items = await GetDraggedStorageItems(packageView);
						await NavigationHelpers.OpenItemsWithExecutableAsync(associatedInstance, items, destination);
						return ReturnResult.Success;
					}
					else
					{
						return await CreateShortcutFromClipboard(packageView, destination, showDialog, registerHistory);
					}
				}
				else if (operation.HasFlag(DataPackageOperation.None))
				{
					return await CopyItemsFromClipboard(packageView, destination, showDialog, registerHistory);
				}
				else
				{
					return default;
				}
			}
			finally
			{
				packageView.ReportOperationCompleted(packageView.RequestedOperation);
			}
		}

		public Task<ReturnResult> CopyItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool showDialog, bool registerHistory)
			=> CopyItemsAsync(source.Select((item) => item.FromStorageItem()), destination, showDialog, registerHistory);

		public Task<ReturnResult> CopyItemAsync(IStorageItem source, string destination, bool showDialog, bool registerHistory)
			=> CopyItemAsync(source.FromStorageItem(), destination, showDialog, registerHistory);

		public async Task<ReturnResult> CopyItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool showDialog, bool registerHistory)
		{
			source = await source.ToListAsync();
			destination = await destination.ToListAsync();

			var returnStatus = ReturnResult.InProgress;

			var banner = StatusCenterHelper.AddCard_Copy(
				returnStatus,
				source,
				destination);

			banner.ProgressEventSource.ProgressChanged += (s, e)
				=> returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

			var token = banner.CancellationToken;

			var (collisions, cancelOperation, itemsResult) = await GetCollision(FilesystemOperationType.Copy, source, destination, showDialog);

			if (cancelOperation)
			{
				_statusCenterViewModel.RemoveItem(banner);
				return ReturnResult.Cancelled;
			}

			itemManipulationModel?.ClearSelection();

			IStorageHistory history = await filesystemOperations.CopyItemsAsync((IList<IStorageItemWithPath>)source, (IList<string>)destination, collisions, banner.ProgressEventSource, token);

			banner.Progress.ReportStatus(FileSystemStatusCode.Success);

			if (registerHistory && history is not null && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
			{
				foreach (var item in history.Source.Zip(history.Destination, (k, v) => new { Key = k, Value = v }).ToDictionary(k => k.Key, v => v.Value))
				{
					foreach (var item2 in itemsResult)
					{
						if (!string.IsNullOrEmpty(item2.CustomName) && item2.SourcePath == item.Key.Path && Path.GetFileName(item2.SourcePath) != item2.CustomName)
						{
							var renameHistory = await filesystemOperations.RenameAsync(item.Value, item2.CustomName, NameCollisionOption.FailIfExists, banner.ProgressEventSource, token);
							history.Destination[history.Source.IndexOf(item.Key)] = renameHistory.Destination[0];
						}
					}
				}
				App.HistoryWrapper.AddHistory(history);
			}

			await Task.Yield();

			var itemsCount = banner.TotalItemsCount;

			_statusCenterViewModel.RemoveItem(banner);

			StatusCenterHelper.AddCard_Copy(
				token.IsCancellationRequested ? ReturnResult.Cancelled : returnStatus,
				source,
				destination,
				itemsCount);

			return returnStatus;
		}

		public Task<ReturnResult> CopyItemAsync(IStorageItemWithPath source, string destination, bool showDialog, bool registerHistory)
			=> CopyItemsAsync(source.CreateEnumerable(), destination.CreateEnumerable(), showDialog, registerHistory);

		public async Task<ReturnResult> CopyItemsFromClipboard(DataPackageView packageView, string destination, bool showDialog, bool registerHistory)
		{
			var source = await GetDraggedStorageItems(packageView);

			if (!source.IsEmpty())
			{
				ReturnResult returnStatus = ReturnResult.InProgress;

				var destinations = new List<string>();
				List<ShellFileItem>? binItems = null;
				foreach (var item in source)
				{
					if (StorageTrashBinService.IsUnderTrashBin(item.Path))
					{
						binItems ??= await StorageTrashBinService.GetAllRecycleBinFoldersAsync();
						if (!binItems.IsEmpty()) // Might still be null because we're deserializing the list from Json
						{
							var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == item.Path); // Get original file name
							destinations.Add(PathNormalization.Combine(destination, matchingItem?.FileName ?? item.Name));
						}
					}
					else
					{
						destinations.Add(PathNormalization.Combine(destination, item.Name));
					}
				}

				returnStatus = await CopyItemsAsync(source, destinations, showDialog, registerHistory);

				return returnStatus;
			}

			if (packageView.Contains(StandardDataFormats.Bitmap))
			{
				try
				{
					var imgSource = await packageView.GetBitmapAsync();
					using var imageStream = await imgSource.OpenReadAsync();
					var folder = await StorageFileExtensions.DangerousGetFolderFromPathAsync(destination);
					// Set the name of the file to be the current time and date
					var file = await folder.CreateFileAsync($"{DateTime.Now:MM-dd-yy-HHmmss}.png", CreationCollisionOption.GenerateUniqueName);

					SoftwareBitmap softwareBitmap;

					// Create the decoder from the stream
					BitmapDecoder decoder = await BitmapDecoder.CreateAsync(imageStream);

					// Get the SoftwareBitmap representation of the file
					softwareBitmap = await decoder.GetSoftwareBitmapAsync();

					await BitmapHelper.SaveSoftwareBitmapToFileAsync(softwareBitmap, file, BitmapEncoder.PngEncoderId);
					return ReturnResult.Success;
				}
				catch (Exception)
				{
					return ReturnResult.UnknownException;
				}
			}

			// Happens if you copy some text and then you Ctrl+V in Files
			return ReturnResult.BadArgumentException;
		}

		public Task<ReturnResult> MoveItemsAsync(IEnumerable<IStorageItem> source, IEnumerable<string> destination, bool showDialog, bool registerHistory)
			=> MoveItemsAsync(source.Select((item) => item.FromStorageItem()), destination, showDialog, registerHistory);

		public Task<ReturnResult> MoveItemAsync(IStorageItem source, string destination, bool showDialog, bool registerHistory)
			=> MoveItemAsync(source.FromStorageItem(), destination, showDialog, registerHistory);

		public async Task<ReturnResult> MoveItemsAsync(IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool showDialog, bool registerHistory)
		{
			source = await source.ToListAsync();
			destination = await destination.ToListAsync();

			var returnStatus = ReturnResult.InProgress;

			var banner = StatusCenterHelper.AddCard_Move(
				returnStatus,
				source,
				destination);

			banner.ProgressEventSource.ProgressChanged += (s, e)
				=> returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

			var token = banner.CancellationToken;

			var (collisions, cancelOperation, itemsResult) = await GetCollision(FilesystemOperationType.Move, source, destination, showDialog);

			if (cancelOperation)
			{
				_statusCenterViewModel.RemoveItem(banner);

				return ReturnResult.Cancelled;
			}

			var sw = new Stopwatch();
			sw.Start();

			itemManipulationModel?.ClearSelection();

			IStorageHistory history = await filesystemOperations.MoveItemsAsync((IList<IStorageItemWithPath>)source, (IList<string>)destination, collisions, banner.ProgressEventSource, token);

			banner.Progress.ReportStatus(FileSystemStatusCode.Success);

			await Task.Yield();

			if (registerHistory && history is not null && source.Any((item) => !string.IsNullOrWhiteSpace(item.Path)))
			{
				foreach (var item in history.Source.Zip(history.Destination, (k, v) => new { Key = k, Value = v }).ToDictionary(k => k.Key, v => v.Value))
				{
					foreach (var item2 in itemsResult)
					{
						if (!string.IsNullOrEmpty(item2.CustomName) && item2.SourcePath == item.Key.Path)
						{
							var renameHistory = await filesystemOperations.RenameAsync(item.Value, item2.CustomName, NameCollisionOption.FailIfExists, banner.ProgressEventSource, token);
							history.Destination[history.Source.IndexOf(item.Key)] = renameHistory.Destination[0];
						}
					}
				}

				App.HistoryWrapper.AddHistory(history);
			}

			// Execute removal tasks concurrently in background
			var sourcePaths = source.Select(x => x.Path);
			_ = Task.WhenAll(sourcePaths.Select(jumpListService.RemoveFolderAsync));

			var itemsCount = banner.TotalItemsCount;

			_statusCenterViewModel.RemoveItem(banner);

			sw.Stop();

			StatusCenterHelper.AddCard_Move(
				token.IsCancellationRequested ? ReturnResult.Cancelled : returnStatus,
				source,
				destination,
				itemsCount);

			return returnStatus;
		}

		public Task<ReturnResult> MoveItemAsync(IStorageItemWithPath source, string destination, bool showDialog, bool registerHistory)
			=> MoveItemsAsync(source.CreateEnumerable(), destination.CreateEnumerable(), showDialog, registerHistory);

		public async Task<ReturnResult> MoveItemsFromClipboard(DataPackageView packageView, string destination, bool showDialog, bool registerHistory)
		{
			if (!HasDraggedStorageItems(packageView))
			{
				// Happens if you copy some text and then you Ctrl+V in Files
				return ReturnResult.BadArgumentException;
			}

			var source = await GetDraggedStorageItems(packageView);

			ReturnResult returnStatus = ReturnResult.InProgress;

			var destinations = new List<string>();
			List<ShellFileItem>? binItems = null;
			foreach (var item in source)
			{
				if (StorageTrashBinService.IsUnderTrashBin(item.Path))
				{
					binItems ??= await StorageTrashBinService.GetAllRecycleBinFoldersAsync();
					if (!binItems.IsEmpty()) // Might still be null because we're deserializing the list from Json
					{
						var matchingItem = binItems.FirstOrDefault(x => x.RecyclePath == item.Path); // Get original file name
						destinations.Add(PathNormalization.Combine(destination, matchingItem?.FileName ?? item.Name));
					}
				}
				else
				{
					destinations.Add(PathNormalization.Combine(destination, item.Name));
				}
			}

			returnStatus = await MoveItemsAsync(source, destinations, showDialog, registerHistory);

			return returnStatus;
		}

		public Task<ReturnResult> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, bool registerHistory, bool showExtensionDialog = true)
			=> RenameAsync(source.FromStorageItem(), newName, collision, registerHistory, showExtensionDialog);

		public async Task<ReturnResult> RenameAsync(IStorageItemWithPath source, string newName, NameCollisionOption collision, bool registerHistory, bool showExtensionDialog = true)
		{
			var returnStatus = ReturnResult.InProgress;
			var progress = new Progress<StatusCenterItemProgressModel>();
			progress.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

			if (!IsValidForFilename(newName))
			{
				await DialogDisplayHelper.ShowDialogAsync(
					"ErrorDialogThisActionCannotBeDone".GetLocalizedResource(),
					"ErrorDialogNameNotAllowed".GetLocalizedResource());
				return ReturnResult.Failed;
			}

			IStorageHistory? history = null;

			switch (source.ItemType)
			{
				case FilesystemItemType.Directory:
					history = await filesystemOperations.RenameAsync(source, newName, collision, progress, cancellationToken);
					break;

				// Prompt user when extension has changed, not when file name has changed
				case FilesystemItemType.File:
					if
					(
						showExtensionDialog &&
						Path.GetExtension(source.Path) != Path.GetExtension(newName) &&
						UserSettingsService.FoldersSettingsService.ShowFileExtensionWarning
					)
					{
						var yesSelected = await DialogDisplayHelper.ShowDialogAsync("Rename".GetLocalizedResource(), "RenameFileDialog/Text".GetLocalizedResource(), "Yes".GetLocalizedResource(), "No".GetLocalizedResource());
						if (yesSelected)
						{
							history = await filesystemOperations.RenameAsync(source, newName, collision, progress, cancellationToken);
							break;
						}

						break;
					}

					history = await filesystemOperations.RenameAsync(source, newName, collision, progress, cancellationToken);
					break;

				default:
					history = await filesystemOperations.RenameAsync(source, newName, collision, progress, cancellationToken);
					break;
			}

			if (registerHistory && !string.IsNullOrWhiteSpace(source.Path))
			{
				App.HistoryWrapper.AddHistory(history);
			}

			await jumpListService.RemoveFolderAsync(source.Path); // Remove items from jump list

			await Task.Yield();
			return returnStatus;
		}

		public async Task<ReturnResult> CreateShortcutFromClipboard(DataPackageView packageView, string destination, bool showDialog, bool registerHistory)
		{
			if (!HasDraggedStorageItems(packageView))
			{
				// Happens if you copy some text and then you Ctrl+V in Files
				return ReturnResult.BadArgumentException;
			}

			var source = await GetDraggedStorageItems(packageView);

			var returnStatus = ReturnResult.InProgress;
			var progress = new Progress<StatusCenterItemProgressModel>();
			progress.ProgressChanged += (s, e) => returnStatus = returnStatus < ReturnResult.Failed ? e.Status!.Value.ToStatus() : returnStatus;

			source = source.Where(x => !string.IsNullOrEmpty(x.Path));

			var dest = source.Select(x => Path.Combine(destination, FilesystemHelpers.GetShortcutNamingPreference(x.Name)));
			source = await source.ToListAsync();
			dest = await dest.ToListAsync();

			var history = await filesystemOperations.CreateShortcutItemsAsync((IList<IStorageItemWithPath>)source, (IList<string>)dest, progress, cancellationToken);

			if (registerHistory)
			{
				App.HistoryWrapper.AddHistory(history);
			}

			await Task.Yield();
			return returnStatus;
		}

		public async Task<ReturnResult> RecycleItemsFromClipboard(DataPackageView packageView, string destination, DeleteConfirmationPolicies showDialog, bool registerHistory)
		{
			if (!HasDraggedStorageItems(packageView))
			{
				// Happens if you copy some text and then you Ctrl+V in Files
				return ReturnResult.BadArgumentException;
			}

			var source = await GetDraggedStorageItems(packageView);
			ReturnResult returnStatus = ReturnResult.InProgress;

			source = source.Where(x => !StorageTrashBinService.IsUnderTrashBin(x.Path)); // Can't recycle items already in recyclebin
			returnStatus = await DeleteItemsAsync(source, showDialog, false, registerHistory);

			return returnStatus;
		}
		public static bool IsValidForFilename(string name)
			=> !string.IsNullOrWhiteSpace(name) && !ContainsRestrictedCharacters(name) && !ContainsRestrictedFileName(name);

		private static async Task<(List<FileNameConflictResolveOptionType> collisions, bool cancelOperation, IEnumerable<IFileSystemDialogConflictItemViewModel>)> GetCollision(FilesystemOperationType operationType, IEnumerable<IStorageItemWithPath> source, IEnumerable<string> destination, bool forceDialog)
		{
			var incomingItems = new List<BaseFileSystemDialogItemViewModel>();
			var conflictingItems = new List<BaseFileSystemDialogItemViewModel>();
			var collisions = new Dictionary<string, FileNameConflictResolveOptionType>();

			foreach (var item in source.Zip(destination, (src, dest, index) => new { src, dest, index }))
			{
				var itemPathOrName = string.IsNullOrEmpty(item.src.Path) ? item.src.Item.Name : item.src.Path;
				incomingItems.Add(new FileSystemDialogConflictItemViewModel() { ConflictResolveOption = FileNameConflictResolveOptionType.None, SourcePath = itemPathOrName, DestinationPath = item.dest, DestinationDisplayName = Path.GetFileName(item.dest) });
				var path = incomingItems.ElementAt(item.index).SourcePath;
				if (path is not null && collisions.ContainsKey(path))
				{
					// Something strange happened, log
					App.Logger.LogWarning($"Duplicate key when resolving conflicts: {incomingItems.ElementAt(item.index).SourcePath}, {item.src.Name}\n" +
						$"Source: {string.Join(", ", source.Select(x => string.IsNullOrEmpty(x.Path) ? x.Item.Name : x.Path))}");
				}
				collisions.AddIfNotPresent(incomingItems.ElementAt(item.index).SourcePath, FileNameConflictResolveOptionType.GenerateNewName);

				// Assume GenerateNewName when source and destination are the same
				if (string.IsNullOrEmpty(item.src.Path) || item.src.Path != item.dest)
				{
					// Same item names in both directories
					if (StorageHelpers.Exists(item.dest) ||
						(FtpHelpers.IsFtpPath(item.dest) &&
						await Ioc.Default.GetRequiredService<IFtpStorageService>().TryGetFileAsync(item.dest) is not null))
					{
						(incomingItems[item.index] as FileSystemDialogConflictItemViewModel)!.ConflictResolveOption = FileNameConflictResolveOptionType.GenerateNewName;
						conflictingItems.Add(incomingItems.ElementAt(item.index));
					}
				}
			}

			IEnumerable<IFileSystemDialogConflictItemViewModel>? itemsResult = null;

			var mustResolveConflicts = !conflictingItems.IsEmpty();
			if (mustResolveConflicts || forceDialog)
			{
				var dialogService = Ioc.Default.GetRequiredService<IDialogService>();

				var dialogViewModel = FileSystemDialogViewModel.GetDialogViewModel(
					new() { ConflictsExist = mustResolveConflicts },
					(false, false),
					operationType,
					incomingItems.Except(conflictingItems).ToList(), // TODO: Could be optimized
					conflictingItems);

				var result = await dialogService.ShowDialogAsync(dialogViewModel);
				itemsResult = dialogViewModel.GetItemsResult();
				if (mustResolveConflicts) // If there were conflicts, result buttons are different
				{
					if (result != DialogResult.Primary) // Operation was cancelled
					{
						return ([], true, itemsResult);
					}
				}

				collisions.Clear();
				foreach (var item in itemsResult)
				{
					collisions.AddIfNotPresent(item.SourcePath, item.ConflictResolveOption);
				}
			}

			// Since collisions are scrambled, we need to sort them PATH--PATH
			var newCollisions = new List<FileNameConflictResolveOptionType>();

			foreach (var src in source)
			{
				var itemPathOrName = string.IsNullOrEmpty(src.Path) ? src.Item.Name : src.Path;
				var match = collisions.SingleOrDefault(x => x.Key == itemPathOrName);
				var fileNameConflictResolveOptionType = (match.Key is not null) ? match.Value : FileNameConflictResolveOptionType.Skip;
				newCollisions.Add(fileNameConflictResolveOptionType);
			}

			return (newCollisions, false, itemsResult ?? new List<IFileSystemDialogConflictItemViewModel>());
		}

		public static bool HasDraggedStorageItems(DataPackageView packageView)
		{
			return packageView is not null && (packageView.Contains(StandardDataFormats.StorageItems) || packageView.Contains("FileDrop"));
		}

		public static async Task<IEnumerable<IStorageItemWithPath>> GetDraggedStorageItems(DataPackageView packageView)
		{
			var itemsList = new List<IStorageItemWithPath>();
			var hasVirtualItems = false;

			if (packageView.Contains(StandardDataFormats.StorageItems))
			{
				try
				{
					var source = await packageView.GetStorageItemsAsync();
					itemsList.AddRange(source.Select(x => x.FromStorageItem()));
				}
				catch (Exception ex) when ((uint)ex.HResult == 0x80040064 || (uint)ex.HResult == 0x8004006A)
				{
					hasVirtualItems = true;
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex, ex.Message);
					return itemsList;
				}
			}

			// workaround for pasting folders from remote desktop (#12318)
			try
			{
				if (hasVirtualItems && packageView.Contains("FileContents"))
				{
					var descriptor = NativeClipboard.CurrentDataObject.GetData<Shell32.FILEGROUPDESCRIPTOR>("FileGroupDescriptorW");
					for (var ii = 0; ii < descriptor.cItems; ii++)
					{
						if (descriptor.fgd[ii].dwFileAttributes.HasFlag(FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY))
							itemsList.Add(new VirtualStorageFolder(descriptor.fgd[ii].cFileName).FromStorageItem());
						else if (NativeClipboard.CurrentDataObject.GetData("FileContents", DVASPECT.DVASPECT_CONTENT, ii) is IStream stream)
						{
							var streamContent = new ComStreamWrapper(stream);
							itemsList.Add(new VirtualStorageFile(streamContent, descriptor.fgd[ii].cFileName).FromStorageItem());
						}
					}
				}
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}

			// workaround for GetStorageItemsAsync() bug that only yields 16 items at most
			// https://learn.microsoft.com/windows/win32/shell/clipboard#cf_hdrop
			if (packageView.Contains("FileDrop"))
			{
				var fileDropData = await SafetyExtensions.IgnoreExceptions(
					() => packageView.GetDataAsync("FileDrop").AsTask());
				if (fileDropData is IRandomAccessStream stream)
				{
					stream.Seek(0);

					byte[]? dropBytes = null;
					int bytesRead = 0;
					try
					{
						dropBytes = new byte[stream.Size];
						bytesRead = await stream.AsStreamForRead().ReadAsync(dropBytes);
					}
					catch (COMException)
					{
					}

					if (bytesRead > 0)
					{
						IntPtr dropStructPointer = Marshal.AllocHGlobal(dropBytes!.Length);

						try
						{
							Marshal.Copy(dropBytes, 0, dropStructPointer, dropBytes.Length);
							HDROP dropStructHandle = new(dropStructPointer);

							var itemPaths = new List<string>();
							uint filesCount = Shell32.DragQueryFile(dropStructHandle, 0xffffffff, null, 0);
							for (uint i = 0; i < filesCount; i++)
							{
								uint charsNeeded = Shell32.DragQueryFile(dropStructHandle, i, null, 0);
								uint bufferSpaceRequired = charsNeeded + 1; // include space for terminating null character
								string buffer = new('\0', (int)bufferSpaceRequired);
								uint charsCopied = Shell32.DragQueryFile(dropStructHandle, i, buffer, bufferSpaceRequired);

								if (charsCopied > 0)
								{
									string path = buffer[..(int)charsCopied];
									itemPaths.Add(Path.GetFullPath(path));
								}
							}

							foreach (var path in itemPaths)
							{
								var isDirectory = Win32Helper.HasFileAttribute(path, FileAttributes.Directory);
								itemsList.Add(StorageHelpers.FromPathAndType(path, isDirectory ? FilesystemItemType.Directory : FilesystemItemType.File));
							}
						}
						finally
						{
							Marshal.FreeHGlobal(dropStructPointer);
						}
					}
				}
			}

			itemsList = itemsList.DistinctBy(x => string.IsNullOrEmpty(x.Path) ? x.Item.Name : x.Path).ToList();
			return itemsList;
		}

		public static string FilterRestrictedCharacters(string input)
		{
			int invalidCharIndex;
			while ((invalidCharIndex = input.IndexOfAny(RestrictedCharacters)) >= 0)
			{
				input = input.Remove(invalidCharIndex, 1);
			}
			return input;
		}

		public static bool ContainsRestrictedCharacters(string input)
		{
			return input.IndexOfAny(RestrictedCharacters) >= 0;
		}

		public static bool ContainsRestrictedFileName(string input)
		{
			foreach (string name in RestrictedFileNames)
			{
				if (input.StartsWith(name, StringComparison.OrdinalIgnoreCase) && (input.Length == name.Length || input[name.Length] == '.'))
					return true;
			}

			return false;
		}


		/// <summary>
		/// Gets the shortcut naming template from File Explorer
		/// </summary>
		public static string GetShortcutNamingPreference(string itemName)
		{
			var keyName = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\NamingTemplates";
			var value = Registry.GetValue(keyName, "ShortcutNameTemplate", null);

			if (value is null)
				return string.Format("ShortcutCreateNewSuffix".GetLocalizedResource(), itemName) + ".lnk";
			else
			{
				// Trim the quotes and the "%s" from the string
				value = value?.ToString()?.TrimStart(['"', '%', 's']);
				value = value?.ToString()?.TrimEnd(['"']);

				return itemName + value;
			}
		}


		public void Dispose()
		{
			filesystemOperations?.Dispose();

			// SUPPRESS: Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625
			associatedInstance = null;
			filesystemOperations = null;
		}
	}
}
