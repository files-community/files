// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.ViewModels.UserControls
{
	public class StatusCenterViewModel : ObservableObject
	{
		public ObservableCollection<StatusCenterItem> StatusCenterItems { get; } = new();

		private int _AverageOperationProgressValue = 0;
		public int AverageOperationProgressValue
		{
			get => _AverageOperationProgressValue;
			private set => SetProperty(ref _AverageOperationProgressValue, value);
		}

		public int InProgressItemCount
		{
			get
			{
				int count = 0;

				foreach (var item in StatusCenterItems)
				{
					if (item.IsInProgress)
						count++;
				}

				return count;
			}
		}

		public bool HasAnyItemInProgress
			=> InProgressItemCount > 0;

		public bool HasAnyItem
			=> StatusCenterItems.Any();

		public int InfoBadgeState
		{
			get
			{
				var anyFailure = StatusCenterItems.Any(i =>
					i.FileSystemOperationReturnResult != ReturnResult.InProgress &&
					i.FileSystemOperationReturnResult != ReturnResult.Success);

				return (anyFailure, HasAnyItemInProgress) switch
				{
					(false, false) => 0, // All successful
					(false, true) => 1,  // In progress
					(true, true) => 2,   // In progress with an error
					(true, false) => 3   // Completed with an error
				};
			}
		}

		public int InfoBadgeValue
			=> InProgressItemCount > 0 ? InProgressItemCount : -1;

		public event EventHandler<StatusCenterItem>? NewItemAdded;

		public StatusCenterViewModel()
		{
			StatusCenterItems.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasAnyItem));
		}

		public StatusCenterItem AddItem(string title, string message, int initialProgress, ReturnResult status, FileOperationType operation, CancellationTokenSource cancellationTokenSource = null)
		{
			var banner = new StatusCenterItem(message, title, initialProgress, status, operation, cancellationTokenSource);

			StatusCenterItems.Insert(0, banner);
			NewItemAdded?.Invoke(this, banner);

			NotifyChanges();

			return banner;
		}

		public bool RemoveItem(StatusCenterItem banner)
		{
			if (!StatusCenterItems.Contains(banner))
				return false;

			StatusCenterItems.Remove(banner);

			NotifyChanges();

			return true;
		}

		public void RemoveAllCompletedItems()
		{
			StatusCenterItems.ToList().RemoveAll(x => !x.IsInProgress);
		}

		public void NotifyChanges()
		{
			OnPropertyChanged(nameof(InProgressItemCount));
			OnPropertyChanged(nameof(HasAnyItemInProgress));
			OnPropertyChanged(nameof(HasAnyItem));
			OnPropertyChanged(nameof(InfoBadgeState));
			OnPropertyChanged(nameof(InfoBadgeValue));
		}

		public void UpdateAverageProgressValue()
		{
			if (HasAnyItemInProgress)
				AverageOperationProgressValue = (int)StatusCenterItems.Where((item) => item.IsInProgress).Average(x => x.ProgressPercentage);
		}
	}
}
