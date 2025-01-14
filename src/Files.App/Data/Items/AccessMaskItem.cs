// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents an access mask details, such as its name and changeability.
	/// </summary>
	public sealed class AccessMaskItem : ObservableObject
	{
		public string AccessMaskName { get; set; }

		public AccessMaskFlags AccessMask { get; set; }

		public bool IsEnabled
		{
			get
			{
				return _ace.AccessMaskFlags.HasFlag(AccessMask);
			}
			set
			{
				if (IsEditable)
					ToggleAccess(AccessMask, value);
			}
		}

		public bool IsEditable { get; set; }

		private readonly AccessControlEntry _ace;

		public AccessMaskItem(AccessControlEntry ace, bool isEditable = true)
		{
			IsEditable = isEditable;

			_ace = ace;
			_ace.PropertyChanged += AccessControlEntry_PropertyChanged;
		}

		private void ToggleAccess(AccessMaskFlags accessMask, bool value)
		{
			if (value && !_ace.AccessMaskFlags.HasFlag(accessMask))
				_ace.AccessMaskFlags |= accessMask;
			else if (!value && _ace.AccessMaskFlags.HasFlag(accessMask))
				_ace.AccessMaskFlags &= ~accessMask;
		}

		private void AccessControlEntry_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(AccessControlEntry.AccessMaskFlags))
				OnPropertyChanged(nameof(IsEnabled));
		}
	}
}
