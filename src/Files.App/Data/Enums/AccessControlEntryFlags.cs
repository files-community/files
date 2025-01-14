// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Represents inheritance flags of an ACE
	/// </summary>
	[Flags]
	public enum AccessControlEntryFlags
	{
		/// <summary>
		/// No ACE flags are set.
		/// </summary>
		None = 0x0,

		/// <summary>
		/// The access mask is propagated onto child leaf objects.
		/// </summary>
		ObjectInherit = 0x1,

		/// <summary>
		/// The access mask is propagated to child container objects.
		/// </summary>
		ContainerInherit = 0x2,

		/// <summary>
		/// The access checks do not apply to the object; they only apply to its children.
		/// </summary>
		NoPropagateInherit = 0x4,

		/// <summary>
		/// The access mask is propagated only to child objects. This includes both container and leaf child objects.
		/// </summary>
		InheritOnly = 0x8,

		/// <summary>
		/// A logical OR of System.Security.AccessControl.AceFlags.ObjectInherit, System.Security.AccessControl.AceFlags.ContainerInherit, 
		/// System.Security.AccessControl.AceFlags.NoPropagateInherit, and System.Security.AccessControl.AceFlags.InheritOnly.
		/// </summary>
		InheritanceFlags = 0xf,

		/// <summary>
		/// An ACE is inherited from a parent container rather than being explicitly set for an object.
		/// </summary>
		Inherited = 0x10,

		/// <summary>
		/// Successful access attempts are audited.
		/// </summary>
		SuccessfulAccess = 0x40,

		/// <summary>
		/// The access mask is propagated onto child leaf objects.
		/// </summary>
		FailedAccess = 0x80,

		/// <summary>
		/// All access attempts are audited.
		/// </summary>
		AuditFlags = 0xc0
	}
}
