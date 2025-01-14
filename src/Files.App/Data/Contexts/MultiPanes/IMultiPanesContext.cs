// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contexts
{
	/// <summary>
	/// Represents context for <see cref="ShellPanesPage"/>, which manages multiple panes.
	/// </summary>
	public interface IMultiPanesContext
	{
		/// <summary>
		/// Gets invoked when active pane is changing.
		/// </summary>
		event EventHandler? ActivePaneChanging;

		/// <summary>
		/// Gets invoked when active pane is changed.
		/// </summary>
		event EventHandler? ActivePaneChanged;

		/// <summary>
		/// Invoked when shell pane arrangement is changed.
		/// </summary>
		event EventHandler? ShellPaneArrangementChanged;

		/// <summary>
		/// Gets active pane.
		/// </summary>
		IShellPage? ActivePane { get; }

		/// <summary>
		/// Gets active pane or column.
		/// </summary>
		IShellPage? ActivePaneOrColumn { get; }

		/// <summary>
		/// Gets current shell pane arrangement.
		/// </summary>
		ShellPaneArrangement ShellPaneArrangement { get; }
	}
}
