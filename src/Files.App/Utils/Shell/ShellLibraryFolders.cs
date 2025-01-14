// Copyright (c) Files Community
// Licensed under the MIT License.

using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace Files.App.Utils.Shell
{
	/// <summary>
	/// Folders of a <see cref="ShellLibrary"/>.
	/// </summary>
	/// <seealso cref="ShellItemArray"/>
	/// <seealso cref="ICollection{ShellItem}"/>
	public sealed class ShellLibraryFolders : ShellItemArray, ICollection<ShellItem>
	{
		private Shell32.IShellLibrary _lib;

		/// <summary>
		/// Initializes a new instance of the <see cref="ShellLibraryFolders"/> class.
		/// </summary>
		/// <param name="lib">The library.</param>
		/// <param name="shellItemArray">The shell item array.</param>
		internal ShellLibraryFolders(Shell32.IShellLibrary lib, Shell32.IShellItemArray shellItemArray) : base(shellItemArray)
		{
			_lib = lib;
		}

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
		/// </summary>
		bool ICollection<ShellItem>.IsReadOnly
			=> false;

		/// <summary>
		/// Adds the specified location.
		/// </summary>
		/// <param name="location">The location.</param>
		/// <exception cref="ArgumentNullException">location</exception>
		public void Add(ShellItem location)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));

			_lib.AddFolder(location.IShellItem);
		}

		/// <summary>
		/// Removes the specified location.
		/// </summary>
		/// <param name="location">The location.</param>
		/// <returns><c>true</c> on success.</returns>
		/// <exception cref="ArgumentNullException">location</exception>
		public bool Remove(ShellItem location)
		{
			if (location is null)
				throw new ArgumentNullException(nameof(location));

			try
			{
				_lib.RemoveFolder(location.IShellItem);

				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Removes all items from the <see cref="ICollection{ShellItem}"/>.
		/// </summary>
		/// <exception cref="NotImplementedException"></exception>
		void ICollection<ShellItem>.Clear()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing,
		/// releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			_lib = null;

			base.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}
