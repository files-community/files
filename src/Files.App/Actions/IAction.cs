// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	public interface IAction
	{
		/// <summary>
		/// A label for display in context menus and toolbars
		/// </summary>
		string Label { get; }

		/// <summary>
		/// A brief description of what the action does.
		/// It will be used as the command name in the command palette.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Glyph information to display icon.
		/// </summary>
		RichGlyph Glyph
			=> RichGlyph.None;

		/// <summary>
		/// Primary hotkey to execute the action.
		/// </summary>
		HotKey HotKey
			=> HotKey.None;

		/// <summary>
		/// Secondary hotkey to execute the action.
		/// </summary>
		HotKey SecondHotKey
			=> HotKey.None;

		/// <summary>
		/// Tertiary hotkey to execute the action.
		/// </summary>
		HotKey ThirdHotKey
			=> HotKey.None;

		/// <summary>
		/// A hotkey with media keys.
		/// </summary>
		HotKey MediaHotKey
			=> HotKey.None;

		/// <summary>
		/// Returns whether the action is executable in the current context.
		/// </summary>
		bool IsExecutable
			=> true;

		/// <summary>
		/// Returns whether the action is accessible in any context.
		/// </summary>
		bool IsAccessibleGlobally
			=> true;

		/// <summary>
		/// Executes the action asynchronously.
		/// </summary>
		/// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
		Task ExecuteAsync(object? parameter = null);
	}
}
