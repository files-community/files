// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared;

namespace Files.App.Data.Models
{
	/// <summary>
	/// Represents a model for AddItemDialog result.
	/// </summary>
	public sealed class AddItemDialogResultModel
	{
		/// <summary>
		/// Gets or sets item type that is added.
		/// </summary>
		public AddItemDialogItemType ItemType { get; set; }

		/// <summary>
		/// Gets or sets added item information.
		/// </summary>
		public ShellNewEntry? ItemInfo { get; set; }
	}
}
