// Copyright (c) Files Community
// Licensed under the MIT License.

using static Files.Core.SourceGenerator.Constants.DiagnosticDescriptors;
using static Files.Core.SourceGenerator.Constants.StringsPropertyGenerator;
using static Files.Core.SourceGenerator.Utilities.SourceGeneratorHelper;

namespace Files.Core.SourceGenerator.Generators
{
	/// <summary>
	/// Generates properties for strings based on resource files.
	/// </summary>
	[Generator]
	internal sealed class StringsPropertyGenerator : IIncrementalGenerator
	{
		// Static HashSet to track generated file names
		private readonly HashSet<string> _generatedFileNames = [];

		/// <summary>
		/// Initializes the generator and registers source output based on resource files.
		/// </summary>
		/// <param name="context">The initialization context.</param>
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var additionalFiles = context
				.AdditionalTextsProvider.Where(af => af.Path.Contains("en-US\\Resources"));

			context.RegisterSourceOutput(additionalFiles, Execute);
		}

		/// <summary>
		/// Executes the generation of string properties based on the provided file.
		/// </summary>
		/// <param name="ctx">The source production context.</param>
		/// <param name="file">The additional text file with its hash.</param>
		private void Execute(SourceProductionContext ctx, AdditionalText file)
		{
			var fileName = SystemIO.Path.GetFileNameWithoutExtension(file.Path);

			lock (_generatedFileNames)
			{
				if (_generatedFileNames.Contains(fileName))
					ctx.ReportDiagnostic(Diagnostic.Create(FSG1003, Location.None, fileName));

				_ = _generatedFileNames.Add(fileName);
			}

			var tabString = Spacing(1);

			var sb = new StringBuilder();
			_ = sb.AppendFullHeader(file.Path);
			_ = sb.AppendLine();
			_ = sb.AppendLine($"namespace {HelperNamespace.Remove(HelperNamespace.Length - 1)}");
			_ = sb.AppendLine("{");
			_ = sb.AppendLine($"{tabString}/// <summary>");
			_ = sb.AppendLine($"{tabString}/// Represents a collection of string resources used throughout the application.");
			_ = sb.AppendLine($"{tabString}/// </summary>");
			_ = sb.AppendLine($"{tabString}public sealed partial class {StringsClassName}");
			_ = sb.AppendLine($"{tabString}{{");

			foreach (var key in ReadAllKeys(file)) // Write all keys from file
				AddKey(
					buffer: sb,
					key: key.Key,
					comment: key.Comment,
					exampleValue: key.Value
				);

			_ = sb.AppendLine($"{tabString}}}");
			_ = sb.AppendLine("}");

			var sourceText = SourceText.From(sb.ToString(), Encoding.UTF8);

			ctx.AddSource($"{StringsClassName}.{fileName}.g.cs", sourceText);
		}

		/// <summary>
		/// Adds a constant string key to the buffer with an optional comment.
		/// </summary>
		/// <param name="buffer">The string builder buffer.</param>
		/// <param name="key">The key name.</param>
		/// <param name="comment">Optional comment describing the key.</param>
		/// <param name="value">Optional value assigned to the key. If null, the key will be used as the value.</param>
		/// <param name="exampleValue">Optional example value for the key.</param>
		/// <param name="tabPos">Position of the tab.</param>
		private void AddKey(StringBuilder buffer, string key, string? comment = null, string? value = null, string? exampleValue = null, int tabPos = 2)
		{
			var tabString = Spacing(tabPos);

			if (comment is not null || exampleValue is not null)
			{
				_ = buffer.AppendLine();
				_ = buffer.AppendLine($"{tabString}/// <summary>");

				if (comment is not null)
					_ = buffer.AppendLine($@"{tabString}/// {comment}");

				_ = buffer.AppendLine($"{tabString}/// </summary>");

				if (exampleValue is not null)
				{
					_ = buffer.AppendLine($"{tabString}/// <remarks>");
					_ = buffer.AppendLine($"{tabString}/// e.g.: <b>{exampleValue}</b>");
					_ = buffer.AppendLine($"{tabString}/// </remarks>");
				}
			}

			_ = buffer.AppendLine($@"{tabString}public const string {KeyNameValidator(key)} = ""{value ?? key}"";");
		}

		/// <summary>
		/// Reads all keys from the provided file based on its extension.
		/// </summary>
		/// <param name="file">The additional text file to read keys from.</param>
		/// <returns>An enumerable of <see cref="ParserItem"/> objects containing the keys and their associated values.</returns>
		private IEnumerable<ParserItem> ReadAllKeys(AdditionalText file)
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
					' ' or '.' or ConstantSeparator => '_',
					_ => keySpan[i],
				};
			}

			return resultSpan.ToString();
		}
	}
}
