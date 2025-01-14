// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	/// <summary>
	/// Represents view model of <see cref="Views.Settings.ActionsPage"/>.
	/// </summary>
	public sealed class ActionsViewModel : ObservableObject
	{
		// Dependency injections

		private IActionsSettingsService ActionsSettingsService { get; } = Ioc.Default.GetRequiredService<IActionsSettingsService>();
		private ICommandManager CommandManager { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		// Properties

		public ObservableCollection<ModifiableActionItem> ValidActionItems { get; } = [];
		public ObservableCollection<ModifiableActionItem> AllActionItems { get; } = [];

		private bool _IsResetAllConfirmationTeachingTipOpened;
		public bool IsResetAllConfirmationTeachingTipOpened
		{
			get => _IsResetAllConfirmationTeachingTipOpened;
			set => SetProperty(ref _IsResetAllConfirmationTeachingTipOpened, value);
		}

		private bool _IsAlreadyUsedTeachingTipOpened;
		public bool IsAlreadyUsedTeachingTipOpened
		{
			get => _IsAlreadyUsedTeachingTipOpened;
			set => SetProperty(ref _IsAlreadyUsedTeachingTipOpened, value);
		}

		private bool _IsInvalidKeyTeachingTipOpened;
		public bool IsInvalidKeyTeachingTipOpened
		{
			get => _IsInvalidKeyTeachingTipOpened;
			set => SetProperty(ref _IsInvalidKeyTeachingTipOpened, value);
		}

		private bool _ShowAddNewKeyBindingBlock;
		public bool ShowAddNewKeyBindingBlock
		{
			get => _ShowAddNewKeyBindingBlock;
			set => SetProperty(ref _ShowAddNewKeyBindingBlock, value);
		}

		private bool _EnableAddNewKeyBindingButton;
		public bool EnableAddNewKeyBindingButton
		{
			get => _EnableAddNewKeyBindingButton;
			set => SetProperty(ref _EnableAddNewKeyBindingButton, value);
		}

		private int _SelectedActionItemIndex;
		public int SelectedActionItemIndex
		{
			get => _SelectedActionItemIndex;
			set => SetProperty(ref _SelectedActionItemIndex, value);
		}

		private ModifiableActionItem? _SelectedActionItem;
		public ModifiableActionItem? SelectedActionItem
		{
			get => _SelectedActionItem;
			set => SetProperty(ref _SelectedActionItem, value);
		}

		// Commands

		public ICommand LoadAllActionsCommand { get; set; }
		public ICommand ShowAddNewKeyBindingBlockCommand { get; set; }
		public ICommand HideAddNewKeyBindingBlockCommand { get; set; }
		public ICommand AddNewKeyBindingCommand { get; set; }
		public ICommand ShowRestoreDefaultsConfirmationCommand { get; set; }
		public ICommand RestoreDefaultsCommand { get; set; }
		public ICommand EditCommand { get; set; }
		public ICommand SaveCommand { get; set; }
		public ICommand CancelCommand { get; set; }
		public ICommand DeleteCommand { get; set; }

		// Constructor

		public ActionsViewModel()
		{
			LoadAllActionsCommand = new AsyncRelayCommand(ExecuteLoadAllActionsCommand);
			ShowAddNewKeyBindingBlockCommand = new RelayCommand(ExecuteShowAddNewKeyBindingBlockCommand);
			HideAddNewKeyBindingBlockCommand = new RelayCommand(ExecuteHideAddNewKeyBindingBlockCommand);
			AddNewKeyBindingCommand = new RelayCommand(ExecuteAddNewKeyBindingCommand);
			ShowRestoreDefaultsConfirmationCommand = new RelayCommand(ExecuteShowRestoreDefaultsConfirmationCommand);
			RestoreDefaultsCommand = new RelayCommand(ExecuteRestoreDefaultsCommand);
			EditCommand = new RelayCommand<ModifiableActionItem>(ExecuteEditCommand);
			SaveCommand = new RelayCommand<ModifiableActionItem>(ExecuteSaveCommand);
			CancelCommand = new RelayCommand<ModifiableActionItem>(ExecuteCancelCommand);
			DeleteCommand = new RelayCommand<ModifiableActionItem>(ExecuteDeleteCommand);
		}

		// Command methods

		private async Task ExecuteLoadAllActionsCommand()
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				ValidActionItems.Clear();
				AllActionItems.Clear();

				foreach (var command in CommandManager)
				{
					var defaultKeyBindings = command.DefaultHotKeys;

					if (command is NoneCommand || (command is IRichCommand richCommand && !richCommand.IsAccessibleGlobally))
						continue;

					AllActionItems.Add(new()
					{
						CommandCode = command.Code,
						CommandLabel = command.Label,
						CommandDescription = command.Description,
						KeyBinding = new(),
						DefaultKeyBindings = defaultKeyBindings,
					});

					foreach (var keyBinding in command.HotKeys)
					{
						// Don't show mouse key bindings for now because no editor provided for mouse input as of now
						if (!keyBinding.IsVisible ||
							keyBinding.LocalizedLabel == string.Empty ||
							keyBinding.RawLabel == string.Empty ||
							keyBinding.IsNone ||
							keyBinding.Key == Keys.None ||
							keyBinding.Key == Keys.Mouse4 ||
							keyBinding.Key == Keys.Mouse5)
							continue;

						ValidActionItems.Add(new()
						{
							CommandCode = command.Code,
							CommandLabel = command.Label,
							CommandDescription = command.Description,
							KeyBinding = keyBinding,
							DefaultKeyBindings = defaultKeyBindings,
							PreviousKeyBinding = keyBinding,
							IsDefinedByDefault = defaultKeyBindings.Contains(keyBinding),
						});
					}
				}
			});
		}

		private void ExecuteShowAddNewKeyBindingBlockCommand()
		{
			ShowAddNewKeyBindingBlock = true;
			EnableAddNewKeyBindingButton = false;

			// Reset edit mode of every item
			foreach (var action in ValidActionItems)
			{
				action.IsInEditMode = false;
				action.LocalizedKeyBindingLabel = action.KeyBinding.LocalizedLabel;
			}
		}

		private void ExecuteHideAddNewKeyBindingBlockCommand()
		{
			ShowAddNewKeyBindingBlock = false;
			EnableAddNewKeyBindingButton = false;

			if (SelectedActionItem is null)
				return;

			SelectedActionItem.LocalizedKeyBindingLabel = "";
			SelectedActionItem = null;
		}

		private void ExecuteAddNewKeyBindingCommand()
		{
			if (SelectedActionItem is null)
				return;

			// Initialize the new key binding
			var newKeyBinding = HotKey.Parse(SelectedActionItem.LocalizedKeyBindingLabel);

			// Check if this key binding is already taken
			foreach (var action in ValidActionItems)
			{
				if (newKeyBinding.RawLabel == action.KeyBinding.RawLabel)
				{
					IsAlreadyUsedTeachingTipOpened = true;
					return;
				}
			}

			var actions =
				ActionsSettingsService.ActionsV2 is not null
					? new List<ActionWithParameterItem>(ActionsSettingsService.ActionsV2)
					: [];

			if (actions.FindAll(x => x.CommandCode == SelectedActionItem.CommandCode.ToString()).Count is 0)
			{
				// Add default ones to the user setting
				foreach (var defaultKey in SelectedActionItem.DefaultKeyBindings)
					actions.Add(new(SelectedActionItem.CommandCode.ToString(), defaultKey.RawLabel));
			}
			else
			{
				// Remove empty one, which was added to hide default ones
				actions.RemoveAll(x => x.CommandCode == SelectedActionItem.CommandCode.ToString() && x.KeyBinding == string.Empty);
			}

			// Add the new one to the user setting
			actions.Add(new(SelectedActionItem.CommandCode.ToString(), newKeyBinding.RawLabel));

			var storedKeyBindings = actions.FindAll(x => x.CommandCode == SelectedActionItem.CommandCode.ToString());

			// If the existing ones in the user setting are all default ones, delete them
			if (storedKeyBindings.Select(x => x.KeyBinding).SequenceEqual(SelectedActionItem.DefaultKeyBindings.Select(x => x.LocalizedLabel)))
				actions.RemoveAll(x => x.CommandCode == SelectedActionItem.CommandCode.ToString());

			// Set to the user settings
			ActionsSettingsService.ActionsV2 = actions;

			// Set as customized
			foreach (var action in ValidActionItems)
			{
				if (action.CommandCode == SelectedActionItem.CommandCode)
					action.IsDefinedByDefault = SelectedActionItem.DefaultKeyBindings.Contains(action.KeyBinding);
			}

			// Create a clone
			var selectedNewItem = new ModifiableActionItem()
			{
				CommandCode = SelectedActionItem.CommandCode,
				CommandLabel = SelectedActionItem.CommandLabel,
				CommandDescription = SelectedActionItem.CommandDescription,
				KeyBinding = HotKey.Parse(SelectedActionItem.LocalizedKeyBindingLabel),
				DefaultKeyBindings = new(SelectedActionItem.DefaultKeyBindings),
				PreviousKeyBinding = HotKey.Parse(SelectedActionItem.LocalizedKeyBindingLabel),
			};

			// Exit edit mode
			ShowAddNewKeyBindingBlock = false;
			SelectedActionItemIndex = -1;

			// Add to existing list
			ValidActionItems.Insert(0, selectedNewItem);
		}

		private void ExecuteShowRestoreDefaultsConfirmationCommand()
		{
			IsResetAllConfirmationTeachingTipOpened = true;
		}

		private void ExecuteRestoreDefaultsCommand()
		{
			ActionsSettingsService.ActionsV2 = null;
			IsResetAllConfirmationTeachingTipOpened = false;

			_ = ExecuteLoadAllActionsCommand();
		}

		private void ExecuteEditCommand(ModifiableActionItem? item)
		{
			if (item is null)
				return;

			// Hide the add command grid
			ShowAddNewKeyBindingBlock = false;

			// Clear the selected item
			if (SelectedActionItem is not null)
			{
				SelectedActionItem.LocalizedKeyBindingLabel = "";
				SelectedActionItem = null;
			}

			// Reset edit mode of every item
			foreach (var action in ValidActionItems)
			{
				action.IsInEditMode = false;
				action.LocalizedKeyBindingLabel = action.KeyBinding.LocalizedLabel;
			}

			// Enter edit mode for the item
			item.IsInEditMode = true;

			// Mark the key binding as invalid to prevent saving it
			item.IsValidKeyBinding = false;
		}

		private void ExecuteSaveCommand(ModifiableActionItem? item)
		{
			if (item is null)
				return;

			if (item.LocalizedKeyBindingLabel == item.PreviousKeyBinding.LocalizedLabel)
			{
				item.IsInEditMode = false;
				return;
			}

			// Check if this key binding is already taken
			foreach (var action in ValidActionItems)
			{
				if (item.LocalizedKeyBindingLabel == action.PreviousKeyBinding.LocalizedLabel)
				{
					IsAlreadyUsedTeachingTipOpened = true;
					return;
				}
			}

			// Get clone of customized key bindings to overwrite
			var actions =
				ActionsSettingsService.ActionsV2 is not null
					? new List<ActionWithParameterItem>(ActionsSettingsService.ActionsV2)
					: [];

			// Get raw string keys stored in the user setting
			var storedKeyBindings = actions.FindAll(x => x.CommandCode == item.CommandCode.ToString());

			// Initialize
			var newKeyBinding = HotKey.Parse(item.LocalizedKeyBindingLabel);

			if (item.IsDefinedByDefault && storedKeyBindings.Count is 0)
			{
				// Any item related this action has never been customized
				foreach (var defaultKey in item.DefaultKeyBindings)
				{
					if (defaultKey.RawLabel != item.PreviousKeyBinding.RawLabel)
						actions.Add(new(item.CommandCode.ToString(), defaultKey.RawLabel));
				}

				actions.Add(new(item.CommandCode.ToString(), newKeyBinding.RawLabel));
			}
			else if (storedKeyBindings.Count == 1 && storedKeyBindings[0].KeyBinding == string.Empty)
			{
				storedKeyBindings.Clear();
				actions.Add(new(item.CommandCode.ToString(), newKeyBinding.RawLabel));
			}
			else
			{
				var previousKeyBinding = actions.Find(x => x.CommandCode == item.CommandCode.ToString() && x.KeyBinding == item.PreviousKeyBinding.LocalizedLabel);
				if (previousKeyBinding is not null)
					previousKeyBinding.KeyBinding = newKeyBinding.RawLabel;
			}

			// If the existing ones in the user setting are all default ones, delete them
			if (storedKeyBindings.Select(x => x.KeyBinding).SequenceEqual(item.DefaultKeyBindings.Select(x => x.LocalizedLabel)))
				actions.RemoveAll(x => x.CommandCode == item.CommandCode.ToString());

			// Set to the user settings
			ActionsSettingsService.ActionsV2 = actions;

			// Set as customized
			foreach (var action in ValidActionItems)
			{
				if (action.CommandCode == item.CommandCode)
					action.IsDefinedByDefault = item.DefaultKeyBindings.Contains(action.KeyBinding);
			}

			// Exit edit mode
			item.PreviousKeyBinding = newKeyBinding;
			item.KeyBinding = newKeyBinding;
			item.IsInEditMode = false;
		}

		private void ExecuteCancelCommand(ModifiableActionItem? item)
		{
			if (item is null)
				return;

			item.IsInEditMode = false;
			item.LocalizedKeyBindingLabel = item.KeyBinding.LocalizedLabel;
		}

		private void ExecuteDeleteCommand(ModifiableActionItem? item)
		{
			if (item is null)
				return;

			// Get clone of customized key bindings to overwrite
			var actions =
				ActionsSettingsService.ActionsV2 is not null
					? new List<ActionWithParameterItem>(ActionsSettingsService.ActionsV2)
					: [];

			// Get raw string keys stored in the user setting
			var storedKeyBindings = actions.FindAll(x => x.CommandCode == item.CommandCode.ToString());

			if (item.IsDefinedByDefault && storedKeyBindings.Count is 0)
			{
				int index = 0;

				// Any item related this action has never been customized
				foreach (var defaultKey in item.DefaultKeyBindings)
				{
					if (defaultKey.RawLabel != item.PreviousKeyBinding.RawLabel)
					{
						actions.Add(new(item.CommandCode.ToString(), defaultKey.RawLabel));
						index++;
					}
				}

				// If the count of default one(s) is 1, add the empty one to hide the default one
				if (index is 0)
					actions.Add(new(item.CommandCode.ToString(), string.Empty));
			}
			else
			{
				// Remove the previous one, which existed in the user setting
				actions.RemoveAll(x => x.CommandCode == item.CommandCode.ToString() && x.KeyBinding == item.PreviousKeyBinding.LocalizedLabel);

				// If the last one is about to be removed, add the empty one to hide the default ones
				if (actions.FindAll(x => x.CommandCode == item.CommandCode.ToString()).IsEmpty() &&
					item.DefaultKeyBindings.Length != 0)
					actions.Add(new(item.CommandCode.ToString(), string.Empty));
			}

			// Set to the user settings
			ActionsSettingsService.ActionsV2 = actions;

			// Set as customized
			foreach (var action in ValidActionItems)
			{
				if (action.CommandCode == item.CommandCode)
					action.IsDefinedByDefault = item.DefaultKeyBindings.Contains(action.KeyBinding);
			}

			// Exit edit mode
			item.IsInEditMode = false;
			ValidActionItems.Remove(item);
		}
	}
}
