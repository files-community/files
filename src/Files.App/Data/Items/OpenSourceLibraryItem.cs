// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents an item for open source library shown on <see cref="Views.Settings.AboutPage"/>.
	/// </summary>
	public class OpenSourceLibraryItem
	{
		/// <summary>
		/// Gets the URL that navigates to the open source library.
		/// </summary>
		public string Url { get; } = "";

		/// <summary>
		/// Gets the name of the open source library.
		/// </summary>
		public string Name { get; } = "";

		/// <summary>
		/// Initializes an instance of <see cref="OpenSourceLibraryItem"/> class.
		/// </summary>
		/// <param name="url">The URL</param>
		/// <param name="name">The name</param>
		public OpenSourceLibraryItem(string url, string name)
		{
			Url = url;
			Name = name;
		}
	}
}
