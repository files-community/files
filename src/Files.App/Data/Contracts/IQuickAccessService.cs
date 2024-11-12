﻿// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections.Specialized;

namespace Files.App.Data.Contracts
{
	public interface IQuickAccessService
	{
		IReadOnlyList<INavigationControlItem> PinnedFolders { get; }

		event EventHandler<NotifyCollectionChangedEventArgs>? PinnedFoldersChanged;

		Task InitializeAsync();

		Task<bool> UpdatePinnedFoldersAsync();

		Task<bool> PinFolderAsync(string[] paths);

		Task<bool> UnpinFolderAsync(string[] paths);
	}
}
