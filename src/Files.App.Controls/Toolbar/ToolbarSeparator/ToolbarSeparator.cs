// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class ToolbarSeparator : Control , IToolbarItemSet
	{
		public ToolbarSeparator()
		{
			DefaultStyleKey = typeof(ToolbarSeparator);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}
