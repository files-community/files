// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Represents options for PowerShell execution.
	/// </summary>
	[Flags]
	public enum PowerShellExecutionOptions
	{
		/// <summary>
		/// Default options.
		/// </summary>
		None = 0x0,

		/// <summary>
		/// Run the PowerShell command hidden.
		/// </summary>
		Hidden = 0x1,

		/// <summary>
		/// Run PowerShell with elevated privileges.
		/// </summary>
		Elevated = 0x2
	}
}