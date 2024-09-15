﻿// Copyright (c) 2018-2024 Files Community
// Licensed under the MIT License.

using Files.App.Data.Enums;

namespace Files.App.ViewModels.Dialogs
{
	public interface IDialog<TViewModel>
		where TViewModel : class, INotifyPropertyChanged
	{
		TViewModel ViewModel { get; set; }

		Task<DialogResult> ShowAsync();

		void Hide();
	}
}
