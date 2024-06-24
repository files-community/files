﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.SourceGenerator.Generators
{
	/// <summary>
	/// Generates properties for strings based on resource files.
	/// </summary>
	[Generator]
	internal sealed class StringsPropertyGenerator : IIncrementalGenerator
	{
		/// <summary>
		/// Initializes the generator and registers source output based on resource files.
		/// </summary>
		/// <param name="context">The initialization context.</param>
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var additionalFiles = context
				.AdditionalTextsProvider.Where(af => af.Path.Contains("en-US\\Resources"))
				.Select((f, _) => new AdditionalTextWithHash(f, Guid.NewGuid()));

			context.RegisterSourceOutput(additionalFiles, Execute);
		}

		/// <summary>
		/// Executes the generation of string properties based on the provided file.
		/// </summary>
		/// <param name="ctx">The source production context.</param>
		/// <param name="fileWithHash">The additional text file with its hash.</param>
		private void Execute(SourceProductionContext ctx, AdditionalTextWithHash fileWithHash)
		{
			var sb = new StringBuilder();
			_ = sb.AppendLine("/// <auto-generated />");
			_ = sb.AppendLine($"/// {fileWithHash}");
			_ = sb.AppendLine();
			_ = sb.AppendLine($"// Copyright (c) {DateTime.Now.Year} Files Community");
			_ = sb.AppendLine("// Licensed under the MIT License. See the LICENSE.");
			_ = sb.AppendLine();
			_ = sb.AppendLine("namespace Files.App.Helpers");
			_ = sb.AppendLine("{");
			_ = sb.AppendLine("    public sealed partial class Strings");
			_ = sb.AppendLine("    {");

			foreach (var key in ReadAllKeys(fileWithHash.File)) // Write all keys from file
				AddKey(
					buffer: sb,
					key: key.Item1,
					comment: key.Item2
				);

			_ = sb.AppendLine("    }");
			_ = sb.AppendLine("}");

			var sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);

			ctx.AddSource($"Strings.Properties.{fileWithHash.Hash}.g.cs", sourceText);
		}

		/// <summary>
		/// Adds a constant string key to the buffer with an optional comment.
		/// </summary>
		/// <param name="buffer">The string builder buffer.</param>
		/// <param name="key">The key name.</param>
		/// <param name="comment">Optional comment describing the key.</param>
		/// <param name="value">Optional value assigned to the key.</param>
		/// <param name="tabPos">Position of the tab.</param>
		private void AddKey(StringBuilder buffer, string key, string? comment = null, string? value = null, int tabPos = 2)
		{
			var tabString = SourceGeneratorHelper.Spacing(tabPos);

			if (comment is not null)
			{
				_ = buffer.AppendLine();
				_ = buffer.AppendLine($"{tabString}/// <summary>");
				_ = buffer.AppendLine($"{tabString}/// {comment}");
				_ = buffer.AppendLine($"{tabString}/// </summary>");
			}

			_ = buffer.AppendLine($@"{tabString}public const string {KeyNameValidator(key)} = ""{value ?? key}"";");
		}

		/// <summary>
		/// Reads all keys from the specified additional text file.
		/// </summary>
		/// <param name="file">The additional text file containing keys.</param>
		/// <returns>An enumerable of key-value pairs.</returns>
		private IEnumerable<Tuple<string, string?>> ReadAllKeys(AdditionalText file)
		{
			return SystemIO.Path.GetExtension(file.Path) switch
			{
				".resw" => ReswParser.GetKeys(file),
				".json" => JsonParser.GetKeys(file),
				_ => []
			};
		}

		/// <summary>
		/// Validates and returns a valid C# identifier name for the given key.
		/// </summary>
		/// <param name="key">The key to validate.</param>
		/// <returns>A valid C# identifier based on the key.</returns>
		private string KeyNameValidator(string key)
		{
			Span<char> resultSpan = key.Length <= 256 ? stackalloc char[key.Length] : new char[key.Length];
			var keySpan = key.AsSpan();

			for (var i = 0; i < keySpan.Length; i++)
			{
				resultSpan[i] = keySpan[i] switch
				{
					'+' => 'P',
					' ' or '.' => '_',
					_ => keySpan[i],
				};
			}

			return resultSpan.ToString();
		}
	}
}
