// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.NetworkManagement.WNet;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;

namespace Files.App.Services
{
	/// <inheritdoc cref="ICommonDialogService"/>
	public sealed class CommonDialogService : ICommonDialogService
	{
		/// <inheritdoc/>
		public unsafe bool Open_FileOpenDialog(nint hWnd, bool pickFoldersOnly, string[] filters, Environment.SpecialFolder defaultFolder, out string filePath)
		{
			filePath = string.Empty;

			try
			{
				using ComPtr<IFileOpenDialog> pDialog = default;
				var dialogInstanceIid = typeof(FileOpenDialog).GUID;
				var dialogIid = typeof(IFileOpenDialog).GUID;

				// Get a new instance of the dialog
				HRESULT hr = PInvoke.CoCreateInstance(
					&dialogInstanceIid,
					null,
					CLSCTX.CLSCTX_INPROC_SERVER,
					&dialogIid,
					(void**)pDialog.GetAddressOf())
				.ThrowOnFailure();

				if (filters.Length is not 0 && filters.Length % 2 is 0)
				{
					List<COMDLG_FILTERSPEC> extensions = [];

					for (int i = 1; i < filters.Length; i += 2)
					{
						COMDLG_FILTERSPEC extension;

						extension.pszSpec = (char*)Marshal.StringToHGlobalUni(filters[i]);
						extension.pszName = (char*)Marshal.StringToHGlobalUni(filters[i - 1]);

						// Add to the exclusive extension list
						extensions.Add(extension);
					}

					// Set the file type using the extension list
					pDialog.Get()->SetFileTypes(extensions.ToArray());
				}

				// Get the default shell folder (My Computer)
				using ComPtr<IShellItem> pDefaultFolderShellItem = default;
				var shellItemIid = typeof(IShellItem).GUID;
				fixed (char* pszDefaultFolderPath = Environment.GetFolderPath(defaultFolder))
				{
					hr = PInvoke.SHCreateItemFromParsingName(
						pszDefaultFolderPath,
						null,
						&shellItemIid,
						(void**)pDefaultFolderShellItem.GetAddressOf())
					.ThrowOnFailure();
				}

				// Folder picker
				if (pickFoldersOnly)
					pDialog.Get()->SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

				// Set the default folder to open in the dialog
				pDialog.Get()->SetFolder(pDefaultFolderShellItem.Get());
				pDialog.Get()->SetDefaultFolder(pDefaultFolderShellItem.Get());

				// Show the dialog
				pDialog.Get()->Show(new HWND(hWnd));

				// Get the file that user chose
				using ComPtr<IShellItem> pResultShellItem = default;
				pDialog.Get()->GetResult(pResultShellItem.GetAddressOf());
				if (pResultShellItem.Get() == null)
					throw new COMException("FileSaveDialog returned invalid shell item.");
				pResultShellItem.Get()->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var lpFilePath);
				filePath = lpFilePath.ToString();

				return true;
			}
			catch (Exception ex)
			{
				App.Logger.LogError(ex, "Failed to open FileOpenDialog.");

				return false;
			}
		}

