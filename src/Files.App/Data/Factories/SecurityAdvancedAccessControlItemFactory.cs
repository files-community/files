// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Files.App.Data.Factories
{
	public static class SecurityAdvancedAccessControlItemFactory
	{
		/// <summary>
		/// Returned list list will be shown in an ACE item in security advanced page
		/// </summary>
		/// <param name="current"></param>
		/// <param name="isAdvanced"></param>
		/// <param name="isInherited"></param>
		/// <param name="isFolder"></param>
		/// <returns></returns>
		public static ObservableCollection<AccessMaskItem> Initialize(AccessControlEntry current, bool isAdvanced, bool isInherited, bool isFolder)
		{
			List<AccessMaskItem> accessControls;

			if (isAdvanced)
			{
				accessControls =
				[
					new(current)
					{
						AccessMask = AccessMaskFlags.FullControl,
						AccessMaskName = "SecurityFullControlLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.Traverse,
						AccessMaskName = "SecurityTraverseLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ExecuteFile,
						AccessMaskName = "SecurityExecuteFileLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ListDirectory,
						AccessMaskName = "SecurityListDirectoryLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ReadData,
						AccessMaskName = "SecurityReadDataLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ReadAttributes,
						AccessMaskName = "SecurityReadAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ReadExtendedAttributes,
						AccessMaskName = "SecurityReadExtendedAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.CreateFiles,
						AccessMaskName = "SecurityCreateFilesLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.CreateDirectories,
						AccessMaskName = "SecurityCreateDirectoriesLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.WriteData,
						AccessMaskName = "SecurityWriteDataLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.AppendData,
						AccessMaskName = "SecurityAppendDataLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.WriteAttributes,
						AccessMaskName = "SecurityWriteAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.WriteExtendedAttributes,
						AccessMaskName = "SecurityWriteExtendedAttributesLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.DeleteSubdirectoriesAndFiles,
						AccessMaskName = "SecurityDeleteSubdirectoriesAndFilesLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.Delete,
						AccessMaskName = "Delete".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ReadPermissions,
						AccessMaskName = "SecurityReadPermissionsLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ChangePermissions,
						AccessMaskName = "SecurityChangePermissionsLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.TakeOwnership,
						AccessMaskName = "SecurityTakeOwnershipLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					}
				];

				if (isFolder)
				{
					accessControls.RemoveAll(x =>
						x.AccessMask == AccessMaskFlags.ExecuteFile ||
						x.AccessMask == AccessMaskFlags.ReadData ||
						x.AccessMask == AccessMaskFlags.WriteData ||
						x.AccessMask == AccessMaskFlags.AppendData);
				}
				else
				{
					accessControls.RemoveAll(x =>
						x.AccessMask == AccessMaskFlags.Traverse ||
						x.AccessMask == AccessMaskFlags.ListDirectory ||
						x.AccessMask == AccessMaskFlags.CreateFiles ||
						x.AccessMask == AccessMaskFlags.CreateDirectories ||
						x.AccessMask == AccessMaskFlags.DeleteSubdirectoriesAndFiles);
				}
			}
			else
			{
				accessControls =
				[
					new(current)
					{
						AccessMask = AccessMaskFlags.FullControl,
						AccessMaskName = "SecurityFullControlLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.Modify,
						AccessMaskName = "Modify".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ReadAndExecute,
						AccessMaskName = "SecurityReadAndExecuteLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.ListDirectory,
						AccessMaskName = "SecurityListDirectoryLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.Read,
						AccessMaskName = "SecurityReadLabel/Text".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current)
					{
						AccessMask = AccessMaskFlags.Write,
						AccessMaskName = "Write".GetLocalizedResource(),
						IsEditable = !isInherited
					},
					new(current, false)
					{
						AccessMaskName = "SecuritySpecialLabel/Text".GetLocalizedResource()
					}
				];

				if (!isFolder)
				{
					accessControls.RemoveAll(x =>
						x.AccessMask == AccessMaskFlags.ListDirectory);
				}
			}

			return new ObservableCollection<AccessMaskItem>(accessControls);
		}
	}
}
