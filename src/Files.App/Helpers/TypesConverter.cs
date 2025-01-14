// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Files.App.Helpers
{
	public sealed class KnownTypesConverter
	{
		public JsonSerializerOptions Options { get; } = new JsonSerializerOptions();

		public KnownTypesConverter()
		{
			Options.Converters.Add(new TypesConverter());
		}
	}

	public sealed class TypesConverter : JsonConverter<Type>
	{
		public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => typeToConvert;
		public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options) => writer.WriteStringValue(value.FullName);
	}
}
