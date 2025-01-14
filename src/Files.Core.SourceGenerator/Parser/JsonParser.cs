// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Text.Json;
using static Files.Core.SourceGenerator.Constants.StringsPropertyGenerator;

namespace Files.Core.SourceGenerator.Parser
{
	/// <summary>
	/// Provides methods to parse JSON files and extract keys with optional context information.
	/// </summary>
	internal static class JsonParser
	{
		/// <summary>
		/// Parses a JSON file and extracts keys with optional context information.
		/// </summary>
		/// <param name="file">The <see cref="AdditionalText"/> representing the JSON file to parse.</param>
		/// <returns>An <see cref="IEnumerable{ParserItem}"/> containing the extracted keys and their associated values.</returns>
		internal static IEnumerable<ParserItem> GetKeys(AdditionalText file)
		{
			var jsonText = new SystemIO.StreamReader(file.Path).ReadToEnd();
			var jsonDocument = JsonDocument.Parse(jsonText);
			var result = new List<ParserItem>();
			ProcessJsonObject(jsonDocument.RootElement, string.Empty, result);
			return result.OrderBy(item => item.Key);
		}

		private static void ProcessJsonObject(JsonElement jsonElement, string prefix, List<ParserItem> result)
		{
			if (jsonElement.ValueKind == JsonValueKind.Object)
			{
				foreach (var property in jsonElement.EnumerateObject())
				{
					var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}{ConstantSeparator}{property.Name}";

					if (property.Value.ValueKind == JsonValueKind.Object)
					{
						var obj = property.Value;
						if (obj.TryGetProperty("text", out var textElement) && obj.TryGetProperty("crowdinContext", out var crowdinContextElement))
						{
							// Add an entry for the "text" field and its corresponding context
							result.Add(new ParserItem
							{
								Key = key,
								Value = textElement.GetString() ?? string.Empty,
								Comment = crowdinContextElement.GetString()
							});
						}
						else
						{
							// Recursively process the object
							ProcessJsonObject(obj, key, result);
						}
					}
					else
					{
						// Handle other value kinds directly
						ProcessJsonElement(property.Value, key, result);
					}
				}
			}
			else
			{
				// If the root element is not an object, process it directly
				ProcessJsonElement(jsonElement, prefix, result);
			}
		}

		private static void ProcessJsonElement(JsonElement element, string key, List<ParserItem> result)
		{
			switch (element.ValueKind)
			{
				case JsonValueKind.String:
					result.Add(new ParserItem
					{
						Key = key,
						Value = element.GetString() ?? string.Empty
					});
					break;

				case JsonValueKind.Number:
					result.Add(new ParserItem
					{
						Key = key,
						Value = element.GetRawText()
					});
					break;

				case JsonValueKind.True:
				case JsonValueKind.False:
					result.Add(new ParserItem
					{
						Key = key,
						Value = element.GetBoolean().ToString()
					});
					break;

				case JsonValueKind.Array:
					var index = 0;
					foreach (var item in element.EnumerateArray())
					{
						ProcessJsonElement(item, $"{key}[{index}]", result);
						index++;
					}

					break;

				default:
					result.Add(new ParserItem
					{
						Key = key,
						Value = element.GetRawText()
					});
					break;
			}
		}
	}
}
