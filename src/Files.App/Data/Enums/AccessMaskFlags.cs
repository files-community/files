// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Enums
{
	/// <summary>
	/// Represents access mask flags of an ACE.
	/// </summary>
	[Flags]
	public enum AccessMaskFlags
	{
		/// <summary>
		/// No access mask flags
		/// </summary>
		NULL = 0,

		/// <summary>
		/// Read data
		/// </summary>
		ReadData = 1,

		/// <summary>
		/// List data
		/// </summary>
		ListDirectory = 1,

		/// <summary>
		/// Write data
		/// </summary>
		WriteData = 2,

		/// <summary>
		/// Create files
		/// </summary>
		CreateFiles = 2,

		/// <summary>
		/// Append data
		/// </summary>
		AppendData = 4,

		/// <summary>
		/// Create folders
		/// </summary>
		CreateDirectories = 4,

		/// <summary>
		/// Read extended attributes
		/// </summary>
		ReadExtendedAttributes = 8,

		/// <summary>
		/// Write extended attributes
		/// </summary>
		WriteExtendedAttributes = 16,

		/// <summary>
		/// Execute file
		/// </summary>
		ExecuteFile = 32,

		/// <summary>
		/// Traverse
		/// </summary>
		Traverse = 32,

		/// <summary>
		/// Delete subfolders and files
		/// </summary>
		DeleteSubdirectoriesAndFiles = 64,

		/// <summary>
		/// Read attributes
		/// </summary>
		ReadAttributes = 128,

		/// <summary>
		/// Write attributes
		/// </summary>
		WriteAttributes = 256,

		/// <summary>
		/// Write
		/// </summary>
		Write = 278,

		/// <summary>
		/// Delete
		/// </summary>
		Delete = 65536,

		/// <summary>
		/// Read permissions
		/// </summary>
		ReadPermissions = 131072,

		/// <summary>
		/// Read
		/// </summary>
		Read = 131209,

		/// <summary>
		/// Read and execute
		/// </summary>
		ReadAndExecute = 131241,

		/// <summary>
		/// Notify
		/// </summary>
		Modify = 197055,

		/// <summary>
		/// Change permissions
		/// </summary>
		ChangePermissions = 262144,

		/// <summary>
		/// Take ownership
		/// </summary>
		TakeOwnership = 524288,

		/// <summary>
		/// Synchronize
		/// </summary>
		Synchronize = 1048576,

		/// <summary>
		/// Full control
		/// </summary>
		FullControl = 2032127
	}
}
