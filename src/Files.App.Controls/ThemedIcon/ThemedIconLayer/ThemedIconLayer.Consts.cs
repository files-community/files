// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace Files.App.Controls
{
	// Template Parts
	[TemplatePart(Name = LayerCanvasPart, Type = typeof(Canvas))]
	[TemplatePart(Name = LayerPathPart, Type = typeof(Path))]
	// Icon Color Visual States
	[TemplateVisualState(Name = BaseStateName, GroupName = IconLayerColorStateGroupName)]
	[TemplateVisualState(Name = AltStateName, GroupName = IconLayerColorStateGroupName)]
	[TemplateVisualState(Name = AccentStateName, GroupName = IconLayerColorStateGroupName)]
	[TemplateVisualState(Name = AccentContrastStateName, GroupName = IconLayerColorStateGroupName)]
	// Non Accent Color States
	[TemplateVisualState(Name = CriticalStateName, GroupName = IconLayerColorStateGroupName)]
	[TemplateVisualState(Name = CautionStateName, GroupName = IconLayerColorStateGroupName)]
	[TemplateVisualState(Name = SuccessStateName, GroupName = IconLayerColorStateGroupName)]
	[TemplateVisualState(Name = NeutralStateName, GroupName = IconLayerColorStateGroupName)]
	[TemplateVisualState(Name = CustomColorStateName, GroupName = IconLayerColorStateGroupName)]
	// Non Accent Contrasting Color States
	[TemplateVisualState(Name = CriticalBGStateName, GroupName = IconLayerColorStateGroupName)]
	[TemplateVisualState(Name = CautionBGStateName, GroupName = IconLayerColorStateGroupName)]
	[TemplateVisualState(Name = SuccessBGStateName, GroupName = IconLayerColorStateGroupName)]
	[TemplateVisualState(Name = NeutralBGStateName, GroupName = IconLayerColorStateGroupName)]
	[TemplateVisualState(Name = CustomColorBGStateName, GroupName = IconLayerColorStateGroupName)]
	/// <summary>
	/// Displays a layer of <see cref="ThemedIcon"/> for <see cref="ThemedIconTypes.Layered"/> icon type.
	/// </summary>
	public partial class ThemedIconLayer
	{
		// Path Control Part
		internal const string LayerCanvasPart = "PART_LayerCanvas";
		internal const string LayerPathPart = "PART_LayerPath";

		// Visual State Group Names
		internal const string IconLayerColorStateGroupName = "IconLayerColorStates";

		// Icon Color Type Visual State Names
		internal const string BaseStateName = "Base";
		internal const string AltStateName = "Alt";
		internal const string AccentStateName = "Accent";
		internal const string AccentContrastStateName = "AccentContrast";

		internal const string CriticalStateName = "Critical";
		internal const string CautionStateName = "Caution";
		internal const string SuccessStateName = "Success";
		internal const string NeutralStateName = "Neutral";
		internal const string CustomColorStateName = "Custom";

		internal const string CriticalBGStateName = "CriticalBG";
		internal const string CautionBGStateName = "CautionBG";
		internal const string SuccessBGStateName = "SuccessBG";
		internal const string NeutralBGStateName = "NeutralBG";
		internal const string CustomColorBGStateName = "CustomBG";
	}
}
