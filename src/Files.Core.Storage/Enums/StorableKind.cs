// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.Storage.Enums
{
	[Flags]
	public enum StorableKind : byte
	{
		None = 0,
		Files = 1,
		Folders = 2,
		All = Files | Folders
	}
}
