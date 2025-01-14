// Copyright (c) Files Community
// Licensed under the MIT License.

using Jeffijoe.MessageFormat;
using Microsoft.Windows.ApplicationModel.Resources;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Files.App.Extensions
{
	/// <summary>
	/// Extension methods for working with localized resources and message formatting.
	/// </summary>
	public static class MessageFormatExtensions
	{
		/// <summary>
		/// Resource map for accessing localized strings.
		/// It is initialized with the main resource map of the application's resources and the subtree "Resources".
		/// </summary>
		private static readonly ResourceMap _resourcesTree = new ResourceManager().MainResourceMap.TryGetSubtree("Resources");

		/// <summary>
		/// CultureInfo based on the application's primary language override.
		/// It is initialized with the selected language of the application.
		/// </summary>
		private static readonly CultureInfo _locale = new(AppLanguageHelper.PreferredLanguage.Code);

		/// <summary>
		/// Gets custom value formatters for the message formatter.
		/// This class is used to customize the formatting of specific value types.
		/// </summary>
		private static readonly CustomValueFormatters _customFormatter = new()
		{
			// Custom formatting for number values.
			Number = (CultureInfo _, object? value, string? style, out string? formatted) =>
			{
				if (style is not null && style == string.Empty)
				{
					// Format the number '{0, number}'
					formatted = string.Format($"{{0:#,##0}}", value);
					return true;
				}

				formatted = null;
				return false;
			}
		};

		/// <summary>
		/// Message formatter with caching enabled, using the current UI culture's two-letter ISO language name.
		/// It is initialized with the options to use cache and the two-letter ISO language name of the current UI culture,
		/// and a custom value formatter for number values.
		/// </summary>
		private static readonly MessageFormatter _formatter = new(useCache: false, locale: _locale.TwoLetterISOLanguageName, customValueFormatter: _customFormatter);

		/// <summary>
		/// Creates a dictionary for format pairs with a string key.
		/// </summary>
		/// <param name="key">The key for the format pair.</param>
		/// <param name="value">The value for the format pair.</param>
		/// <returns>A dictionary containing the format pair.</returns>
		public static IReadOnlyDictionary<string, object?> ToFormatPairs(this string key, object value) => new Dictionary<string, object?> { [key] = value };

		/// <summary>
		/// Creates a dictionary for format pairs with an integer key.
		/// </summary>
		/// <param name="key">The key for the format pair.</param>
		/// <param name="value">The value for the format pair.</param>
		/// <returns>A dictionary containing the format pair.</returns>
		public static IReadOnlyDictionary<string, object?> ToFormatPairs(this int key, object value) => new Dictionary<string, object?> { [key.ToString()] = value };

		/// <summary>
		/// Retrieves a localized resource string, formatting it with the provided pairs.
		/// </summary>
		/// <param name="resourceKey">The key for the resource string.</param>
		/// <param name="pairs">The format pairs to use when formatting the resource string.</param>
		/// <returns>The formatted localized resource string.</returns>
		public static string GetLocalizedFormatResource(this string resourceKey, IReadOnlyDictionary<string, object?> pairs)
		{
			var value = _resourcesTree?.TryGetValue(resourceKey)?.ValueAsString;

			if (value is null)
				return string.Empty;

			try
			{
				value = _formatter.FormatMessage(value, pairs);
			}
			catch
			{
				value = string.Empty;
				App.Logger.LogWarning($"Formatter could not get a valid result value for: '{resourceKey}'");
			}

			return value;
		}

		/// <summary>
		/// Merges multiple dictionaries of format pairs and retrieves a localized resource string, formatting it with the merged pairs.
		/// </summary>
		/// <param name="resourceKey">The key for the resource string.</param>
		/// <param name="pairs">An array of dictionaries containing the format pairs to use when formatting the resource string.</param>
		/// <returns>The formatted localized resource string.</returns>
		public static string GetLocalizedFormatResource(this string resourceKey, params IReadOnlyDictionary<string, object?>[] pairs)
		{
			var mergedPairs = pairs.SelectMany(dict => dict).ToDictionary(pair => pair.Key, pair => pair.Value);
			return GetLocalizedFormatResource(resourceKey, mergedPairs);
		}

		/// <summary>
		/// Converts multiple values to a dictionary with their indices as keys and retrieves a localized resource string, formatting it with the created pairs.
		/// </summary>
		/// <param name="resourceKey">The key for the resource string.</param>
		/// <param name="values">An array of values to use when formatting the resource string.</param>
		/// <returns>The formatted localized resource string.</returns>
		public static string GetLocalizedFormatResource(this string resourceKey, params object[] values)
		{
			var pairs = values.Select((value, index) => new KeyValuePair<string, object?>(index.ToString(), value))
							  .ToDictionary(pair => pair.Key, pair => pair.Value);
			return GetLocalizedFormatResource(resourceKey, pairs);
		}

		//TODO: Could replace `GetLocalizedResource()` in the future
		/// <summary>
		/// Retrieves a localized resource string without any format pairs.
		/// </summary>
		/// <param name="resourceKey">The key for the resource string.</param>
		/// <returns>The localized resource string.</returns>
		public static string GetLocalizedFormatResource(this string resourceKey) => GetLocalizedFormatResource(resourceKey, new Dictionary<string, object?>());
	}
}
