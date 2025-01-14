// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Files.App.Helpers
{
	// TODO: Remove this class
	public static class UIFilesystemHelpers
	{
		public static async Task PasteItemAsync(string destinationPath, IShellPage associatedInstance)
		{
			FilesystemResult<DataPackageView> packageView = await FilesystemTasks.Wrap(() => Task.FromResult(Clipboard.GetContent()));
			if (packageView && packageView.Result is not null)
			{
				await associatedInstance.FilesystemHelpers.PerformOperationTypeAsync(packageView.Result.RequestedOperation, packageView, destinationPath, false, true);
				associatedInstance.SlimContentPage?.ItemManipulationModel?.RefreshItemsOpacity();
				await associatedInstance.RefreshIfNoWatcherExistsAsync();
			}
		}

		public static async Task PasteItemAsShortcutAsync(string destinationPath, IShellPage associatedInstance)
		{
			FilesystemResult<DataPackageView> packageView = await FilesystemTasks.Wrap(() => Task.FromResult(Clipboard.GetContent()));
			if (packageView.Result.Contains(StandardDataFormats.StorageItems))
			{
				var items = await packageView.Result.GetStorageItemsAsync();
				await Task.WhenAll(items.Select(async item =>
				{
					var fileName = FilesystemHelpers.GetShortcutNamingPreference(item.Name);
					var filePath = Path.Combine(destinationPath ?? string.Empty, fileName);

					if (!await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, item.Path))
						await HandleShortcutCannotBeCreated(fileName, item.Path);
				}));
			}

			if (associatedInstance is not null)
				await associatedInstance.RefreshIfNoWatcherExistsAsync();
		}

		public static async Task<bool> RenameFileItemAsync(ListedItem item, string newName, IShellPage associatedInstance, bool showExtensionDialog = true)
		{
			if (item is AlternateStreamItem ads) // For alternate streams Name is not a substring ItemNameRaw
			{
				newName = item.ItemNameRaw.Replace(
					item.Name.Substring(item.Name.LastIndexOf(':') + 1),
					newName.Substring(newName.LastIndexOf(':') + 1),
					StringComparison.Ordinal);
				newName = $"{ads.MainStreamName}:{newName}";
			}
			else if (string.IsNullOrEmpty(item.Name))
			{
				newName = string.Concat(newName, item.FileExtension);
			}
			else
			{
				newName = item.ItemNameRaw.Replace(item.Name, newName, StringComparison.Ordinal);
			}

			if (item.ItemNameRaw == newName || string.IsNullOrEmpty(newName))
				return true;

			FilesystemItemType itemType = (item.PrimaryItemAttribute == StorageItemTypes.Folder) ? FilesystemItemType.Directory : FilesystemItemType.File;

			ReturnResult renamed = await associatedInstance.FilesystemHelpers.RenameAsync(StorageHelpers.FromPathAndType(item.ItemPath, itemType), newName, NameCollisionOption.FailIfExists, true, showExtensionDialog);

			if (renamed == ReturnResult.Success)
			{
				associatedInstance.ToolbarViewModel.CanGoForward = false;
				await associatedInstance.RefreshIfNoWatcherExistsAsync();
				return true;
			}

			return false;
		}

		public static async Task CreateFileFromDialogResultTypeAsync(AddItemDialogItemType itemType, ShellNewEntry? itemInfo, IShellPage associatedInstance)
		{
			await CreateFileFromDialogResultTypeForResult(itemType, itemInfo, associatedInstance);
			await associatedInstance.RefreshIfNoWatcherExistsAsync();
		}

		private static async Task<IStorageItem?> CreateFileFromDialogResultTypeForResult(AddItemDialogItemType itemType, ShellNewEntry? itemInfo, IShellPage associatedInstance)
		{
			string? currentPath = null;

			if (associatedInstance.SlimContentPage is not null)
			{
				currentPath = associatedInstance.ShellViewModel.WorkingDirectory;
				if (App.LibraryManager.TryGetLibrary(currentPath, out var library) &&
					!library.IsEmpty &&
					library.Folders.Count == 1) // TODO: handle libraries with multiple folders
				{
					currentPath = library.Folders.First();
				}
			}

			// Skip rename dialog when ShellNewEntry has a Command (e.g. ".accdb", ".gdoc")
			string? userInput = null;
			if (itemType != AddItemDialogItemType.File || itemInfo?.Command is null)
			{
				DynamicDialog dialog = DynamicDialogFactory.GetFor_CreateItemDialog(itemType.ToString().GetLocalizedResource().ToLower());
				await dialog.TryShowAsync(); // Show rename dialog

				if (dialog.DynamicResult != DynamicDialogResult.Primary)
					return null;

				userInput = dialog.ViewModel.AdditionalData as string;
			}

			// Create file based on dialog result
			(ReturnResult Status, IStorageItem Item) created = (ReturnResult.Failed, null);
			switch (itemType)
			{
				case AddItemDialogItemType.Folder:
					userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : "NewFolder".GetLocalizedResource();
					created = await associatedInstance.FilesystemHelpers.CreateAsync(
						StorageHelpers.FromPathAndType(PathNormalization.Combine(currentPath, userInput), FilesystemItemType.Directory),
						true);
					break;

				case AddItemDialogItemType.File:
					userInput = !string.IsNullOrWhiteSpace(userInput) ? userInput : itemInfo?.Name ?? "NewFile".GetLocalizedResource();
					created = await associatedInstance.FilesystemHelpers.CreateAsync(
						StorageHelpers.FromPathAndType(PathNormalization.Combine(currentPath, userInput + itemInfo?.Extension), FilesystemItemType.File),
						true);
					break;
			}

			// Add newly created item to recent files list
			if (created.Status == ReturnResult.Success && created.Item?.Path is not null)
			{
				IWindowsRecentItemsService windowsRecentItemsService = Ioc.Default.GetRequiredService<IWindowsRecentItemsService>();
				windowsRecentItemsService.Add(created.Item.Path);
			}
			else if (created.Status == ReturnResult.AccessUnauthorized)
			{
				await DialogDisplayHelper.ShowDialogAsync
				(
					"AccessDenied".GetLocalizedResource(),
					"AccessDeniedCreateDialog/Text".GetLocalizedResource()
				);
			}

			return created.Item;
		}

		public static async Task CreateFolderWithSelectionAsync(IShellPage associatedInstance)
		{
			try
			{
				var items = associatedInstance.SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
					item.ItemPath,
					item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
				var folder = await CreateFileFromDialogResultTypeForResult(AddItemDialogItemType.Folder, null, associatedInstance);
				if (folder is null)
					return;

				await associatedInstance.FilesystemHelpers.MoveItemsAsync(items, items.Select(x => PathNormalization.Combine(folder.Path, x.Name)), false, true);
				await associatedInstance.RefreshIfNoWatcherExistsAsync();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, null);
			}
		}

		/// <summary>
		/// Set a single file or folder to hidden or unhidden and refresh the
		/// view after setting the flag
		/// </summary>
		/// <param name="item"></param>
		/// <param name="isHidden"></param>
		public static void SetHiddenAttributeItem(ListedItem item, bool isHidden, ItemManipulationModel itemManipulationModel)
		{
			item.IsHiddenItem = isHidden;
			itemManipulationModel.RefreshItemsOpacity();
		}

		public static async Task CreateShortcutAsync(IShellPage? associatedInstance, IReadOnlyList<ListedItem> selectedItems)
		{
			var currentPath = associatedInstance?.ShellViewModel.WorkingDirectory;

			if (App.LibraryManager.TryGetLibrary(currentPath ?? string.Empty, out var library) && !library.IsEmpty)
				currentPath = library.DefaultSaveFolder;

			foreach (ListedItem selectedItem in selectedItems)
			{
				var fileName = FilesystemHelpers.GetShortcutNamingPreference(selectedItem.Name);
				var filePath = Path.Combine(currentPath ?? string.Empty, fileName);

				if (!await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, selectedItem.ItemPath))
					await HandleShortcutCannotBeCreated(fileName, selectedItem.ItemPath);
			}

			if (associatedInstance is not null)
				await associatedInstance.RefreshIfNoWatcherExistsAsync();
		}

		public static async Task CreateShortcutFromDialogAsync(IShellPage associatedInstance)
		{
			var currentPath = associatedInstance.ShellViewModel.WorkingDirectory;
			if (App.LibraryManager.TryGetLibrary(currentPath, out var library) &&
				!library.IsEmpty)
			{
				currentPath = library.DefaultSaveFolder;
			}

			var viewModel = new CreateShortcutDialogViewModel(currentPath);
			var dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			var result = await dialogService.ShowDialogAsync(viewModel);

			if (result != DialogResult.Primary || viewModel.ShortcutCreatedSuccessfully)
				return;

			await HandleShortcutCannotBeCreated(viewModel.ShortcutCompleteName, viewModel.FullPath, viewModel.Arguments);

			await associatedInstance.RefreshIfNoWatcherExistsAsync();
		}

		public static async Task<bool> HandleShortcutCannotBeCreated(string shortcutName, string destinationPath, string arguments = "")
		{
			var result = await DialogDisplayHelper.ShowDialogAsync
			(
				"CannotCreateShortcutDialogTitle".ToLocalized(),
				"CannotCreateShortcutDialogMessage".ToLocalized(),
				"Create".ToLocalized(),
				"Cancel".ToLocalized()
			);
			if (!result)
				return false;

			var shortcutPath = Path.Combine(Constants.UserEnvironmentPaths.DesktopPath, shortcutName);

			return await FileOperationsHelpers.CreateOrUpdateLinkAsync(shortcutPath, destinationPath, arguments);
		}

		/// <summary>
		/// Updates ListedItem properties for a shortcut
		/// </summary>
		public static void UpdateShortcutItemProperties(IShortcutItem item, string targetPath, string arguments, string workingDir, bool runAsAdmin, SHOW_WINDOW_CMD showWindowCommand)
		{
			item.TargetPath = Environment.ExpandEnvironmentVariables(targetPath);
			item.Arguments = arguments;
			item.WorkingDirectory = workingDir;
			item.RunAsAdmin = runAsAdmin;
			item.ShowWindowCommand = showWindowCommand;
		}

		public async static Task<StorageCredential> RequestPassword(IPasswordProtectedItem sender)
		{
			var path = ((IStorageItem)sender).Path;
			var isFtp = FtpHelpers.IsFtpPath(path);

			var credentialDialogViewModel = new CredentialDialogViewModel() { CanBeAnonymous = isFtp, PasswordOnly = !isFtp };
			IDialogService dialogService = Ioc.Default.GetRequiredService<IDialogService>();
			var dialogResult = await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
				dialogService.ShowDialogAsync(credentialDialogViewModel));

			if (dialogResult != DialogResult.Primary || credentialDialogViewModel.IsAnonymous)
				return new();

			// Can't do more than that to mitigate immutability of strings. Perhaps convert DisposableArray to SecureString immediately?
			var credentials = new StorageCredential(credentialDialogViewModel.UserName, Encoding.UTF8.GetString(credentialDialogViewModel.Password));
			credentialDialogViewModel.Password?.Dispose();

			if (isFtp)
			{
				var host = FtpHelpers.GetFtpHost(path);
				FtpManager.Credentials[host] = new NetworkCredential(credentials.UserName, credentials.SecurePassword);
			}

			return credentials;
		}
	}
}