// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents item for a hotkey that is registered for a <see cref="IRichCommand"/>.
	/// </summary>
	public class ModifiableActionItem : ObservableObject
	{
		public CommandCodes CommandCode { get; set; }

		public string CommandLabel { get; set; } = string.Empty;

		public string CommandDescription { get; set; } = string.Empty;

		public bool IsCommandParameterSupported { get; set; }

		public string CommandParameter { get; set; } = string.Empty;

		public HotKeyCollection DefaultKeyBindings { get; set; } = HotKeyCollection.Empty;

		public HotKey PreviousKeyBinding { get; set; } = HotKey.None;

		public HotKeyCollection KeyBindings { get; set; }

		private string _LocalizedKeyBindingLabel = string.Empty;
		public string LocalizedKeyBindingLabel
		{
			get => _LocalizedKeyBindingLabel;
			set => SetProperty(ref _LocalizedKeyBindingLabel, value);
		}

		private bool _IsInEditMode;
		public bool IsInEditMode
		{
			get => _IsInEditMode;
			set => SetProperty(ref _IsInEditMode, value);
		}

		private bool _IsValidKeyBinding;
		public bool IsValidKeyBinding
		{
			get => _IsValidKeyBinding;
			set => SetProperty(ref _IsValidKeyBinding, value);
		}

		private bool _IsDefinedByDefault;
		public bool IsDefinedByDefault
		{
			get => _IsDefinedByDefault;
			set => SetProperty(ref _IsDefinedByDefault, value);
		}

		private HotKey _KeyBinding;
		public HotKey KeyBinding
		{
			get => _KeyBinding;
			set
			{
				if (SetProperty(ref _KeyBinding, value))
				{
					LocalizedKeyBindingLabel = KeyBinding.LocalizedLabel;
					KeyBindings = new(value);
					OnPropertyChanged(nameof(KeyBindings));
				}
			}
		}
	}
}
