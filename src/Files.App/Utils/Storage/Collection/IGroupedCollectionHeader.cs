// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Utils.Storage
{
	/// <summary>
	/// Represents an interface that is used to allow using x:Bind for the group header template.
	/// <br/>
	/// This is needed because x:Bind does not work with generic types, however it does work with interfaces.
	/// that are implemented by generic types.
	/// </summary>
	public interface IGroupedCollectionHeader
	{
		public GroupedHeaderViewModel Model { get; set; }
	}
}
