// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Input;

namespace Files.App.ViewModels.Dialogs
{
	public sealed class GitHubLoginDialogViewModel : ObservableObject
	{
		private const string URL = "https://github.com/login/device";

		private CancellationTokenSource _loginCancellationToken { get; init; }

		public string UserCode { get; init; }

		public Uri NavigateUri { get; init; }

		public string LoginUrl { get; init; }

		public string _Subtitle = string.Empty;
		public string Subtitle
		{
			get => _Subtitle;
			set => SetProperty(ref _Subtitle, value);
		}

		private bool _LoginConfirmed;
		public bool LoginConfirmed
		{
			get => _LoginConfirmed;
			set => SetProperty(ref _LoginConfirmed, value);
		}

		public ICommand CloseButtonCommand { get; }

		public GitHubLoginDialogViewModel(string userCode, string subtitle, CancellationTokenSource loginCTS)
		{
			UserCode = userCode;
			Subtitle = subtitle;
			_loginCancellationToken = loginCTS;
			LoginUrl = URL;
			NavigateUri = new Uri(URL);

			CloseButtonCommand = new RelayCommand(_loginCancellationToken.Cancel);
		}
	}
}
