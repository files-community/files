// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Files.App.Controls
{
	// TemplateParts
	[TemplatePart(Name = ContainerPartName, Type = typeof(Grid))]
	[TemplatePart(Name = ValueColumnPartName, Type = typeof(ColumnDefinition))]
	[TemplatePart(Name = GapColumnPartName, Type = typeof(ColumnDefinition))]
	[TemplatePart(Name = TrackColumnPartName, Type = typeof(ColumnDefinition))]
	[TemplatePart(Name = ValueBorderPartName, Type = typeof(Border))]
	[TemplatePart(Name = TrackBorderPartName, Type = typeof(Border))]
	// VisualStates
	[TemplateVisualState(Name = SafeStateName, GroupName = ControlStateGroupName)]
	[TemplateVisualState(Name = CautionStateName, GroupName = ControlStateGroupName)]
	[TemplateVisualState(Name = CriticalStateName, GroupName = ControlStateGroupName)]
	[TemplateVisualState(Name = DisabledStateName, GroupName = ControlStateGroupName)]
	public partial class StorageBar : RangeBase
	{
		internal const string ContainerPartName = "PART_Container";

		internal const string ValueColumnPartName = "PART_ValueColumn";
		internal const string GapColumnPartName = "PART_GapColumn";
		internal const string TrackColumnPartName = "PART_TrackColumn";

		internal const string ValueBorderPartName = "PART_ValueBar";
		internal const string TrackBorderPartName = "PART_TrackBar";

		internal const string ControlStateGroupName = "ControlStates";

		internal const string SafeStateName = "Safe";
		internal const string CautionStateName = "Caution";
		internal const string CriticalStateName = "Critical";
		internal const string DisabledStateName = "Disabled";
	}
}
