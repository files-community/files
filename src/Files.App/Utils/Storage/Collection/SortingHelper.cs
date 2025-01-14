// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;

namespace Files.App.Utils.Storage
{
	public static class SortingHelper
	{
		private static object OrderByNameFunc(ListedItem item)
			=> item.Name;

		public static Func<ListedItem, object> GetSortFunc(SortOption directorySortOption)
		{
			return directorySortOption switch
			{
				SortOption.Name => item => item.Name,
				SortOption.DateModified => item => item.ItemDateModifiedReal,
				SortOption.DateCreated => item => item.ItemDateCreatedReal,
				SortOption.FileType => item => item.ItemType,
				SortOption.Size => item => item.FileSizeBytes,
				SortOption.SyncStatus => item => item.SyncStatusString,
				SortOption.FileTag => item => item.FileTags?.FirstOrDefault(),
				SortOption.Path => item => item.ItemPath,
				SortOption.OriginalFolder => item => (item as RecycleBinItem)?.ItemOriginalFolder,
				SortOption.DateDeleted => item => (item as RecycleBinItem)?.ItemDateDeletedReal,
				_ => item => item.Name,
			};
		}

		public static IEnumerable<ListedItem> OrderFileList(IList<ListedItem> filesAndFolders, SortOption directorySortOption, SortDirection directorySortDirection,
			bool sortDirectoriesAlongsideFiles, bool sortFilesFirst)
		{
			var orderFunc = GetSortFunc(directorySortOption);
			var naturalStringComparer = NaturalStringComparer.GetForProcessor();

			// Function to prioritize folders (if sortFilesFirst is false) or files (if sortFilesFirst is true)
			bool PrioritizeFilesOrFolders(ListedItem listedItem)
				=> (listedItem.PrimaryItemAttribute == StorageItemTypes.File || listedItem.IsShortcut || listedItem.IsArchive) ^ sortFilesFirst;

			IOrderedEnumerable<ListedItem> ordered;

			if (directorySortDirection == SortDirection.Ascending)
			{
				ordered = directorySortOption switch
				{
					SortOption.Name => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderBy(orderFunc, naturalStringComparer)
						: filesAndFolders.OrderBy(PrioritizeFilesOrFolders).ThenBy(orderFunc, naturalStringComparer),

					SortOption.FileTag => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderBy(x => string.IsNullOrEmpty(orderFunc(x) as string)).ThenBy(orderFunc)
						: filesAndFolders.OrderBy(PrioritizeFilesOrFolders)
							.ThenBy(x => string.IsNullOrEmpty(orderFunc(x) as string))
							.ThenBy(orderFunc),

					_ => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderBy(orderFunc)
						: filesAndFolders.OrderBy(PrioritizeFilesOrFolders).ThenBy(orderFunc)
				};
			}
			else
			{
				ordered = directorySortOption switch
				{
					SortOption.Name => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderByDescending(orderFunc, naturalStringComparer)
						: filesAndFolders.OrderBy(PrioritizeFilesOrFolders)
							.ThenByDescending(orderFunc, naturalStringComparer),

					SortOption.FileTag => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderBy(x => string.IsNullOrEmpty(orderFunc(x) as string))
							.ThenByDescending(orderFunc)
						: filesAndFolders.OrderBy(PrioritizeFilesOrFolders)
							.ThenBy(x => string.IsNullOrEmpty(orderFunc(x) as string))
							.ThenByDescending(orderFunc),

					_ => sortDirectoriesAlongsideFiles
						? filesAndFolders.OrderByDescending(orderFunc)
						: filesAndFolders.OrderBy(PrioritizeFilesOrFolders).ThenByDescending(orderFunc)
				};
			}

			// Further order by name if applicable
			if (directorySortOption != SortOption.Name)
			{
				ordered = directorySortDirection == SortDirection.Ascending
					? ordered.ThenBy(OrderByNameFunc, naturalStringComparer)
					: ordered.ThenByDescending(OrderByNameFunc, naturalStringComparer);
			}

			return ordered;
		}
	}
}
