// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	public enum FileNameConflictResolveOptionType : uint
	{
		/// <summary>
		/// Generate new names.
		/// </summary>
		GenerateNewName = 0,

		/// <summary>
		/// Replace existing files.
		/// </summary>
		ReplaceExisting = 1,

		/// <summary>
		/// Skip pasting files.
		/// </summary>
		Skip = 2,

		/// <summary>
		/// No resolve option were selected.
		/// </summary>
		None = 4
	}
}
