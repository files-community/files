// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Actions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.Immutable;

namespace Files.App.Data.Commands
{
	[DebuggerDisplay("Command {Code} (Modifiable)")]
	internal sealed class ModifiableCommand : ObservableObject, IRichCommand
	{
		public event EventHandler? CanExecuteChanged;

		private IRichCommand BaseCommand;
		private ImmutableDictionary<KeyModifiers, IRichCommand> ModifiedCommands;

		/// <inheritdoc/>
		public CommandCodes Code => BaseCommand.Code;

		/// <inheritdoc/>
		public string Label => BaseCommand.Label;

		/// <inheritdoc/>
		public string LabelWithHotKey => BaseCommand.LabelWithHotKey;

		/// <inheritdoc/>
		public string AutomationName => BaseCommand.AutomationName;

		/// <inheritdoc/>
		public string Description => BaseCommand.Description;

		/// <inheritdoc/>
		public RichGlyph Glyph => BaseCommand.Glyph;

		/// <inheritdoc/>
		public object? Icon => BaseCommand.Icon;

		/// <inheritdoc/>
		public FontIcon? FontIcon => BaseCommand.FontIcon;

		/// <inheritdoc/>
		public Style? ThemedIconStyle => BaseCommand.ThemedIconStyle;

		/// <inheritdoc/>
		public bool IsCustomHotKeys => BaseCommand.IsCustomHotKeys;

		/// <inheritdoc/>
		public string? HotKeyText => BaseCommand.HotKeyText;

		/// <inheritdoc/>
		public HotKeyCollection HotKeys
		{
			get => BaseCommand.HotKeys;
			set => BaseCommand.HotKeys = value;
		}

		/// <inheritdoc/>
		public HotKeyCollection DefaultHotKeys { get; }

		/// <inheritdoc/>
		public bool IsToggle
			=> BaseCommand.IsToggle;

		/// <inheritdoc/>
		public bool IsOn
		{
			get => BaseCommand.IsOn;
			set => BaseCommand.IsOn = value;
		}

		/// <inheritdoc/>
		public bool IsExecutable
			=> BaseCommand.IsExecutable;

		/// <inheritdoc/>
		public bool IsAccessibleGlobally
			=> BaseCommand.IsAccessibleGlobally;

		public ModifiableCommand(IRichCommand baseCommand, Dictionary<KeyModifiers, IRichCommand> modifiedCommands)
		{
			BaseCommand = baseCommand;
			ModifiedCommands = modifiedCommands.ToImmutableDictionary();
			DefaultHotKeys = new(BaseCommand.HotKeys);

			if (baseCommand is ActionCommand actionCommand)
			{
				if (actionCommand.Action is INotifyPropertyChanging notifyPropertyChanging)
					notifyPropertyChanging.PropertyChanging += Action_PropertyChanging;
				if (actionCommand.Action is INotifyPropertyChanged notifyPropertyChanged)
					notifyPropertyChanged.PropertyChanged += Action_PropertyChanged;
			}
		}

		/// <inheritdoc/>
		public bool CanExecute(object? parameter)
		{
			return BaseCommand.CanExecute(parameter);
		}

		/// <inheritdoc/>
		public async void Execute(object? parameter)
		{
			await ExecuteAsync(parameter);
		}

		/// <inheritdoc/>
		public Task ExecuteAsync(object? parameter = null)
		{
			if (ModifiedCommands.TryGetValue(HotKeyHelpers.GetCurrentKeyModifiers(), out var modifiedCommand) &&
				modifiedCommand.IsExecutable)
				return modifiedCommand.ExecuteAsync(parameter);
			else
				return BaseCommand.ExecuteAsync(parameter);
		}

		/// <inheritdoc/>
		public async void ExecuteTapped(object sender, TappedRoutedEventArgs e)
		{
			await ExecuteAsync();
		}

		private void Action_PropertyChanging(object? sender, PropertyChangingEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IAction.Label):
					OnPropertyChanging(nameof(Label));
					OnPropertyChanging(nameof(LabelWithHotKey));
					OnPropertyChanging(nameof(AutomationName));
					break;
				case nameof(IToggleAction.IsOn) when IsToggle:
					OnPropertyChanging(nameof(IsOn));
					break;
				case nameof(IAction.IsExecutable):
					OnPropertyChanging(nameof(IsExecutable));
					break;
			}
		}

		private void Action_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IAction.Label):
					OnPropertyChanged(nameof(Label));
					OnPropertyChanged(nameof(LabelWithHotKey));
					OnPropertyChanged(nameof(AutomationName));
					break;
				case nameof(IToggleAction.IsOn) when IsToggle:
					OnPropertyChanged(nameof(IsOn));
					break;
				case nameof(IAction.IsExecutable):
					OnPropertyChanged(nameof(IsExecutable));
					CanExecuteChanged?.Invoke(this, EventArgs.Empty);
					break;
			}
		}
	}
}
