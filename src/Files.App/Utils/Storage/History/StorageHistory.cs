// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Storage
{
	public sealed class StorageHistory : IStorageHistory
	{
		public FileOperationType OperationType { get; private set; }

		public IList<IStorageItemWithPath> Source { get; private set; }

		public IList<IStorageItemWithPath> Destination { get; private set; }

		public StorageHistory(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination)
			: this(operationType, source.CreateList(), destination.CreateList())
		{
		}

		public StorageHistory(FileOperationType operationType, IList<IStorageItemWithPath> source, IList<IStorageItemWithPath> destination)
		{
			OperationType = operationType;
			Source = source;
			Destination = destination;
		}

		public void Modify(IStorageHistory newHistory)
			=> (OperationType, Source, Destination) = (newHistory.OperationType, newHistory.Source, newHistory.Destination);
		public void Modify(FileOperationType operationType, IStorageItemWithPath source, IStorageItemWithPath destination)
			=> (OperationType, Source, Destination) = (operationType, source.CreateList(), destination.CreateList());
		public void Modify(FileOperationType operationType, IList<IStorageItemWithPath> source, IList<IStorageItemWithPath> destination)
			=> (OperationType, Source, Destination) = (operationType, source, destination);
	}
}