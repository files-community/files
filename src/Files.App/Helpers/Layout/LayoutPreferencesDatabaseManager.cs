// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Files.App.Helpers
{
	/// <summary>
	/// Represents manager for the database of layout preferences.
	/// </summary>
	public class LayoutPreferencesDatabaseManager
	{
		// Fields
		private static readonly Lazy<LayoutPreferencesDatabase> dbInstance = new(() => new());

		// Methods
		public LayoutPreferencesItem? GetPreferences(string filePath, ulong? frn = null)
		{
			return dbInstance.Value.GetPreferences(filePath, frn);
		}

		public void SetPreferences(string filePath, ulong? frn, LayoutPreferencesItem? preferencesItem)
		{
			dbInstance.Value.SetPreferences(filePath, frn, preferencesItem);
		}

		public void ResetAll()
		{
			dbInstance.Value.ResetAll();
		}

		public void Import(string json)
		{
			dbInstance.Value.Import(json);
		}

		public string Export()
		{
			return dbInstance.Value.Export();
		}
	}
}
