// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Data.Commands
{
	[DebuggerDisplay("Command None")]
	internal sealed class NoneCommand : IRichCommand
	{
		public event EventHandler? CanExecuteChanged { add { } remove { } }
		public event PropertyChangingEventHandler? PropertyChanging { add { } remove { } }
		public event PropertyChangedEventHandler? PropertyChanged { add { } remove { } }

		/// <inheritdoc/>
		public CommandCodes Code
			=> CommandCodes.None;

		/// <inheritdoc/>
		public string Label
			=> string.Empty;

		/// <inheritdoc/>
		public string LabelWithHotKey
			=> string.Empty;

		/// <inheritdoc/>
		public string AutomationName
			=> string.Empty;

		/// <inheritdoc/>
		public string Description
			=> string.Empty;

		/// <inheritdoc/>
		public RichGlyph Glyph
			=> RichGlyph.None;

		/// <inheritdoc/>
		public object? Icon
			=> null;

		/// <inheritdoc/>
		public FontIcon? FontIcon
			=> null;

		/// <inheritdoc/>
		public Style? ThemedIconStyle
			=> null;

		/// <inheritdoc/>
		public bool IsCustomHotKeys
			=> false;

		/// <inheritdoc/>
		public string? HotKeyText
			=> null;

		/// <inheritdoc/>
		public HotKeyCollection HotKeys
		{
			get => HotKeyCollection.Empty;
			set => throw new InvalidOperationException("This command is readonly.");
		}

		/// <inheritdoc/>
		public HotKeyCollection DefaultHotKeys
		{
			get => HotKeyCollection.Empty;
			set => throw new InvalidOperationException("This command is readonly.");
		}

		/// <inheritdoc/>
		public bool IsToggle
			=> false;

		/// <inheritdoc/>
		public bool IsOn
		{
			get => false;
			set { }
		}

		/// <inheritdoc/>
		public bool IsExecutable
			=> false;

		/// <inheritdoc/>
		public bool IsAccessibleGlobally
			=> false;

		public bool CanExecute(object? parameter)
			=> false;

		/// <inheritdoc/>
		public void Execute(object? parameter)
		{
		}

		/// <inheritdoc/>
		public Task ExecuteAsync(object? parameter = null)
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc/>
		public void ExecuteTapped(object sender, TappedRoutedEventArgs e)
		{
		}

		public void ResetHotKeys()
		{
		}
	}
}
