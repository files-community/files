// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers;

internal sealed class RegistryHelpers
{
	internal static string CombineKeys(string key1, string key2)
	{
		key1 = key1.Replace('/', '\\').Trim('\\');
		key2 = key2.Replace('/', '\\').Trim('\\');
		return $"{key1}\\{key2}";
	}

	internal static string CombineKeys(string key1, string key2, string key3)
	{
		key1 = key1.Replace('/', '\\').Trim('\\');
		key2 = key2.Replace('/', '\\').Trim('\\');
		key3 = key3.Replace('/', '\\').Trim('\\');
		return $"{key1}\\{key2}\\{key3}";
	}
}
