// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	public record BranchItem(string Name, bool IsHead, bool IsRemote, int? AheadBy, int? BehindBy);
}
