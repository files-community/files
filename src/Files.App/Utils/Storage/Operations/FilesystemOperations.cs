// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using System.Text;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Win32;

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Provides group of file system operation for given page instance.
	/// </summary>
	public sealed class FilesystemOperations : IFilesystemOperations
	{
		private readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();

		private IShellPage _associatedInstance;

		public FilesystemOperations(IShellPage associatedInstance)
		{
			_associatedInstance = associatedInstance;
		}

		public async Task<(IStorageHistory, IStorageItem)> CreateAsync(IStorageItemWithPath source, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken, bool asAdmin = false)
		{
			IStorageItemWithPath item = null;
			FilesystemResult fsResult = (FilesystemResult)false;
			StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress, 1);
			fsProgress.Report();

			try
			{
				switch (source.ItemType)
				{
					case FilesystemItemType.File:
						{
							var newEntryInfo = await ShellNewEntryExtensions.GetNewContextMenuEntryForType(Path.GetExtension(source.Path));
							if (newEntryInfo is null)
							{
								var fsFolderResult = await _associatedInstance.ShellViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(source.Path));
								fsResult = fsFolderResult;
								if (fsResult)
								{
									var fsCreateResult = await FilesystemTasks.Wrap(() => fsFolderResult.Result.CreateFileAsync(Path.GetFileName(source.Path), CreationCollisionOption.GenerateUniqueName).AsTask());
									fsResult = fsCreateResult;
									item = fsCreateResult.Result.FromStorageItem();
								}
								if (fsResult == FileSystemStatusCode.Unauthorized)
								{
									// Can't do anything, already tried with admin FTP
								}
							}
							else
							{
								var fsCreateResult = await newEntryInfo.Create(source.Path, _associatedInstance);
								fsResult = fsCreateResult;
								if (fsResult)
								{
									item = fsCreateResult.Result.FromStorageItem();
								}
								if (fsResult == FileSystemStatusCode.Unauthorized)
								{
									// Can't do anything, already tried with admin FTP
								}
							}

							break;
						}

					case FilesystemItemType.Directory:
						{
							var fsFolderResult = await _associatedInstance.ShellViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(source.Path));
							fsResult = fsFolderResult;
							if (fsResult)
							{
								var fsCreateResult = await FilesystemTasks.Wrap(() => fsFolderResult.Result.CreateFolderAsync(Path.GetFileName(source.Path), CreationCollisionOption.GenerateUniqueName).AsTask());
								fsResult = fsCreateResult;
								item = fsCreateResult.Result.FromStorageItem();
							}
							if (fsResult == FileSystemStatusCode.Unauthorized)
							{
								// Can't do anything, already tried with admin FTP
							}
							break;
						}

					default:
						Debugger.Break();
						break;
				}

				fsProgress.AddProcessedItemsCount(1);
				fsProgress.ReportStatus(fsResult);
				return item is not null
					? (new StorageHistory(FileOperationType.CreateNew, item.CreateList(), null), item.Item)
					: (null, null);
			}
			catch (Exception e)
			{
				fsProgress.ReportStatus(FilesystemTasks.GetErrorCode(e));
				return (null, null);
			}
		}

		public Task<IStorageHistory> CopyAsync(IStorageItem source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return CopyAsync(source.FromStorageItem(), destination, collision, progress, cancellationToken);
		}

		public async Task<IStorageHistory> CopyAsync(IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			StatusCenterItemProgressModel fsProgress = new(
				progress,
				true,
				FileSystemStatusCode.InProgress);

			fsProgress.Report();

			if (destination.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Unauthorized);

				// Do not paste files and folders inside the recycle bin
				await DialogDisplayHelper.ShowDialogAsync(
					"ErrorDialogThisActionCannotBeDone".GetLocalizedResource(),
					"ErrorDialogUnsupportedOperation".GetLocalizedResource());

				return null;
			}

			IStorageItem copiedItem = null;

			if (source.ItemType == FilesystemItemType.Directory)
			{
				if (!string.IsNullOrWhiteSpace(source.Path) &&
					PathNormalization.GetParentDir(destination).IsSubPathOf(source.Path)) // We check if user tried to copy anything above the source.ItemPath
				{
					var destinationName = destination.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
					var sourceName = source.Path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();

					ContentDialog dialog = new()
					{
						Title = "ErrorDialogThisActionCannotBeDone".GetLocalizedResource(),
						Content = $"{"ErrorDialogTheDestinationFolder".GetLocalizedResource()} ({destinationName}) {"ErrorDialogIsASubfolder".GetLocalizedResource()} ({sourceName})",
						//PrimaryButtonText = "Skip".GetLocalizedResource(),
						CloseButtonText = "Cancel".GetLocalizedResource()
					};

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					ContentDialogResult result = await dialog.TryShowAsync();

					if (result == ContentDialogResult.Primary)
						fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Success);
					else
						fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Generic);

					return null;
				}
				else
				{
					// CopyFileFromApp only works on file not directories
					var fsSourceFolder = await source.ToStorageItemResult();
					var fsDestinationFolder = await _associatedInstance.ShellViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
					var fsResult = (FilesystemResult)(fsSourceFolder.ErrorCode | fsDestinationFolder.ErrorCode);

					if (fsResult)
					{
						if (fsSourceFolder.Result is IPasswordProtectedItem ppis)
							ppis.PasswordRequestedCallback = UIFilesystemHelpers.RequestPassword;

						var fsCopyResult = await FilesystemTasks.Wrap(() => CloneDirectoryAsync((BaseStorageFolder)fsSourceFolder, (BaseStorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, collision.Convert()));

						if (fsSourceFolder.Result is IPasswordProtectedItem ppiu)
							ppiu.PasswordRequestedCallback = null;

						if (fsCopyResult == FileSystemStatusCode.AlreadyExists)
						{
							fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);

							return null;
						}

						if (fsCopyResult)
						{
							if (Win32Helper.HasFileAttribute(source.Path, SystemIO.FileAttributes.Hidden))
							{
								// The source folder was hidden, apply hidden attribute to destination
								Win32Helper.SetFileAttribute(fsCopyResult.Result.Path, SystemIO.FileAttributes.Hidden);
							}

							copiedItem = (BaseStorageFolder)fsCopyResult;
						}

						fsResult = fsCopyResult;
					}

					if (fsResult == FileSystemStatusCode.Unauthorized)
					{
						// Cannot do anything, already tried with admin FTP
					}

					fsProgress.ReportStatus(fsResult.ErrorCode);

					if (!fsResult)
						return null;
				}
			}
			else if (source.ItemType == FilesystemItemType.File)
			{
				var fsResult = (FilesystemResult)await Task.Run(() => PInvoke.CopyFileFromApp(source.Path, destination, true));

				if (!fsResult)
				{
					Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

					FilesystemResult<BaseStorageFolder> destinationResult = await _associatedInstance.ShellViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
					var sourceResult = await source.ToStorageItemResult();
					fsResult = sourceResult.ErrorCode | destinationResult.ErrorCode;

					if (fsResult)
					{
						if (sourceResult.Result is IPasswordProtectedItem ppis)
							ppis.PasswordRequestedCallback = UIFilesystemHelpers.RequestPassword;

						var file = (BaseStorageFile)sourceResult;
						var fsResultCopy = new FilesystemResult<BaseStorageFile>(null, FileSystemStatusCode.Generic);
						if (string.IsNullOrEmpty(file.Path) && collision == NameCollisionOption.GenerateUniqueName)
						{
							// If collision is GenerateUniqueName we will manually check for existing file and generate a new name
							// HACK: If file is dragged from zip file in windows explorer for example. The file path is empty and
							// GenerateUniqueName isn't working correctly. Below is a possible solution.
							var desiredNewName = Path.GetFileName(file.Name);
							string nameWithoutExt = Path.GetFileNameWithoutExtension(desiredNewName);
							string extension = Path.GetExtension(desiredNewName);
							ushort attempt = 1;

							do
							{
								fsResultCopy = await FilesystemTasks.Wrap(() => file.CopyAsync(destinationResult.Result, desiredNewName, NameCollisionOption.FailIfExists).AsTask());
								desiredNewName = $"{nameWithoutExt} ({attempt}){extension}";
							} while (fsResultCopy.ErrorCode == FileSystemStatusCode.AlreadyExists && ++attempt < 1024);
						}
						else
						{
							fsResultCopy = await FilesystemTasks.Wrap(() => file.CopyAsync(destinationResult.Result, Path.GetFileName(file.Name), collision).AsTask());
						}

						if (sourceResult.Result is IPasswordProtectedItem ppiu)
							ppiu.PasswordRequestedCallback = null;

						if (fsResultCopy == FileSystemStatusCode.AlreadyExists)
						{
							fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);

							return null;
						}

						if (fsResultCopy)
							copiedItem = fsResultCopy.Result;

						fsResult = fsResultCopy;
					}

					if (fsResult == FileSystemStatusCode.Unauthorized)
					{
						// Cannot do anything, already tried with admin FTP
					}
				}

				fsProgress.ReportStatus(fsResult.ErrorCode);

				if (!fsResult)
					return null;
			}

			if (collision == NameCollisionOption.ReplaceExisting)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Success);

				// Cannot undo overwrite operation
				return null;
			}

			var pathWithType = copiedItem.FromStorageItem(destination, source.ItemType);

			return new StorageHistory(FileOperationType.Copy, source, pathWithType);
		}

		public Task<IStorageHistory> MoveAsync(IStorageItem source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return MoveAsync(source.FromStorageItem(), destination, collision, progress, cancellationToken);
		}

		public async Task<IStorageHistory> MoveAsync(IStorageItemWithPath source, string destination, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			StatusCenterItemProgressModel fsProgress = new(
				progress,
				true,
				FileSystemStatusCode.InProgress);

			fsProgress.Report();

			if (source.Path == destination)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Success);

				return null;
			}

			if (string.IsNullOrWhiteSpace(source.Path))
			{
				// Cannot move (only copy) files from MTP devices because:
				// StorageItems returned in DataPackageView are read-only
				// The item.Path property will be empty and there's no way of retrieving a new StorageItem with R/W access
				return await CopyAsync(source, destination, collision, progress, cancellationToken);
			}

			if (destination.StartsWith(Constants.UserEnvironmentPaths.RecycleBinPath, StringComparison.Ordinal))
			{
				fsProgress.ReportStatus(FileSystemStatusCode.Unauthorized);

				// Do not paste files and folders inside the recycle bin
				await DialogDisplayHelper.ShowDialogAsync(
					"ErrorDialogThisActionCannotBeDone".GetLocalizedResource(),
					"ErrorDialogUnsupportedOperation".GetLocalizedResource());

				return null;
			}

			IStorageItem movedItem = null;

			if (source.ItemType == FilesystemItemType.Directory)
			{
				// Also check if user tried to move anything above the source.ItemPath
				if (!string.IsNullOrWhiteSpace(source.Path) &&
					PathNormalization.GetParentDir(destination).IsSubPathOf(source.Path))
				{
					var destinationName = destination.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();
					var sourceName = source.Path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).Last();

					ContentDialog dialog = new()
					{
						Title = "ErrorDialogThisActionCannotBeDone".GetLocalizedResource(),
						Content = $"{"ErrorDialogTheDestinationFolder".GetLocalizedResource()} ({destinationName}) {"ErrorDialogIsASubfolder".GetLocalizedResource()} ({sourceName})",
						//PrimaryButtonText = "Skip".GetLocalizedResource(),
						CloseButtonText = "Cancel".GetLocalizedResource()
					};

					if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
						dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

					ContentDialogResult result = await dialog.TryShowAsync();

					if (result == ContentDialogResult.Primary)
						fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Success);
					else
						fsProgress.ReportStatus(FileSystemStatusCode.InProgress | FileSystemStatusCode.Generic);

					return null;
				}
				else
				{
					var fsResult = (FilesystemResult)await Task.Run(() => PInvoke.MoveFileFromApp(source.Path, destination));

					if (!fsResult)
					{
						Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

						var fsSourceFolder = await source.ToStorageItemResult();
						var fsDestinationFolder = await _associatedInstance.ShellViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
						fsResult = fsSourceFolder.ErrorCode | fsDestinationFolder.ErrorCode;

						if (fsResult)
						{
							if (fsSourceFolder.Result is IPasswordProtectedItem ppis)
								ppis.PasswordRequestedCallback = UIFilesystemHelpers.RequestPassword;

							var srcFolder = (BaseStorageFolder)fsSourceFolder;
							var fsResultMove = await FilesystemTasks.Wrap(() => srcFolder.MoveAsync(fsDestinationFolder.Result, collision).AsTask());

							if (!fsResultMove) // Use generic move folder operation (move folder items one by one)
							{
								// Moving folders using Storage API can result in data loss, copy instead
								//var fsResultMove = await FilesystemTasks.Wrap(() => MoveDirectoryAsync((BaseStorageFolder)fsSourceFolder, (BaseStorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, collision.Convert(), true));

								if (await DialogDisplayHelper.ShowDialogAsync("ErrorDialogThisActionCannotBeDone".GetLocalizedResource(), "ErrorDialogUnsupportedMoveOperation".GetLocalizedResource(), "OK", "Cancel".GetLocalizedResource()))
									fsResultMove = await FilesystemTasks.Wrap(() => CloneDirectoryAsync((BaseStorageFolder)fsSourceFolder, (BaseStorageFolder)fsDestinationFolder, fsSourceFolder.Result.Name, collision.Convert()));
							}

							if (fsSourceFolder.Result is IPasswordProtectedItem ppiu)
								ppiu.PasswordRequestedCallback = null;

							if (fsResultMove == FileSystemStatusCode.AlreadyExists)
							{
								fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);

								return null;
							}

							if (fsResultMove)
							{
								if (Win32Helper.HasFileAttribute(source.Path, SystemIO.FileAttributes.Hidden))
								{
									// The source folder was hidden, apply hidden attribute to destination
									Win32Helper.SetFileAttribute(fsResultMove.Result.Path, SystemIO.FileAttributes.Hidden);
								}

								movedItem = (BaseStorageFolder)fsResultMove;
							}
							fsResult = fsResultMove;
						}
						if (fsResult == FileSystemStatusCode.Unauthorized || fsResult == FileSystemStatusCode.ReadOnly)
						{
							// Cannot do anything, already tried with admin FTP
						}
					}

					fsProgress.ReportStatus(fsResult.ErrorCode);
				}
			}
			else if (source.ItemType == FilesystemItemType.File)
			{
				var fsResult = (FilesystemResult)await Task.Run(() => PInvoke.MoveFileFromApp(source.Path, destination));

				if (!fsResult)
				{
					Debug.WriteLine(System.Runtime.InteropServices.Marshal.GetLastWin32Error());

					FilesystemResult<BaseStorageFolder> destinationResult = await _associatedInstance.ShellViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));
					var sourceResult = await source.ToStorageItemResult();
					fsResult = sourceResult.ErrorCode | destinationResult.ErrorCode;

					if (fsResult)
					{
						if (sourceResult.Result is IPasswordProtectedItem ppis)
							ppis.PasswordRequestedCallback = UIFilesystemHelpers.RequestPassword;

						var file = (BaseStorageFile)sourceResult;
						var fsResultMove = await FilesystemTasks.Wrap(() => file.MoveAsync(destinationResult.Result, Path.GetFileName(file.Name), collision).AsTask());

						if (sourceResult.Result is IPasswordProtectedItem ppiu)
							ppiu.PasswordRequestedCallback = null;

						if (fsResultMove == FileSystemStatusCode.AlreadyExists)
						{
							fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);

							return null;
						}

						if (fsResultMove)
							movedItem = file;

						fsResult = fsResultMove;
					}
					if (fsResult == FileSystemStatusCode.Unauthorized || fsResult == FileSystemStatusCode.ReadOnly)
					{
						// Cannot do anything, already tried with admin FTP
					}
				}
				fsProgress.ReportStatus(fsResult.ErrorCode);
			}

			if (collision == NameCollisionOption.ReplaceExisting)
			{
				// Cannot undo overwrite operation
				return null;
			}

			bool sourceInCurrentFolder = PathNormalization.TrimPath(_associatedInstance.ShellViewModel.CurrentFolder.ItemPath) ==
				PathNormalization.GetParentDir(source.Path);
			if (fsProgress.Status == FileSystemStatusCode.Success && sourceInCurrentFolder)
			{
				await _associatedInstance.ShellViewModel.RemoveFileOrFolderAsync(source.Path);
				await _associatedInstance.ShellViewModel.ApplyFilesAndFoldersChangesAsync();
			}

			var pathWithType = movedItem.FromStorageItem(destination, source.ItemType);

			return new StorageHistory(FileOperationType.Move, source, pathWithType);
		}

		public Task<IStorageHistory> DeleteAsync(IStorageItem source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
		{
			return DeleteAsync(source.FromStorageItem(), progress, permanently, cancellationToken);
		}

		public async Task<IStorageHistory> DeleteAsync(IStorageItemWithPath source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
		{
			StatusCenterItemProgressModel fsProgress = new(
				progress,
				true,
				FileSystemStatusCode.InProgress);

			fsProgress.Report();

			bool deleteFromRecycleBin = StorageTrashBinService.IsUnderTrashBin(source.Path);

			FilesystemResult fsResult = FileSystemStatusCode.InProgress;

			if (permanently)
			{
				fsResult = (FilesystemResult)PInvoke.DeleteFileFromApp(source.Path);
			}
			if (!fsResult)
			{
				if (source.ItemType == FilesystemItemType.File)
				{
					fsResult = await _associatedInstance.ShellViewModel.GetFileFromPathAsync(source.Path)
						.OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
				}
				else if (source.ItemType == FilesystemItemType.Directory)
				{
					fsResult = await _associatedInstance.ShellViewModel.GetFolderFromPathAsync(source.Path)
						.OnSuccess((t) => t.DeleteAsync(permanently ? StorageDeleteOption.PermanentDelete : StorageDeleteOption.Default).AsTask());
				}
			}

			fsProgress.ReportStatus(fsResult);

			if (fsResult == FileSystemStatusCode.Unauthorized)
			{
				// Cannot do anything, already tried with admin FTP
			}
			else if (fsResult == FileSystemStatusCode.InUse)
			{
				// TODO: Retry
				await DialogDisplayHelper.ShowDialogAsync(DynamicDialogFactory.GetFor_FileInUseDialog());
			}

			if (deleteFromRecycleBin)
			{
				// Recycle bin also stores a file starting with $I for each item
				string iFilePath = Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileName(source.Path).Replace("$R", "$I", StringComparison.Ordinal));

				await _associatedInstance.ShellViewModel.GetFileFromPathAsync(iFilePath)
					.OnSuccess(iFile => iFile.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
			}
			fsProgress.ReportStatus(fsResult);

			if (fsResult)
			{
				await _associatedInstance.ShellViewModel.RemoveFileOrFolderAsync(source.Path);

				if (!permanently)
				{
					// Enumerate Recycle Bin
					IEnumerable<ShellFileItem> nameMatchItems, items = await StorageTrashBinService.GetAllRecycleBinFoldersAsync();

					// Get name matching files
					if (FileExtensionHelpers.IsShortcutOrUrlFile(source.Path)) // We need to check if it is a shortcut file
						nameMatchItems = items.Where((item) => item.FilePath == Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileNameWithoutExtension(source.Path)));
					else
						nameMatchItems = items.Where((item) => item.FilePath == source.Path);

					// Get newest file
					ShellFileItem item = nameMatchItems.OrderBy((item) => item.RecycleDate).FirstOrDefault();

					return new StorageHistory(FileOperationType.Recycle, source, StorageHelpers.FromPathAndType(item?.RecyclePath, source.ItemType));
				}

				return new StorageHistory(FileOperationType.Delete, source, null);
			}
			else
			{
				// Stop at first error
				return null;
			}
		}

		public Task<IStorageHistory> RenameAsync(IStorageItem source, string newName, NameCollisionOption collision, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return RenameAsync(StorageHelpers.FromStorageItem(source), newName, collision, progress, cancellationToken);
		}

		public async Task<IStorageHistory> RenameAsync(
			IStorageItemWithPath source,
			string newName,
			NameCollisionOption collision,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken cancellationToken,
			bool asAdmin = false)
		{
			StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress);

			fsProgress.Report();

			if (Path.GetFileName(source.Path) == newName && collision == NameCollisionOption.FailIfExists)
			{
				fsProgress.ReportStatus(FileSystemStatusCode.AlreadyExists);

				return null;
			}

			if (!string.IsNullOrWhiteSpace(newName) &&
				!FilesystemHelpers.ContainsRestrictedCharacters(newName) &&
				!FilesystemHelpers.ContainsRestrictedFileName(newName))
			{
				var renamed = await source.ToStorageItemResult()
					.OnSuccess(async (t) =>
					{
						if (t.Name.Equals(newName, StringComparison.CurrentCultureIgnoreCase))
							await t.RenameAsync(newName, NameCollisionOption.ReplaceExisting);
						else
							await t.RenameAsync(newName, collision);

						return t;
					});

				if (renamed)
				{
					fsProgress.ReportStatus(FileSystemStatusCode.Success);

					return new StorageHistory(FileOperationType.Rename, source, renamed.Result.FromStorageItem());
				}
				else if (renamed == FileSystemStatusCode.Unauthorized)
				{
					// Try again with MoveFileFromApp
					var destination = Path.Combine(Path.GetDirectoryName(source.Path), newName);
					if (PInvoke.MoveFileFromApp(source.Path, destination))
					{
						fsProgress.ReportStatus(FileSystemStatusCode.Success);

						return new StorageHistory(FileOperationType.Rename, source, StorageHelpers.FromPathAndType(destination, source.ItemType));
					}
					else
					{
						// Cannot do anything, already tried with admin FTP
					}
				}
				else if (renamed == FileSystemStatusCode.NotAFile || renamed == FileSystemStatusCode.NotAFolder)
				{
					await DialogDisplayHelper.ShowDialogAsync("RenameError/NameInvalid/Title".GetLocalizedResource(), "RenameError/NameInvalid/Text".GetLocalizedResource());
				}
				else if (renamed == FileSystemStatusCode.NameTooLong)
				{
					await DialogDisplayHelper.ShowDialogAsync("RenameError/TooLong/Title".GetLocalizedResource(), "RenameError/TooLong/Text".GetLocalizedResource());
				}
				else if (renamed == FileSystemStatusCode.InUse)
				{
					// TODO: Retry
					await DialogDisplayHelper.ShowDialogAsync(DynamicDialogFactory.GetFor_FileInUseDialog());
				}
				else if (renamed == FileSystemStatusCode.NotFound)
				{
					await DialogDisplayHelper.ShowDialogAsync("RenameError/ItemDeleted/Title".GetLocalizedResource(), "RenameError/ItemDeleted/Text".GetLocalizedResource());
				}
				else if (renamed == FileSystemStatusCode.AlreadyExists)
				{
					var ItemAlreadyExistsDialog = new ContentDialog()
					{
						Title = "ItemAlreadyExistsDialogTitle".GetLocalizedResource(),
						Content = "ItemAlreadyExistsDialogContent".GetLocalizedResource(),
						PrimaryButtonText = "GenerateNewName".GetLocalizedResource(),
						SecondaryButtonText = "ItemAlreadyExistsDialogSecondaryButtonText".GetLocalizedResource(),
						CloseButtonText = "Cancel".GetLocalizedResource()
					};

					ContentDialogResult result = await ItemAlreadyExistsDialog.TryShowAsync();

					if (result == ContentDialogResult.Primary)
					{
						return await RenameAsync(source, newName, NameCollisionOption.GenerateUniqueName, progress, cancellationToken);
					}
					else if (result == ContentDialogResult.Secondary)
					{
						return await RenameAsync(source, newName, NameCollisionOption.ReplaceExisting, progress, cancellationToken);
					}
				}

				fsProgress.ReportStatus(renamed);
			}

			return null;
		}

		public async Task<IStorageHistory> RestoreItemsFromTrashAsync(IList<IStorageItem> source, IList<string> destination, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return await RestoreItemsFromTrashAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, progress, cancellationToken);
		}

		public async Task<IStorageHistory> RestoreItemsFromTrashAsync(
			IList<IStorageItemWithPath> source,
			IList<string> destination,
			IProgress<StatusCenterItemProgressModel> progress,
			CancellationToken token,
			bool asAdmin = false)
		{
			StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
			fsProgress.Report();

			var rawStorageHistory = new List<IStorageHistory>();

			for (int i = 0; i < source.Count; i++)
			{
				if (token.IsCancellationRequested)
					break;

				rawStorageHistory.Add(await RestoreFromTrashAsync(source[i], destination[i], null, token));

				fsProgress.AddProcessedItemsCount(1);
				fsProgress.Report();

			}

			if (rawStorageHistory.Any() && rawStorageHistory.All((item) => item is not null))
			{
				return new StorageHistory(
					rawStorageHistory[0].OperationType,
					await rawStorageHistory.SelectMany((item) => item.Source).ToListAsync(),
					await rawStorageHistory.SelectMany((item) => item.Destination).ToListAsync());
			}

			return null;
		}

		public Task<IStorageHistory> RestoreFromTrashAsync(IStorageItem source, string destination, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return RestoreFromTrashAsync(source.FromStorageItem(), destination, progress, cancellationToken);
		}

		public async Task<IStorageHistory> RestoreFromTrashAsync(IStorageItemWithPath source, string destination, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress);
			fsProgress.Report();

			FilesystemResult fsResult = FileSystemStatusCode.InProgress;

			fsResult = (FilesystemResult)await Task.Run(() => PInvoke.MoveFileFromApp(source.Path, destination));

			if (!fsResult)
			{
				if (source.ItemType == FilesystemItemType.Directory)
				{
					FilesystemResult<BaseStorageFolder> sourceFolder = await _associatedInstance.ShellViewModel.GetFolderFromPathAsync(source.Path);
					FilesystemResult<BaseStorageFolder> destinationFolder = await _associatedInstance.ShellViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));

					fsResult = sourceFolder.ErrorCode | destinationFolder.ErrorCode;
					fsProgress.ReportStatus(fsResult);

					if (fsResult)
					{
						// Moving folders using Storage API can result in data loss, copy instead
						//fsResult = await FilesystemTasks.Wrap(() => MoveDirectoryAsync(sourceFolder.Result, destinationFolder.Result, Path.GetFileName(destination), CreationCollisionOption.FailIfExists, true));

						if (await DialogDisplayHelper.ShowDialogAsync("ErrorDialogThisActionCannotBeDone".GetLocalizedResource(), "ErrorDialogUnsupportedMoveOperation".GetLocalizedResource(), "OK", "Cancel".GetLocalizedResource()))
							fsResult = await FilesystemTasks.Wrap(() => CloneDirectoryAsync(sourceFolder.Result, destinationFolder.Result, Path.GetFileName(destination), CreationCollisionOption.FailIfExists));

						// TODO: we could use here FilesystemHelpers with registerHistory false?
					}

					fsProgress.ReportStatus(fsResult);
				}
				else
				{
					FilesystemResult<BaseStorageFile> sourceFile = await _associatedInstance.ShellViewModel.GetFileFromPathAsync(source.Path);
					FilesystemResult<BaseStorageFolder> destinationFolder = await _associatedInstance.ShellViewModel.GetFolderFromPathAsync(PathNormalization.GetParentDir(destination));

					fsResult = sourceFile.ErrorCode | destinationFolder.ErrorCode;
					fsProgress.ReportStatus(fsResult);

					if (fsResult)
						fsResult = await FilesystemTasks.Wrap(() => sourceFile.Result.MoveAsync(destinationFolder.Result, Path.GetFileName(destination), NameCollisionOption.GenerateUniqueName).AsTask());

					fsProgress.ReportStatus(fsResult);
				}

				if (fsResult == FileSystemStatusCode.Unauthorized || fsResult == FileSystemStatusCode.ReadOnly)
				{
					// Can't do anything, already tried with admin FTP
				}
			}

			if (fsResult)
			{
				// Recycle bin also stores a file starting with $I for each item
				string iFilePath = Path.Combine(Path.GetDirectoryName(source.Path), Path.GetFileName(source.Path).Replace("$R", "$I", StringComparison.Ordinal));

				await _associatedInstance.ShellViewModel.GetFileFromPathAsync(iFilePath)
					.OnSuccess(iFile => iFile.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask());
			}

			fsProgress.ReportStatus(fsResult);

			if (fsResult != FileSystemStatusCode.Success)
			{
				if (((FileSystemStatusCode)fsResult).HasFlag(FileSystemStatusCode.Unauthorized))
				{
					await DialogDisplayHelper.ShowDialogAsync("AccessDenied".GetLocalizedResource(), "AccessDeniedDeleteDialog/Text".GetLocalizedResource());
				}
				else if (((FileSystemStatusCode)fsResult).HasFlag(FileSystemStatusCode.NotFound))
				{
					await DialogDisplayHelper.ShowDialogAsync("FileNotFoundDialog/Title".GetLocalizedResource(), "FileNotFoundDialog/Text".GetLocalizedResource());
				}
				else if (((FileSystemStatusCode)fsResult).HasFlag(FileSystemStatusCode.AlreadyExists))
				{
					await DialogDisplayHelper.ShowDialogAsync("ItemAlreadyExistsDialogTitle".GetLocalizedResource(), "ItemAlreadyExistsDialogContent".GetLocalizedResource());
				}
			}

			return new StorageHistory(FileOperationType.Restore, source, StorageHelpers.FromPathAndType(destination, source.ItemType));
		}

		private async static Task<BaseStorageFolder> CloneDirectoryAsync(BaseStorageFolder sourceFolder, BaseStorageFolder destinationFolder, string sourceRootName, CreationCollisionOption collision = CreationCollisionOption.FailIfExists)
		{
			BaseStorageFolder createdRoot;
			if (collision is CreationCollisionOption.ReplaceExisting)
				// Dont't delete the contents of the folder
				createdRoot = await destinationFolder.CreateFolderAsync(sourceRootName, CreationCollisionOption.OpenIfExists);
			else
				createdRoot = await destinationFolder.CreateFolderAsync(sourceRootName, collision);

			destinationFolder = createdRoot;

			foreach (BaseStorageFile fileInSourceDir in await sourceFolder.GetFilesAsync())
			{
				await fileInSourceDir.CopyAsync(destinationFolder, fileInSourceDir.Name, collision.ConvertBack());
			}

			foreach (BaseStorageFolder folderinSourceDir in await sourceFolder.GetFoldersAsync())
			{
				await CloneDirectoryAsync(folderinSourceDir, destinationFolder, folderinSourceDir.Name, collision);
			}

			return createdRoot;
		}

		public async Task<IStorageHistory> CopyItemsAsync(IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return await CopyItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, cancellationToken);
		}

		public async Task<IStorageHistory> CopyItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken token, bool asAdmin = false)
		{
			StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
			fsProgress.Report();

			var rawStorageHistory = new List<IStorageHistory>();

			for (int i = 0; i < source.Count; i++)
			{
				if (token.IsCancellationRequested)
				{
					break;
				}

				if (collisions[i] != FileNameConflictResolveOptionType.Skip)
				{
					rawStorageHistory.Add(await CopyAsync(
						source[i],
						destination[i],
						collisions[i].Convert(),
						null,
						token));
				}

				fsProgress.AddProcessedItemsCount(1);
				fsProgress.Report();

			}

			if (rawStorageHistory.Any() && rawStorageHistory.All((item) => item is not null))
			{
				return new StorageHistory(
					rawStorageHistory[0].OperationType,
					await rawStorageHistory.SelectMany((item) => item.Source).ToListAsync(),
					await rawStorageHistory.SelectMany((item) => item.Destination).ToListAsync());
			}
			return null;
		}

		public async Task<IStorageHistory> MoveItemsAsync(IList<IStorageItem> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken cancellationToken)
		{
			return await MoveItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), destination, collisions, progress, cancellationToken);
		}

		public async Task<IStorageHistory> MoveItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IList<FileNameConflictResolveOptionType> collisions, IProgress<StatusCenterItemProgressModel> progress, CancellationToken token, bool asAdmin = false)
		{
			StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
			fsProgress.Report();

			var rawStorageHistory = new List<IStorageHistory>();

			for (int i = 0; i < source.Count; i++)
			{
				if (token.IsCancellationRequested)
				{
					break;
				}

				if (collisions[i] != FileNameConflictResolveOptionType.Skip)
				{
					rawStorageHistory.Add(await MoveAsync(
						source[i],
						destination[i],
						collisions[i].Convert(),
						null,
						token));
				}

				fsProgress.AddProcessedItemsCount(1);
				fsProgress.Report();
			}

			if (rawStorageHistory.Any() && rawStorageHistory.All((item) => item is not null))
			{
				return new StorageHistory(
					rawStorageHistory[0].OperationType,
					await rawStorageHistory.SelectMany((item) => item.Source).ToListAsync(),
					await rawStorageHistory.SelectMany((item) => item.Destination).ToListAsync());
			}
			return null;
		}

		public async Task<IStorageHistory> DeleteItemsAsync(IList<IStorageItem> source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken cancellationToken)
		{
			return await DeleteItemsAsync(await source.Select((item) => item.FromStorageItem()).ToListAsync(), progress, permanently, cancellationToken);
		}

		public async Task<IStorageHistory> DeleteItemsAsync(IList<IStorageItemWithPath> source, IProgress<StatusCenterItemProgressModel> progress, bool permanently, CancellationToken token, bool asAdmin = false)
		{
			StatusCenterItemProgressModel fsProgress = new(progress, true, FileSystemStatusCode.InProgress, source.Count);
			fsProgress.Report();

			bool originalPermanently = permanently;
			var rawStorageHistory = new List<IStorageHistory>();

			for (int i = 0; i < source.Count; i++)
			{
				if (token.IsCancellationRequested)
					break;

				permanently = StorageTrashBinService.IsUnderTrashBin(source[i].Path) || originalPermanently;

				rawStorageHistory.Add(await DeleteAsync(source[i], null, permanently, token));
				fsProgress.AddProcessedItemsCount(1);
				fsProgress.Report();
			}

			if (rawStorageHistory.Any() && rawStorageHistory.All((item) => item is not null))
			{
				return new StorageHistory(
					rawStorageHistory[0].OperationType,
					await rawStorageHistory.SelectMany((item) => item.Source).ToListAsync(),
					await rawStorageHistory.SelectMany((item) => item.Destination).ToListAsync());
			}
			return null;
		}

		public Task<IStorageHistory> CreateShortcutItemsAsync(IList<IStorageItemWithPath> source, IList<string> destination, IProgress<StatusCenterItemProgressModel> progress, CancellationToken token)
		{
			throw new NotImplementedException("Cannot create shortcuts in UWP.");
		}

		public void Dispose()
		{
			_associatedInstance = null;
		}
	}
}
