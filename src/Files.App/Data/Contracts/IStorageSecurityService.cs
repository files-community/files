// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32.Foundation;

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides service to manage storage security objects on NTFS and ReFS.
	/// </summary>
	public interface IStorageSecurityService
	{
		/// <summary>
		/// Get the owner of the object specified by the path.
		/// </summary>
		/// <param name="path">The file full path</param>
		/// <returns>The SID string of the owner</returns>
		string GetOwner(string path);

		/// <summary>
		/// Set the owner of the object specified by the path.
		/// </summary>
		/// <param name="path">The file full path</param>
		/// <param name="sid">The owner security identifier (SID)</param>
		/// <returns></returns>
		bool SetOwner(string path, string sid);

		/// <summary>
		/// Get information about an access control list (ACL).
		/// </summary>
		/// <param name="path"></param>
		/// <param name="isFolder"></param>
		/// <returns>If the function succeeds, an instance of AccessControlList; otherwise, null. To get extended error information, call GetLastError.</returns>
		WIN32_ERROR GetAcl(string path, bool isFolder, out AccessControlList acl);

		/// <summary>
		/// Add an default Access Control Entry (ACE) to the specified object's DACL
		/// </summary>
		/// <param name="path">The object's path to add an new ACE to its DACL</param>
		/// <param name="sid">Principal's SID</param>
		/// <returns> If the function succeeds, the return value is ERROR_SUCCESS. If the function fails, the return value is a nonzero error code defined in WinError.h.</returns>
		WIN32_ERROR AddAce(string szPath, bool isFolder, string szSid);

		/// <summary>
		/// Add an Access Control Entry (ACE) from the specified object's DACL
		/// </summary>
		/// <param name="szPath">The object's path to remove an ACE from its DACL</param>
		/// <param name="dwAceIndex"></param>
		/// <returns></returns>
		WIN32_ERROR DeleteAce(string szPath, uint dwAceIndex);
	}
}
