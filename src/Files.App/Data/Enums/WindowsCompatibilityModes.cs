// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	[Flags]
	public enum WindowsCompatDpiOverrideKind
	{
		[Description("")]
		None = 0,
		[Description("HIGHDPIAWARE")]
		Application = 1,
		[Description("DPIUNAWARE")]
		System = 2,
		[Description("GDIDPISCALING")]
		Advanced = 4,
		[Description("GDIDPISCALING DPIUNAWARE")]
		SystemAdvanced = 6,
	}

	public enum WindowsCompatDPIOptionKind
	{
		[Description("")]
		None,
		[Description("PERPROCESSSYSTEMDPIFORCEOFF")]
		UseDPIOnLogin,
		[Description("PERPROCESSSYSTEMDPIFORCEON")]
		UseDPIOnProgramStart,
	}

	public enum WindowsCompatReducedColorModeKind
	{
		[Description("")]
		None,
		[Description("256COLOR")]
		Color8Bit,
		[Description("16BITCOLOR")]
		Color16Bit
	}

	public enum WindowsCompatModeKind
	{
		[Description("")]
		None,
		[Description("VISTARTM")]
		WindowsVista,
		[Description("VISTASP1")]
		WindowsVistaSP1,
		[Description("VISTASP2")]
		WindowsVistaSP2,
		[Description("WIN7RTM")]
		Windows7,
		[Description("WIN8RTM")]
		Windows8
	}
}
