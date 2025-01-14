// Copyright (c) Files Community
// Licensed under the MIT License.

using Sentry;
using Files.App.Actions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Data.Commands
{
	[DebuggerDisplay("Command {Code}")]
	internal sealed class ActionCommand : ObservableObject, IRichCommand
	{
		private IActionsSettingsService ActionsSettingsService { get; } = Ioc.Default.GetRequiredService<IActionsSettingsService>();

		public event EventHandler? CanExecuteChanged;

		public IAction Action { get; }

		/// <inheritdoc/>
		public CommandCodes Code { get; }

		/// <inheritdoc/>
		public string Label
			=> Action.Label;

		/// <inheritdoc/>
		public string LabelWithHotKey
			=> HotKeyText is null ? Label : $"{Label} ({HotKeyText})";

		/// <inheritdoc/>
		public string AutomationName
			=> Label;

		/// <inheritdoc/>
		public string Description
			=> Action.Description;

		/// <inheritdoc/>
		public RichGlyph Glyph
			=> Action.Glyph;

		/// <inheritdoc/>
		public object? Icon { get; }

		/// <inheritdoc/>
		public FontIcon? FontIcon { get; }

		/// <inheritdoc/>
		public Style? ThemedIconStyle { get; }

		private bool isCustomHotKeys = false;
		/// <inheritdoc/>
		public bool IsCustomHotKeys
		{
			get => isCustomHotKeys;
			set => SetProperty(ref isCustomHotKeys, value);
		}

		/// <inheritdoc/>
		public string? HotKeyText
		{
			get
			{
				string text = HotKeys.LocalizedLabel;
				if (string.IsNullOrEmpty(text))
					return null;
				return text;
			}
		}

		private HotKeyCollection hotKeys;
		/// <inheritdoc/>
		public HotKeyCollection HotKeys
		{
			get => hotKeys;
			set
			{
				if (SetProperty(ref hotKeys, value))
				{
					OnPropertyChanged(nameof(HotKeyText));
					OnPropertyChanged(nameof(LabelWithHotKey));
					IsCustomHotKeys = true;
				}
			}
		}

		/// <inheritdoc/>
		public HotKeyCollection DefaultHotKeys { get; }

		/// <inheritdoc/>
		public bool IsToggle
			=> Action is IToggleAction;

		/// <inheritdoc/>
		public bool IsOn
		{
			get => Action is IToggleAction toggleAction && toggleAction.IsOn;
			set
			{
				if (Action is IToggleAction toggleAction && toggleAction.IsOn != value)
					Execute(null);
			}
		}

		/// <inheritdoc/>
		public bool IsExecutable
			=> Action.IsExecutable;

		/// <inheritdoc/>
		public bool IsAccessibleGlobally
			=> Action.IsAccessibleGlobally;

		public ActionCommand(CommandManager manager, CommandCodes code, IAction action)
		{
			Code = code;
			Action = action;
			Icon = action.Glyph.ToIcon();
			FontIcon = action.Glyph.ToFontIcon();
			ThemedIconStyle = action.Glyph.ToThemedIconStyle();
			hotKeys = CommandManager.GetDefaultKeyBindings(action);
			DefaultHotKeys = CommandManager.GetDefaultKeyBindings(action);

			if (action is INotifyPropertyChanging notifyPropertyChanging)
				notifyPropertyChanging.PropertyChanging += Action_PropertyChanging;
			if (action is INotifyPropertyChanged notifyPropertyChanged)
				notifyPropertyChanged.PropertyChanged += Action_PropertyChanged;
		}

		/// <inheritdoc/>
		public bool CanExecute(object? parameter)
		{
			return Action.IsExecutable;
		}

		/// <inheritdoc/>
		public async void Execute(object? parameter)
		{
			await ExecuteAsync(parameter);
		}

		/// <inheritdoc/>
		public Task ExecuteAsync(object? parameter = null)
		{
			if (IsExecutable)
			{
				// Re-enable when Metris feature is available again
				// SentrySdk.Metrics.Increment("actions", tags: new Dictionary<string, string> { { "command", Code.ToString() } });
				return Action.ExecuteAsync(parameter);
			}

			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public async void ExecuteTapped(object sender, TappedRoutedEventArgs e)
		{
			await ExecuteAsync();
		}

		internal void OverwriteKeyBindings(HotKeyCollection hotKeys)
		{
			HotKeys = hotKeys;
		}

		internal void RestoreKeyBindings()
		{
			hotKeys = DefaultHotKeys;
			IsCustomHotKeys = false;
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
