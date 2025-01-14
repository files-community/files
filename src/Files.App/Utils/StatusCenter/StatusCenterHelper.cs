// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.StatusCenter
{
	/// <summary>
	/// Provide static helper for the StatusCenter.
	/// </summary>
	public static class StatusCenterHelper
	{
		private readonly static StatusCenterViewModel _statusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();

		public static StatusCenterItem AddCard_Copy(
			ReturnResult returnStatus,
			IEnumerable<IStorageItemWithPath> source,
			IEnumerable<string> destination,
			long itemsCount = 0,
			long totalSize = 0)
		{
			string? sourceDir = string.Empty;
			string? destinationDir = string.Empty;

			if (source is not null && source.Any())
				sourceDir = PathNormalization.GetParentDir(source.First().Path);

			if (destination is not null && destination.Any())
				destinationDir = PathNormalization.GetParentDir(destination.First());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyCanceled_Header",
					"StatusCenter_CopyCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyInProgress_Header",
					"StatusCenter_CopyInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyComplete_Header",
					"StatusCenter_CopyComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CopyFailed_Header",
					"StatusCenter_CopyFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Copy,
					source?.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Move(
			ReturnResult returnStatus,
			IEnumerable<IStorageItemWithPath> source,
			IEnumerable<string> destination,
			long itemsCount = 0,
			long totalSize = 0)
		{
			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault()?.Path);
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveCanceled_Header",
					"StatusCenter_MoveCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveInProgress_Header",
					"StatusCenter_MoveInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveComplete_Header",
					"StatusCenter_MoveComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_MoveFailed_Header",
					"StatusCenter_MoveFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Move,
					source.Select(x => x.Path),
					destination,
					true,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Recycle(
			ReturnResult returnStatus,
			IEnumerable<IStorageItemWithPath>? source,
			long itemsCount = 0,
			long totalSize = 0)
		{
			string? sourceDir = string.Empty;

			if (source is not null && source.Any())
				sourceDir = PathNormalization.GetParentDir(source.First().Path);

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteCanceled_Header",
					string.Empty,
					ReturnResult.Cancelled,
					FileOperationType.Recycle,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteInProgress_Header",
					string.Empty,
					ReturnResult.InProgress,
					FileOperationType.Recycle,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteComplete_Header",
					string.Empty,
					ReturnResult.Success,
					FileOperationType.Recycle,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteFailed_Header",
					string.Empty,
					ReturnResult.Failed,
					FileOperationType.Recycle,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Delete(
			ReturnResult returnStatus,
			IEnumerable<IStorageItemWithPath>? source,
			long itemsCount = 0,
			long totalSize = 0)
		{
			string? sourceDir = string.Empty;

			if (source is not null && source.Any())
				sourceDir = PathNormalization.GetParentDir(source.First().Path);

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteCanceled_Header",
					string.Empty,
					ReturnResult.Cancelled,
					FileOperationType.Delete,
					source?.Select(x => x.Path) ?? string.Empty.CreateEnumerable(),
					null,
					true,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteInProgress_Header",
					string.Empty,
					ReturnResult.InProgress,
					FileOperationType.Delete,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteComplete_Header",
					string.Empty,
					ReturnResult.Success,
					FileOperationType.Delete,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DeleteFailed_Header",
					"StatusCenter_DeleteFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Delete,
					source?.Select(x => x.Path),
					null,
					true,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Compress(
			IEnumerable<string> source,
			IEnumerable<string> destination,
			ReturnResult returnStatus,
			long itemsCount = 0,
			long totalSize = 0)
		{
			// Currently not supported accurate progress report for emptying the recycle bin

			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressCanceled_Header",
					"StatusCenter_CompressCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.Compressed,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressInProgress_Header",
					"StatusCenter_CompressInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.Compressed,
					source,
					destination,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressComplete_Header",
					"StatusCenter_CompressComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.Compressed,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_CompressFailed_Header",
					"StatusCenter_CompressFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Compressed,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Decompress(
			IEnumerable<string> source,
			IEnumerable<string> destination,
			ReturnResult returnStatus,
			long itemsCount = 0,
			long totalSize = 0)
		{
			// Currently not supported accurate progress report for emptying the recycle bin

			var sourceDir = PathNormalization.GetParentDir(source.FirstOrDefault());
			var destinationDir = PathNormalization.GetParentDir(destination.FirstOrDefault());

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressCanceled_Header",
					"StatusCenter_DecompressCanceled_SubHeader",
					ReturnResult.Cancelled,
					FileOperationType.Extract,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressInProgress_Header",
					"StatusCenter_DecompressInProgress_SubHeader",
					ReturnResult.InProgress,
					FileOperationType.Extract,
					source,
					destination,
					true,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressComplete_Header",
					"StatusCenter_DecompressComplete_SubHeader",
					ReturnResult.Success,
					FileOperationType.Extract,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_DecompressFailed_Header",
					"StatusCenter_DecompressFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Extract,
					source,
					destination,
					false,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_EmptyRecycleBin(
			ReturnResult returnStatus,
			long itemsCount = 0,
			long totalSize = 0)
		{
			// Currently not supported accurate progress report for emptying the recycle bin

			if (returnStatus == ReturnResult.Cancelled)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinCancel_Header",
					string.Empty,
					ReturnResult.Cancelled,
					FileOperationType.Delete,
					null,
					null,
					false,
					itemsCount,
					totalSize);
			}
			else if (returnStatus == ReturnResult.InProgress)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinInProgress_Header",
					string.Empty,
					ReturnResult.InProgress,
					FileOperationType.Delete,
					null,
					null,
					false,
					itemsCount,
					totalSize,
					new CancellationTokenSource());
			}
			else if (returnStatus == ReturnResult.Success)
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinComplete_Header",
					string.Empty,
					ReturnResult.Success,
					FileOperationType.Delete,
					null,
					null,
					false,
					itemsCount,
					totalSize);
			}
			else
			{
				return _statusCenterViewModel.AddItem(
					"StatusCenter_EmptyRecycleBinFailed_Header",
					"StatusCenter_EmptyRecycleBinFailed_SubHeader",
					ReturnResult.Failed,
					FileOperationType.Delete,
					null,
					null,
					false,
					itemsCount,
					totalSize);
			}
		}

		public static StatusCenterItem AddCard_Prepare()
		{
			return _statusCenterViewModel.AddItem(
				"StatusCenter_Prepare_Header",
				string.Empty,
				ReturnResult.InProgress,
				FileOperationType.Prepare,
				null,
				null,
				false);
		}

		public static void UpdateCardStrings(StatusCenterItem card)
		{
			// Aren't used for now
			string sourcePath = string.Empty;
			string destinationPath = string.Empty;

			string sourceFileName = string.Empty;
			string sourceDirName = string.Empty;
			string destinationDirName = string.Empty;

			if (card.Source is not null && card.Source.Any())
			{
				// Include null check for items that don't have a parent dir
				// This can happen when dragging an image from the browser
				// https://github.com/files-community/Files/issues/13590
				if (card.Source.First() != null)
				{
					sourcePath = PathNormalization.GetParentDir(card.Source.First());
					sourceDirName = sourcePath.Split('\\').Last();
					sourceFileName = card.Source.First().Split('\\').Last();
				}
			}

			if (card.Destination is not null && card.Destination.Any())
			{
				destinationPath = PathNormalization.GetParentDir(card.Destination.First());
				destinationDirName = destinationPath.Split('\\').Last();
			}

			string headerString = string.IsNullOrWhiteSpace(card.HeaderStringResource) ? string.Empty : card.HeaderStringResource.GetLocalizedResource();
			string subHeaderString = string.IsNullOrWhiteSpace(card.SubHeaderStringResource) ? string.Empty : card.SubHeaderStringResource.GetLocalizedResource();

			// Update string resources
			switch (card.Operation)
			{
				case FileOperationType.Copy:
					{
						if (headerString is not null)
						{
							card.Header = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								ReturnResult.Success => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								ReturnResult.Failed => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								ReturnResult.InProgress => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								_ => string.Format(headerString, card.TotalItemsCount, destinationDirName),
							};
						}
						if (subHeaderString is not null)
						{
							card.SubHeader = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								ReturnResult.Success => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								ReturnResult.Failed => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								ReturnResult.InProgress => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								_ => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
							};
						}
						break;
					}
				case FileOperationType.Move:
					{
						if (headerString is not null)
						{
							card.Header = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								ReturnResult.Success => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								ReturnResult.Failed => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								ReturnResult.InProgress => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								_ => string.Format(headerString, card.TotalItemsCount, destinationDirName),
							};
						}
						if (subHeaderString is not null)
						{
							card.SubHeader = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								ReturnResult.Success => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								ReturnResult.Failed => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								ReturnResult.InProgress => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								_ => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
							};
						}
						break;
					}
				case FileOperationType.Delete:
					{
						if (headerString is not null)
						{
							card.Header = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(headerString, card.TotalItemsCount, sourceDirName),
								ReturnResult.Success => string.Format(headerString, card.TotalItemsCount, sourceDirName),
								ReturnResult.Failed => string.Format(headerString, card.TotalItemsCount, sourceDirName),
								ReturnResult.InProgress => string.Format(headerString, card.TotalItemsCount, sourceDirName),
								_ => string.Format(headerString, card.TotalItemsCount, sourceDirName),
							};
						}
						if (subHeaderString is not null)
						{
							card.SubHeader = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(subHeaderString, card.TotalItemsCount, sourcePath),
								ReturnResult.Success => string.Format(subHeaderString, card.TotalItemsCount, sourcePath),
								ReturnResult.Failed => string.Format(subHeaderString, card.TotalItemsCount, sourcePath),
								ReturnResult.InProgress => string.Format(subHeaderString, card.TotalItemsCount, sourcePath),
								_ => string.Format(subHeaderString, card.TotalItemsCount, sourcePath),
							};
						}
						break;
					}
				case FileOperationType.Recycle:
					{
						if (headerString is not null)
						{
							card.Header = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(headerString, card.TotalItemsCount, sourceDirName),
								ReturnResult.Success => string.Format(headerString, card.TotalItemsCount, sourceDirName),
								ReturnResult.Failed => string.Format(headerString, card.TotalItemsCount, sourceDirName),
								ReturnResult.InProgress => string.Format(headerString, card.TotalItemsCount, sourceDirName),
								_ => string.Format(headerString, card.TotalItemsCount, sourceDirName),
							};
						}
						if (subHeaderString is not null)
						{
							card.SubHeader = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(subHeaderString, card.TotalItemsCount, sourcePath),
								ReturnResult.Success => string.Format(subHeaderString, card.TotalItemsCount, sourcePath),
								ReturnResult.Failed => string.Format(subHeaderString, card.TotalItemsCount, sourcePath),
								ReturnResult.InProgress => string.Format(subHeaderString, card.TotalItemsCount, sourcePath),
								_ => string.Format(subHeaderString, card.TotalItemsCount, sourcePath),
							};
						}
						break;
					}
				case FileOperationType.Extract:
					{
						if (headerString is not null)
						{
							card.Header = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(headerString, sourceFileName, destinationDirName),
								ReturnResult.Success => string.Format(headerString, sourceFileName, destinationDirName),
								ReturnResult.Failed => string.Format(headerString, sourceFileName, destinationDirName),
								ReturnResult.InProgress => string.Format(headerString, sourceFileName, destinationDirName),
								_ => string.Format(headerString, sourceFileName, destinationDirName),
							};
						}
						if (subHeaderString is not null)
						{
							card.SubHeader = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(subHeaderString, sourceFileName, sourcePath, destinationPath),
								ReturnResult.Success => string.Format(subHeaderString, sourceFileName, sourcePath, destinationPath),
								ReturnResult.Failed => string.Format(subHeaderString, sourceFileName, sourcePath, destinationPath),
								ReturnResult.InProgress => string.Format(subHeaderString, sourceFileName, sourcePath, destinationPath),
								_ => string.Format(subHeaderString, sourceFileName, sourcePath, destinationPath),
							};
						}
						break;
					}
				case FileOperationType.Compressed:
					{
						if (headerString is not null)
						{
							card.Header = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								ReturnResult.Success => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								ReturnResult.Failed => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								ReturnResult.InProgress => string.Format(headerString, card.TotalItemsCount, destinationDirName),
								_ => string.Format(headerString, card.TotalItemsCount, destinationDirName),
							};
						}
						if (subHeaderString is not null)
						{
							card.SubHeader = card.FileSystemOperationReturnResult switch
							{
								ReturnResult.Cancelled => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								ReturnResult.Success => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								ReturnResult.Failed => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								ReturnResult.InProgress => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
								_ => string.Format(subHeaderString, card.TotalItemsCount, sourcePath, destinationPath),
							};
						}
						break;
					}
			}
		}
	}
}
