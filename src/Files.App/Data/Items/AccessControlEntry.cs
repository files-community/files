// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents an access control entry (ACE).
	/// </summary>
	public sealed class AccessControlEntry : ObservableObject
	{
		/// <summary>
		/// Whether the path indicates folder or not
		/// </summary>
		public bool IsFolder { get; private set; }

		/// <summary>
		/// The owner in the security descriptor (SD).
		/// NULL if the security descriptor has no owner SID.
		/// </summary>
		public AccessControlPrincipal Principal { get; set; }

		/// <summary>
		/// Whether the ACE is inherited or not
		/// </summary>
		public bool IsInherited { get; private set; }

		/// <summary>
		/// Whether the ACE is editable or not
		/// </summary>
		public bool IsEditable
			=> IsSelected && !IsInherited && false;

		/// <summary>
		/// AccessControlTypeHumanized
		/// </summary>
		public string AccessControlTypeHumanized
			=> AccessControlType switch
			{
				AccessControlEntryType.Allow => "Allow".GetLocalizedResource(),
				_ => "Deny".GetLocalizedResource() // AccessControlType.Deny
			};

		/// <summary>
		/// AccessControlTypeGlyph
		/// </summary>
		public string AccessControlTypeGlyph
			=> AccessControlType switch
			{
				AccessControlEntryType.Allow => "\xE73E",
				_ => "\xF140" // AccessControlType.Deny
			};

		/// <summary>
		/// AccessMaskFlagsHumanized
		/// </summary>
		public string AccessMaskFlagsHumanized
		{
			get
			{
				var accessMaskStrings = new List<string>();

				if (AccessMaskFlags == AccessMaskFlags.NULL)
					accessMaskStrings.Add("None".GetLocalizedResource());

				if (FullControlAccess)
					accessMaskStrings.Add("SecurityFullControlLabel/Text".GetLocalizedResource());
				else if (ModifyAccess)
					accessMaskStrings.Add("Modify".GetLocalizedResource());
				else if (ReadAndExecuteAccess)
					accessMaskStrings.Add("SecurityReadAndExecuteLabel/Text".GetLocalizedResource());
				else if (ReadAccess)
					accessMaskStrings.Add("SecurityReadLabel/Text".GetLocalizedResource());

				if (!FullControlAccess && !ModifyAccess && WriteAccess)
					accessMaskStrings.Add("Write".GetLocalizedResource());

				if (SpecialAccess)
					accessMaskStrings.Add("SecuritySpecialLabel/Text".GetLocalizedResource());

				return string.Join(", ", accessMaskStrings);
			}
		}

		/// <summary>
		/// IsInheritedHumanized
		/// </summary>
		public string IsInheritedHumanized
			=> IsInherited ? "Yes".GetLocalizedResource() : "No".GetLocalizedResource();

		/// <summary>
		/// InheritanceFlagsHumanized
		/// </summary>
		public string InheritanceFlagsHumanized
		{
			get
			{
				var inheritanceStrings = new List<string>();

				if (AccessControlEntryFlags == AccessControlEntryFlags.None ||
					AccessControlEntryFlags == AccessControlEntryFlags.NoPropagateInherit)
					inheritanceStrings.Add("SecurityAdvancedFlagsFolderLabel".GetLocalizedResource());

				if (AccessControlEntryFlags.HasFlag(AccessControlEntryFlags.ContainerInherit))
					inheritanceStrings.Add("SecurityAdvancedFlagsSubfoldersLabel".GetLocalizedResource());

				if (AccessControlEntryFlags.HasFlag(AccessControlEntryFlags.ObjectInherit))
					inheritanceStrings.Add("SecurityAdvancedFlagsFilesLabel".GetLocalizedResource());

				// Capitalize the first letter
				if (inheritanceStrings.Any())
					inheritanceStrings[0] = char.ToUpperInvariant(inheritanceStrings[0].First()) + inheritanceStrings[0][1..];

				return string.Join(", ", inheritanceStrings);
			}
		}

		/// <summary>
		/// AccessMaskItems
		/// </summary>
		public ObservableCollection<AccessMaskItem> AccessMaskItems { get; set; }

		private AccessControlEntryType _AccessControlType;
		public AccessControlEntryType AccessControlType
		{
			get => _AccessControlType;
			set
			{
				if (SetProperty(ref _AccessControlType, value))
				{
					OnPropertyChanged(nameof(AccessControlTypeGlyph));
					OnPropertyChanged(nameof(AccessControlTypeHumanized));
				}
			}
		}

		#region Access Mask Properties
		private AccessMaskFlags _AccessMaskFlags;
		public AccessMaskFlags AccessMaskFlags
		{
			get => _AccessMaskFlags;
			set
			{
				if (SetProperty(ref _AccessMaskFlags, value))
					OnPropertyChanged(nameof(AccessMaskFlagsHumanized));
			}
		}

		private AccessMaskFlags _AllowedAccessMaskFlags;
		public AccessMaskFlags AllowedAccessMaskFlags
		{
			get => _AllowedAccessMaskFlags;
			set
			{
				if (SetProperty(ref _AllowedAccessMaskFlags, value))
				{
					OnPropertyChanged(nameof(AllowedWriteAccess));
					OnPropertyChanged(nameof(AllowedFullControlAccess));
					OnPropertyChanged(nameof(AllowedListDirectoryAccess));
					OnPropertyChanged(nameof(AllowedModifyAccess));
					OnPropertyChanged(nameof(AllowedReadAccess));
					OnPropertyChanged(nameof(AllowedReadAndExecuteAccess));
				}
			}
		}

		private AccessMaskFlags _DeniedAccessMaskFlags;
		public AccessMaskFlags DeniedAccessMaskFlags
		{
			get => _DeniedAccessMaskFlags;
			set
			{
				if (SetProperty(ref _DeniedAccessMaskFlags, value))
				{
					OnPropertyChanged(nameof(DeniedWriteAccess));
					OnPropertyChanged(nameof(DeniedFullControlAccess));
					OnPropertyChanged(nameof(DeniedListDirectoryAccess));
					OnPropertyChanged(nameof(DeniedModifyAccess));
					OnPropertyChanged(nameof(DeniedReadAccess));
					OnPropertyChanged(nameof(DeniedReadAndExecuteAccess));
				}
			}
		}

		private AccessControlEntryFlags _InheritanceFlags;
		public AccessControlEntryFlags AccessControlEntryFlags
		{
			get => _InheritanceFlags;
			set
			{
				if (SetProperty(ref _InheritanceFlags, value))
					OnPropertyChanged(nameof(InheritanceFlagsHumanized));
			}
		}

		public AccessMaskFlags InheritedAllowAccessMaskFlags { get; set; }

		public AccessMaskFlags InheritedDenyAccessMaskFlags { get; set; }
		#endregion

		private bool _IsSelected;
		public bool IsSelected
		{
			get => _IsSelected;
			set
			{
				if (SetProperty(ref _IsSelected, value))
				{
					AreAdvancedPermissionsShown = false;

					OnPropertyChanged(nameof(IsEditable));
				}
			}
		}

		private bool _AreAdvancedPermissionsShown;
		public bool AreAdvancedPermissionsShown
		{
			get => _AreAdvancedPermissionsShown;
			set
			{
				// Reinitialize list
				if (SetProperty(ref _AreAdvancedPermissionsShown, value))
					AccessMaskItems = SecurityAdvancedAccessControlItemFactory.Initialize(this, value, IsInherited, IsFolder);
			}
		}

		#region Security page
		public bool WriteAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.Write);
		public bool ReadAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.Read);
		public bool ListDirectoryAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory);
		public bool ReadAndExecuteAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool ModifyAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.Modify);
		public bool FullControlAccess => AccessMaskFlags.HasFlag(AccessMaskFlags.FullControl);
		public bool SpecialAccess
			=> (AccessMaskFlags &
				~AccessMaskFlags.Synchronize &
				(FullControlAccess ? ~AccessMaskFlags.FullControl : AccessMaskFlags.FullControl) &
				(ModifyAccess ? ~AccessMaskFlags.Modify : AccessMaskFlags.FullControl) &
				(ReadAndExecuteAccess ? ~AccessMaskFlags.ReadAndExecute : AccessMaskFlags.FullControl) &
				(ReadAccess ? ~AccessMaskFlags.Read : AccessMaskFlags.FullControl) &
				(WriteAccess ? ~AccessMaskFlags.Write : AccessMaskFlags.FullControl)) != 0;

		public bool AllowedInheritedWriteAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Write);
		public bool AllowedInheritedReadAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Read);
		public bool AllowedInheritedListDirectoryAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory);
		public bool AllowedInheritedReadAndExecuteAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool AllowedInheritedModifyAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.Modify);
		public bool AllowedInheritedFullControlAccess => InheritedAllowAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl);

		public bool DeniedInheritedWriteAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Write);
		public bool DeniedInheritedReadAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Read);
		public bool DeniedInheritedListDirectoryAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory);
		public bool DeniedInheritedReadAndExecuteAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute);
		public bool DeniedInheritedModifyAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.Modify);
		public bool DeniedInheritedFullControlAccess => InheritedDenyAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl);
		#endregion

		#region SecurityAdvanced page
		public bool AllowedWriteAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.Write) || AllowedInheritedWriteAccess;
			set => ToggleAllowAccess(AccessMaskFlags.Write, value);
		}

		public bool AllowedReadAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.Read) || AllowedInheritedReadAccess;
			set => ToggleAllowAccess(AccessMaskFlags.Read, value);
		}

		public bool AllowedListDirectoryAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory) || AllowedInheritedListDirectoryAccess;
			set => ToggleAllowAccess(AccessMaskFlags.ListDirectory, value);
		}

		public bool AllowedReadAndExecuteAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute) || AllowedInheritedReadAndExecuteAccess;
			set => ToggleAllowAccess(AccessMaskFlags.ReadAndExecute, value);
		}

		public bool AllowedModifyAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.Modify) || AllowedInheritedModifyAccess;
			set => ToggleAllowAccess(AccessMaskFlags.Modify, value);
		}

		public bool AllowedFullControlAccess
		{
			get => AllowedAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl) || AllowedInheritedFullControlAccess;
			set => ToggleAllowAccess(AccessMaskFlags.FullControl, value);
		}

		public bool DeniedWriteAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.Write) || DeniedInheritedWriteAccess;
			set => ToggleDenyAccess(AccessMaskFlags.Write, value);
		}

		public bool DeniedReadAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.Read) || DeniedInheritedReadAccess;
			set => ToggleDenyAccess(AccessMaskFlags.Read, value);
		}

		public bool DeniedListDirectoryAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.ListDirectory) || DeniedInheritedListDirectoryAccess;
			set => ToggleDenyAccess(AccessMaskFlags.ListDirectory, value);
		}

		public bool DeniedReadAndExecuteAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.ReadAndExecute) || DeniedInheritedReadAndExecuteAccess;
			set => ToggleDenyAccess(AccessMaskFlags.ReadAndExecute, value);
		}

		public bool DeniedModifyAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.Modify) || DeniedInheritedModifyAccess;
			set => ToggleDenyAccess(AccessMaskFlags.Modify, value);
		}

		public bool DeniedFullControlAccess
		{
			get => DeniedAccessMaskFlags.HasFlag(AccessMaskFlags.FullControl) || DeniedInheritedFullControlAccess;
			set => ToggleDenyAccess(AccessMaskFlags.FullControl, value);
		}
		#endregion

		public IRelayCommand<string> ChangeAccessControlTypeCommand { get; set; }
		public IRelayCommand<string> ChangeInheritanceFlagsCommand { get; set; }

		public AccessControlEntry(bool isFolder, string ownerSid, AccessControlEntryType type, AccessMaskFlags accessMaskFlags, bool isInherited, AccessControlEntryFlags inheritanceFlags)
		{
			AccessMaskItems = SecurityAdvancedAccessControlItemFactory.Initialize(this, AreAdvancedPermissionsShown, IsInherited, IsFolder);

			ChangeAccessControlTypeCommand = new RelayCommand<string>(x =>
			{
				//AccessControlType = Enum.Parse<AccessControlType>(x);
			});

			ChangeInheritanceFlagsCommand = new RelayCommand<string>(x =>
			{
				//var parts = x.Split(',');
				//InheritanceFlags = Enum.Parse<AccessControlEntryFlags>(parts[0]);
			});

			IsFolder = isFolder;
			Principal = new(ownerSid);
			AccessControlType = type;
			AccessMaskFlags = accessMaskFlags;
			IsInherited = isInherited;
			AccessControlEntryFlags = inheritanceFlags;

			switch (AccessControlType)
			{
				case AccessControlEntryType.Allow:
					if (IsInherited)
						InheritedAllowAccessMaskFlags |= AccessMaskFlags;
					else
						AllowedAccessMaskFlags |= AccessMaskFlags;
					break;
				case AccessControlEntryType.Deny:
					if (IsInherited)
						InheritedDenyAccessMaskFlags |= AccessMaskFlags;
					else
						DeniedAccessMaskFlags |= AccessMaskFlags;
					break;
			}
		}

		private void ToggleAllowAccess(AccessMaskFlags accessMask, bool value)
		{
			if (value &&
				!AllowedAccessMaskFlags.HasFlag(accessMask) &&
				!InheritedAllowAccessMaskFlags.HasFlag(accessMask))
			{
				AllowedAccessMaskFlags |= accessMask;
				DeniedAccessMaskFlags &= ~accessMask;
			}
			else if (!value &&
				AllowedAccessMaskFlags.HasFlag(accessMask))
			{
				AllowedAccessMaskFlags &= ~accessMask;
			}
		}

		private void ToggleDenyAccess(AccessMaskFlags accessMask, bool value)
		{
			if (value &&
				!DeniedAccessMaskFlags.HasFlag(accessMask) &&
				!InheritedDenyAccessMaskFlags.HasFlag(accessMask))
			{
				DeniedAccessMaskFlags |= accessMask;
				AllowedAccessMaskFlags &= ~accessMask;
			}
			else if (!value &&
				DeniedAccessMaskFlags.HasFlag(accessMask))
			{
				DeniedAccessMaskFlags &= ~accessMask;
			}
		}

		/// <summary>
		/// Gets access control list (ACL) initialized with default data.
		/// </summary>
		/// <param name="isFolder"></param>
		/// <param name="ownerSid"></param>
		/// <returns>If the function succeeds, an instance of AccessControlList; otherwise, null.</returns>
		public static AccessControlEntry GetDefault(bool isFolder, string ownerSid)
		{
			return new(
				isFolder,
				ownerSid,
				AccessControlEntryType.Allow,
				AccessMaskFlags.ReadAndExecute,
				false,
				isFolder
					? AccessControlEntryFlags.ContainerInherit | AccessControlEntryFlags.ObjectInherit
					: AccessControlEntryFlags.None);
		}
	}
}
