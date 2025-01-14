// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Forms;
using Vanara.Extensions;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Represents an encapsulated item for shell library.
	/// </summary>
	public sealed class ShellLibraryEx : ShellFolder
	{
		//private const string ext = ".library-ms";

		internal Shell32.IShellLibrary _lib;

		private ShellLibraryFolders _folders;

		private string _name;

		/// <summary>
		/// Initializes a new instance of the <see cref="ShellLibrary"/>Ex class.
		/// </summary>
		/// <param name="knownFolderId">The known folder identifier.</param>
		/// <param name="readOnly">If set to <c>true</c> [read only].</param>
		public ShellLibraryEx(Shell32.KNOWNFOLDERID knownFolderId, bool readOnly = false)
		{
			_lib = new Shell32.IShellLibrary();
			_lib.LoadLibraryFromKnownFolder(knownFolderId.Guid(), readOnly ? STGM.STGM_READ : STGM.STGM_READWRITE);

			Init(knownFolderId.GetIShellItem());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ShellLibrary"/>Ex class.
		/// </summary>
		/// <param name="libraryName">Name of the library.</param>
		/// <param name="kf">The known folder identifier.</param>
		/// <param name="overwrite">If set to <c>true</c> [overwrite].</param>
		public ShellLibraryEx(string libraryName, Shell32.KNOWNFOLDERID kf = Shell32.KNOWNFOLDERID.FOLDERID_Libraries, bool overwrite = false)
		{
			_lib = new Shell32.IShellLibrary();
			_name = libraryName;
			var item = _lib.SaveInKnownFolder(kf.Guid(), libraryName, overwrite ? Shell32.LIBRARYSAVEFLAGS.LSF_OVERRIDEEXISTING : Shell32.LIBRARYSAVEFLAGS.LSF_FAILIFTHERE);

			Init(item);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ShellLibrary"/>Ex class.
		/// </summary>
		/// <param name="libraryName">Name of the library.</param>
		/// <param name="parent">The parent.</param>
		/// <param name="overwrite">If set to <c>true</c> [overwrite].</param>
		public ShellLibraryEx(string libraryName, ShellFolder parent, bool overwrite = false)
		{
			_lib = new Shell32.IShellLibrary();
			_name = libraryName;
			var item = _lib.Save(parent.IShellItem, libraryName, overwrite ? Shell32.LIBRARYSAVEFLAGS.LSF_OVERRIDEEXISTING : Shell32.LIBRARYSAVEFLAGS.LSF_FAILIFTHERE);

			Init(item);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ShellLibrary"/> class.
		/// </summary>
		/// <param name="libraryItem">The library item.</param>
		/// <param name="readOnly">If set to <c>true</c> [read only].</param>
		public ShellLibraryEx(Shell32.IShellItem libraryItem, bool readOnly = false)
		{
			_lib = new Shell32.IShellLibrary();
			_lib.LoadLibraryFromItem(libraryItem, readOnly ? STGM.STGM_READ : STGM.STGM_READWRITE);

			Init(libraryItem);
		}

		/// <summary>
		/// Gets or sets the default target folder the library uses for save operations.
		/// </summary>
		/// <value>The default save folder.</value>
		public ShellItem DefaultSaveFolder
		{
			get => Open(_lib.GetDefaultSaveFolder<Shell32.IShellItem>(Shell32.DEFAULTSAVEFOLDERTYPE.DSFT_DETECT));
			set => _lib.SetDefaultSaveFolder(Shell32.DEFAULTSAVEFOLDERTYPE.DSFT_DETECT, value.IShellItem);
		}

		/// <summary>Gets the set of child folders that are contained in the library.</summary>
		/// <value>A <see cref="ShellItemArray"/> containing the child folders.</value>
		public ShellLibraryFolders Folders
			=> _folders ??= GetFilteredFolders();

		/// <summary>
		/// Gets or sets a string that describes the location of the default icon.
		/// The string must be formatted as
		/// <c>ModuleFileName,ResourceIndex or ModuleFileName,-ResourceID</c>.
		/// </summary>
		/// <value>
		/// The default icon location.
		/// </value>
		public IconLocation IconLocation
		{
			get
			{
				_ = IconLocation.TryParse(_lib.GetIcon(), out var l);
				return l;
			}
			set => _lib.SetIcon(value.ToString());
		}

		/// <summary>
		/// Gets the name relative to the parent for the item.
		/// </summary>
		public override string Name
		{
			get => _name;
			protected set => _name = value;
		}

		/// <summary>
		/// Gets or sets a value indicating whether to pin the library to the navigation pane.
		/// </summary>
		/// <value>
		/// <c>true</c> if pinned to the navigation pane; otherwise, <c>false</c>.
		/// </value>
		public bool PinnedToNavigationPane
		{
			get => _lib.GetOptions().IsFlagSet(Shell32.LIBRARYOPTIONFLAGS.LOF_PINNEDTONAVPANE);
			set => _lib.SetOptions(Shell32.LIBRARYOPTIONFLAGS.LOF_PINNEDTONAVPANE, value ? Shell32.LIBRARYOPTIONFLAGS.LOF_PINNEDTONAVPANE : 0);
		}

		/// <summary>
		/// Gets or sets the library's View Template.
		/// </summary>
		/// <value>
		/// The View Template.
		/// </value>
		public LibraryViewTemplate ViewTemplate
		{
			get => (LibraryViewTemplate)ShlGuidExt.Lookup<Shell32.FOLDERTYPEID>(ViewTemplateId);
			set
			{
				if (value != LibraryViewTemplate.Custom)
					ViewTemplateId = ((Shell32.FOLDERTYPEID)value).Guid();
			}
		}

		/// <summary>
		/// Gets or sets the library's View Template identifier.
		/// </summary>
		/// <value>
		/// The View Template identifier.
		/// </value>
		public Guid ViewTemplateId
		{
			get => _lib.GetFolderType();
			set => _lib.SetFolderType(value);
		}

		/// <summary>
		/// Reload library folders.
		/// </summary>
		public void Reload()
		{
			_folders = GetFilteredFolders();
		}

		/// <summary>
		/// Commits library updates.
		/// </summary>
		public void Commit()
		{
			_lib.Commit();
		}

		/// <summary>
		/// Gets the set of child folders that are contained in the library.
		/// </summary>
		/// <param name="filter">A value that determines the folders to get.</param>
		/// <returns>A <see cref="ShellItemArray"/> containing the child folders.</returns>
		public ShellLibraryFolders GetFilteredFolders(LibraryFolderFilter filter = LibraryFolderFilter.AllItems)
		{
			return new(_lib, _lib.GetFolders<Shell32.IShellItemArray>((Shell32.LIBRARYFOLDERFILTER)filter));
		}

		/// <summary>
		/// Resolves the target location of a library folder, even if the folder has been moved or renamed.
		/// </summary>
		/// <param name="item">A ShellItem object that represents the library folder to locate.</param>
		/// <param name="timeout">
		/// The maximum time the method will attempt to locate the folder before returning. If the folder could not be located before the
		/// specified time elapses, an error is returned.
		/// </param>
		/// <returns>The resulting target location.</returns>
		public ShellItem ResolveFolder(ShellItem item, TimeSpan timeout)
		{
			return Open(_lib.ResolveFolder<Shell32.IShellItem>(item.IShellItem, Convert.ToUInt32(timeout.TotalMilliseconds)));
		}

		/// <summary>
		/// Shows the library management dialog box, which enables users to manage the library folders and default save location.
		/// </summary>
		/// <param name="parentWindow">
		/// The handle for the window that owns the library management dialog box. The value of this parameter can be NULL.
		/// </param>
		/// <param name="title">
		/// The title for the library management dialog. To display the generic title string, set the value of this parameter to NULL.
		/// </param>
		/// <param name="instruction">
		/// The help string to display below the title string in the library management dialog box. To display the generic help string, set
		/// the value of this parameter to NULL.
		/// </param>
		/// <param name="allowUnindexableLocations">
		/// If set to <c>true</c> do not display a warning dialog to the user in collisions that concern network locations that cannot be indexed.
		/// </param>
		public void ShowLibraryManagementDialog(IWin32Window parentWindow = null, string title = null, string instruction = null, bool allowUnindexableLocations = false)
		{
			Shell32.SHShowManageLibraryUI(
				IShellItem,
				parentWindow?.Handle ?? IntPtr.Zero,
				title, instruction,
				allowUnindexableLocations ? Shell32.LIBRARYMANAGEDIALOGOPTIONS.LMD_ALLOWUNINDEXABLENETWORKLOCATIONS : Shell32.LIBRARYMANAGEDIALOGOPTIONS.LMD_DEFAULT
			).ThrowIfFailed();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			_lib = null;

			_folders?.Dispose();
			_folders = null;

			base.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
