// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents item for the database of a folder's layout preferences.
	/// </summary>
	[RegistrySerializable]
	public sealed class LayoutPreferencesDatabaseItem
	{
		public ulong? Frn { get; set; }

		public string FilePath { get; set; } = string.Empty;

		public LayoutPreferencesItem LayoutPreferencesManager { get; set; } = new();
	}
}
