// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Core.Storage;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Represents an item that is tagged.
	/// </summary>
	/// <param name="TagUids">Tag UIDs that the item is tagged with.</param>
	/// <param name="Storable">The item that contains the tags.</param>
	public sealed record class TaggedItemModel(string[] TagUids, IStorable Storable);
}
