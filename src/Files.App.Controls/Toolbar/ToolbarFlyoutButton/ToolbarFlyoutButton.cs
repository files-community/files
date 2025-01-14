// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class ToolbarFlyoutButton : DropDownButton, IToolbarItemSet
	{
		public ToolbarFlyoutButton()
		{
			this.DefaultStyleKey = typeof( ToolbarFlyoutButton );
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}
