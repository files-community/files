using Files.App.Data.Items;
using Files.App.Extensions;
using Files.App.Utils;
using Files.App.Helpers;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;

namespace Files.App.ViewModels.Properties
{
	internal class DriveProperties : BaseProperties
	{
		public SidebarDriveItem Drive { get; }

		public DriveProperties(SelectedItemsPropertiesViewModel viewModel, SidebarDriveItem driveItem, IShellPage instance)
		{
			ViewModel = viewModel;
			Drive = driveItem;
			AppInstance = instance;
			GetBaseProperties();
		}

		public override void GetBaseProperties()
		{
			if (Drive is null)
				return;

			//Drive.IconSource;
			ViewModel.CustomIconSource = null;

			ViewModel.IconData = Drive.IconData;

			// Drive.IconSource is not null && Drive.IconData is null;
			ViewModel.LoadCustomIcon = false;
			ViewModel.LoadFileIcon = Drive.IconData is not null;

			ViewModel.ItemName = Drive.Text;
			ViewModel.OriginalItemName = Drive.Text;

			// NOTE: If DriveType enum changes, the corresponding resource keys should change too
			ViewModel.ItemType = string.Format("DriveType{0}", Drive.Type).GetLocalizedResource();
		}

		public async override Task GetSpecialPropertiesAsync()
		{
			ViewModel.ItemAttributesVisibility = false;
			var item = await FilesystemTasks.Wrap(() => DriveHelpers.GetRootFromPathAsync(Drive.Path));
			BaseStorageFolder diskRoot = await FilesystemTasks.Wrap(() => StorageFileExtensions.DangerousGetFolderFromPathAsync(Drive.Path, item));

			if (ViewModel.LoadFileIcon)
			{
				var result = await FileThumbnailHelper.GetIconAsync(
					Drive.Path,
					Constants.ShellIconSizes.ExtraLarge,
					true,
					false,
					IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale);

				if (result.IconData is not null)
					ViewModel.IconData = result.IconData;
				else
				{
					result = await FileThumbnailHelper.GetIconAsync(
						Drive.DeviceID,
						Constants.ShellIconSizes.ExtraLarge,
						true, false, IconOptions.ReturnIconOnly | IconOptions.UseCurrentScale); // For network shortcuts

					ViewModel.IconData = result.IconData;
				}
			}

			if (diskRoot is null || diskRoot.Properties is null)
			{
				ViewModel.LastSeparatorVisibility = false;

				return;
			}

			try
			{
				string freeSpace = "System.FreeSpace";
				string capacity = "System.Capacity";
				string fileSystem = "System.Volume.FileSystem";

				var properties = await diskRoot.Properties.RetrievePropertiesAsync(new[] { freeSpace, capacity, fileSystem });

				ViewModel.DriveCapacityValue = (ulong)properties[capacity];
				ViewModel.DriveFreeSpaceValue = (ulong)properties[freeSpace];
				ViewModel.DriveUsedSpaceValue = ViewModel.DriveCapacityValue - ViewModel.DriveFreeSpaceValue;
				ViewModel.DriveFileSystem = (string)properties[fileSystem];
			}
			catch (Exception e)
			{
				ViewModel.LastSeparatorVisibility = false;
				App.Logger.LogWarning(e, e.Message);
			}
		}
	}
}
