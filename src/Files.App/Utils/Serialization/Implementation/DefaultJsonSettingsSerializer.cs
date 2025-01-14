// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Text.Json;

namespace Files.App.Utils.Serialization.Implementation
{
	internal sealed class DefaultJsonSettingsSerializer : IJsonSettingsSerializer
	{
		public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
		{
			WriteIndented = true
		};

		public string? SerializeToJson(object? obj)
		{
			return JsonSerializer.Serialize(obj, Options);
		}

		public T? DeserializeFromJson<T>(string json)
		{
			return JsonSerializer.Deserialize<T?>(json);
		}
	}
}
