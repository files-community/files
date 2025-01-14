// Copyright (c) Files Community
// Licensed under the MIT License.

using static Files.Core.SourceGenerator.Constants.DiagnosticDescriptors;
using static Files.Core.SourceGenerator.Constants.StringsPropertyGenerator;

namespace Files.Core.SourceGenerator.CodeFixProviders
{
	/// <summary>
	/// Code fix provider that replaces string literals with constants from the <c>Strings</c> class.
	/// </summary>
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringsPropertyCodeFixProvider)), Shared]
	internal sealed class StringsPropertyCodeFixProvider : CodeFixProvider
	{
		/// <summary>
		/// Gets a list of diagnostic IDs that this provider can fix.
		/// </summary>
		public sealed override ImmutableArray<string> FixableDiagnosticIds => [FSG1002.Id];

		/// <summary>
		/// Gets the fix all provider for this code fix provider.
		/// </summary>
		/// <returns>A <see cref="FixAllProvider"/>.</returns>
		public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

		/// <summary>
		/// Registers code fixes for the specified diagnostic.
		/// </summary>
		/// <param name="context">The context for the code fix.</param>
		public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
		{
			var diagnostic = context.Diagnostics.First();

			var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
			if (root == null)
				return;

			var diagnosticSpan = diagnostic.Location.SourceSpan;

			SyntaxNode? node = null;

			if (root.FindNode(diagnosticSpan) is LiteralExpressionSyntax literalExpression)
				node = literalExpression;

			if (root.FindNode(diagnosticSpan) is InterpolatedStringTextSyntax interpolatedStringText)
				node = interpolatedStringText.Parent;

			if (node is null)
				return;

			var constantName = diagnostic.Properties[ConstantNameProperty];
			var newExpression = SyntaxFactory.ParseExpression($"{StringsClassName}.{constantName}").WithTriviaFrom(node);
			var newRoot = root.ReplaceNode(node, newExpression);
			var newDocument = context.Document.WithSyntaxRoot(newRoot);

			context.RegisterCodeFix(
				CodeAction.Create(
					CodeFixProviderTitle,
					c => Task.FromResult(newDocument),
					null),
				diagnostic);
		}
	}
}
