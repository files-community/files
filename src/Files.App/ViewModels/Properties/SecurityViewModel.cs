// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Win32.Foundation;

namespace Files.App.ViewModels.Properties
{
	public sealed class SecurityViewModel : ObservableObject
	{
		private readonly IStorageSecurityService StorageSecurityService = Ioc.Default.GetRequiredService<IStorageSecurityService>();

		private readonly PropertiesPageNavigationParameter _navigationParameter;

		private readonly Window _window;

		private readonly string _path;

		private readonly bool _isFolder;

		public bool DisplayElements { get; private set; }

		public string ErrorMessage { get; private set; }

		public bool IsAddAccessControlEntryButtonEnabled =>
			AccessControlList is not null &&
			AccessControlList.IsValid;

		public bool IsDeleteAccessControlEntryButtonEnabled =>
			AccessControlList is not null &&
			AccessControlList.IsValid &&
			SelectedAccessControlEntry is not null &&
			SelectedAccessControlEntry.IsInherited is false;

		public string SelectedItemHeaderText =>
			SelectedAccessControlEntry is null
				? "Permissions".GetLocalizedResource()
				: string.Format("SecurityPermissionsHeaderText".GetLocalizedResource(), SelectedAccessControlEntry?.Principal.DisplayName);

		private AccessControlList _AccessControlList;
		public AccessControlList AccessControlList
		{
			get => _AccessControlList;
			set => SetProperty(ref _AccessControlList, value);
		}

		private AccessControlEntry? _SelectedAccessControlEntry;
		public AccessControlEntry? SelectedAccessControlEntry
		{
			get => _SelectedAccessControlEntry;
			set
			{
				// Previous selection
				if (_SelectedAccessControlEntry is not null)
					_SelectedAccessControlEntry.IsSelected = false;

				if (value is not null && SetProperty(ref _SelectedAccessControlEntry, value))
				{
					value.IsSelected = true;

					OnPropertyChanged(nameof(IsDeleteAccessControlEntryButtonEnabled));
					OnPropertyChanged(nameof(SelectedItemHeaderText));
				}
			}
		}

		public IAsyncRelayCommand AddAccessControlEntryCommand { get; set; }
		public IAsyncRelayCommand RemoveAccessControlEntryCommand { get; set; }

		public SecurityViewModel(PropertiesPageNavigationParameter parameter)
		{
			_navigationParameter = parameter;
			_window = parameter.Window;

			switch (parameter.Parameter)
			{
				case ListedItem listedItem:
					_path = listedItem.ItemPath;
					_isFolder = listedItem.PrimaryItemAttribute == StorageItemTypes.Folder && !listedItem.IsShortcut;
					break;
				case DriveItem driveItem:
					_path = driveItem.Path;
					_isFolder = true;
					break;
				default:
					var defaultlistedItem = (ListedItem)parameter.Parameter;
					_path = defaultlistedItem.ItemPath;
					_isFolder = defaultlistedItem.PrimaryItemAttribute == StorageItemTypes.Folder && !defaultlistedItem.IsShortcut;
					break;
			};

			var error = StorageSecurityService.GetAcl(_path, _isFolder, out _AccessControlList);
			_SelectedAccessControlEntry = AccessControlList.AccessControlEntries.FirstOrDefault();

			if (!AccessControlList.IsValid)
			{
				DisplayElements = false;
				ErrorMessage = error is WIN32_ERROR.ERROR_ACCESS_DENIED
					? "SecurityRequireReadPermissions".GetLocalizedResource() + "\r\n" + "SecurityClickAdvancedPermissions".GetLocalizedResource()
					: "SecurityUnableToDisplayPermissions".GetLocalizedResource();
			}
			else
			{
				DisplayElements = true;
				ErrorMessage = string.Empty;
			}

			AddAccessControlEntryCommand = new AsyncRelayCommand(ExecuteAddAccessControlEntryCommandAsync);
			RemoveAccessControlEntryCommand = new AsyncRelayCommand(ExecuteRemoveAccessControlEntryCommandAsync);
		}

		private async Task ExecuteAddAccessControlEntryCommandAsync()
		{
			// Pick an user or a group with Object Picker UI
			var sid = await FileOperationsHelpers.OpenObjectPickerAsync(FilePropertiesHelpers.GetWindowHandle(_window).ToInt64());
			if (string.IsNullOrEmpty(sid))
				return;

			await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() =>
			{
				// Run Win32API
				var win32Result = StorageSecurityService.AddAce(_path, _isFolder, sid);

				// Add a new ACE to the ACL
				var ace = AccessControlEntry.GetDefault(_isFolder, sid);
				AccessControlList.AccessControlEntries.Insert(0, ace);
			});
		}

		private async Task ExecuteRemoveAccessControlEntryCommandAsync()
		{
			await MainWindow.Instance.DispatcherQueue.EnqueueAsync(() =>
			{
				// Get index of the ACE
				var index = AccessControlList.AccessControlEntries.IndexOf(SelectedAccessControlEntry);

				// Run Win32API
				var win32Result = StorageSecurityService.DeleteAce(_path, (uint)index);

				// Remove the ACE
				AccessControlList.AccessControlEntries.Remove(SelectedAccessControlEntry);

				if (AccessControlList.AccessControlEntries.Count == 0)
					return;

				// Re-select item
				SelectedAccessControlEntry = AccessControlList.AccessControlEntries.First();
			});
		}
	}
}
