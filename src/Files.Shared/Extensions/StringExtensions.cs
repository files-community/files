// Copyright (c) Files Community
// Licensed under the MIT License.

using System;

namespace Files.Shared.Extensions
{
	public static class StringExtensions
	{
		/// <summary>Gets the leftmost <paramref name="length" /> characters from a string.</summary>
		/// <param name="value">The string to retrieve the substring from.</param>
		/// <param name="length">The number of characters to retrieve.</param>
		/// <returns>The substring.</returns>
		public static string Left(this string value, int length)
		{
			if (value is null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(length), length, "Length is less than zero");
			}

			return length > value.Length ? value : value.Substring(0, length);
		}

		/// <summary>Gets the rightmost <paramref name="length" /> characters from a string.</summary>
		/// <param name="value">The string to retrieve the substring from.</param>
		/// <param name="length">The number of characters to retrieve.</param>
		/// <returns>The substring.</returns>
		public static string Right(this string value, int length)
		{
			if (value is null)
			{
				throw new ArgumentNullException(nameof(value));
			}
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(length), length, "Length is less than zero");
			}

			return length > value.Length ? value : value.Substring(value.Length - length);
		}
	}
}
