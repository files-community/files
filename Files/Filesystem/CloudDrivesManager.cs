using Files.Filesystem.Cloud;
using Files.UserControls.Widgets;
using Files.Views;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Uwp.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace Files.Filesystem
{
    public class CloudDrivesManager : ObservableObject
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly List<DriveItem> drivesList = new List<DriveItem>();

        public IReadOnlyList<DriveItem> Drives
        {
            get
            {
                lock (drivesList)
                {
                    return drivesList.ToList().AsReadOnly();
                }
            }
        }

        public CloudDrivesManager()
        {
        }

        public async Task EnumerateDrivesAsync()
        {
            var cloudProviderController = new CloudProviderController();
            var cloudProviders = await cloudProviderController.DetectInstalledCloudProvidersAsync();

            foreach (var provider in cloudProviders)
            {
                Logger.Info($"Adding cloud provider \"{provider.Name}\" mapped to {provider.SyncFolder}");
                var cloudProviderItem = new DriveItem()
                {
                    Text = provider.Name,
                    Path = provider.SyncFolder,
                    Type = DriveType.CloudDrive,
                };
                lock (drivesList)
                {
                    if (!drivesList.Any(x => x.Path == cloudProviderItem.Path))
                    {
                        drivesList.Add(cloudProviderItem);
                    }
                }
            }

            await RefreshUI();
        }

        private async Task RefreshUI()
        {
            try
            {
                await SyncSideBarItemsUI();
            }
            catch (Exception ex) // UI Thread not ready yet, so we defer the previous operation until it is.
            {
                Logger.Error(ex, "UI thread not ready yet");
                System.Diagnostics.Debug.WriteLine($"RefreshUI Exception");
                // Defer because UI-thread is not ready yet (and DriveItem requires it?)
                CoreApplication.MainView.Activated += RefreshUI;
            }
        }

        private async void RefreshUI(CoreApplicationView sender, Windows.ApplicationModel.Activation.IActivatedEventArgs args)
        {
            CoreApplication.MainView.Activated -= RefreshUI;
            await SyncSideBarItemsUI();
        }

        private async Task SyncSideBarItemsUI()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                ObservableCollection<INavigationControlItem> items = new ObservableCollection<INavigationControlItem>();

                await MainPage.SideBarItemsSemaphore.WaitAsync();
                try
                {
                    MainPage.SideBarItems.BeginBulkOperation();

                    foreach (DriveItem drive in Drives)
                    {
                        if (!MainPage.SideBarItems.Contains(drive))
                        {
                            items.Add(drive);

                            if (drive.Type != DriveType.VirtualDrive)
                            {
                                DrivesWidget.ItemsAdded.Add(drive);
                            }
                        }
                    }

                    MainPage.SideBarItems.Add(new DriveItem(items)
                    {
                        Text = "SidebarCloudDrives".GetLocalized(),
                        IsExpanded = false
                    });

                    MainPage.SideBarItems.EndBulkOperation();
                }
                finally
                {
                    MainPage.SideBarItemsSemaphore.Release();
                }
            });
        }
    }
}