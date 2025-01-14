// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	public enum GroupOption
	{
		/// <summary>
		/// No grouping.
		/// </summary>
		None = 0,

		/// <summary>
		/// Group by name
		/// </summary>
		Name = 1,

		/// <summary>
		/// Group by date modified.
		/// </summary>
		DateModified = 2,

		/// <summary>
		/// Group by date created.
		/// </summary>
		DateCreated = 3,

		/// <summary>
		/// Group by size.
		/// </summary>
		Size = 4,

		/// <summary>
		/// Group by file type.
		/// </summary>
		FileType = 5,

		/// <summary>
		/// Group by sync status.
		/// </summary>
		/// <remarks>
		/// Preserved for cloud drives.
		/// </remarks>
		SyncStatus = 6,

		/// <summary>
		/// Group by file tags.
		/// </summary>
		FileTag = 7,

		/// <summary>
		/// Group by original folder.
		/// </summary>
		/// <remarks>
		/// Preserved for recycle bin.
		/// </remarks>
		OriginalFolder = 8,

		/// <summary>
		/// Group by date deleted.
		/// </summary>
		/// <remarks>
		/// Preserved for recycle bin.
		/// </remarks>
		DateDeleted = 9,

		/// <summary>
		/// Group by folder path.
		/// </summary>
		/// <remarks>
		/// Preserved for libraries.
		/// </remarks>
		FolderPath = 10,
	}
}
