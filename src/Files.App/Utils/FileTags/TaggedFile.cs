// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.FileTags
{
	[RegistrySerializable]
	public sealed class TaggedFile
	{
		public ulong? Frn { get; set; }
		public string FilePath { get; set; } = string.Empty;
		public string[] Tags { get; set; } = [];
	}
}
