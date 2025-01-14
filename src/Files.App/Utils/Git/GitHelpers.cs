// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Sentry;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;

namespace Files.App.Utils.Git
{
	internal static class GitHelpers
	{
		private const string GIT_RESOURCE_NAME = "Files:https://github.com";

		private const string GIT_RESOURCE_USERNAME = "Personal Access Token";

		private const string CLIENT_ID_SECRET = Constants.AutomatedWorkflowInjectionKeys.GitHubClientId;

		private const int END_OF_ORIGIN_PREFIX = 7;

		private const int MAX_NUMBER_OF_BRANCHES = 30;

		private static readonly ILogger _logger = Ioc.Default.GetRequiredService<ILogger<App>>();

		private static readonly IDialogService _dialogService = Ioc.Default.GetRequiredService<IDialogService>();

		private static readonly FetchOptions _fetchOptions = new()
		{
			Prune = true
		};

		private static readonly PullOptions _pullOptions = new();

		private static readonly string _clientId = AppLifecycleHelper.AppEnvironment is AppEnvironment.Dev
				? string.Empty
				: CLIENT_ID_SECRET;

		private static readonly SemaphoreSlim GitOperationSemaphore = new SemaphoreSlim(1, 1);

		private static bool _IsExecutingGitAction;
		public static bool IsExecutingGitAction
		{
			get => _IsExecutingGitAction;
			private set
			{
				if (_IsExecutingGitAction != value)
				{
					_IsExecutingGitAction = value;
					IsExecutingGitActionChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(IsExecutingGitAction)));
				}
			}
		}

		public static event PropertyChangedEventHandler? IsExecutingGitActionChanged;

		public static event EventHandler? GitFetchCompleted;

		public static string? GetGitRepositoryPath(string? path, string root)
		{
			if (string.IsNullOrEmpty(root))
				return null;

			if (root.EndsWith('\\'))
				root = root.Substring(0, root.Length - 1);

			if (string.IsNullOrWhiteSpace(path) ||
				path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
				path.Equals("Home", StringComparison.OrdinalIgnoreCase) ||
				ShellStorageFolder.IsShellPath(path))
			{
				return null;
			}

			try
			{
				if (IsRepoValid(path))
					return path;
				else
				{
					var parentDir = PathNormalization.GetParentDir(path);
					if (parentDir == path)
						return null;
					else
						return GetGitRepositoryPath(parentDir, root);
				}
			}
			catch (Exception ex) when (ex is LibGit2SharpException or EncoderFallbackException)
			{
				_logger.LogWarning(ex.Message);

				return null;
			}
		}

		public static string GetOriginRepositoryName(string? path)
		{
			if (string.IsNullOrWhiteSpace(path) || !IsRepoValid(path))
				return string.Empty;

			using var repository = new Repository(path);
			var repositoryUrl = repository.Network.Remotes.FirstOrDefault()?.Url;

			if (string.IsNullOrEmpty(repositoryUrl))
				return string.Empty;

			var repositoryName = repositoryUrl.Split('/').Last();
			return repositoryName[..repositoryName.LastIndexOf(".git")];
		}

		public static async Task<BranchItem[]> GetBranchesNames(string? path)
		{
			if (string.IsNullOrWhiteSpace(path) || !IsRepoValid(path))
				return [];

			var (result, returnValue) = await DoGitOperationAsync<(GitOperationResult, BranchItem[])>(() =>
			{
				var branches = Array.Empty<BranchItem>();
				var result = GitOperationResult.Success;
				try
				{
					using var repository = new Repository(path);

					branches = GetValidBranches(repository.Branches)
						.Where(b => !b.IsRemote || b.RemoteName == "origin")
						.OrderByDescending(b => b.Tip?.Committer.When)
						.Take(MAX_NUMBER_OF_BRANCHES)
						.OrderByDescending(b => b.IsCurrentRepositoryHead)
						.Select(b => new BranchItem(b.FriendlyName, b.IsCurrentRepositoryHead, b.IsRemote, TryGetTrackingDetails(b)?.AheadBy ?? 0, TryGetTrackingDetails(b)?.BehindBy ?? 0))
						.ToArray();
				}
				catch (Exception)
				{
					result = GitOperationResult.GenericError;
				}

				return (result, branches);
			});

			return returnValue;
		}

		public static async Task<BranchItem?> GetRepositoryHead(string? path)
		{
			if (string.IsNullOrWhiteSpace(path) || !IsRepoValid(path))
				return null;

			var (_, returnValue) = await DoGitOperationAsync<(GitOperationResult, BranchItem?)>(() =>
			{
				BranchItem? head = null;
				try
				{
					using var repository = new Repository(path);
					var branch = GetValidBranches(repository.Branches).FirstOrDefault(b => b.IsCurrentRepositoryHead);
					if (branch is not null)
						head = new BranchItem(
							branch.FriendlyName,
							branch.IsCurrentRepositoryHead,
							branch.IsRemote,
							TryGetTrackingDetails(branch)?.AheadBy ?? 0,
							TryGetTrackingDetails(branch)?.BehindBy ?? 0
						);
				}
				catch
				{
					return (GitOperationResult.GenericError, head);
				}

				return (GitOperationResult.Success, head);
			}, true);

			return returnValue;
		}

		public static async Task<bool> Checkout(string? repositoryPath, string? branch)
		{
			// Re-enable when Metris feature is available again
			// SentrySdk.Metrics.Increment("Triggered git checkout");

			if (string.IsNullOrWhiteSpace(repositoryPath) || !IsRepoValid(repositoryPath))
				return false;

			using var repository = new Repository(repositoryPath);
			var checkoutBranch = repository.Branches[branch];
			if (checkoutBranch is null)
				return false;

			var options = new CheckoutOptions();
			var isBringingChanges = false;

			IsExecutingGitAction = true;

			if (repository.RetrieveStatus().IsDirty)
			{
				var dialog = DynamicDialogFactory.GetFor_GitCheckoutConflicts(checkoutBranch.FriendlyName, repository.Head.FriendlyName);
				await dialog.ShowAsync();

				var resolveConflictOption = (GitCheckoutOptions)dialog.ViewModel.AdditionalData;

				switch (resolveConflictOption)
				{
					case GitCheckoutOptions.None:
						return false;
					case GitCheckoutOptions.DiscardChanges:
						options.CheckoutModifiers = CheckoutModifiers.Force;
						break;
					case GitCheckoutOptions.BringChanges:
					case GitCheckoutOptions.StashChanges:
						var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
						if (signature is null)
							return false;

						repository.Stashes.Add(signature);

						isBringingChanges = resolveConflictOption is GitCheckoutOptions.BringChanges;
						break;
				}
			}

			var result = await DoGitOperationAsync<GitOperationResult>(() =>
			{
				try
				{
					if (checkoutBranch.IsRemote)
						CheckoutRemoteBranch(repository, checkoutBranch);
					else
						LibGit2Sharp.Commands.Checkout(repository, checkoutBranch, options);

					if (isBringingChanges)
					{
						var lastStashIndex = repository.Stashes.Count() - 1;
						repository.Stashes.Pop(lastStashIndex, new StashApplyOptions());
					}
				}
				catch (Exception)
				{
					return GitOperationResult.GenericError;
				}

				return GitOperationResult.Success;
			});

			IsExecutingGitAction = false;

			return result is GitOperationResult.Success;
		}

		public static async Task CreateNewBranchAsync(string repositoryPath, string activeBranch)
		{
			// Re-enable when Metris feature is available again
			// SentrySdk.Metrics.Increment("Triggered create git branch");

			var viewModel = new AddBranchDialogViewModel(repositoryPath, activeBranch);
			var loadBranchesTask = viewModel.LoadBranches();
			var dialog = _dialogService.GetDialog(viewModel);

			await loadBranchesTask;
			var result = await dialog.TryShowAsync();

			if (result != DialogResult.Primary)
				return;

			using var repository = new Repository(repositoryPath);

			IsExecutingGitAction = true;

			if (repository.Head.FriendlyName.Equals(viewModel.NewBranchName) ||
				await Checkout(repositoryPath, viewModel.BasedOn))
			{
				repository.CreateBranch(viewModel.NewBranchName);

				if (viewModel.Checkout)
					await Checkout(repositoryPath, viewModel.NewBranchName);
			}

			IsExecutingGitAction = false;
		}

		public static async Task DeleteBranchAsync(string? repositoryPath, string? activeBranch, string? branchToDelete)
		{
			// Re-enable when Metris feature is available again
			// SentrySdk.Metrics.Increment("Triggered delete git branch");

			if (string.IsNullOrWhiteSpace(repositoryPath) ||
				string.IsNullOrWhiteSpace(activeBranch) ||
				string.IsNullOrWhiteSpace(branchToDelete) ||
				activeBranch.Equals(branchToDelete, StringComparison.OrdinalIgnoreCase) ||
				!IsRepoValid(repositoryPath))
			{
				return;
			}

			var dialog = DynamicDialogFactory.GetFor_DeleteGitBranchConfirmation(branchToDelete);
			await dialog.TryShowAsync();
			if (!(dialog.ViewModel.AdditionalData as bool? ?? false))
				return;

			IsExecutingGitAction = true;

			await DoGitOperationAsync<GitOperationResult>(() =>
			{
				try
				{
					using var repository = new Repository(repositoryPath);
					repository.Branches.Remove(branchToDelete);
				}
				catch (Exception)
				{
					return GitOperationResult.GenericError;
				}

				return GitOperationResult.Success;
			});

			IsExecutingGitAction = false;
		}


		public static bool ValidateBranchNameForRepository(string branchName, string repositoryPath)
		{
			if (string.IsNullOrEmpty(branchName) || !IsRepoValid(repositoryPath))
				return false;

			var nameValidator = RegexHelpers.GitBranchName();
			if (!nameValidator.IsMatch(branchName))
				return false;

			using var repository = new Repository(repositoryPath);
			return !repository.Branches.Any(branch =>
				branch.FriendlyName.Equals(branchName, StringComparison.OrdinalIgnoreCase));
		}

		public static async void FetchOrigin(string? repositoryPath)
		{
			if (string.IsNullOrWhiteSpace(repositoryPath))
				return;

			using var repository = new Repository(repositoryPath);
			var signature = repository.Config.BuildSignature(DateTimeOffset.Now);

			var token = CredentialsHelpers.GetPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
			if (signature is not null && !string.IsNullOrWhiteSpace(token))
			{
				_fetchOptions.CredentialsProvider = (url, user, cred)
					=> new UsernamePasswordCredentials
					{
						Username = signature.Name,
						Password = token
					};
			}

			MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
			{
				IsExecutingGitAction = true;
			});

			await DoGitOperationAsync<GitOperationResult>(() =>
			{
				var result = GitOperationResult.Success;
				try
				{
					foreach (var remote in repository.Network.Remotes)
					{
						LibGit2Sharp.Commands.Fetch(
							repository,
							remote.Name,
							remote.FetchRefSpecs.Select(rs => rs.Specification),
							_fetchOptions,
							"git fetch updated a ref");
					}
				}
				catch (Exception ex)
				{
					result = IsAuthorizationException(ex)
						? GitOperationResult.AuthorizationError
						: GitOperationResult.GenericError;
				}

				return result;
			});

			MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
			{
				IsExecutingGitAction = false;
				GitFetchCompleted?.Invoke(null, EventArgs.Empty);
			});
		}

		public static async Task PullOriginAsync(string? repositoryPath)
		{
			if (string.IsNullOrWhiteSpace(repositoryPath))
				return;

			using var repository = new Repository(repositoryPath);
			var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
			if (signature is null)
				return;

			var token = CredentialsHelpers.GetPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
			if (!string.IsNullOrWhiteSpace(token))
			{
				_pullOptions.FetchOptions ??= _fetchOptions;
				_pullOptions.FetchOptions.CredentialsProvider = (url, user, cred)
					=> new UsernamePasswordCredentials
					{
						Username = signature.Name,
						Password = token
					};
			}

			MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
			{
				IsExecutingGitAction = true;
			});

			var result = await DoGitOperationAsync<GitOperationResult>(() =>
			{
				try
				{
					LibGit2Sharp.Commands.Pull(
						repository,
						signature,
						_pullOptions);
				}
				catch (Exception ex)
				{
					return IsAuthorizationException(ex)
						? GitOperationResult.AuthorizationError
						: GitOperationResult.GenericError;
				}

				return GitOperationResult.Success;
			});

			if (result is GitOperationResult.AuthorizationError)
			{
				await RequireGitAuthenticationAsync();
			}
			else if (result is GitOperationResult.GenericError)
			{
				var viewModel = new DynamicDialogViewModel()
				{
					TitleText = "GitError".GetLocalizedResource(),
					SubtitleText = "PullTimeoutError".GetLocalizedResource(),
					CloseButtonText = "Close".GetLocalizedResource(),
					DynamicButtons = DynamicDialogButtons.Cancel
				};
				var dialog = new DynamicDialog(viewModel);
				await dialog.TryShowAsync();
			}

			MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
			{
				IsExecutingGitAction = false;
			});
		}

		public static async Task PushToOriginAsync(string? repositoryPath, string? branchName)
		{
			if (string.IsNullOrWhiteSpace(repositoryPath) || string.IsNullOrWhiteSpace(branchName))
				return;

			using var repository = new Repository(repositoryPath);
			var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
			if (signature is null)
				return;

			var token = CredentialsHelpers.GetPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
			if (string.IsNullOrWhiteSpace(token))
			{
				await RequireGitAuthenticationAsync();
				token = CredentialsHelpers.GetPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
			}

			var options = new PushOptions()
			{
				CredentialsProvider = (url, user, cred)
					=> new UsernamePasswordCredentials
					{
						Username = signature.Name,
						Password = token
					}
			};

			MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
			{
				IsExecutingGitAction = true;
			});

			try
			{
				var branch = repository.Branches[branchName];
				if (!branch.IsTracking)
				{
					var origin = repository.Network.Remotes["origin"];
					repository.Branches.Update(
						branch,
						b => b.Remote = origin.Name,
						b => b.UpstreamBranch = branch.CanonicalName);
				}

				var result = await DoGitOperationAsync<GitOperationResult>(() =>
				{
					try
					{
						repository.Network.Push(branch, options);
					}
					catch (Exception ex)
					{
						return IsAuthorizationException(ex)
							? GitOperationResult.AuthorizationError
							: GitOperationResult.GenericError;
					}

					return GitOperationResult.Success;
				});

				if (result is GitOperationResult.AuthorizationError)
					await RequireGitAuthenticationAsync();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex.Message);
			}

			MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
			{
				IsExecutingGitAction = false;
			});
		}

		public static async Task RequireGitAuthenticationAsync()
		{
			var pending = true;
			var client = new HttpClient();
			client.DefaultRequestHeaders.Add("Accept", "application/json");
			client.DefaultRequestHeaders.Add("User-Agent", "Files App");

			JsonDocument? codeJsonContent;
			try
			{
				var codeResponse = await client.PostAsync(
					$"https://github.com/login/device/code?client_id={_clientId}&scope=repo",
					new StringContent(""));

				if (!codeResponse.IsSuccessStatusCode)
				{
					await DynamicDialogFactory.GetFor_GitHubConnectionError().TryShowAsync();
					return;
				}

				codeJsonContent = await codeResponse.Content.ReadFromJsonAsync<JsonDocument>();
				if (codeJsonContent is null)
				{
					await DynamicDialogFactory.GetFor_GitHubConnectionError().TryShowAsync();
					return;
				}
			}
			catch
			{
				await DynamicDialogFactory.GetFor_GitHubConnectionError().TryShowAsync();
				return;
			}

			var userCode = codeJsonContent.RootElement.GetProperty("user_code").GetString() ?? string.Empty;
			var deviceCode = codeJsonContent.RootElement.GetProperty("device_code").GetString() ?? string.Empty;
			var interval = codeJsonContent.RootElement.GetProperty("interval").GetInt32();
			var expiresIn = codeJsonContent.RootElement.GetProperty("expires_in").GetInt32();

			var loginCTS = new CancellationTokenSource();
			var viewModel = new GitHubLoginDialogViewModel(userCode, "ConnectGitHubDescription".GetLocalizedResource(), loginCTS);

			var dialog = _dialogService.GetDialog(viewModel);
			var loginDialogTask = dialog.TryShowAsync();

			while (!loginCTS.Token.IsCancellationRequested && pending && expiresIn > 0)
			{
				try
				{
					var loginResponse = await client.PostAsync(
					$"https://github.com/login/oauth/access_token?client_id={_clientId}&device_code={deviceCode}&grant_type=urn:ietf:params:oauth:grant-type:device_code",
					new StringContent(""));

					expiresIn -= interval;

					if (!loginResponse.IsSuccessStatusCode)
					{
						dialog.Hide();
						break;
					}

					var loginJsonContent = await loginResponse.Content.ReadFromJsonAsync<JsonDocument>();
					if (loginJsonContent is null)
					{
						dialog.Hide();
						break;
					}

					if (loginJsonContent.RootElement.TryGetProperty("error", out var error))
					{
						if (error.GetString() == "authorization_pending")
						{
							await Task.Delay(TimeSpan.FromSeconds(interval));
							continue;
						}

						dialog.Hide();
						break;
					}

					var token = loginJsonContent.RootElement.GetProperty("access_token").GetString();
					if (token is null)
						continue;

					pending = false;

					CredentialsHelpers.SavePassword(
						GIT_RESOURCE_NAME,
						GIT_RESOURCE_USERNAME,
						token);

					viewModel.Subtitle = "AuthorizationSucceded".GetLocalizedResource();
					viewModel.LoginConfirmed = true;
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex.Message);
					dialog.Hide();
					break;
				}
			}

			await loginDialogTask;
		}

		public static bool IsRepositoryEx(string path, out string repoRootPath)
		{
			repoRootPath = path;

			var rootPath = SystemIO.Path.GetPathRoot(path);
			if (string.IsNullOrEmpty(rootPath))
				return false;

			var repositoryRootPath = GetGitRepositoryPath(path, rootPath);
			if (string.IsNullOrEmpty(repositoryRootPath))
				return false;

			if (IsRepoValid(repositoryRootPath))
			{
				repoRootPath = repositoryRootPath;
				return true;
			}

			return false;
		}

		public static GitItemModel GetGitInformationForItem(Repository repository, string path, bool getStatus = true, bool getCommit = true)
		{
			var rootRepoPath = repository.Info.WorkingDirectory;
			var relativePath = path.Substring(rootRepoPath.Length).Replace('\\', '/');

			Commit? commit = null;
			if (getCommit)
			{
				commit = GetLastCommitForFile(repository, relativePath);
				//var commit = repository.Commits.QueryBy(relativePath).FirstOrDefault()?.Commit; // Considers renames but slow
			}

			ChangeKind? changeKind = null;
			string? changeKindHumanized = null;
			if (getStatus)
			{
				changeKind = ChangeKind.Unmodified;
				//foreach (TreeEntryChanges c in repository.Diff.Compare<TreeChanges>())
				foreach (TreeEntryChanges c in repository.Diff.Compare<TreeChanges>(repository.Commits.FirstOrDefault()?.Tree, DiffTargets.Index | DiffTargets.WorkingDirectory))
				{
					if (c.Path.StartsWith(relativePath))
					{
						changeKind = c.Status;
						break;
					}
				}

				if (changeKind is not ChangeKind.Ignored)
				{
					changeKindHumanized = changeKind switch
					{
						ChangeKind.Added => "Added".GetLocalizedResource(),
						ChangeKind.Deleted => "Deleted".GetLocalizedResource(),
						ChangeKind.Modified => "Modified".GetLocalizedResource(),
						ChangeKind.Untracked => "Untracked".GetLocalizedResource(),
						_ => null,
					};
				}
			}

			var gitItemModel = new GitItemModel()
			{
				Status = changeKind,
				StatusHumanized = changeKindHumanized,
				LastCommit = commit,
				Path = relativePath,
			};

			return gitItemModel;
		}

		// Remove saved credentails
		public static void RemoveSavedCredentials()
		{
			CredentialsHelpers.DeleteSavedPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
		}

		// Get saved credentails
		public static string GetSavedCredentials()
		{
			return CredentialsHelpers.GetPassword(GIT_RESOURCE_NAME, GIT_RESOURCE_USERNAME);
		}

		public static async Task InitializeRepositoryAsync(string? path)
		{
			if (string.IsNullOrWhiteSpace(path))
				return;

			try
			{
				Repository.Init(path);
			}
			catch (LibGit2SharpException ex)
			{
				_logger.LogWarning(ex.Message);
				await DynamicDialogFactory.GetFor_GitCannotInitializeqRepositoryHere().TryShowAsync();
			}
		}

		private static bool IsRepoValid(string path)
		{
			return SafetyExtensions.IgnoreExceptions(() => Repository.IsValid(path));
		}

		private static IEnumerable<Branch> GetValidBranches(BranchCollection branches)
		{
			foreach (var branch in branches)
			{
				try
				{
					var throwIfInvalid = branch.IsCurrentRepositoryHead;
				}
				catch (LibGit2SharpException)
				{
					continue;
				}

				yield return branch;
			}
		}

		private static BranchTrackingDetails? TryGetTrackingDetails(Branch branch)
		{
			try
			{
				return branch.TrackingDetails;
			}
			catch (LibGit2SharpException)
			{
				return null;
			}
		}

		private static Commit? GetLastCommitForFile(Repository repository, string currentPath)
		{
			foreach (var currentCommit in repository.Commits)
			{
				var currentTreeEntry = currentCommit.Tree[currentPath];
				if (currentTreeEntry == null)
					return null;

				var parentCount = currentCommit.Parents.Take(2).Count();
				if (parentCount == 0)
				{
					return currentCommit;
				}
				else if (parentCount == 1)
				{
					var parentCommit = currentCommit.Parents.Single();

					// Does not consider renames
					var parentPath = currentPath;

					var parentTreeEntry = parentCommit.Tree[parentPath];

					if (parentTreeEntry == null ||
						parentTreeEntry.Target.Id != currentTreeEntry.Target.Id ||
						parentPath != currentPath)
					{
						return currentCommit;
					}
				}
			}

			return null;
		}

		private static void CheckoutRemoteBranch(Repository repository, Branch branch)
		{
			var uniqueName = branch.FriendlyName.Substring(END_OF_ORIGIN_PREFIX);

			// TODO: This is a temp fix to avoid an issue where Files would create many branches in a loop
			if (repository.Branches.Any(b => !b.IsRemote && b.FriendlyName == uniqueName))
				return;

			//var discriminator = 0;
			//while (repository.Branches.Any(b => !b.IsRemote && b.FriendlyName == uniqueName))
			//	uniqueName = $"{branch.FriendlyName}_{++discriminator}";

			var newBranch = repository.CreateBranch(uniqueName, branch.Tip);
			repository.Branches.Update(newBranch, b => b.TrackedBranch = branch.CanonicalName);

			LibGit2Sharp.Commands.Checkout(repository, newBranch);
		}

		private static bool IsAuthorizationException(Exception ex)
		{
			return
				ex.Message.Contains("status code: 401", StringComparison.OrdinalIgnoreCase) ||
				ex.Message.Contains("authentication replays", StringComparison.OrdinalIgnoreCase);
		}

		private static async Task<T?> DoGitOperationAsync<T>(Func<object> payload, bool useSemaphore = false)
		{
			if (useSemaphore)
				await GitOperationSemaphore.WaitAsync();
			else
				await Task.Yield();

			try
			{
				return (T)payload();
			}
			finally
			{
				if (useSemaphore)
					GitOperationSemaphore.Release();
			}
		}
	}
}
