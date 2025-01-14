// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;

namespace Files.App.Data.Commands
{
	/// <summary>
	/// Represents richer <see cref="ICommand"/>, which provides title, description, hotkeys and more.
	/// </summary>
	public interface IRichCommand : ICommand, INotifyPropertyChanging, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets the code of this command.
		/// </summary>
		CommandCodes Code { get; }

		/// <summary>
		/// Gets the label of this command.
		/// </summary>
		string Label { get; }

		/// <summary>
		/// Gets the combined string of the command label and humanized hotkey string.
		/// </summary>
		string LabelWithHotKey { get; }

		/// <summary>
		/// Gets the automation name of this command that is used for the interaction test.
		/// </summary>
		string AutomationName { get; }

		/// <summary>
		/// Gets the description of this command.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Gets string glyph or <see cref="ThemedIcon"/> style.
		/// </summary>
		RichGlyph Glyph { get; }

		/// <summary>
		/// Gets icon UI element.
		/// </summary>
		object? Icon { get; }

		/// <summary>
		/// Gets the font icon of this command.
		/// </summary>
		FontIcon? FontIcon { get; }

		/// <summary>
		/// Gets the commands <see cref="ThemedIcon"/> style.
		/// </summary>
		Style? ThemedIconStyle { get; }

		/// <summary>
		/// Gets the value that indicates whether the hotkey is customized by user setting.
		/// </summary>
		bool IsCustomHotKeys { get; }

		/// <summary>
		/// Gets the humanized hotkey string.
		/// </summary>
		string? HotKeyText { get; }

		/// <summary>
		/// Gets the hotkey that is assigned to this command.
		/// </summary>
		HotKeyCollection HotKeys { get; set; }

		/// <summary>
		/// Gets the default hotkeys that is assigned to this command.
		/// </summary>
		HotKeyCollection DefaultHotKeys { get; }

		/// <summary>
		/// Gets or sets the value that indicates whether the command is toggleable.
		/// </summary>
		bool IsToggle { get; }

		/// <summary>
		/// Gets or sets the value that indicates whether the command is toggleable and toggled.
		/// </summary>
		bool IsOn { get; set; }

		/// <summary>
		/// Gets the value that indicates whether the command is executable.
		/// </summary>
		bool IsExecutable { get; }

		/// <summary>
		/// Returns whether the action is accessible in any context.
		/// </summary>
		bool IsAccessibleGlobally { get; }

		/// <summary>
		/// Executes the command.
		/// </summary>
		/// <param name="parameter">Parameter that is passed when executed.</param>
		/// <returns></returns>
		Task ExecuteAsync(object? parameter = null);

		/// <summary>
		/// Gets invoked when user tapped <see cref="UIElement"/> that is bound to this command.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void ExecuteTapped(object sender, TappedRoutedEventArgs e);
	}
}
