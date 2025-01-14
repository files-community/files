// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	// TemplateParts
	[TemplatePart( Name = ToolbarItemsRepeaterPartName		, Type = typeof( ItemsRepeater ) )]

	// VisualStates
	[TemplateVisualState( Name = OverflowOnStateName		, GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = OverflowOffStateName		, GroupName = CommonStatesGroupName )]
	public partial class Toolbar : Control
	{
		// TemplatePart Names
		internal const string ToolbarItemsRepeaterPartName	= "PART_ItemsRepeater";

		// VisualState Group Names
		internal const string CommonStatesGroupName			= "OverflowStates";

		// VisualState Names
		internal const string OverflowOnStateName			= "OverflowOn";
		internal const string OverflowOffStateName			= "OverflowOff";
		// ResourceDictionary Keys
		internal const string SmallMinWidthResourceKey		= "ToolbarButtonSmallMinWidth";
		internal const string SmallMinHeightResourceKey		= "ToolbarButtonSmallMinHeight";

		internal const string MediumMinWidthResourceKey		= "ToolbarButtonMediumMinWidth";
		internal const string MediumMinHeightResourceKey	= "ToolbarButtonMediumMinHeight";

		internal const string LargeMinWidthResourceKey		= "ToolbarButtonLargeMinWidth";
		internal const string LargeMinHeightResourceKey		= "ToolbarButtonLargeMinHeight";
	}
}
