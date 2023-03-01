﻿using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;

namespace Files.App.Filesystem.Security
{
	/// <summary>
	/// Represents an access mask details, such as its name and changeability.
	/// </summary>
	public class AccessMaskItem : ObservableObject
	{
		public AccessMaskItem(AccessControlEntry ace, bool isEditable = true)
		{
			IsEditable = isEditable;

			_ace = ace;
			_ace.PropertyChanged += AccessControlEntry_PropertyChanged;
		}

		public string AccessMaskName { get; set; }

		public AccessMaskFlags AccessMask { get; set; }

		public bool IsEnabled
		{
			get => _ace.AccessMaskFlags.HasFlag(AccessMask);
			set
			{
				if (IsEditable)
					ToggleAccess(AccessMask, value);
			}
		}

		public bool IsEditable { get; set; }

		private readonly AccessControlEntry _ace;

		private void ToggleAccess(AccessMaskFlags accessMask, bool value)
		{
			if (value && !_ace.AccessMaskFlags.HasFlag(accessMask))
				_ace.AccessMaskFlags |= accessMask;
			else if (!value && _ace.AccessMaskFlags.HasFlag(accessMask))
				_ace.AccessMaskFlags &= ~accessMask;
		}

		private void AccessControlEntry_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(AccessControlEntry.AccessMaskFlags))
				OnPropertyChanged(nameof(IsEnabled));
		}
	}
}
