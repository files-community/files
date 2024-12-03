// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using System.Globalization;

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Provides an interface for managing real-time layout updates and related operations.
	/// </summary>
	public interface IRealTimeLayoutService
	{
		/// <summary>
		/// Occurs when the flow direction of the layout changes.
		/// </summary>
		public event EventHandler<FlowDirection>? FlowDirectionChanged;

		/// <summary>
		/// Gets the current flow direction for layout (e.g., LeftToRight or RightToLeft).
		/// </summary>
		FlowDirection FlowDirection { get; }

		/// <summary>
		/// Updates the culture settings for the layout.
		/// </summary>
		/// <param name="culture">The culture information to apply.</param>
		void UpdateCulture(CultureInfo culture);

		/// <summary>
		/// Adds a callback for a <see cref="Window"/> implementing <see cref="IRealTimeWindow"/>.
		/// The callback is automatically removed when the window is closed.
		/// </summary>
		/// <param name="target">The <see cref="Window"/> instance that implements <see cref="IRealTimeWindow"/>.</param>
		/// <param name="callback">The action to be executed when the callback is triggered.</param>
		void AddCallback(Window target, Action callback);

		/// <summary>
		/// Adds a callback for a <see cref="FrameworkElement"/> implementing <see cref="IRealTimeControl"/>.
		/// The callback is automatically removed when the element is unloaded.
		/// </summary>
		/// <param name="target">The <see cref="FrameworkElement"/> instance that implements <see cref="IRealTimeControl"/>.</param>
		/// <param name="callback">The action to be executed when the callback is triggered.</param>
		void AddCallback(FrameworkElement target, Action callback);

		/// <summary>
		/// Updates the title bar layout of the specified window based on the current flow direction.
		/// </summary>
		/// <param name="window">The window whose title bar layout needs updating.</param>
		/// <returns>True if the title bar layout was successfully updated; otherwise, false.</returns>
		bool UpdateTitleBar(Window window);

		/// <summary>
		/// Updates the content layout of the specified window to match the current flow direction.
		/// </summary>
		/// <param name="window">The window whose content layout needs updating.</param>
		void UpdateContent(Window window);

		/// <summary>
		/// Updates the content layout of the specified framework element to match the current flow direction.
		/// </summary>
		/// <param name="frameworkElement">The framework element whose content layout needs updating.</param>
		void UpdateContent(FrameworkElement frameworkElement);
	}
}
