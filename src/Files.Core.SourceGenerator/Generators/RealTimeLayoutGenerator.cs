// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using static Files.Core.SourceGenerator.Constants.RealTimeLayoutGenerator;

namespace Files.Core.SourceGenerator.Generators
{
	/// <summary>
	/// Generates additional source code for classes based on their inheritance from specific types 
	/// (e.g., IRealTimeWindow or IRealTimeControl).
	/// </summary>
	[Generator]
	public class RealTimeLayoutGenerator : IIncrementalGenerator
	{
		/// <summary>
		/// Initializes the incremental source generator with context-specific configuration.
		/// </summary>
		/// <param name="context">The incremental generator initialization context.</param>
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var candidateClasses = context.SyntaxProvider
				.CreateSyntaxProvider(
					predicate: (node, cancellationToken) => IsValidCandidate(node),
					transform: (context, cancellationToken) => GetCandidateClass(context))
				.Where(candidate => candidate != null);

			context.RegisterSourceOutput(candidateClasses, (context, candidate) =>
			{
				var classDeclaration = candidate!.Value.Class;
				var type = candidate!.Value.Type;
				var className = classDeclaration.Identifier.Text;
				var namespaceName = GetNamespace(classDeclaration);

				var source = GenerateClass(namespaceName, className, type);
				context.AddSource($"{className}.{Guid.NewGuid()}.g.cs", SourceText.From(source, Encoding.UTF8));
			});
		}

		/// <summary>
		/// Determines if the syntax node is a valid candidate for generation.
		/// </summary>
		/// <param name="syntaxNode">The syntax node to evaluate.</param>
		/// <returns>True if the node is a valid class candidate; otherwise, false.</returns>
		private static bool IsValidCandidate(SyntaxNode syntaxNode)
		{
			if (syntaxNode is ClassDeclarationSyntax classDeclaration)
			{
				return classDeclaration.BaseList?.Types.Any(baseType => baseType.Type is IdentifierNameSyntax identifier &&
					(identifier.Identifier.Text == SpecificationWindowName || identifier.Identifier.Text == SpecificationControlName)) == true;
			}
			return false;
		}

		/// <summary>
		/// Retrieves a class declaration and its specification type if it matches the criteria.
		/// </summary>
		/// <param name="context">The syntax context for the generator.</param>
		/// <returns>A tuple containing the class declaration and its specification type, or null if no match.</returns>
		private static (ClassDeclarationSyntax Class, SpecificationType Type)? GetCandidateClass(GeneratorSyntaxContext context)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;

			var type = SpecificationType.None;
			foreach (var baseType in classDeclaration.BaseList!.Types)
			{
				if (baseType.Type is IdentifierNameSyntax identifier)
				{
					if (identifier.Identifier.Text == SpecificationWindowName)
						type = SpecificationType.Window;
					else if (identifier.Identifier.Text == SpecificationControlName)
						type = SpecificationType.Control;
				}
			}

