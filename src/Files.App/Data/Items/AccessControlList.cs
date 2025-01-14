// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents an access control list (ACL).
	/// </summary>
	public sealed class AccessControlList : ObservableObject
	{
		/// <summary>
		/// Object path.
		/// </summary>
		public string Path { get; private set; }

		/// <summary>
		/// Whether the path indicates folder or not.
		/// </summary>
		public bool IsFolder { get; private set; }

		/// <summary>
		/// The owner in the security descriptor (SD).
		/// NULL if the security descriptor has no owner SID.
		/// </summary>
		public AccessControlPrincipal Owner { get; private set; }

		/// <summary>
		/// Validates an access control list (ACL).
		/// </summary>
		public bool IsValid { get; private set; }

		/// <summary>
		/// Access control entry (ACE) list
		/// </summary>
		public ObservableCollection<AccessControlEntry> AccessControlEntries { get; private set; }

		public AccessControlList(string path, bool isFolder, AccessControlPrincipal owner, bool isValid)
		{
			Path = path;
			IsFolder = isFolder;
			Owner = owner;
			IsValid = isValid;
			AccessControlEntries = [];
		}

		public AccessControlList()
		{
			Path = string.Empty;
			Owner = new(string.Empty);
			AccessControlEntries = [];
		}
	}
}
