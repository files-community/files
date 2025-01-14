// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Enums;
using Files.Shared.Extensions;
using System.Text;

namespace Files.App.ViewModels.Dialogs.FileSystemDialog
{
	public sealed class FileSystemDialogViewModel : BaseDialogViewModel, IRecipient<FileSystemDialogOptionChangedMessage>
	{
		private readonly IUserSettingsService _userSettingsService;

		private readonly CancellationTokenSource _dialogClosingCts;

		private readonly IMessenger _messenger;

		public ObservableCollection<BaseFileSystemDialogItemViewModel> Items { get; }

		public FileSystemDialogMode FileSystemDialogMode { get; }

		private FileNameConflictResolveOptionType _AggregatedResolveOption;
		public FileNameConflictResolveOptionType AggregatedResolveOption
		{
			get => _AggregatedResolveOption;
			set
			{
				if (SetProperty(ref _AggregatedResolveOption, value))
					ApplyConflictOptionToAll(value);
			}
		}

		private string? _Description;
		public string? Description
		{
			get => _Description;
			set => SetProperty(ref _Description, value);
		}

		private bool _DeletePermanently;
		public bool DeletePermanently
		{
			get => _DeletePermanently;
			set => SetProperty(ref _DeletePermanently, value);
		}

		private bool _IsDeletePermanentlyEnabled;
		public bool IsDeletePermanentlyEnabled
		{
			get => _IsDeletePermanentlyEnabled;
			set => SetProperty(ref _IsDeletePermanentlyEnabled, value);
		}

		private FileSystemDialogViewModel(FileSystemDialogMode fileSystemDialogMode, IEnumerable<BaseFileSystemDialogItemViewModel> items)
		{
			// Dependency injection
			_userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

			FileSystemDialogMode = fileSystemDialogMode;

			_dialogClosingCts = new();

			_AggregatedResolveOption = _userSettingsService.GeneralSettingsService.ConflictsResolveOption;

			_messenger = new WeakReferenceMessenger();
			_messenger.Register(this);

			foreach (var item in items)
			{
				if (item is FileSystemDialogConflictItemViewModel conflictItem && conflictItem.IsConflict)
					conflictItem.ConflictResolveOption = _AggregatedResolveOption;

				item.Messenger = _messenger;
			}

			Items = new(items);

			SecondaryButtonClickCommand = new RelayCommand(SecondaryButtonClick);
		}

		public bool IsNameAvailableForItem(BaseFileSystemDialogItemViewModel item, string name)
		{
			return Items.Where(x => !x.SourcePath!.Equals(item.SourcePath)).Cast<FileSystemDialogConflictItemViewModel>().All(x => x.DestinationDisplayName != name);
		}

		public void ApplyConflictOptionToAll(FileNameConflictResolveOptionType e)
		{
			if (!FileSystemDialogMode.IsInDeleteMode &&
				e != FileNameConflictResolveOptionType.None)
			{
				foreach (var item in Items)
				{
					if (item is FileSystemDialogConflictItemViewModel conflictItem && conflictItem.ConflictResolveOption != FileNameConflictResolveOptionType.None)
						conflictItem.ConflictResolveOption = e;
				}

				PrimaryButtonEnabled = true;
			}
		}

		public IEnumerable<IFileSystemDialogConflictItemViewModel> GetItemsResult()
		{
			return Items.Cast<IFileSystemDialogConflictItemViewModel>();
		}

		public void Receive(FileSystemDialogOptionChangedMessage message)
		{
			if (message.Value.ConflictResolveOption != FileNameConflictResolveOptionType.None)
			{
				var itemsWithoutNone = Items.Where(x => (x as FileSystemDialogConflictItemViewModel)!.ConflictResolveOption != FileNameConflictResolveOptionType.None);

				// If all items have the same resolve option -- set the aggregated option to that choice
				var first = (itemsWithoutNone.First() as FileSystemDialogConflictItemViewModel)!.ConflictResolveOption;

				AggregatedResolveOption = itemsWithoutNone.All(x
					=> (x as FileSystemDialogConflictItemViewModel)!.ConflictResolveOption == first)
						? first
						: FileNameConflictResolveOptionType.None;
			}
		}

		public void SaveConflictResolveOption()
		{
			if (AggregatedResolveOption != FileNameConflictResolveOptionType.None &&
				AggregatedResolveOption != _userSettingsService.GeneralSettingsService.ConflictsResolveOption)
			{
				_userSettingsService.GeneralSettingsService.ConflictsResolveOption = AggregatedResolveOption;
			}
		}

		public void CancelCts()
		{
			_dialogClosingCts.Cancel();
		}

		private void SecondaryButtonClick()
		{
			ApplyConflictOptionToAll(FileNameConflictResolveOptionType.Skip);
		}