			return type != SpecificationType.None ? (classDeclaration, type) : null;
		}

		/// <summary>
		/// Generates the source code for a class based on the provided namespace, class name, and type.
		/// </summary>
		/// <param name="namespaceName">The namespace of the class.</param>
		/// <param name="className">The name of the class.</param>
		/// <param name="type">The type of the specification (Window or Control).</param>
		/// <returns>The generated source code as a string.</returns>
		private static string GenerateClass(string namespaceName, string className, SpecificationType type)
		{
			// Namespace
			var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
				.WithLeadingTrivia(SourceGeneratorHelper.GetLicenceHeader())
				.NormalizeWhitespace();

			// Usings
			var usings = Array.Empty<UsingDirectiveSyntax>();

			// Property: IRealTimeLayoutService RealTimeLayoutService;
			var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
					SyntaxFactory.IdentifierName(ServiceInterfaceName),
					SyntaxFactory.Identifier(ServiceVariableName))
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
				.AddAccessorListAccessors(
					SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
						.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
					SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
						.AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
						.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
				.AddAttributeLists(SourceGeneratorHelper.GetAttributeForField(nameof(RealTimeLayoutGenerator)));

			// Method: InitializeContentLayout
			var initializeContentLayoutMethod = SyntaxFactory.MethodDeclaration(
					SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
					"InitializeContentLayout")
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
				.WithBody(SyntaxFactory.Block(CreateContentLayoutBody(type, isInitialize: true)))
				.AddAttributeLists(SourceGeneratorHelper.GetAttributeForMethod(nameof(RealTimeLayoutGenerator)));

			// Method: UpdateContentLayout
			var updateContentLayoutMethod = SyntaxFactory.MethodDeclaration(
					SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
					"UpdateContentLayout")
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
				.WithBody(SyntaxFactory.Block(CreateContentLayoutBody(type)))
				.AddAttributeLists(SourceGeneratorHelper.GetAttributeForMethod(nameof(RealTimeLayoutGenerator)));

			// Class declaration
			var classDeclaration = SyntaxFactory.ClassDeclaration(className)
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.PartialKeyword))
				.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(
					type is SpecificationType.Window ? SpecificationWindowName : SpecificationControlName)))
				.AddMembers(propertyDeclaration, initializeContentLayoutMethod, updateContentLayoutMethod)
				.AddAttributeLists(SourceGeneratorHelper.GetAttributeForField(nameof(RealTimeLayoutGenerator)));

			// Add class to namespace
			namespaceDeclaration = namespaceDeclaration.AddMembers(classDeclaration);

			var compilationUnit = SyntaxFactory.CompilationUnit()
				.AddUsings(usings)
				.AddMembers(namespaceDeclaration)
				.NormalizeWhitespace();

			return SyntaxFactory.SyntaxTree(compilationUnit, encoding: Encoding.UTF8).GetText().ToString();
		}

		/// <summary>
		/// Creates a collection of statements for updating the content layout body.
		/// Depending on the specification type, it will update the title bar and content layout.
		/// If the <paramref name="isInitialize"/> flag is set to true, a callback is added for updating the content layout.
		/// </summary>
		/// <param name="type">The specification type, used to determine if the title bar should be updated.</param>
		/// <param name="isInitialize">A flag indicating whether to add a callback for content layout initialization.</param>
		/// <returns>An IEnumerable of <see cref="StatementSyntax"/> representing the generated statements.</returns>
		private static IEnumerable<StatementSyntax> CreateContentLayoutBody(SpecificationType type, bool isInitialize = false)
		{
			var statements = new List<StatementSyntax>();

			if (isInitialize)
			{

				statements.Add(
					SyntaxFactory.ExpressionStatement(
				SyntaxFactory.AssignmentExpression(
					SyntaxKind.SimpleAssignmentExpression,
					SyntaxFactory.IdentifierName(ServiceVariableName),
					SyntaxFactory.InvocationExpression(
						SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							SyntaxFactory.MemberAccessExpression(
								SyntaxKind.SimpleMemberAccessExpression,
								SyntaxFactory.IdentifierName("Ioc"),
								SyntaxFactory.IdentifierName("Default")),
							SyntaxFactory.GenericName(
								SyntaxFactory.Identifier("GetRequiredService"))
							.WithTypeArgumentList(
								SyntaxFactory.TypeArgumentList(
									SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
										SyntaxFactory.IdentifierName(ServiceInterfaceName)))))))));
			}

			if (type == SpecificationType.Window)
			{
				statements.Add(
					SyntaxFactory.ExpressionStatement(
						SyntaxFactory.InvocationExpression(
							SyntaxFactory.MemberAccessExpression(
								SyntaxKind.SimpleMemberAccessExpression,
								SyntaxFactory.IdentifierName(ServiceVariableName),
								SyntaxFactory.IdentifierName("UpdateTitleBar")))
						.WithArgumentList(
							SyntaxFactory.ArgumentList(
								SyntaxFactory.SingletonSeparatedList(
									SyntaxFactory.Argument(SyntaxFactory.ThisExpression()))))));
			}

			statements.Add(
				SyntaxFactory.ExpressionStatement(
					SyntaxFactory.InvocationExpression(
						SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							SyntaxFactory.IdentifierName(ServiceVariableName),
							SyntaxFactory.IdentifierName("UpdateContent")))
					.WithArgumentList(
						SyntaxFactory.ArgumentList(
							SyntaxFactory.SingletonSeparatedList(
								SyntaxFactory.Argument(SyntaxFactory.ThisExpression()))))));

			if (isInitialize)
			{
				statements.Add(
				SyntaxFactory.ExpressionStatement(
					SyntaxFactory.InvocationExpression(
						SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							SyntaxFactory.IdentifierName(ServiceVariableName),
							SyntaxFactory.IdentifierName("AddCallback")))
					.WithArgumentList(
						SyntaxFactory.ArgumentList(
							SyntaxFactory.SeparatedList(
								new[]{
									SyntaxFactory.Argument(SyntaxFactory.ThisExpression()),
									SyntaxFactory.Argument(SyntaxFactory.IdentifierName("UpdateContentLayout"))
								})))));
			}

			return statements;
		}

		/// <summary>
		/// Retrieves the namespace of a given syntax node.
		/// </summary>
		/// <param name="node">The syntax node to evaluate.</param>
		/// <returns>The namespace name as a string.</returns>
		private static string GetNamespace(SyntaxNode node)
		{
			while (node != null)
			{
				if (node is NamespaceDeclarationSyntax namespaceDeclaration)
					return namespaceDeclaration.Name.ToString();
				node = node.Parent!;
			}

			return "GlobalNamespace";
		}
	}
}