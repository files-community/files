// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Files.App.Helpers
{
	public static class DynamicDialogFactory
	{
		public static readonly SolidColorBrush _transparentBrush = new SolidColorBrush(Colors.Transparent);

		public static DynamicDialog GetFor_PropertySaveErrorDialog()
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "PropertySaveErrorDialog/Title".GetLocalizedResource(),
				SubtitleText = "PropertySaveErrorMessage/Text".GetLocalizedResource(), // We can use subtitle here as our content
				PrimaryButtonText = "Retry".GetLocalizedResource(),
				SecondaryButtonText = "PropertySaveErrorDialog/SecondaryButtonText".GetLocalizedResource(),
				CloseButtonText = "Cancel".GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary | DynamicDialogButtons.Cancel
			});
			return dialog;
		}

		public static DynamicDialog GetFor_ConsentDialog()
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "WelcomeDialog/Title".GetLocalizedResource(),
				SubtitleText = "WelcomeDialogTextBlock/Text".GetLocalizedResource(), // We can use subtitle here as our content
				PrimaryButtonText = "WelcomeDialog/PrimaryButtonText".GetLocalizedResource(),
				PrimaryButtonAction = async (vm, e) => await Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-broadfilesystemaccess")),
				DynamicButtons = DynamicDialogButtons.Primary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_ShortcutNotFound(string targetPath)
		{
			DynamicDialog dialog = new(new DynamicDialogViewModel
			{
				TitleText = "ShortcutCannotBeOpened".GetLocalizedResource(),
				SubtitleText = string.Format("DeleteShortcutDescription".GetLocalizedResource(), targetPath),
				PrimaryButtonText = "Delete".GetLocalizedResource(),
				SecondaryButtonText = "No".GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_CreateItemDialog(string itemType)
		{
			DynamicDialog? dialog = null;
			TextBox inputText = new()
			{
				PlaceholderText = "EnterAnItemName".GetLocalizedResource()
			};

			TeachingTip warning = new()
			{
				Title = "InvalidFilename/Text".GetLocalizedResource(),
				PreferredPlacement = TeachingTipPlacementMode.Bottom,
				DataContext = new CreateItemDialogViewModel(),
			};

			warning.SetBinding(TeachingTip.TargetProperty, new Binding()
			{
				Source = inputText
			});
			warning.SetBinding(TeachingTip.IsOpenProperty, new Binding()
			{
				Mode = BindingMode.OneWay,
				Path = new PropertyPath("IsNameInvalid")
			});

			inputText.Resources.Add("InvalidNameWarningTip", warning);

			inputText.TextChanged += (textBox, args) =>
			{
				var isInputValid = FilesystemHelpers.IsValidForFilename(inputText.Text);
				((CreateItemDialogViewModel)warning.DataContext).IsNameInvalid = !string.IsNullOrEmpty(inputText.Text) && !isInputValid;
				dialog!.ViewModel.DynamicButtonsEnabled = isInputValid
														? DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
														: DynamicDialogButtons.Cancel;
				if (isInputValid)
					dialog.ViewModel.AdditionalData = inputText.Text;
			};

			inputText.Loaded += (s, e) =>
			{
				// dispatching to the ui thread fixes an issue where the primary dialog button would steal focus
				_ = inputText.DispatcherQueue.EnqueueOrInvokeAsync(() => inputText.Focus(FocusState.Programmatic));
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = string.Format("CreateNewItemTitle".GetLocalizedResource(), itemType),
				SubtitleText = null,
				DisplayControl = new Grid()
				{
					MinWidth = 300d,
					Children =
					{
						inputText
					}
				},
				PrimaryButtonAction = (vm, e) =>
				{
					vm.HideDialog(); // Rename successful
				},
				PrimaryButtonText = "Create".GetLocalizedResource(),
				CloseButtonText = "Cancel".GetLocalizedResource(),
				DynamicButtonsEnabled = DynamicDialogButtons.Cancel,
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
			});

			dialog.Closing += (s, e) =>
			{
				warning.IsOpen = false;
			};

			return dialog;
		}

		public static DynamicDialog GetFor_FileInUseDialog(List<Win32Process> lockingProcess = null)
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "FileInUseDialog/Title".GetLocalizedResource(),
				SubtitleText = lockingProcess.IsEmpty() ? "FileInUseDialog/Text".GetLocalizedResource() :
					string.Format("FileInUseByDialog/Text".GetLocalizedResource(), string.Join(", ", lockingProcess.Select(x => $"{x.AppName ?? x.Name} (PID: {x.Pid})"))),
				PrimaryButtonText = "OK",
				DynamicButtons = DynamicDialogButtons.Primary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_CredentialEntryDialog(string path)
		{
			string[] userAndPass = new string[3];
			DynamicDialog? dialog = null;

			TextBox inputUsername = new()
			{
				PlaceholderText = "CredentialDialogUserName/PlaceholderText".GetLocalizedResource()
			};

			PasswordBox inputPassword = new()
			{
				PlaceholderText = "Password".GetLocalizedResource()
			};

			CheckBox saveCreds = new()
			{
				Content = "NetworkAuthenticationSaveCheckbox".GetLocalizedResource()
			};

			inputUsername.TextChanged += (textBox, args) =>
			{
				userAndPass[0] = inputUsername.Text;
				dialog.ViewModel.AdditionalData = userAndPass;
			};

			inputPassword.PasswordChanged += (textBox, args) =>
			{
				userAndPass[1] = inputPassword.Password;
				dialog.ViewModel.AdditionalData = userAndPass;
			};

			saveCreds.Checked += (textBox, args) =>
			{
				userAndPass[2] = "y";
				dialog.ViewModel.AdditionalData = userAndPass;
			};

			saveCreds.Unchecked += (textBox, args) =>
			{
				userAndPass[2] = "n";
				dialog.ViewModel.AdditionalData = userAndPass;
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "NetworkAuthenticationDialogTitle".GetLocalizedResource(),
				PrimaryButtonText = "OK".GetLocalizedResource(),
				CloseButtonText = "Cancel".GetLocalizedResource(),
				SubtitleText = string.Format("NetworkAuthenticationDialogMessage".GetLocalizedResource(), path.Substring(2)),
				DisplayControl = new Grid()
				{
					MinWidth = 250d,
					Children =
					{
						new StackPanel()
						{
							Spacing = 10d,
							Children =
							{
								inputUsername,
								inputPassword,
								saveCreds
							}
						}
					}
				},
				CloseButtonAction = (vm, e) =>
				{
					dialog.ViewModel.AdditionalData = null;
					vm.HideDialog();
				}

			});

			return dialog;
		}

		public static DynamicDialog GetFor_GitCheckoutConflicts(string checkoutBranchName, string headBranchName)
		{
			DynamicDialog dialog = null!;

			var optionsListView = new ListView()
			{
				ItemsSource = new string[]
				{
					string.Format("BringChanges".GetLocalizedResource(), checkoutBranchName),
					string.Format("StashChanges".GetLocalizedResource(), headBranchName),
					"DiscardChanges".GetLocalizedResource()
				},
				SelectionMode = ListViewSelectionMode.Single
			};
			optionsListView.SelectedIndex = 0;

			optionsListView.SelectionChanged += (listView, args) =>
			{
				dialog.ViewModel.AdditionalData = (GitCheckoutOptions)optionsListView.SelectedIndex;
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "SwitchBranch".GetLocalizedResource(),
				PrimaryButtonText = "Switch".GetLocalizedResource(),
				CloseButtonText = "Cancel".GetLocalizedResource(),
				SubtitleText = "UncommittedChanges".GetLocalizedResource(),
				DisplayControl = new Grid()
				{
					MinWidth = 250d,
					Children =
					{
						optionsListView
					}
				},
				AdditionalData = GitCheckoutOptions.BringChanges,
				CloseButtonAction = (vm, e) =>
				{
					dialog.ViewModel.AdditionalData = GitCheckoutOptions.None;
					vm.HideDialog();
				}
			});

			return dialog;
		}

		public static DynamicDialog GetFor_GitHubConnectionError()
		{
			DynamicDialog dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "Error".GetLocalizedResource(),
				SubtitleText = "CannotReachGitHubError".GetLocalizedResource(),
				PrimaryButtonText = "Close".GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary
			});
			return dialog;
		}

		public static DynamicDialog GetFor_GitCannotInitializeqRepositoryHere()
		{
			return new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "Error".GetLocalizedResource(),
				SubtitleText = "CannotInitializeGitRepo".GetLocalizedResource(),
				PrimaryButtonText = "Close".GetLocalizedResource(),
				DynamicButtons = DynamicDialogButtons.Primary
			});
		}

		public static DynamicDialog GetFor_DeleteGitBranchConfirmation(string branchName)
		{
			DynamicDialog dialog = null!;
			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "GitDeleteBranch".GetLocalizedResource(),
				SubtitleText = string.Format("GitDeleteBranchSubtitle".GetLocalizedResource(), branchName),
				PrimaryButtonText = "OK".GetLocalizedResource(),
				CloseButtonText = "Cancel".GetLocalizedResource(),
				AdditionalData = true,
				CloseButtonAction = (vm, e) =>
				{
					dialog.ViewModel.AdditionalData = false;
					vm.HideDialog();
				}
			});

			return dialog;
		}

		public static DynamicDialog GetFor_RenameRequiresHigherPermissions(string path)
		{
			DynamicDialog dialog = null!;
			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = "ItemRenameFailed".GetLocalizedResource(),
				SubtitleText = string.Format("HigherPermissionsRequired".GetLocalizedResource(), path),
				PrimaryButtonText = "OK".GetLocalizedResource(),
				SecondaryButtonText = "EditPermissions".GetLocalizedResource(),
				SecondaryButtonAction = (vm, e) =>
				{
					var context = Ioc.Default.GetRequiredService<IContentPageContext>();
					var item = context.ShellPage?.ShellViewModel.FilesAndFolders.FirstOrDefault(li => li.ItemPath.Equals(path));

					if (context.ShellPage is not null && item is not null)
						FilePropertiesHelpers.OpenPropertiesWindow(item, context.ShellPage, PropertiesNavigationViewItemType.Security);
				}
			});

			return dialog;
		}

		public static DynamicDialog GetFor_CreateAlternateDataStreamDialog()
		{
			DynamicDialog? dialog = null;
			TextBox inputText = new()
			{
				PlaceholderText = Strings.EnterDataStreamName.GetLocalizedResource()
			};

			TeachingTip warning = new()
			{
				Title = Strings.InvalidFilename_Text.GetLocalizedResource(),
				PreferredPlacement = TeachingTipPlacementMode.Bottom,
				DataContext = new CreateItemDialogViewModel(),
			};

			warning.SetBinding(TeachingTip.TargetProperty, new Binding()
			{
				Source = inputText
			});
			warning.SetBinding(TeachingTip.IsOpenProperty, new Binding()
			{
				Mode = BindingMode.OneWay,
				Path = new PropertyPath("IsNameInvalid")
			});

			inputText.Resources.Add("InvalidNameWarningTip", warning);

			inputText.TextChanged += (textBox, args) =>
			{
				var isInputValid = FilesystemHelpers.IsValidForFilename(inputText.Text);
				((CreateItemDialogViewModel)warning.DataContext).IsNameInvalid = !string.IsNullOrEmpty(inputText.Text) && !isInputValid;
				dialog!.ViewModel.DynamicButtonsEnabled = isInputValid
														? DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
														: DynamicDialogButtons.Cancel;
				if (isInputValid)
					dialog.ViewModel.AdditionalData = inputText.Text;
			};

			inputText.Loaded += (s, e) =>
			{
				// dispatching to the ui thread fixes an issue where the primary dialog button would steal focus
				_ = inputText.DispatcherQueue.EnqueueOrInvokeAsync(() => inputText.Focus(FocusState.Programmatic));
			};

			dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = string.Format(Strings.CreateAlternateDataStream.GetLocalizedResource()),
				SubtitleText = null,
				DisplayControl = new Grid()
				{
					MinWidth = 300d,
					Children =
					{
						inputText
					}
				},
				PrimaryButtonAction = (vm, e) =>
				{
					vm.HideDialog();
				},
				PrimaryButtonText = Strings.Create.GetLocalizedResource(),
				CloseButtonText = Strings.Cancel.GetLocalizedResource(),
				DynamicButtonsEnabled = DynamicDialogButtons.Cancel,
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
			});

			dialog.Closing += (s, e) =>
			{
				warning.IsOpen = false;
			};

			return dialog;
		}
	}
}
