// Copyright(c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using TagLib;
using Windows.Storage;

namespace Files.App.ViewModels.Properties
{
	public abstract class BasePropertiesPage : Page, IDisposable
	{
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();

		public IShellPage AppInstance = null;

		public BaseProperties BaseProperties { get; set; }

		public SelectedItemsPropertiesViewModel ViewModel { get; set; } = new();

		protected virtual void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			BaseProperties?.GetSpecialPropertiesAsync();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = (PropertiesPageNavigationParameter)e.Parameter;
			AppInstance = np.AppInstance;

			// Library
			if (np.Parameter is LibraryItem library)
				BaseProperties = new LibraryProperties(ViewModel, np.CancellationTokenSource, DispatcherQueue, library, AppInstance);
			// Drive
			else if (np.Parameter is DriveItem drive)
			{
				var props = new DriveProperties(ViewModel, drive, AppInstance);
				BaseProperties = props;

				ViewModel.CleanupVisibility = props.Drive.Type != DriveType.Network;
				ViewModel.FormatVisibility = !(props.Drive.Type == DriveType.Network || string.Equals(props.Drive.Path, $@"{Constants.UserEnvironmentPaths.SystemDrivePath}\", StringComparison.OrdinalIgnoreCase));
				ViewModel.CleanupDriveCommand = new AsyncRelayCommand(() => StorageSenseHelper.OpenStorageSenseAsync(props.Drive.Path));
				ViewModel.FormatDriveCommand = new RelayCommand(async () =>
				{
					try
					{
						await Win32Helper.OpenFormatDriveDialog(props.Drive.Path);
					}
					catch (Exception)
					{
					}
				});
			}
			// Storage objects (multi-selected)
			else if (np.Parameter is List<ListedItem> items)
			{
				// Selection only contains files
				if (items.All(item => item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive))
				{
					BaseProperties = new CombinedFileProperties(ViewModel, np.CancellationTokenSource, DispatcherQueue, items, AppInstance);

					ViewModel.IsEditAlbumCoverVisible =
						items.All(item => FileExtensionHelpers.IsVideoFile(item.FileExtension)) ||
						items.All(item => FileExtensionHelpers.IsAudioFile(item.FileExtension));
				}
				// Selection includes folders
				else
					BaseProperties = new CombinedProperties(ViewModel, np.CancellationTokenSource, DispatcherQueue, items, AppInstance);
			}
			// A storage object
			else if (np.Parameter is ListedItem item)
			{
				// File or Archive
				if (item.PrimaryItemAttribute == StorageItemTypes.File || item.IsArchive)
					BaseProperties = new FileProperties(ViewModel, np.CancellationTokenSource, DispatcherQueue, item, AppInstance);
				// Folder
				else if (item.PrimaryItemAttribute == StorageItemTypes.Folder)
					BaseProperties = new FolderProperties(ViewModel, np.CancellationTokenSource, DispatcherQueue, item, AppInstance);
			}

			ViewModel.EditAlbumCoverCommand = new RelayCommand(async () =>
			{
				var hWnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(np.Window.AppWindow.Id);

				string[] extensions =
				[
					"BitmapFiles".GetLocalizedResource(), "*.bmp",
					"JPEG", "*.jpg;*.jpeg",
					"PNG", "*.png",
				];

				var result = CommonDialogService.Open_FileOpenDialog(hWnd, false, extensions, Environment.SpecialFolder.Desktop, out var filePath);
				if (result)
				{
					ViewModel.IsAblumCoverModified = true;
					ViewModel.ModifiedAlbumCover = new Picture(filePath);

					var iconData = await FileThumbnailHelper.GetIconAsync(
						filePath,
						Constants.ShellIconSizes.ExtraLarge,
						false,
						IconOptions.UseCurrentScale);

					ViewModel.IconData = iconData;
				}
			});

			base.OnNavigatedTo(e);
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			if (BaseProperties is not null &&
				BaseProperties.TokenSource is not null)
			{
				//BaseProperties.TokenSource.Cancel();
			}

			base.OnNavigatedFrom(e);
		}

		/// <summary>
		/// Try to save changed properties to the file.
		/// </summary>
		/// <returns>Returns true if properties have been saved successfully</returns>
		public abstract Task<bool> SaveChangesAsync();

		/// <summary>
		/// Dispose unmanaged resources.
		/// </summary>
		public abstract void Dispose();
	}
}
