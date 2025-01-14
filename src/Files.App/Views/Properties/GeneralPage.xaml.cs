// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.IO;
using Windows.Storage;
using Windows.Win32;
using Files.App.Helpers;

namespace Files.App.Views.Properties
{
	public sealed partial class GeneralPage : BasePropertiesPage
	{
		private readonly DispatcherQueueTimer _updateDateDisplayTimer;
		public GeneralPage()
		{
			InitializeComponent();

			_updateDateDisplayTimer = DispatcherQueue.CreateTimer();
			_updateDateDisplayTimer.Interval = TimeSpan.FromSeconds(1);
			_updateDateDisplayTimer.Tick += UpdateDateDisplayTimer_Tick;
			_updateDateDisplayTimer.Start();
		}

		private void ItemFileName_GettingFocus(UIElement _, GettingFocusEventArgs e)
		{
			ItemFileName.Text = RegexHelpers.DriveLetter().Replace(ItemFileName.Text, string.Empty);
		}

		private void ItemFileName_LosingFocus(UIElement _, LosingFocusEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(ItemFileName.Text))
			{
				ItemFileName.Text = ViewModel.ItemName;
				return;
			}

			var match = RegexHelpers.DriveLetter().Match(ViewModel.OriginalItemName);
			if (match.Success)
				ItemFileName.Text += match.Value;
		}

		private void UpdateDateDisplayTimer_Tick(object sender, object e)
		{
			if (App.AppModel.PropertiesWindowCount == 0)
				return;

			// Reassign values to update date display
			ViewModel.ItemCreatedTimestampReal = ViewModel.ItemCreatedTimestampReal;
			ViewModel.ItemModifiedTimestampReal = ViewModel.ItemModifiedTimestampReal;
			ViewModel.ItemAccessedTimestampReal = ViewModel.ItemAccessedTimestampReal;
		}

		public override async Task<bool> SaveChangesAsync()
		{
			return BaseProperties switch
			{
				DriveProperties properties => SaveDrive(properties.Drive),
				LibraryProperties properties => await SaveLibraryAsync(properties.Library),
				CombinedProperties properties => await SaveCombinedAsync(properties.List),
				FileProperties properties => await SaveBaseAsync(properties.Item),
				FolderProperties properties => await SaveBaseAsync(properties.Item),
			};

			bool GetNewName(out string newName)
			{
				if (ItemFileName is not null)
				{
					ViewModel.ItemName = ItemFileName.Text; // Make sure Name is updated
					newName = ViewModel.ItemName;
					string oldName = ViewModel.OriginalItemName;
					return !string.IsNullOrWhiteSpace(newName) && newName != oldName;
				}
				newName = "";
				return false;
			}

			bool SaveDrive(DriveItem drive)
			{
				var fsVM = AppInstance.ShellViewModel;
				if (!GetNewName(out var newName) || fsVM is null)
					return false;

				newName = RegexHelpers.DriveLetter().Replace(newName, string.Empty); // Remove "(C:)" from the new label

				if (drive.Type == Data.Items.DriveType.Network)
					Win32Helper.SetNetworkDriveLabel(drive.DeviceID, newName);
				else
					Win32Helper.SetVolumeLabel(drive.Path, newName);

				_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					await drive.UpdateLabelAsync();
					await fsVM.SetWorkingDirectoryAsync(drive.Path);
				});
				return true;
			}

			async Task<bool> SaveLibraryAsync(LibraryItem library)
			{
				var fsVM = AppInstance.ShellViewModel;
				if (!GetNewName(out var newName) || fsVM is null || !App.LibraryManager.CanCreateLibrary(newName).result)
					return false;

				newName = $"{newName}{ShellLibraryItem.EXTENSION}";

				var file = new StorageFileWithPath(null, library.ItemPath);
				var renamed = await AppInstance!.FilesystemHelpers.RenameAsync(file, newName, NameCollisionOption.FailIfExists, false, false);
				if (renamed is ReturnResult.Success)
				{
					var newPath = Path.Combine(Path.GetDirectoryName(library.ItemPath)!, newName);
					_ = MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
					{
						await fsVM.SetWorkingDirectoryAsync(newPath);
					});
					return true;
				}

				return false;
			}

			async Task<bool> SaveCombinedAsync(IList<ListedItem> fileOrFolders)
			{
				// Handle the visibility attribute for multiple files
				var itemMM = AppInstance?.SlimContentPage?.ItemManipulationModel;
				if (itemMM is not null) // null on homepage
				{
					foreach (var fileOrFolder in fileOrFolders)
					{
						if (ViewModel.IsHiddenEditedValue is not null)
						{
							var isHiddenEditedValue = (bool)ViewModel.IsHiddenEditedValue;
							await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
								UIFilesystemHelpers.SetHiddenAttributeItem(fileOrFolder, isHiddenEditedValue, itemMM)
							);
							ViewModel.IsHidden = isHiddenEditedValue;
						}

						ViewModel.IsReadOnly = ViewModel.IsReadOnlyEditedValue;
						ViewModel.IsContentCompressed = ViewModel.IsContentCompressedEditedValue;

						if (ViewModel.IsAblumCoverModified)
						{
							MediaFileHelper.ChangeAlbumCover(fileOrFolder.ItemPath, ViewModel.ModifiedAlbumCover);

							await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
							{
								AppInstance?.ShellViewModel?.RefreshItems(null);
							});
						}
					}
				}
				return true;
			}

			async Task<bool> SaveBaseAsync(ListedItem item)
			{
				// Handle the visibility attribute for a single file
				var itemMM = AppInstance?.SlimContentPage?.ItemManipulationModel;
				if (itemMM is not null && ViewModel.IsHiddenEditedValue is not null) // null on homepage
				{
					await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
						UIFilesystemHelpers.SetHiddenAttributeItem(item, (bool)ViewModel.IsHiddenEditedValue, itemMM)
					);
				}

				if (ViewModel.IsUnblockFileSelected)
					PInvoke.DeleteFileFromApp($"{item.ItemPath}:Zone.Identifier");

				if (ViewModel.IsAblumCoverModified)
				{
					MediaFileHelper.ChangeAlbumCover(item.ItemPath, ViewModel.ModifiedAlbumCover);

					await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
					{
						AppInstance?.ShellViewModel?.RefreshItems(null);
					});
				}

				ViewModel.IsReadOnly = ViewModel.IsReadOnlyEditedValue;
				ViewModel.IsHidden = ViewModel.IsHiddenEditedValue;
				ViewModel.IsContentCompressed = ViewModel.IsContentCompressedEditedValue;

				if (!GetNewName(out var newName))
					return true;

				return await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
					UIFilesystemHelpers.RenameFileItemAsync(item, ViewModel.ItemName, AppInstance, false)
				);
			}
		}

		public override void Dispose()
		{
			_updateDateDisplayTimer.Stop();
		}
	}
}
