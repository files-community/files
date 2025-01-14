// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.SourceGenerator.Data
{
	/// <summary>
	/// Represents an item used for parsing data.
	/// </summary>
	internal class ParserItem
	{
		/// <summary>
		/// Gets or sets the key of the item.
		/// </summary>
		/// <remarks>
		/// This property is required and cannot be null or empty.
		/// </remarks>
		internal required string Key { get; set; } = default!;

		/// <summary>
		/// Gets or sets the value of the item.
		/// </summary>
		/// <remarks>
		/// The default value is an empty string.
		/// </remarks>
		internal string Value { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the comment associated with the item.
		/// </summary>
		/// <remarks>
		/// The default value is null, indicating no comment.
		/// </remarks>
		internal string? Comment { get; set; } = null;
	}
}
