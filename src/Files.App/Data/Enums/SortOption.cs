// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	public enum SortOption
	{
		/// <summary>
		/// Sort by name.
		/// </summary>
		Name = 0,

		/// <summary>
		/// Sort by date modified.
		/// </summary>
		DateModified = 1,

		/// <summary>
		/// Sort by date created.
		/// </summary>
		DateCreated = 2,

		/// <summary>
		/// Sort by size.
		/// </summary>
		Size = 3,

		/// <summary>
		/// Sort by file type.
		/// </summary>
		FileType = 4,

		/// <summary>
		/// Sort by sync status.
		/// </summary>
		/// <remarks>
		/// Reserved for cloud drives.
		/// </remarks>
		SyncStatus = 5,

		/// <summary>
		/// Sort by file tags.
		/// </summary>
		FileTag = 6,

		/// <summary>
		/// Sort by original folder.
		/// </summary>
		/// <remarks>
		/// Preserved for recycle bin.
		/// </remarks>
		OriginalFolder = 7,

		/// <summary>
		/// Sort by date deleted.
		/// </summary>
		/// <remarks>
		/// Preserved for recycle bin.
		/// </remarks>
		DateDeleted = 8,

		/// <summary>
		/// Sort by path.
		/// </summary>
		/// <remarks>
		/// Preserved for search results.
		/// </remarks>
		Path = 9
	}
}
