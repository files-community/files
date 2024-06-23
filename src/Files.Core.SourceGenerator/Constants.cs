﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.Core.SourceGenerator
{
	internal class Constants
	{
		internal class DiagnosticDescriptors
		{
			internal static readonly DiagnosticDescriptor FSG1001 = new(nameof(FSG1001), "Types that are not supported by Windows Registry", "Type '{0}' is not supported by Windows Registry", "Design", DiagnosticSeverity.Error, true);
		}

		internal class DependencyPropertyGenerator
		{
			internal static readonly string AttributeName = "DependencyPropertyAttribute";
		}
	}
}
