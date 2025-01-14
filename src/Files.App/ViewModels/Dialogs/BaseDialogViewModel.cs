// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Input;

namespace Files.App.ViewModels.Dialogs
{
	/// <summary>
	/// Serves as the base dialog view model containing reusable,
	/// and optional boilerplate code for every dialog.
	/// </summary>
	public abstract class BaseDialogViewModel : ObservableObject
	{
		private string? _Title;
		public string? Title
		{
			get => _Title;
			set => SetProperty(ref _Title, value);
		}

		private bool _PrimaryButtonEnabled;
		public bool PrimaryButtonEnabled
		{
			get => _PrimaryButtonEnabled;
			set => SetProperty(ref _PrimaryButtonEnabled, value);
		}

		private bool _SecondaryButtonEnabled;
		public bool SecondaryButtonEnabled
		{
			get => _SecondaryButtonEnabled;
			set => SetProperty(ref _SecondaryButtonEnabled, value);
		}

		private string? _PrimaryButtonText;
		public string? PrimaryButtonText
		{
			get => _PrimaryButtonText;
			set => SetProperty(ref _PrimaryButtonText, value);
		}

		private string? _SecondaryButtonText;
		public string? SecondaryButtonText
		{
			get => _SecondaryButtonText;
			set => SetProperty(ref _SecondaryButtonText, value);
		}

		private string? _CloseButtonText;
		public string? CloseButtonText
		{
			get => _CloseButtonText;
			set => SetProperty(ref _CloseButtonText, value);
		}

		public ICommand? PrimaryButtonClickCommand { get; protected init; }

		public ICommand? SecondaryButtonClickCommand { get; protected init; }

		public ICommand? CloseButtonClickCommand { get; protected init; }
	}
}
