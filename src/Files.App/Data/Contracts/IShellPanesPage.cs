// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Represents interface of <see cref="ShellPanesPage"/>.
	/// </summary>
	public interface IShellPanesPage : IDisposable, INotifyPropertyChanged
	{
		public bool IsLeftPaneActive { get; }

		public bool IsRightPaneActive { get; }

		public bool IsMultiPaneActive { get; }

		public bool IsMultiPaneAvailable { get; }

		// TODO: Remove this our of this class
		public IFilesystemHelpers? FilesystemHelpers { get; }

		// TODO: Remove this our of this class
		public TabBarItemParameter? TabBarItemParameter { get; set; }

		/// <summary>
		/// Gets current shell pane arrangement.
		/// </summary>
		public ShellPaneArrangement ShellPaneArrangement { get; }

		/// <summary>
		/// Gets the current focused shell pane.
		/// </summary>
		public IShellPage? ActivePane { get; }

		/// <summary>
		/// Gets the current focused shell pane, or the last column when the focused shell pane layout type is <see cref="ColumnsLayoutPage"/>.
		/// </summary>
		public IShellPage? ActivePaneOrColumn { get; }

		/// <summary>
		/// Adds a new pane with path and pane addition direction if needed.
		/// </summary>
		/// <param name="path">The path to open in the new pane.</param>
		public void OpenSecondaryPane(string path = "", ShellPaneArrangement arrangement = ShellPaneArrangement.None);

		/// <summary>
		/// Arranges panes with the specified arrangement.
		/// </summary>
		/// <param name="arrangement">The arrangement to set.</param>
		public void ArrangePanes(ShellPaneArrangement arrangement = ShellPaneArrangement.None);

		/// <summary>
		/// Closes the shell active/focused pane.
		/// </summary>
		public void CloseActivePane();

		/// <summary>
		/// Focuses the other pane.
		/// </summary>
		public void FocusOtherPane();
	}
}
