// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.EventArguments
{
	internal sealed class PaneNavigationArguments
	{
		public string? LeftPaneNavPathParam { get; set; }

		public string? LeftPaneSelectItemParam { get; set; }

		public string? RightPaneNavPathParam { get; set; }

		public string? RightPaneSelectItemParam { get; set; }

		public ShellPaneArrangement ShellPaneArrangement { get; set; }

		public static bool operator ==(PaneNavigationArguments? a1, PaneNavigationArguments? a2)
		{
			if (a1 is null && a2 is null)
				return true;

			if (a1 is null || a2 is null)
				return false;

			return a1.LeftPaneNavPathParam == a2.LeftPaneNavPathParam &&
				a1.LeftPaneSelectItemParam == a2.LeftPaneSelectItemParam &&
				a1.RightPaneNavPathParam == a2.RightPaneNavPathParam &&
				a1.RightPaneSelectItemParam == a2.RightPaneSelectItemParam &&
				a1.ShellPaneArrangement == a2.ShellPaneArrangement;
		}

		public static bool operator !=(PaneNavigationArguments? a1, PaneNavigationArguments? a2)
		{
			return !(a1 == a2);
		}
	}
}
