﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents item for Widget card, shown inner Widget control and displayed as a list.
	/// </summary>
	public abstract class WidgetCardItem : ObservableObject
	{
		/// <summary>
		/// Gets or sets the path that indicates folder or file location.
		/// </summary>
		public virtual string? Path { get; set; }

		/// <summary>
		/// Gets or sets the object of the card.
		/// </summary>
		public virtual object? Item { get; set; }
	}
}
