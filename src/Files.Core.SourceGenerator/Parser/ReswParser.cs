// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Xml.Linq;
using static Files.Core.SourceGenerator.Constants.StringsPropertyGenerator;

namespace Files.Core.SourceGenerator.Parser
{
	/// <summary>
	/// Provides methods to parse RESW (Resource) files and extract keys with optional comments.
	/// </summary>
	internal static class ReswParser
	{
		/// <summary>
		/// Parses a RESW (Resource) file and extracts keys with optional comments.
		/// </summary>
		/// <param name="file">The <see cref="AdditionalText"/> representing the RESW file to parse.</param>
		/// <returns>An <see cref="IEnumerable{ParserItem}"/> containing the extracted keys and their corresponding values and comments.</returns>
		internal static IEnumerable<ParserItem> GetKeys(AdditionalText file)
		{
			var document = XDocument.Load(file.Path);
			var keys = document
				.Descendants("data")
				.Select(element => new ParserItem {
					Key = element.Attribute("name")?.Value.Replace('.', ConstantSeparator)!,
					Value = element.Element("value")?.Value ?? string.Empty,
					Comment = element.Element("comment")?.Value })
				.Where(item => !string.IsNullOrEmpty(item.Key));

			return keys is not null
				? keys.OrderBy(item => item.Key)
				: Enumerable.Empty<ParserItem>();
		}
	}
}
