// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	// TemplateParts
	[TemplatePart( Name = ThemedIconPartName			, Type = typeof( ThemedIcon ) )]
	[TemplatePart( Name = ContentPresenterPartName		, Type = typeof( ContentPresenter ) )]

	// VisualStates
	[TemplateVisualState( Name = NormalStateName		, GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = PointerOverStateName	, GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = PressedStateName		, GroupName = CommonStatesGroupName )]
	[TemplateVisualState( Name = DisabledStateName		, GroupName = CommonStatesGroupName )]

	[TemplateVisualState( Name = HasContentStateName	, GroupName = ContentStatesGroupName )]
	[TemplateVisualState( Name = HasNoContentStateName	, GroupName = ContentStatesGroupName )]
	public partial class ToolbarButton : Button, IToolbarItemSet
	{
		// TemplatePart Names
		internal const string ThemedIconPartName		= "PART_ThemedIcon";
		internal const string ContentPresenterPartName	= "PART_ContentPresenter";

		// VisualState Group Names
		internal const string CommonStatesGroupName		= "CommonStates";
		internal const string ContentStatesGroupName	= "ContentStates";

		// VisualState Names
		internal const string NormalStateName			= "Normal";
		internal const string PointerOverStateName		= "PointerOver";
		internal const string PressedStateName			= "Pressed";
		internal const string DisabledStateName			= "Disabled";

		internal const string HasContentStateName       = "HasContent";
		internal const string HasNoContentStateName     = "HasNoContent";
	}
}
