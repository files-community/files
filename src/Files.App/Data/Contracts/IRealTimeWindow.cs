// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Contracts
{
	/// <summary>
	/// Defines an interface for real-time window components that manage content layout updates.
	/// </summary>
	internal interface IRealTimeWindow
	{
		/// <summary>
		/// Gets the service for managing real-time layout updates.
		/// </summary>
		IRealTimeLayoutService RealTimeLayoutService { get; }

		/// <summary>
		/// Initializes the content layout for the window.
		/// </summary>
		void InitializeContentLayout();

		/// <summary>
		/// Updates the content layout of the window.
		/// </summary>
		void UpdateContentLayout();
	}
}
