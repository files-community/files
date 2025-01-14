// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Text.RegularExpressions;

namespace Files.App.Helpers;

public static partial class RegexHelpers
{
	[GeneratedRegex(@"\w:\w")]
	public static partial Regex AlternateStream();
	
	[GeneratedRegex("(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)")]
	public static partial Regex SpaceSplit();
	
	[GeneratedRegex(@"^\\\\\?\\[^\\]*\\?")]
	public static partial Regex WindowsPath();
	
	[GeneratedRegex(@"^[A-Z]:\\\$Recycle\.Bin\\", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	public static partial Regex RecycleBinPath();
	
	[GeneratedRegex(@"^(?!/)(?!.*//)[^\000-\037\177 ~^:?*[]+(?!.*\.\.)(?!.*@\{)(?!.*\\)(?<!/\.)(?<!\.)(?<!/)(?<!\.lock)$")]
	public static partial Regex GitBranchName();
	
	[GeneratedRegex(@"\s+")]
	public static partial Regex WhitespaceAtLeastOnce();
	
	[GeneratedRegex(@"\s*\(\w:\)$")]
	public static partial Regex DriveLetter();
}