		public static FileSystemDialogViewModel GetDialogViewModel(FileSystemDialogMode dialogMode, (bool deletePermanently, bool IsDeletePermanentlyEnabled) deleteOption, FilesystemOperationType operationType, List<BaseFileSystemDialogItemViewModel> nonConflictingItems, List<BaseFileSystemDialogItemViewModel> conflictingItems)
		{
			var titleText = string.Empty;
			var descriptionText = string.Empty;
			var primaryButtonText = string.Empty;
			var secondaryButtonText = string.Empty;
			var isInDeleteMode = false;
			var totalCount = nonConflictingItems.Count + conflictingItems.Count;

			if (dialogMode.ConflictsExist)
			{
				titleText = "ConflictingItemsDialogTitle".GetLocalizedFormatResource(totalCount);

				descriptionText = nonConflictingItems.Count > 0
					? "ConflictingItemsDialogSubtitleConflictsNonConflicts".GetLocalizedFormatResource(conflictingItems.Count, nonConflictingItems.Count)
					: "ConflictingItemsDialogSubtitleConflicts".GetLocalizedFormatResource(conflictingItems.Count);

				primaryButtonText = "ConflictingItemsDialogPrimaryButtonText".ToLocalized();
				secondaryButtonText = "Cancel".ToLocalized();
			}
			else
			{
				switch (operationType)
				{
					case FilesystemOperationType.Copy:
						{
							titleText = "CopyItemsDialogTitle".GetLocalizedFormatResource(totalCount);

							descriptionText = "CopyItemsDialogSubtitle".GetLocalizedFormatResource(totalCount);
							primaryButtonText = "Copy".ToLocalized();
							secondaryButtonText = "Cancel".ToLocalized();

							break;
						}

					case FilesystemOperationType.Move:
						{
							titleText = "MoveItemsDialogTitle".GetLocalizedFormatResource(totalCount);

							descriptionText = "MoveItemsDialogSubtitle".GetLocalizedFormatResource(totalCount);
							primaryButtonText = "MoveItemsDialogPrimaryButtonText".ToLocalized();
							secondaryButtonText = "Cancel".ToLocalized();

							break;
						}

					case FilesystemOperationType.Delete:
						{
							titleText = "DeleteItemsDialogTitle".GetLocalizedFormatResource(totalCount);

							descriptionText = "DeleteItemsDialogSubtitle".GetLocalizedFormatResource(totalCount);
							primaryButtonText = "Delete".ToLocalized();
							secondaryButtonText = "Cancel".ToLocalized();

							isInDeleteMode = true;

							break;
						}
				}
			}

			var viewModel = new FileSystemDialogViewModel(
				new()
				{
					IsInDeleteMode = isInDeleteMode,
					ConflictsExist = !conflictingItems.IsEmpty()
				},
				conflictingItems.Concat(nonConflictingItems))
			{
				Title = titleText,
				Description = descriptionText,
				PrimaryButtonText = primaryButtonText,
				SecondaryButtonText = secondaryButtonText,
				DeletePermanently = deleteOption.deletePermanently,
				IsDeletePermanentlyEnabled = deleteOption.IsDeletePermanentlyEnabled
			};

			_ = LoadItemsIcon(viewModel.Items, viewModel._dialogClosingCts.Token);

			return viewModel;
		}

		public static FileSystemDialogViewModel GetDialogViewModel(List<BaseFileSystemDialogItemViewModel> nonConflictingItems, string titleText, string descriptionText, string primaryButtonText, string secondaryButtonText)
		{
			var viewModel = new FileSystemDialogViewModel(
				new()
				{
					IsInDeleteMode = false,
					ConflictsExist = false
				},
				nonConflictingItems)
			{
				Title = titleText,
				Description = descriptionText,
				PrimaryButtonText = primaryButtonText,
				SecondaryButtonText = secondaryButtonText,
				DeletePermanently = false,
				IsDeletePermanentlyEnabled = false
			};

			_ = LoadItemsIcon(viewModel.Items, viewModel._dialogClosingCts.Token);

			return viewModel;
		}

		private static Task LoadItemsIcon(IEnumerable<BaseFileSystemDialogItemViewModel> items, CancellationToken token)
		{
			var imagingService = Ioc.Default.GetRequiredService<IImageService>();
			var threadingService = Ioc.Default.GetRequiredService<IThreadingService>();

			var task = items.ParallelForEachAsync(async (item) =>
			{
				try
				{
					if (token.IsCancellationRequested)
						return;

					await threadingService.ExecuteOnUiThreadAsync(async () =>
					{
						item.ItemIcon = await imagingService.GetImageModelFromPathAsync(item.SourcePath!, 64u);
					});
				}
				catch (Exception ex)
				{
					_ = ex;
				}
			},
			10,
			token);

			return task;
		}
	}

	public sealed class FileSystemDialogMode
	{
		/// <summary>
		/// Determines whether to show delete options for the dialog.
		/// </summary>
		public bool IsInDeleteMode { get; init; }

		/// <summary>
		/// Determines whether conflicts are visible.
		/// </summary>
		public bool ConflictsExist { get; init; }
	}
}
