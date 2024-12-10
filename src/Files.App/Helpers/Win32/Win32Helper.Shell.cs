// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for Win32.
	/// </summary>
	public static partial class Win32Helper
	{
		private readonly static ShellFolder _controlPanel = new(Shell32.KNOWNFOLDERID.FOLDERID_ControlPanelFolder);

		private readonly static ShellFolder _controlPanelCategoryView = new("::{26EE0668-A00A-44D7-9371-BEB064C98683}");

		public static async Task<(ShellFileItem Folder, List<ShellFileItem> Enumerate)> GetShellFolderAsync(string path, bool getFolder, bool getEnumerate, int from, int count, params string[] properties)
		{
			if (path.StartsWith("::{", StringComparison.Ordinal))
			{
				path = $"shell:{path}";
			}

			return await Win32Helper.StartSTATask(() =>
			{
				var flc = new List<ShellFileItem>();
				var folder = (ShellFileItem)null;

				try
				{
					using var shellFolder = ShellFolderExtensions.GetShellItemFromPathOrPIDL(path) as ShellFolder;

					if (shellFolder is null ||
						(_controlPanel.PIDL.IsParentOf(shellFolder.PIDL, false) ||
						_controlPanelCategoryView.PIDL.IsParentOf(shellFolder.PIDL, false)) &&
						!shellFolder.Any())
					{
						// Return null to force open unsupported items in explorer
						// only if inside control panel and folder appears empty
						return (null, flc);
					}

					if (getFolder)
						folder = ShellFolderExtensions.GetShellFileItem(shellFolder);

					if (getEnumerate)
					{
						foreach (var folderItem in shellFolder.Skip(from).Take(count))
						{
							try
							{
								var shellFileItem = folderItem is ShellLink link ?
									ShellFolderExtensions.GetShellLinkItem(link) :
									ShellFolderExtensions.GetShellFileItem(folderItem);

								foreach (var prop in properties)
									shellFileItem.Properties[prop] = SafetyExtensions.IgnoreExceptions(() => folderItem.Properties[prop]);

								flc.Add(shellFileItem);
							}
							catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
							{
								// Happens if files are being deleted
							}
							finally
							{
								folderItem.Dispose();
							}
						}
					}
				}
				catch
				{
				}

				return (folder, flc);
			});
		}

		public static string GetFolderFromKnownFolderGUID(Guid guid)
		{
			nint pszPath;
			Win32PInvoke.SHGetKnownFolderPath(guid, 0, nint.Zero, out pszPath);
			string path = Marshal.PtrToStringUni(pszPath);
			Marshal.FreeCoTaskMem(pszPath);

			return path;
		}
	}
}
