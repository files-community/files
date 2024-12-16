// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Controls
{
	public partial class ToolbarRadioButton : RadioButton, IToolbarItemSet
	{
		public ToolbarRadioButton()
		{
			DefaultStyleKey = typeof( ToolbarRadioButton );
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}