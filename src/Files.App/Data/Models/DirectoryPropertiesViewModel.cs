// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Input;

namespace Files.App.ViewModels.UserControls
{
	public sealed class StatusBarViewModel : ObservableObject
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		private IDevToolsSettingsService DevToolsSettingsService = Ioc.Default.GetRequiredService<IDevToolsSettingsService>();

		// The first branch will always be the active one.
		public const int ACTIVE_BRANCH_INDEX = 0;

		private string? _gitRepositoryPath;

		private readonly ObservableCollection<BranchItem> _localBranches = [];

		private readonly ObservableCollection<BranchItem> _remoteBranches = [];

		public bool IsBranchesFlyoutExpanded { get; set; } = false;

		private string? _DirectoryItemCount;
		public string? DirectoryItemCount
		{
			get => _DirectoryItemCount;
			set => SetProperty(ref _DirectoryItemCount, value);
		}

		private string? _GitBranchDisplayName;
		public string? GitBranchDisplayName
		{
			get => _GitBranchDisplayName;
			private set => SetProperty(ref _GitBranchDisplayName, value);
		}

		private int _SelectedBranchIndex;
		public int SelectedBranchIndex
		{
			get => _SelectedBranchIndex;
			set
			{
				if (SetProperty(ref _SelectedBranchIndex, value) && 
					value != -1 && 
					(value != ACTIVE_BRANCH_INDEX || !_ShowLocals) &&
					value < Branches.Count)
				{
					CheckoutRequested?.Invoke(this, Branches[value].Name);
				}
			}
		}

		private bool _ShowLocals = true;
		public bool ShowLocals
		{
			get => _ShowLocals;
			set
			{
				if (SetProperty(ref _ShowLocals, value))
				{
					OnPropertyChanged(nameof(Branches));

					if (value)
						SelectedBranchIndex = ACTIVE_BRANCH_INDEX;
				}
			}
		}

		private string _StatusInfo = "0 / 0";
		public string StatusInfo
		{
			get => _StatusInfo;
			set => SetProperty(ref _StatusInfo, value);
		}

		private string _ExtendedStatusInfo = string.Format("CommitsNumber".GetLocalizedResource(), 0);
		public string ExtendedStatusInfo
		{
			get => _ExtendedStatusInfo;
			set => SetProperty(ref _ExtendedStatusInfo, value);
		}

		public bool ShowOpenInIDEButton
		{
			get
			{
				return DevToolsSettingsService.OpenInIDEOption == OpenInIDEOption.AllLocations ||
					   (DevToolsSettingsService.OpenInIDEOption == OpenInIDEOption.GitRepos && GitBranchDisplayName is not null);
			}
		}

		public ObservableCollection<BranchItem> Branches => _ShowLocals 
			? _localBranches 
			: _remoteBranches;

		public EventHandler<string>? CheckoutRequested;

		public ICommand NewBranchCommand { get; }

		public StatusBarViewModel()
		{
			NewBranchCommand = new AsyncRelayCommand(()
				=> GitHelpers.CreateNewBranchAsync(_gitRepositoryPath!, _localBranches[ACTIVE_BRANCH_INDEX].Name));

			DevToolsSettingsService.PropertyChanged += (s, e) =>
			{
				switch (e.PropertyName)
				{
					case nameof(DevToolsSettingsService.OpenInIDEOption):
						OnPropertyChanged(nameof(ShowOpenInIDEButton));
						break;
				}
			};
		}

		public void UpdateGitInfo(bool isGitRepository, string? repositoryPath, BranchItem? head)
		{
			GitBranchDisplayName =
				isGitRepository &&
				head is not null &&
				ContentPageContext.ShellPage is not null &&
				!ContentPageContext.ShellPage.InstanceViewModel.IsPageTypeSearchResults
					? head.Name
					: null;

			_gitRepositoryPath = repositoryPath;
			
			// Change ShowLocals value only if branches flyout is closed
			if (!IsBranchesFlyoutExpanded)
				ShowLocals = true;

			var behind = head is not null ? head.BehindBy ?? 0 : 0;
			var ahead = head is not null ? head.AheadBy ?? 0 : 0;

			ExtendedStatusInfo = string.Format("GitSyncStatusExtendedInfo".GetLocalizedResource(), ahead, behind);
			StatusInfo = $"{ahead} / {behind}";

			OnPropertyChanged(nameof(ShowOpenInIDEButton));
		}

		public async Task LoadBranches()
		{
			if (string.IsNullOrEmpty(_gitRepositoryPath))
				return;

			var branches = await GitHelpers.GetBranchesNames(_gitRepositoryPath);

			_localBranches.Clear();
			_remoteBranches.Clear();

			foreach (var branch in branches)
			{
				if (branch.IsRemote)
					_remoteBranches.Add(branch);
				else
					_localBranches.Add(branch);
			}

			SelectedBranchIndex = ShowLocals ? ACTIVE_BRANCH_INDEX : -1;
		}

		public Task ExecuteDeleteBranch(string? branchName)
		{
			return GitHelpers.DeleteBranchAsync(_gitRepositoryPath, GitBranchDisplayName, branchName);
		}
	}
}
