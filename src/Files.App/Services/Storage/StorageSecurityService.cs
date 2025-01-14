// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authorization;
using Windows.Win32.System.Memory;
using SystemSecurity = System.Security.AccessControl;

namespace Files.App.Services
{
	/// <inheritdoc cref="IStorageSecurityService"/>
	public class StorageSecurityService : IStorageSecurityService
	{
		/// <inheritdoc/>
		public unsafe string GetOwner(string path)
		{
			PInvoke.GetNamedSecurityInfo(
				path,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				OBJECT_SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION,
				out var pSidOwner,
				out _,
				null,
				null,
				out _);

			PInvoke.ConvertSidToStringSid(pSidOwner, out var sid);

			return sid.ToString();
		}

		/// <inheritdoc/>
		public unsafe bool SetOwner(string path, string sid)
		{
			PSID pSid = default;

			// Get SID
			fixed (char* cSid = sid)
				PInvoke.ConvertStringSidToSid(new PCWSTR(cSid), &pSid);

			WIN32_ERROR result = default;

			fixed (char* cPath = path)
			{
				// Change owner
				result = PInvoke.SetNamedSecurityInfo(
					new PWSTR(cPath),
					SE_OBJECT_TYPE.SE_FILE_OBJECT,
					OBJECT_SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION,
					pSid,
					new PSID((void*)0));
			}

			// Run PowerShell as Admin
			if (result is not WIN32_ERROR.ERROR_SUCCESS)
			{
				return Win32Helper.RunPowershellCommand(
					$"-command \"try {{ $path = '{path}'; $ID = new-object System.Security.Principal.SecurityIdentifier('{sid}'); $acl = get-acl $path; $acl.SetOwner($ID); set-acl -path $path -aclObject $acl }} catch {{ exit 1; }}\"",
					PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
			}

			return true;
		}

		/// <inheritdoc/>
		public unsafe WIN32_ERROR GetAcl(string path, bool isFolder, out AccessControlList acl)
		{
			acl = new();
			ACL* pDACL = default;

			// Get DACL
			var result = PInvoke.GetNamedSecurityInfo(
				path,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				&pDACL,
				null,
				out _);

			if (result is not WIN32_ERROR.ERROR_SUCCESS || pDACL == null)
				return result;

			ACL_SIZE_INFORMATION aclSizeInfo = default;

			// Get ACL size info
			bool bResult = PInvoke.GetAclInformation(
				*pDACL,
				&aclSizeInfo,
				(uint)Marshal.SizeOf<ACL_SIZE_INFORMATION>(),
				ACL_INFORMATION_CLASS.AclSizeInformation);

			if (!bResult)
				return (WIN32_ERROR)Marshal.GetLastPInvokeError();

			// Get owner
			var szOwnerSid = GetOwner(path);
			var principal = new AccessControlPrincipal(szOwnerSid);

			var isValidAcl = PInvoke.IsValidAcl(pDACL);

			List<AccessControlEntry> aces = [];

			// Get ACEs
			for (uint i = 0; i < aclSizeInfo.AceCount; i++)
			{
				bResult = PInvoke.GetAce(*pDACL, i, out var pAce);
				if (!bResult)
					return (WIN32_ERROR)Marshal.GetLastPInvokeError();

				if (pAce is null)
					continue;

				var ace = Marshal.PtrToStructure<ACCESS_ALLOWED_ACE>((nint)pAce);

				PWSTR pszSid = default;

				var offset = Marshal.SizeOf(typeof(ACE_HEADER)) + sizeof(uint);

				//if (pAce.IsObjectAce())
				//	offset += sizeof(uint) + Marshal.SizeOf(typeof(Guid)) * 2;

				nint pAcePtr = new((long)pAce + offset);
				PInvoke.ConvertSidToStringSid((PSID)pAcePtr, &pszSid);

				AccessControlEntryType type;
				AccessControlEntryFlags inheritanceFlags = AccessControlEntryFlags.None;
				AccessMaskFlags accessMaskFlags = (AccessMaskFlags)ace.Mask;

				var header = ace.Header;
				type = (SystemSecurity.AceType)header.AceType switch
				{
					SystemSecurity.AceType.AccessAllowed => AccessControlEntryType.Allow,
					_ => AccessControlEntryType.Deny
				};

				var flags = (SystemSecurity.AceFlags)header.AceFlags;

				bool isInherited = flags.HasFlag(SystemSecurity.AceFlags.InheritanceFlags);

				if (flags.HasFlag(SystemSecurity.AceFlags.ContainerInherit))
					inheritanceFlags |= AccessControlEntryFlags.ContainerInherit;
				if (flags.HasFlag(SystemSecurity.AceFlags.ObjectInherit))
					inheritanceFlags |= AccessControlEntryFlags.ObjectInherit;
				if (flags.HasFlag(SystemSecurity.AceFlags.NoPropagateInherit))
					inheritanceFlags |= AccessControlEntryFlags.NoPropagateInherit;
				if (flags.HasFlag(SystemSecurity.AceFlags.InheritOnly))
					inheritanceFlags |= AccessControlEntryFlags.InheritOnly;

				// Initialize an ACE
				aces.Add(new(isFolder, pszSid.ToString(), type, accessMaskFlags, isInherited, inheritanceFlags));
			}

			// Initialize with proper data
			acl = new AccessControlList(path, isFolder, principal, isValidAcl);

			// Set access control entries
			foreach (var ace in aces)
				acl.AccessControlEntries.Add(ace);

			return (WIN32_ERROR)Marshal.GetLastPInvokeError();
		}

		/// <inheritdoc/>
		public unsafe WIN32_ERROR AddAce(string szPath, bool isFolder, string szSid)
		{
			ACL* pDACL = default;
			ACL* pNewDACL = default;
			PSID pSid = default;
			ACL_SIZE_INFORMATION aclSizeInfo = default;
			ACCESS_ALLOWED_ACE* pTempAce = default;

			// Get DACL for the specified object
			var result = PInvoke.GetNamedSecurityInfo(
				szPath,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				&pDACL,
				null,
				out _);

			if (result is not WIN32_ERROR.ERROR_SUCCESS)
				return result;

			// Get ACL size info
			bool bResult = PInvoke.GetAclInformation(
				pDACL,
				&aclSizeInfo,
				(uint)Marshal.SizeOf<ACL_SIZE_INFORMATION>(),
				ACL_INFORMATION_CLASS.AclSizeInformation);

			if (!bResult)
				return (WIN32_ERROR)Marshal.GetLastPInvokeError();

			var cbNewDACL = aclSizeInfo.AclBytesInUse + aclSizeInfo.AclBytesFree + Marshal.SizeOf<ACCESS_ALLOWED_ACE>() + 1024;

			pNewDACL = (ACL*)PInvoke.LocalAlloc(LOCAL_ALLOC_FLAGS.LPTR, (nuint)cbNewDACL);
			if (pNewDACL == default)
				return (WIN32_ERROR)Marshal.GetLastPInvokeError();

			// Initialize the new DACL
			PInvoke.InitializeAcl(pNewDACL, (uint)cbNewDACL, ACE_REVISION.ACL_REVISION);

			// Copy ACEs from the old DACL
			for (uint dwAceIndex = 0u; dwAceIndex < aclSizeInfo.AceCount; dwAceIndex++)
			{
				bResult = PInvoke.GetAce(pDACL, dwAceIndex, (void**)&pTempAce);
				PInvoke.AddAce(pNewDACL, ACE_REVISION.ACL_REVISION, uint.MaxValue, pTempAce, pTempAce->Header.AceSize);
			}

			// Get the principal's SID of the new ACE
			fixed (char* cSid = szSid)
				PInvoke.ConvertStringSidToSid(new PCWSTR(cSid), &pSid);

			bResult = PInvoke.AddAccessAllowedAceEx(
				pNewDACL,
				ACE_REVISION.ACL_REVISION,
				isFolder ? ACE_FLAGS.CONTAINER_INHERIT_ACE | ACE_FLAGS.OBJECT_INHERIT_ACE : ACE_FLAGS.NO_INHERITANCE,
				0x20000000 | 0x80000000 /* GENERIC_EXECUTE and GENERIC_READ */,
				pSid);

			if (!bResult)
				return (WIN32_ERROR)Marshal.GetLastPInvokeError();

			fixed (char* cPath = szPath)
			{
				// Set the new ACL
				result = PInvoke.SetNamedSecurityInfo(
					new PWSTR(cPath),
					SE_OBJECT_TYPE.SE_FILE_OBJECT,
					OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
					new PSID((void*)0),
					new PSID((void*)0),
					pNewDACL);
			}

			if (result is not WIN32_ERROR.ERROR_SUCCESS)
				return result;

			return result;
		}

		/// <inheritdoc/>
		public unsafe WIN32_ERROR DeleteAce(string szPath, uint dwAceIndex)
		{
			ACL* pDACL = default;

			// Get DACL for the specified object
			var result = PInvoke.GetNamedSecurityInfo(
				szPath,
				SE_OBJECT_TYPE.SE_FILE_OBJECT,
				OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
				out _,
				out _,
				&pDACL,
				null,
				out _);

			if (result is not WIN32_ERROR.ERROR_SUCCESS)
				return result;

			// Remove an ACE
			bool bResult = PInvoke.DeleteAce(pDACL, dwAceIndex);

			if (!bResult)
				return (WIN32_ERROR)Marshal.GetLastPInvokeError();

			fixed (char* cPath = szPath)
			{
				// Set the new ACL
				result = PInvoke.SetNamedSecurityInfo(
					new PWSTR(cPath),
					SE_OBJECT_TYPE.SE_FILE_OBJECT,
					OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION,
					new PSID((void*)0),
					new PSID((void*)0),
					pDACL);

				if (result is not WIN32_ERROR.ERROR_SUCCESS)
					return result;

				return result;
			}
		}
	}
}
