// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.Core.SourceGenerator.Generators
{
	/// <summary>
	/// A generator for serializing/deserializing objects to/from the Windows Registry using attributes.
	/// </summary>
	[Generator]
	internal sealed class RegistrySerializationGenerator : IIncrementalGenerator
	{
		/// <summary>
		/// Initializes the generator with the provided context for incremental generation.
		/// </summary>
		/// <param name="context">The context for initializing the incremental generator.</param>
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			var valueProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
				"Files.Shared.Attributes.RegistrySerializableAttribute",
				(node, _) => node.IsKind(SyntaxKind.ClassDeclaration),
				(ctx, _) => (ITypeSymbol)ctx.TargetSymbol);

			context.RegisterSourceOutput(valueProvider, (ctx, symbol) =>
			{
				var queue = new Queue<ITypeSymbol>();
				var generatedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
				var diagnostics = new List<Diagnostic>();
				queue.Enqueue(symbol);

				var sb = new StringBuilder();

				_ = sb.AppendFullHeader();
				_ = sb.AppendLine();
				_ = sb.AppendLine("using System;");
				_ = sb.AppendLine("using Microsoft.Win32;");
				_ = sb.AppendLine();
				if (symbol.ContainingNamespace is { IsGlobalNamespace: false })
				{
					_ = sb.AppendLine($"namespace {symbol.ContainingNamespace};");
					_ = sb.AppendLine();
				}

				_ = sb.AppendLine($"public sealed class {symbol.Name}Registry");
				_ = sb.AppendLine("{");
				_ = sb.AppendLine();

				while (queue.Count > 0)
				{
					var type = queue.Dequeue();
					if (!generatedTypes.Add(type))
					{
						continue;
					}

					EmitBindValues(diagnostics, sb, queue, type);
					EmitSaveValues(diagnostics, sb, queue, type);
				}

				_ = sb.AppendLine("}");
				if (diagnostics.Count == 0)
				{
					ctx.AddSource($"{symbol.Name}Registry.g.cs", sb.ToString());
				}
				else
				{
					foreach (var diagnostic in diagnostics)
					{
						ctx.ReportDiagnostic(diagnostic);
					}
				}
			});
		}

		/// <summary>
		/// Emits the binding of registry values to properties of the specified target object.
		/// </summary>
		/// <param name="diagnostics">List to collect diagnostics.</param>
		/// <param name="sb">String builder for generating source code.</param>
		/// <param name="queue">Queue of type symbols for processing.</param>
		/// <param name="type">The type symbol representing the object type.</param>
		private void EmitBindValues(List<Diagnostic> diagnostics, StringBuilder sb, Queue<ITypeSymbol> queue, ITypeSymbol type)
		{
			_ = sb.AppendLine($"	internal  static void BindValues(RegistryKey key, {type.Name} target, string prefix = \"\")");
			_ = sb.AppendLine("	{");

			_ = sb.AppendLine(
				$$"""
						if (target is null)
						{
							return;
						}
				""");

			var properties = new Queue<(ImmutableArray<Location> Locations, ITypeSymbol Type, string Name, bool EmitNullBranch)>();
			foreach (var member in type.GetMembers())
			{
				if (member is IPropertySymbol { IsReadOnly: false } property
					&& !property.GetAttributes().Any(a => a.AttributeClass?.MetadataName == "RegistryIgnoreAttribute"))
				{
					properties.Enqueue((property.Locations, property.Type, property.Name, false));
				}
			}

			while (properties.Count > 0)
			{
				var (propertyLocation, propertyType, propertyName, emitNullBranch) = properties.Dequeue();

				switch (propertyType)
				{
					case { SpecialType: SpecialType.System_String }:
						_ = sb.AppendLine(
							$$"""
									if (key.GetValue($"{prefix}{{propertyName}}") is string valueOf{{propertyName}})
									{
										target.{{propertyName}} = valueOf{{propertyName}};
									}
							""");
						break;
					case { SpecialType: SpecialType.System_Boolean }:
						_ = sb.AppendLine(
							$$"""
									if (key.GetValue($"{prefix}{{propertyName}}") is int valueOf{{propertyName}})
									{
										target.{{propertyName}} = valueOf{{propertyName}} is not 0;
									}
							""");
						EmitNullBranch(emitNullBranch, propertyName);
						break;
					case
					{
						SpecialType: SpecialType.System_Byte or
										SpecialType.System_SByte or
										SpecialType.System_UInt16 or
										SpecialType.System_Int16 or
										SpecialType.System_UInt32 or
										SpecialType.System_Int32
					}:
						_ = sb.AppendLine(
							$$"""
									if (key.GetValue($"{prefix}{{propertyName}}") is int valueOf{{propertyName}})
									{
										target.{{propertyName}} = ({{propertyType}})valueOf{{propertyName}};
									}
							""");
						EmitNullBranch(emitNullBranch, propertyName);
						break;
					case
					{
						SpecialType: SpecialType.System_UInt64 or
										SpecialType.System_Int64
					}:
						_ = sb.AppendLine(
							$$"""
									if (key.GetValue($"{prefix}{{propertyName}}") is long valueOf{{propertyName}})
									{
										target.{{propertyName}} = ({{propertyType}})valueOf{{propertyName}};
									}
							""");
						EmitNullBranch(emitNullBranch, propertyName);
						break;
					case { SpecialType: SpecialType.System_Single }:
						_ = sb.AppendLine(
							$$"""
									if (key.GetValue($"{prefix}{{propertyName}}") is int valueOf{{propertyName}})
									{
										target.{{propertyName}} = BitConverter.Int32BitsToSingle(valueOf{{propertyName}});
									}
							""");
						EmitNullBranch(emitNullBranch, propertyName);
						break;
					case { SpecialType: SpecialType.System_Double }:
						_ = sb.AppendLine(
							$$"""
									if (key.GetValue($"{prefix}{{propertyName}}") is long valueOf{{propertyName}})
									{
										target.{{propertyName}} = BitConverter.Int64BitsToDouble(valueOf{{propertyName}});
									}
							""");
						EmitNullBranch(emitNullBranch, propertyName);
						break;
					case { TypeKind: TypeKind.Enum }:
						_ = sb.AppendLine(
							$$"""
									if (key.GetValue($"{prefix}{{propertyName}}") is string valueOf{{propertyName}})
									{
										target.{{propertyName}} = Enum.Parse<{{propertyType}}>(valueOf{{propertyName}});
									}
							""");
						EmitNullBranch(emitNullBranch, propertyName);
						break;
					case INamedTypeSymbol { TypeKind: TypeKind.Struct, NullableAnnotation: NullableAnnotation.Annotated, TypeArguments: [var underlyingType] }:
						properties.Enqueue((propertyLocation, underlyingType, propertyName, true));
						break;
					case IArrayTypeSymbol { TypeKind: TypeKind.Array, ElementType.SpecialType: SpecialType.System_String }:
						_ = sb.AppendLine(
							$$"""
									if (key.GetValue($"{prefix}{{propertyName}}") is string[] valueOf{{propertyName}})
									{
										target.{{propertyName}} = valueOf{{propertyName}};
									}
							""");
						break;
					case { TypeKind: TypeKind.Class or TypeKind.Struct, SpecialType: SpecialType.None }:
						_ = sb.AppendLine(
							$$"""
									BindValues(key, target.{{propertyName}}, $"{prefix}{{propertyName}}.");
							""");
						queue.Enqueue(propertyType);
						continue;
					default:
						if (!diagnostics.Any(d => d.Id == Constants.DiagnosticDescriptors.FSG1001.Id && d.Location.SourceSpan == propertyLocation[0].SourceSpan))
						{
							diagnostics.Add(Diagnostic.Create(Constants.DiagnosticDescriptors.FSG1001, propertyLocation[0], $"{propertyType}{(emitNullBranch ? "?" : "")}"));
						}

						break;
				}
			}

			_ = sb.AppendLine("	}");
			_ = sb.AppendLine();

			void EmitNullBranch(bool emitNullBranch, string propertyName)
			{
				if (emitNullBranch)
				{
					_ = sb.AppendLine(
						$$"""
								else
								{
									target.{{propertyName}} = null;
								}
						""");
				}
			}
		}

		/// <summary>
		/// Emits saving property values of the specified source object into the Windows Registry.
		/// </summary>
		/// <param name="diagnostics">List to collect diagnostics.</param>
		/// <param name="sb">String builder for generating source code.</param>
		/// <param name="queue">Queue of type symbols for processing.</param>
		/// <param name="type">The type symbol representing the object type.</param>
		private void EmitSaveValues(List<Diagnostic> diagnostics, StringBuilder sb, Queue<ITypeSymbol> queue, ITypeSymbol type)
		{
			_ = sb.AppendLine($"	internal  static void SaveValues(RegistryKey key, {type.Name} source, string prefix = \"\")");
			_ = sb.AppendLine("	{");

			_ = sb.AppendLine(
				$$"""
						if (source is null)
						{
							foreach (var name in key.GetValueNames())
							{
								if (name.StartsWith(prefix, StringComparison.Ordinal))
								{
									key.DeleteValue(name, false);
								}
							}

							return;
						}
				""");

			var properties = new Queue<(ImmutableArray<Location> Locations, ITypeSymbol Type, string Name, bool EmitNullBranch)>();
			foreach (var member in type.GetMembers())
			{
				if (member is IPropertySymbol { IsReadOnly: false } property
					&& !property.GetAttributes().Any(a => a.AttributeClass?.MetadataName == "RegistryIgnoreAttribute"))
				{
					properties.Enqueue((property.Locations, property.Type, property.Name, false));
				}
			}

			while (properties.Count > 0)
			{
				var (propertyLocation, propertyType, propertyName, emitNullBranch) = properties.Dequeue();

				switch (propertyType)
				{
					case { SpecialType: SpecialType.System_String }:
						_ = sb.AppendLine(
							$$"""
									key.SetValue($"{prefix}{{propertyName}}", source.{{propertyName}}, RegistryValueKind.String);
							""");
						break;
					case { SpecialType: SpecialType.System_Boolean }:
						EmitNullBranch(emitNullBranch, propertyName);
						_ = sb.AppendLine(
							$$"""
									{
										key.SetValue($"{prefix}{{propertyName}}", source.{{propertyName}} ? 1 : 0, RegistryValueKind.DWord);
									}
							""");
						break;
					case
					{
						SpecialType: SpecialType.System_Byte or
										SpecialType.System_SByte or
										SpecialType.System_UInt16 or
										SpecialType.System_Int16 or
										SpecialType.System_UInt32 or
										SpecialType.System_Int32
					}:
						EmitNullBranch(emitNullBranch, propertyName);
						_ = sb.AppendLine(
							$$"""
									{
										key.SetValue($"{prefix}{{propertyName}}", (int)source.{{propertyName}}, RegistryValueKind.DWord);
									}
							""");
						break;
					case
					{
						SpecialType: SpecialType.System_UInt64 or
										SpecialType.System_Int64
					}:
						EmitNullBranch(emitNullBranch, propertyName);
						_ = sb.AppendLine(
							$$"""
									{
										key.SetValue($"{prefix}{{propertyName}}", (long)source.{{propertyName}}, RegistryValueKind.QWord);
									}
							""");
						break;
					case { SpecialType: SpecialType.System_Single }:
						EmitNullBranch(emitNullBranch, propertyName);
						_ = sb.AppendLine(
							$$"""
									{
										key.SetValue($"{prefix}{{propertyName}}", BitConverter.SingleToInt32Bits(source.{{propertyName}}), RegistryValueKind.DWord);
									}
							""");
						break;
					case { SpecialType: SpecialType.System_Double }:
						EmitNullBranch(emitNullBranch, propertyName);
						_ = sb.AppendLine(
							$$"""
									{
										key.SetValue($"{prefix}{{propertyName}}", BitConverter.DoubleToInt64Bits(source.{{propertyName}}), RegistryValueKind.QWord);
									}
							""");
						break;
					case { TypeKind: TypeKind.Enum }:
						EmitNullBranch(emitNullBranch, propertyName);
						_ = sb.AppendLine(
							$$"""
									{
										key.SetValue($"{prefix}{{propertyName}}", source.{{propertyName}}.ToString(), RegistryValueKind.String);
									}
							""");
						break;
					case INamedTypeSymbol { TypeKind: TypeKind.Struct, NullableAnnotation: NullableAnnotation.Annotated, TypeArguments: [var underlyingType] }:
						properties.Enqueue((propertyLocation, underlyingType, propertyName, true));
						break;
					case IArrayTypeSymbol { TypeKind: TypeKind.Array, ElementType.SpecialType: SpecialType.System_String }:
						_ = sb.AppendLine(
							$$"""
									{
										key.SetValue($"{prefix}{{propertyName}}", source.{{propertyName}}, RegistryValueKind.MultiString);
									}
							""");
						break;
					case { TypeKind: TypeKind.Class or TypeKind.Struct, SpecialType: SpecialType.None }:
						_ = sb.AppendLine(
							$$"""
									SaveValues(key, source.{{propertyName}}, $"{prefix}{{propertyName}}.");
							""");
						queue.Enqueue(propertyType);
						continue;
					default:
						if (!diagnostics.Any(d => d.Id == Constants.DiagnosticDescriptors.FSG1001.Id && d.Location.SourceSpan == propertyLocation[0].SourceSpan))
						{
							diagnostics.Add(Diagnostic.Create(Constants.DiagnosticDescriptors.FSG1001, propertyLocation[0], $"{propertyType}{(emitNullBranch ? "?" : "")}"));
						}

						break;
				}
			}

			_ = sb.AppendLine("	}");
			_ = sb.AppendLine();

			void EmitNullBranch(bool emitNullBranch, string propertyName)
			{
				if (emitNullBranch)
				{
					_ = sb.AppendLine(
						$$"""
								if (source.{{propertyName}} is null)
								{
									key.DeleteValue($"{prefix}{{propertyName}}", false);
								}
								else
						""");
				}
			}
		}
	}
}
