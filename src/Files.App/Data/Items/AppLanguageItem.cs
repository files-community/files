// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Globalization;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents a language in the application.
	/// </summary>
	public sealed class AppLanguageItem
	{
		/// <summary>
		/// Gets the language code. e.g. en-US.
		/// </summary>
		public string Code { get; set; }

		/// <summary>
		/// Gets the language name. e.g. English (United States)
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AppLanguageItem"/> class.
		/// </summary>
		/// <param name="code">The code of the language.</param>
		/// <param name="systemDefault">Indicates whether the language code is the system default.</param>
		public AppLanguageItem(string code, bool systemDefault = false)
		{
			if (systemDefault || string.IsNullOrEmpty(code))
			{
				Code = new CultureInfo(code).Name;
				Name = "SettingsPreferencesSystemDefaultLanguageOption".GetLocalizedResource();
			}
			else
			{
				var culture = new CultureInfo(code);
				Code = culture.Name;
				Name = culture.NativeName;
			}
		}

		public override string ToString() => Name;
	}
}
