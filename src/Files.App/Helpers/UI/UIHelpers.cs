// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Helpers.Application;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace Files.App.Helpers
{
	public static class UIHelpers
	{
		public static event PropertyChangedEventHandler? PropertyChanged;

		private static bool canShowDialog = true;
		public static bool CanShowDialog
		{
			get => canShowDialog;
			private set
			{
				if (value == canShowDialog)
					return;
				canShowDialog = value;
				PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(CanShowDialog)));
			}
		}

		/// <summary>
		/// Displays a toast or dialog to indicate the result of
		/// a device ejection operation.
		/// </summary>
		/// <param name="type">Type of drive to eject</param>
		/// <param name="result">Only true implies a successful device ejection</param>
		/// <returns></returns>
		public static async Task ShowDeviceEjectResultAsync(Data.Items.DriveType type, bool result)
		{
			if (type != Data.Items.DriveType.CDRom && result)
			{
				Debug.WriteLine("Device successfully ejected");

				SafetyExtensions.IgnoreExceptions(() =>
				{
					AppToastNotificationHelper.ShowDriveEjectToast();
				});
			}
			else if (!result)
			{
				Debug.WriteLine("Can't eject device");

				await DialogDisplayHelper.ShowDialogAsync(
					"EjectNotificationErrorDialogHeader".GetLocalizedResource(),
					"EjectNotificationErrorDialogBody".GetLocalizedResource());
			}
		}

		public static async Task<ContentDialogResult> TryShowAsync(this ContentDialog dialog)
		{
			if (!canShowDialog)
				return ContentDialogResult.None;

			try
			{
				CanShowDialog = false;
				return await SetContentDialogRoot(dialog).ShowAsync();
			}
			catch // A content dialog is already open
			{
				return ContentDialogResult.None;
			}
			finally
			{
				CanShowDialog = true;
			}
		}

		public static async Task<DialogResult> TryShowAsync<TViewModel>(this IDialog<TViewModel> dialog)
			where TViewModel : class, INotifyPropertyChanged
		{
			return (DialogResult)await ((ContentDialog)dialog).TryShowAsync();
		}

		// WINUI3
		private static ContentDialog SetContentDialogRoot(ContentDialog contentDialog)
		{
			if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				contentDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;
			}
			return contentDialog;
		}

		public static void CloseAllDialogs()
		{
			if (MainWindow.Instance?.Content?.XamlRoot == null)
				return;

			var openedDialogs = VisualTreeHelper.GetOpenPopupsForXamlRoot(MainWindow.Instance.Content.XamlRoot);

			foreach (var item in openedDialogs)
				if (item.Child is ContentDialog dialog)
					dialog.Hide();
		}

		private static IEnumerable<IconFileInfo> SidebarIconResources = LoadSidebarIconResources();

		private static IconFileInfo ShieldIconResource = LoadShieldIconResource();

		public static IconFileInfo GetSidebarIconResourceInfo(int index)
		{
			var icons = UIHelpers.SidebarIconResources;
			return icons?.FirstOrDefault(x => x.Index == index);
		}

		public static async Task<BitmapImage?> GetSidebarIconResource(int index)
		{
			var iconInfo = GetSidebarIconResourceInfo(index);

			return iconInfo is not null
				? await iconInfo.IconData.ToBitmapAsync()
				: null;
		}

		public static async Task<BitmapImage?> GetShieldIconResource()
		{
			return ShieldIconResource is not null
				? await ShieldIconResource.IconData.ToBitmapAsync()
				: null;
		}

		private static IEnumerable<IconFileInfo> LoadSidebarIconResources()
		{
			string imageres = Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");
			var imageResList = Win32Helper.ExtractSelectedIconsFromDLL(imageres, new List<int>() {
					Constants.ImageRes.RecycleBin,
					Constants.ImageRes.Network,
					Constants.ImageRes.Libraries,
					Constants.ImageRes.ThisPC,
					Constants.ImageRes.CloudDrives,
					Constants.ImageRes.Folder,
					Constants.ImageRes.OneDrive
				}, 32);

			return imageResList;
		}

		private static IconFileInfo LoadShieldIconResource()
		{
			string imageres = Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");
			var imageResList = Win32Helper.ExtractSelectedIconsFromDLL(imageres, new List<int>() {
					Constants.ImageRes.ShieldIcon
				}, 16);

			return imageResList.FirstOrDefault();
		}
	}
}
