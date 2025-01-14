// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.SourceGenerator
{
	/// <summary>
	/// Contains various constants used within the source generator.
	/// </summary>
	internal class Constants
	{
		/// <summary>
		/// Contains diagnostic descriptors used for error reporting.
		/// </summary>
		internal class DiagnosticDescriptors
		{
			/// <summary>
			/// Diagnostic descriptor for unsupported types in Windows Registry.
			/// </summary>
			internal static readonly DiagnosticDescriptor FSG1001 = new(
				id: nameof(FSG1001),
				title: "Types that are not supported by Windows Registry",
				messageFormat: "Type '{0}' is not supported by Windows Registry",
				category: "Design",
				defaultSeverity: DiagnosticSeverity.Error,
				isEnabledByDefault: true);

			/// <summary>
			/// Diagnostic descriptor for a refactoring suggestion to replace string literals with constants from the Strings class.
			/// </summary>
			internal static readonly DiagnosticDescriptor FSG1002 = new(
				id: nameof(FSG1002),
				title: "String literal can be replaced with constant",
				messageFormat: $"Replace '{{0}}' with '{StringsPropertyGenerator.StringsClassName}.{{1}}'",
				category: "Refactoring",
				defaultSeverity: DiagnosticSeverity.Warning,
				isEnabledByDefault: true,
				description: $"Detects string literals that can be replaced with constants from the {StringsPropertyGenerator.StringsClassName} class.");

			/// <summary>
			/// Diagnostic descriptor for a scenario where multiple files with the same name are detected.
			/// </summary>
			internal static readonly DiagnosticDescriptor FSG1003 = new(
				id: nameof(FSG1003),
				title: "Multiple files with the same name detected",
				messageFormat: "Multiple files named '{0}' were detected. Ensure all generated localization string files have unique names.",
				category: "FileGeneration",
				defaultSeverity: DiagnosticSeverity.Error,
				isEnabledByDefault: true,
				description: "This diagnostic detects cases where multiple localization string files are being generated with the same name," +
				"which can cause conflicts and overwrite issues.");

		}

		/// <summary>
		/// Contains constants related to DependencyProperty generation.
		/// </summary>
		internal class DependencyPropertyGenerator
		{
			/// <summary>
			/// The name of the attribute used for DependencyProperty.
			/// </summary>
			internal static readonly string AttributeName = "DependencyPropertyAttribute";
		}

		internal class StringsPropertyGenerator
		{
			/// <summary>
			/// The name of the generated class that contains string constants.
			/// </summary>
			internal const string StringsClassName = "Strings";

			/// <summary>
			/// The fully qualified name of the generated metadata class that contains string constants.
			/// </summary>
			internal const string StringsMetadataName = $"{SourceGeneratorHelper.HelperNamespace}{StringsClassName}";

			/// <summary>
			/// The name of the property that represents the name of the constant.
			/// </summary>
			internal const string ConstantNameProperty = nameof(ConstantNameProperty);

			/// <summary>
			/// A collection of method names that are considered localized methods.
			/// These methods are used to identify string literals that can be replaced with constants from the Strings class.
			/// </summary>
			internal static HashSet<string> LocalizedMethodNames = [
				/* TODO: Future use only this */ "ToLocalized",
				/* TODO: Rewrite with ToLocalized */ "GetLocalizedResource",
				/* TODO: Rewrite with ToLocalized */ "GetLocalizedFormatResource"];

			/// <summary>
			/// The title of the code fix provider that suggests replacing string literals with constants from the Strings class.
			/// </summary>
			internal const string CodeFixProviderTitle = $"Replace with constant from {StringsClassName}";

			/// <summary>
			/// Represents a character used as a separator in constant names.
			/// </summary>
			internal const char ConstantSeparator = '/';
		}
	}
}
