// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides service to launch common dialog through Win32API.
	/// </summary>
	public interface ICommonDialogService
	{
		/// <summary>
		/// Opens a common dialog called FileOpenDialog through native Win32API.
		/// </summary>
		/// <param name="hWnd">The Window handle that the dialog launches based on.</param>
		/// <param name="pickFoldersOnly">The value that indicates whether the picker is only for folders.</param>
		/// <param name="filters">The extension filters that the dialog uses to exclude unnecessary files.<br/>The filter must have a pair:<code>[ "Application", ".exe" ]</code></param>
		/// <param name="filePath">The file that that user chose.</param>
		/// <remarks>
		/// NOTE: There's a WinRT API to launch this dialog, but the API doesn't support windows that are launched by those who is in Administrators group or has broader privileges.
		/// </remarks>
		/// <returns>True if the 'Open' button was clicked; otherwise, false.</returns>
		bool Open_FileOpenDialog(nint hWnd, bool pickFoldersOnly, string[] filters, Environment.SpecialFolder defaultFolder, out string filePath);

		/// <summary>
		/// Opens a common dialog called FileSaveDialog through native Win32API.
		/// </summary>
		/// <param name="hWnd">The Window handle that the dialog launches based on.</param>
		/// <param name="pickFoldersOnly">The value that indicates whether the picker is only for folders.</param>
		/// <param name="filters">The extension filters that the dialog uses to exclude unnecessary files.<br/>The filter must have a pair:<code>[ "Application", ".exe" ]</code></param>
		/// <param name="filePath">The file that that user chose.</param>
		/// <remarks>
		/// NOTE: There's a WinRT API to launch this dialog, but the API doesn't support windows that are launched by those who is in Administrators group or has broader privileges.
		/// </remarks>
		/// <returns>True if the 'Open' button was clicked; otherwise, false.</returns>
		bool Open_FileSaveDialog(nint hWnd, bool pickFoldersOnly, string[] filters, Environment.SpecialFolder defaultFolder, out string filePath);

		/// <summary>
		/// Opens a common dialog called NetworkConnectionDialog through native Win32API.
		/// </summary>
		/// <param name="hideRestoreConnectionCheckBox">The value indicating whether to hide the check box allowing the user to restore the connection at logon.</param>
		/// <param name="persistConnectionAtLogon">The value indicating whether restore the connection at logon.</param>
		/// <param name="readOnlyPath">The value indicating whether to display a read-only path instead of allowing the user to type in a path. This is only valid if <see cref="RemoteNetworkName"/> is not <see langword="null"/>.</param>
		/// <param name="remoteNetworkName">The name of the remote network.</param>
		/// <param name="useMostRecentPath">The value indicating whether to enter the most recently used paths into the combination box.</param>
		/// <returns>True if the 'OK' button was clicked; otherwise, false.</returns>
		bool Open_NetworkConnectionDialog(nint hWnd, bool hideRestoreConnectionCheckBox = false, bool persistConnectionAtLogon = false, bool readOnlyPath = false, string? remoteNetworkName = null, bool useMostRecentPath = false);
	}
}
