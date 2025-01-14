// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.ViewModels.UserControls
{
	public sealed class StatusCenterViewModel : ObservableObject
	{
		public ObservableCollection<StatusCenterItem> StatusCenterItems { get; } = [];

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

		public StatusCenterItem AddItem(
			string headerResource,
			string subHeaderResource,
			ReturnResult status,
			FileOperationType operation,
			IEnumerable<string>? source,
			IEnumerable<string>? destination,
			bool canProvideProgress = true,
			long itemsCount = 0,
			long totalSize = 0,
			CancellationTokenSource cancellationTokenSource = null)
		{
			var banner = new StatusCenterItem(
				headerResource,
				subHeaderResource,
				status,
				operation,
				source,
				destination,
				canProvideProgress,
				itemsCount,
				totalSize,
				cancellationTokenSource);

			StatusCenterItems.Insert(0, banner);
			NewItemAdded?.Invoke(this, banner);

			NotifyChanges();

			return banner;
		}

		public bool RemoveItem(StatusCenterItem card)
		{
			if (!StatusCenterItems.Contains(card))
				return false;

			StatusCenterItems.Remove(card);

			NotifyChanges();

			return true;
		}

		public void RemoveAllCompletedItems()
		{
			for (var i = StatusCenterItems.Count - 1; i >= 0; i--)
			{
				if (!StatusCenterItems[i].IsInProgress)
					StatusCenterItems.RemoveAt(i);
			}

			NotifyChanges();
		}

		public void NotifyChanges()
		{
			OnPropertyChanged(nameof(InProgressItemCount));
			OnPropertyChanged(nameof(HasAnyItemInProgress));
			OnPropertyChanged(nameof(HasAnyItem));
			OnPropertyChanged(nameof(InfoBadgeState));
			OnPropertyChanged(nameof(InfoBadgeValue));

			UpdateAverageProgressValue();
		}

		public void UpdateAverageProgressValue()
		{
			if (HasAnyItemInProgress)
				AverageOperationProgressValue = (int)StatusCenterItems.Where((item) => item.IsInProgress).Average(x => x.ProgressPercentage);
		}
	}
}
