// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.UserControls.TabBar
{
	/// <summary>
	/// Represents content item for <see cref="TabBarItem"/>.
	/// </summary>
	public interface ITabBarItemContent
	{
		public bool IsCurrentInstance { get; set; }

		public TabBarItemParameter? TabBarItemParameter { get; }

		public event EventHandler<TabBarItemParameter> ContentChanged;

		public Task TabItemDragOver(object sender, DragEventArgs e);

		public Task TabItemDrop(object sender, DragEventArgs e);
	}
}
