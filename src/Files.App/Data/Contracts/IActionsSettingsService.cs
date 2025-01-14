// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	internal interface IActionsSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// A dictionary to determine the custom hotkeys
		/// </summary>
		List<ActionWithParameterItem>? ActionsV2 { get; set; }
	}
}
