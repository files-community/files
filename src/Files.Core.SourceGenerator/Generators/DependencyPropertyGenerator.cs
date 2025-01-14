// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.SourceGenerator.Generators
{
	/// <summary>
	/// Generates a set of dependency property and its backing field.
	/// </summary>
	[Generator(LanguageNames.CSharp)]
	public sealed class DependencyPropertyGenerator : IIncrementalGenerator
	{
		/// <inheritdoc/>
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var valueProvider =
				context.SyntaxProvider.ForAttributeWithMetadataName(
					"Files.Shared.Attributes.DependencyPropertyAttribute`1",
					(_, _) => true,
					(syntaxContext, _) => syntaxContext)
				.Combine(context.CompilationProvider);

			context.RegisterSourceOutput(valueProvider, (ctx, tuple) =>
			{
				var (ga, compilation) = tuple;

				if (ga.TargetSymbol is not INamedTypeSymbol symbol)
					return;

				// Generate file name to emit
				var fileName = $"{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))}_{Guid.NewGuid()}.g.cs";

				// Generate C#
				var fullSyntaxTree = EmitSyntaxTree(symbol, ga.Attributes);
				if (string.IsNullOrEmpty(fullSyntaxTree))
					return;

				// Emit
				ctx.AddSource(fileName, fullSyntaxTree);
			});
		}

		private string EmitSyntaxTree(INamedTypeSymbol typeSymbol, ImmutableArray<AttributeData> attributeList)
		{
			var members = new List<MemberDeclarationSyntax>();

			foreach (var attribute in attributeList)
			{
				if (attribute.AttributeClass is not { TypeArguments: [var type, ..] })
					return string.Empty;

				// Get property name and property changed method name from attribute constructor
				// e.g. DependencyProperty("Property", "OnPropertyChanged")
				if (attribute.ConstructorArguments is not [{ Value: string propertyName }, { Value: string callbackMethodName }, ..])
					continue;

				var fieldName = $"{propertyName}Property";
				var isSetterPrivate = false;
				var defaultValue = "global::Microsoft.UI.Xaml.DependencyProperty.UnsetValue";
				var isNullable = false;

				// Get values from the attribute properties
				foreach (var namedArgument in attribute.NamedArguments)
				{
					if (namedArgument.Value.Value is { } value)
					{
						switch (namedArgument.Key)
						{
							case "IsSetterPrivate":
								isSetterPrivate = (bool)value;
								break;
							case "DefaultValue":
								defaultValue = (string)value;
								break;
							case "IsNullable":
								isNullable = (bool)value;
								break;
						}
					}
				}

				// Emit "new PropertyMetadata(...)" expression
				var dpPropertyMetadata = EmitPMObjectCreationExpression(SyntaxFactory.ParseExpression(defaultValue));

				// Append callback to PropertyMetadata
				if (!string.IsNullOrEmpty(callbackMethodName))
					dpPropertyMetadata = dpPropertyMetadata.AddArgumentListArguments(
						SyntaxFactory.Argument(EmitDPCallbackParenthesizedLambdaExpression(callbackMethodName, type, isNullable, typeSymbol)));

				// Emit "DependencyProperty.Register(...)" expression
				var dpRegisteringExpression = EmitDPRegisterInvocationExpression(propertyName, type, typeSymbol, dpPropertyMetadata);

				// Emit the backing DependencyProperty field with attributes
				var staticFieldDeclaration = SourceGeneratorHelper.GetStaticFieldDeclaration(fieldName, dpRegisteringExpression)
					.AddAttributeLists(SourceGeneratorHelper.GetAttributeForField(nameof(DependencyPropertyGenerator)));

				// Emit getter and setter of the property
				var getter = SourceGeneratorHelper.GetGetter(fieldName, isNullable, type);
				var setter = SourceGeneratorHelper.GetSetter(fieldName, isSetterPrivate);

				// Emit the property with attributes
				var propertyDeclaration = SourceGeneratorHelper.GetPropertyDeclaration(propertyName, isNullable, type, getter, setter)
					.AddAttributeLists(SourceGeneratorHelper.GetAttributeForMethod(nameof(DependencyPropertyGenerator)));

				// Add to the class members
				members.Add(staticFieldDeclaration);
				members.Add(propertyDeclaration);
			}

			if (members.Count is 0)
				return string.Empty;

			// Generate class block
			var generatedClass = SourceGeneratorHelper.GetClassDeclaration(typeSymbol, members);

			// Generate namespace block
			var generatedNamespace = SourceGeneratorHelper.GetFileScopedNamespaceDeclaration(typeSymbol, generatedClass);

			// Generate complication uint
			var compilationUnit = SourceGeneratorHelper.GetCompilationUnit(generatedNamespace);

			// Get full syntax tree and return as UTF8 string
			return SyntaxFactory.SyntaxTree(compilationUnit, encoding: Encoding.UTF8).GetText().ToString();
		}

		private ParenthesizedLambdaExpressionSyntax EmitDPCallbackParenthesizedLambdaExpression(string callbackName, ITypeSymbol type, bool isNullable, ITypeSymbol classSymbol)
		{
			// (d, e) => ((class)d).callbackName((type)e.OldValue, (type)e.NewValue)
			return SyntaxFactory.ParenthesizedLambdaExpression()
				.AddParameterListParameters(
					SyntaxFactory.Parameter(SyntaxFactory.Identifier("d")),
					SyntaxFactory.Parameter(SyntaxFactory.Identifier("e")))
				.WithExpressionBody(
					SyntaxFactory.InvocationExpression(
						SyntaxFactory.MemberAccessExpression(
							SyntaxKind.SimpleMemberAccessExpression,
							SyntaxFactory.ParenthesizedExpression(
								SyntaxFactory.CastExpression(
									classSymbol.GetTypeSyntax(false),
									SyntaxFactory.IdentifierName("d"))),
							SyntaxFactory.IdentifierName(callbackName)))
						.AddArgumentListArguments(
							SyntaxFactory.Argument(
								SyntaxFactory.CastExpression(
									type.GetTypeSyntax(isNullable),
									SyntaxFactory.MemberAccessExpression(
										SyntaxKind.SimpleMemberAccessExpression,
										SyntaxFactory.IdentifierName("e"),
										SyntaxFactory.IdentifierName("OldValue")))),
							SyntaxFactory.Argument(
								SyntaxFactory.CastExpression(
									type.GetTypeSyntax(isNullable),
									SyntaxFactory.MemberAccessExpression(
										SyntaxKind.SimpleMemberAccessExpression,
										SyntaxFactory.IdentifierName("e"),
										SyntaxFactory.IdentifierName("NewValue"))))));
		}

		private ObjectCreationExpressionSyntax EmitPMObjectCreationExpression(ExpressionSyntax defaultValueExpression)
		{
			// new PropertyMetadata(defaultValueExpression);
			return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("global::Microsoft.UI.Xaml.PropertyMetadata"))
				.AddArgumentListArguments(
					SyntaxFactory.Argument(defaultValueExpression));
		}

		private InvocationExpressionSyntax EmitDPRegisterInvocationExpression(string propertyName, ITypeSymbol type, ITypeSymbol className, ExpressionSyntax propertyMetadataExpression)
		{
			// DependencyProperty.Register(nameof(propertyName, type, typeof(className), propertyMetadataExpression);
			return SyntaxFactory.InvocationExpression(
				SyntaxFactory.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					SyntaxFactory.IdentifierName("global::Microsoft.UI.Xaml.DependencyProperty"),
					SyntaxFactory.IdentifierName("Register")))
						.AddArgumentListArguments(
							SyntaxFactory.Argument(SourceGeneratorHelper.NameOfExpression(SyntaxFactory.IdentifierName(propertyName))),
							SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(type.GetTypeSyntax(false))),
							SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(className.GetTypeSyntax(false))),
							SyntaxFactory.Argument(propertyMetadataExpression));
		}
	}
}
