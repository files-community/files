// Copyright (c) Files Community
// Licensed under the MIT License.

using static Files.Core.SourceGenerator.Constants.DiagnosticDescriptors;
using static Files.Core.SourceGenerator.Constants.StringsPropertyGenerator;

namespace Files.Core.SourceGenerator.Analyzers
{
	/// <summary>
	/// Analyzer that detects if string literals can be replaced with constants from the <c>Strings</c> class.
	/// </summary>
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	internal sealed class StringsPropertyAnalyzer : DiagnosticAnalyzer
	{
		/// <summary>
		/// Represents a collection of constant string names and their corresponding values.
		/// </summary>
		internal static FrozenDictionary<string, string?>? StringsConstants { get; private set; } = null;

		/// <summary>
		/// Gets the supported diagnostics for this analyzer.
		/// </summary>
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [FSG1002];

		/// <summary>
		/// Initializes the analyzer and registers the syntax node action.
		/// </summary>
		/// <param name="context">The analysis context.</param>
		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.StringLiteralExpression, SyntaxKind.InterpolatedStringText);
		}

		/// <summary>
		/// Analyzes the syntax node to detect if a string literal can be replaced with a constant from the Strings class.
		/// </summary>
		/// <param name="context">The syntax node analysis context.</param>
		private void AnalyzeNode(SyntaxNodeAnalysisContext context)
		{
			var literalValue = string.Empty;
			Location locationValue = default!;
			SyntaxNode? parent = null;

			if (context.Node is InterpolatedStringTextSyntax interpolatedStringText)
			{
				literalValue = interpolatedStringText.TextToken.ValueText;
				locationValue = interpolatedStringText.GetLocation();
				parent = interpolatedStringText.Parent?.Parent;
			}

			if (context.Node is LiteralExpressionSyntax literalExpression)
			{
				literalValue = literalExpression.Token.ValueText;
				locationValue = literalExpression.GetLocation();
				parent = literalExpression.Parent;
			}

			if (string.IsNullOrEmpty(literalValue) ||
				parent is not MemberAccessExpressionSyntax memberAccessExpression ||
				!LocalizedMethodNames.Contains(memberAccessExpression.Name.Identifier.Text))
				return;

			if (StringsConstants is null)
			{
				var semanticModel = context.SemanticModel;
				var compilation = semanticModel.Compilation;

				// Get the type symbol for the Strings class
				var stringsTypeSymbol = compilation.GetTypeByMetadataName(StringsMetadataName);

				if (stringsTypeSymbol == null)
					return;

				// Extract constants from the Strings class
				StringsConstants = stringsTypeSymbol.GetMembers()
					.OfType<IFieldSymbol>()
					.Where(f => f.IsConst)
					.ToDictionary(f => f.Name, f => f.ConstantValue?.ToString())
					.ToFrozenDictionary();
			}

			// Check if the literal value matches any of the constants
			var match = StringsConstants.FirstOrDefault(pair => pair.Value == literalValue);

			if (!match.Equals(default(KeyValuePair<string, string>)))
			{
				var properties = ImmutableDictionary<string, string>
					.Empty
					.Add(ConstantNameProperty, match.Key);

				var diagnostic = Diagnostic.Create(FSG1002, locationValue, properties!, literalValue, match.Key);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
