// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Type of operation on Files filesystem that took place
	/// </summary>
	public enum FileOperationType : byte
	{
		/// <summary>
		/// An item has been created
		/// </summary>
		CreateNew = 0,

		/// <summary>
		/// An item has been renamed
		/// </summary>
		Rename = 1,

		/// <summary>
		/// An item has been copied to destination
		/// </summary>
		Copy = 3,

		/// <summary>
		/// An item has been moved to destination
		/// </summary>
		Move = 4,

		/// <summary>
		/// An archive has been extracted
		/// </summary>
		Extract = 5,

		/// <summary>
		/// An item has been recycled
		/// </summary>
		Recycle = 6,

		/// <summary>
		/// An item has been restored from Recycle Bin
		/// </summary>
		Restore = 7,

		/// <summary>
		/// A item has been deleted
		/// </summary>
		Delete = 8,

		/// <summary>
		/// A link to an item has been created
		/// </summary>
		CreateLink = 9,

		/// <summary>
		/// An item is being prepared for copy/move/drag
		/// </summary>
		Prepare = 10,

		/// <summary>
		/// An item has been added to an archive
		/// </summary>
		Compressed = 11,
	}
}
