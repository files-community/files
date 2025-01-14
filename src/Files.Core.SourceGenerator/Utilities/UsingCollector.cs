// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.SourceGenerator.Utilities
{
	/// <summary>
	/// Syntax walker that collects namespaces from using directives within C# syntax trees.
	/// </summary>
	internal sealed class UsingCollector : CSharpSyntaxWalker
	{
		private readonly HashSet<string> _namespaces;

		/// <summary>
		/// Initializes a new instance of the <see cref="UsingCollector"/> class with the specified set of namespaces.
		/// </summary>
		/// <param name="namespaces">The set to store collected namespaces.</param>
		internal UsingCollector(HashSet<string> namespaces) => _namespaces = namespaces;

		/// <summary>
		/// Visits a using directive node and adds the namespace to the set.
		/// </summary>
		/// <param name="node">The using directive syntax node.</param>
		public override void VisitUsingDirective(UsingDirectiveSyntax node) => _namespaces.Add(node.Name!.ToString());
	}
}
