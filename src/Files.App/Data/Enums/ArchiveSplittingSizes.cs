// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Defines constants that specify 7zip archive splitting size.
	/// </summary>
	public enum ArchiveSplittingSizes
	{
		/// <summary>
		/// Don't split out.
		/// </summary>
		None,

		/// <summary>
		/// Split into each 10 MB.
		/// </summary>
		Mo10,

		/// <summary>
		/// Split into each 100 MB.
		/// </summary>
		Mo100,

		/// <summary>
		/// Split into each 1 GB.
		/// </summary>
		Mo1024,

		/// <summary>
		/// Split into each 2 GB.
		/// </summary>
		Mo2048,

		/// <summary>
		/// Split into each 5 GB.
		/// </summary>
		Mo5120,

		/// <summary>
		/// Split into each 4 GB - FAT.
		/// </summary>
		Fat4092,

		/// <summary>
		/// Split into each 650 MB - CD.
		/// </summary>
		Cd650,

		/// <summary>
		/// Split into each 700 MB - CD.
		/// </summary>
		Cd700,

		/// <summary>
		/// Split into each 4.38 GB - DVD.
		/// </summary>
		Dvd4480,

		/// <summary>
		/// Split into each 7.94 GB - DVD.
		/// </summary>
		Dvd8128,

		/// <summary>
		/// Split into each 22.5 GB - Blu-ray.
		/// </summary>
		Bd23040
	}
}
