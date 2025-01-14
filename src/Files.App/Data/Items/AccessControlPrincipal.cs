// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents a principal of an ACE or an owner of an ACL.
	/// </summary>
	public sealed class AccessControlPrincipal : ObservableObject
	{
		/// <summary>
		/// Account type.
		/// </summary>
		public AccessControlPrincipalType PrincipalType { get; private set; }

		/// <summary>
		/// Account security identifier (SID).
		/// </summary>
		public string? Sid { get; private set; }

		/// <summary>
		/// A domain the account belongs to
		/// </summary>
		public string? Domain { get; private set; }

		/// <summary>
		/// Account name
		/// </summary>
		public string? Name { get; private set; }

		/// <summary>
		/// Indicates whether this instance is valid or not
		/// </summary>
		public bool IsValid { get; private set; }

		/// <summary>
		/// Account type glyph.
		/// </summary>
		public string Glyph
			=> PrincipalType switch
			{
				AccessControlPrincipalType.User => "\xE77B",
				AccessControlPrincipalType.Group => "\xE902",
				_ => "\xE716",
			};

		/// <summary>
		/// Account display name
		/// </summary>
		public string? DisplayName
			=> string.IsNullOrEmpty(Name) ? Sid : Name;

		/// <summary>
		/// Account full name or just name
		/// </summary>
		public string? FullNameHumanized
			=> string.IsNullOrEmpty(Domain) ? Name : $"{Domain}\\{Name}";

		/// <summary>
		/// Account humanized full name.
		/// </summary>
		public string FullNameHumanizedWithBrackes
			=> string.IsNullOrEmpty(Domain) ? string.Empty : $"({Domain}\\{Name})";

		public unsafe AccessControlPrincipal(string sid)
		{
			if (string.IsNullOrEmpty(sid))
				return;

			Sid = sid;
			PSID lpSid = default;
			SID_NAME_USE snu = default;

			fixed (char* cSid = sid)
				PInvoke.ConvertStringSidToSid(new PCWSTR(cSid), &lpSid);

			PWSTR lpName = default;
			PWSTR lpDomain = default;
			uint cchName = 0, cchDomainName = 0;

			// Get size of account name and domain name
			bool bResult = PInvoke.LookupAccountSid(new PCWSTR(), lpSid, lpName, &cchName, lpDomain, &cchDomainName, null);

			// Ensure requested capacity
			fixed (char* cName = new char[cchName])
				lpName = new(cName);

			fixed (char* cDomain = new char[cchDomainName])
				lpDomain = new(cDomain);

			// Get account name and domain
			bResult = PInvoke.LookupAccountSid(new PCWSTR(), lpSid, lpName, &cchName, lpDomain, &cchDomainName, &snu);
			if(!bResult)
				return;

			PrincipalType = snu switch
			{
				// Group
				var x when
					(x == SID_NAME_USE.SidTypeAlias ||
					x == SID_NAME_USE.SidTypeGroup ||
					x == SID_NAME_USE.SidTypeWellKnownGroup)
					=> AccessControlPrincipalType.Group,

				// User
				SID_NAME_USE.SidTypeUser
					=> AccessControlPrincipalType.User,

				// Unknown
				_ => AccessControlPrincipalType.Unknown
			};

			// Replace domain name with computer name if the account type is user or alias type
			if (snu == SID_NAME_USE.SidTypeUser || snu == SID_NAME_USE.SidTypeAlias)
			{
				uint size = 256;
				fixed (char* cDomain = new char[size])
					lpDomain = new(cDomain);

				bResult = PInvoke.GetComputerName(lpDomain, ref size);
				if (!bResult)
					return;
			}

			Name = lpName.ToString();
			Domain = lpDomain.ToString().ToLower();

			IsValid = true;
		}
	}
}