		/// <inheritdoc/>
		public unsafe bool Open_FileSaveDialog(nint hWnd, bool pickFoldersOnly, string[] filters, Environment.SpecialFolder defaultFolder, out string filePath)
		{
			filePath = string.Empty;

			try
			{
				using ComPtr<IFileSaveDialog> pDialog = default;
				var dialogInstanceIid = typeof(FileSaveDialog).GUID;
				var dialogIid = typeof(IFileSaveDialog).GUID;

				// Get a new instance of the dialog
				HRESULT hr = PInvoke.CoCreateInstance(
					&dialogInstanceIid,
					null,
					CLSCTX.CLSCTX_INPROC_SERVER,
					&dialogIid,
					(void**)pDialog.GetAddressOf())
				.ThrowOnFailure();

				if (filters.Length is not 0 && filters.Length % 2 is 0)
				{
					List<COMDLG_FILTERSPEC> extensions = [];

					for (int i = 1; i < filters.Length; i += 2)
					{
						COMDLG_FILTERSPEC extension;

						extension.pszSpec = (char*)Marshal.StringToHGlobalUni(filters[i]);
						extension.pszName = (char*)Marshal.StringToHGlobalUni(filters[i - 1]);

						// Add to the exclusive extension list
						extensions.Add(extension);
					}

					// Set the file type using the extension list
					pDialog.Get()->SetFileTypes(extensions.ToArray());
				}

				// Get the default shell folder (My Computer)
				using ComPtr<IShellItem> pDefaultFolderShellItem = default;
				var shellItemIid = typeof(IShellItem).GUID;
				fixed (char* pszDefaultFolderPath = Environment.GetFolderPath(defaultFolder))
				{
					hr = PInvoke.SHCreateItemFromParsingName(
						pszDefaultFolderPath,
						null,
						&shellItemIid,
						(void**)pDefaultFolderShellItem.GetAddressOf())
					.ThrowOnFailure();
				}

				// Folder picker
				if (pickFoldersOnly)
					pDialog.Get()->SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

				// Set the default folder to open in the dialog
				pDialog.Get()->SetFolder(pDefaultFolderShellItem.Get());
				pDialog.Get()->SetDefaultFolder(pDefaultFolderShellItem.Get());

				// Show the dialog
				pDialog.Get()->Show(new HWND(hWnd));

				// Get the file that user chose
				using ComPtr<IShellItem> pResultShellItem = default;
				pDialog.Get()->GetResult(pResultShellItem.GetAddressOf());
				if (pResultShellItem.Get() == null)
					throw new COMException("FileSaveDialog returned invalid shell item.");
				pResultShellItem.Get()->GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var lpFilePath);
				filePath = lpFilePath.ToString();

				return true;
			}
			catch (Exception ex)
			{
				App.Logger.LogError(ex, "Failed to open FileSaveDialog.");

				return false;
			}
		}

		/// <inheritdoc/>
		public unsafe bool Open_NetworkConnectionDialog(nint hWnd, bool hideRestoreConnectionCheckBox = false, bool persistConnectionAtLogon = false, bool readOnlyPath = false, string? remoteNetworkName = null, bool useMostRecentPath = false)
		{
			if (useMostRecentPath && !string.IsNullOrEmpty(remoteNetworkName))
				throw new ArgumentException($"{nameof(useMostRecentPath)} cannot be set to true if {nameof(remoteNetworkName)} has a value.");

			NETRESOURCEW netResource = default;
			CONNECTDLGSTRUCTW connectDlgOptions = default;
			WIN32_ERROR res = default;

			if (hideRestoreConnectionCheckBox)
				connectDlgOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_HIDE_BOX;
			if (persistConnectionAtLogon)
				connectDlgOptions.dwFlags |= (CONNECTDLGSTRUCT_FLAGS.CONNDLG_PERSIST & CONNECTDLGSTRUCT_FLAGS.CONNDLG_NOT_PERSIST);
			if (useMostRecentPath)
				connectDlgOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_USE_MRU;
			if (readOnlyPath && !string.IsNullOrEmpty(remoteNetworkName))
				connectDlgOptions.dwFlags |= CONNECTDLGSTRUCT_FLAGS.CONNDLG_RO_PATH;

			fixed (char* pszRemoteName = remoteNetworkName)
			{
				netResource.dwType = NET_RESOURCE_TYPE.RESOURCETYPE_DISK;
				netResource.lpRemoteName = pszRemoteName;

				connectDlgOptions.cbStructure = (uint)sizeof(CONNECTDLGSTRUCTW);
				connectDlgOptions.hwndOwner = new(hWnd);
				connectDlgOptions.lpConnRes = &netResource;

				res = PInvoke.WNetConnectionDialog1W(ref connectDlgOptions);
			}

			// User canceled
			if ((uint)res == unchecked((uint)-1))
				return false;

			// Unexpected error happened
			if (res is not WIN32_ERROR.NO_ERROR)
				throw new Win32Exception("Failed to process the network connection dialog successfully.");

			return true;
		}
	}
}
