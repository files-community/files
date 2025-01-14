// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Storage
{
	public sealed class StorageHistoryWrapper : IDisposable
	{
		private int index = -1;

		private List<IStorageHistory> histories = [];

		public bool CanRedo() => index + 1 < histories.Count;
		public bool CanUndo() => index >= 0 && histories.Count > 0;

		public IStorageHistory GetCurrentHistory() => histories[index];

		public void AddHistory(IStorageHistory? history)
		{
			if (history is not null)
			{
				++index;
				histories.Insert(index, history);
				histories.RemoveRange(index + 1, histories.Count - index - 1);
			}
		}

		public void RemoveHistory(IStorageHistory history, bool decreaseIndex = true)
		{
			if (history is not null)
			{
				histories.RemoveRange(index + 1, histories.Count - index - 1);
				if (decreaseIndex)
				{
					--index;
				}
				histories.Remove(history);
			}
		}

		public void ModifyCurrentHistory(IStorageHistory newHistory)
			=> histories[index].Modify(newHistory);

		public void DecreaseIndex() => --index;
		public void IncreaseIndex() => ++index;

		public void Dispose()
		{
			histories = null;
		}
	}
}
