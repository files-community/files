// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Windows.Storage;
using Windows.Win32.Foundation;

namespace Files.App.ViewModels.Properties
{
	public sealed class SecurityAdvancedViewModel : ObservableObject
	{
		private readonly IStorageSecurityService StorageSecurityService = Ioc.Default.GetRequiredService<IStorageSecurityService>();

		private readonly PropertiesPageNavigationParameter _navigationParameter;

		private readonly Window _window;

		private readonly string _path;

		private readonly bool _isFolder;

		public bool IsAddAccessControlEntryButtonEnabled =>
			AccessControlList is not null &&
			AccessControlList.IsValid;

		public bool IsDeleteAccessControlEntryButtonEnabled =>
			AccessControlList is not null &&
			AccessControlList.IsValid &&
			SelectedAccessControlEntry is not null &&
			SelectedAccessControlEntry.IsInherited is false;

		public IconFileInfo ShieldIconFileInfo { get; private set; }

		public bool CurrentInstanceCanReadPermissions { get; private set; }

		public bool CurrentInstanceCanChangePermissions { get; private set; }

		public string DisableInheritanceOption
		{
			get
			{
				//if (!_isProtected)
				//	return "SecurityAdvancedInheritedEnable/Text".GetLocalizedResource();
				//else if (_preserveInheritance)
				//	return "SecurityAdvancedInheritedConvert/Text".GetLocalizedResource();
				//else
				//	return "SecurityAdvancedInheritedRemove/Text".GetLocalizedResource();

				return string.Empty;
			}
		}

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

				if (SetProperty(ref _SelectedAccessControlEntry, value))
				{
					if(value is not null)
						value.IsSelected = true;

					OnPropertyChanged(nameof(IsDeleteAccessControlEntryButtonEnabled));
				}
			}
		}

		private bool _DisplayElements;
		public bool DisplayElements
		{
			get => _DisplayElements;
			set => SetProperty(ref _DisplayElements, value);
		}

		private string _ErrorMessage = string.Empty;
		public string ErrorMessage
		{
			get => _ErrorMessage;
			set => SetProperty(ref _ErrorMessage, value);
		}

		private GridLength _ColumnTypeGridLength = new(64d);
		public GridLength ColumnTypeGridLength
		{
			get => _ColumnTypeGridLength;
			set => SetProperty(ref _ColumnTypeGridLength, value);
		}

		private GridLength _ColumnPrincipalGridLength = new(200d);
		public GridLength ColumnPrincipalGridLength
		{
			get => _ColumnPrincipalGridLength;
			set => SetProperty(ref _ColumnPrincipalGridLength, value);
		}

		private GridLength _ColumnAccessGridLength = new(160d);
		public GridLength ColumnAccessGridLength
		{
			get => _ColumnAccessGridLength;
			set => SetProperty(ref _ColumnAccessGridLength, value);
		}

		private GridLength _ColumnInheritedGridLength = new(70d);
		public GridLength ColumnInheritedGridLength
		{
			get => _ColumnInheritedGridLength;
			set => SetProperty(ref _ColumnInheritedGridLength, value);
		}

		public IAsyncRelayCommand ChangeOwnerCommand { get; set; }
		public IAsyncRelayCommand AddAccessControlEntryCommand { get; set; }
		public IAsyncRelayCommand RemoveAccessControlEntryCommand { get; set; }

		public SecurityAdvancedViewModel(PropertiesPageNavigationParameter parameter)
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

			LoadShieldIconResource();

			LoadAccessControlEntry();

			ChangeOwnerCommand = new AsyncRelayCommand(ExecuteChangeOwnerCommandAsync);
			AddAccessControlEntryCommand = new AsyncRelayCommand(ExecuteAddAccessControlEntryCommandAsync);
			RemoveAccessControlEntryCommand = new AsyncRelayCommand(ExecuteRemoveAccessControlEntryCommandAsync);
		}

		private void LoadShieldIconResource()
		{
			string imageres = System.IO.Path.Combine(Constants.UserEnvironmentPaths.SystemRootPath, "System32", "imageres.dll");

			var imageResList = Win32Helper.ExtractSelectedIconsFromDLL(
				imageres,
				new List<int>() { Constants.ImageRes.ShieldIcon },
				16);

			ShieldIconFileInfo = imageResList.First();
		}

		private void LoadAccessControlEntry()
		{
			var error = StorageSecurityService.GetAcl(_path, _isFolder, out _AccessControlList);
			OnPropertyChanged(nameof(AccessControlList));

			SelectedAccessControlEntry = AccessControlList.AccessControlEntries.FirstOrDefault();

			if (!AccessControlList.IsValid)
			{
				DisplayElements = false;

				if (error is WIN32_ERROR.ERROR_ACCESS_DENIED)
				{
					ErrorMessage = 
						"SecurityRequireReadPermissions".GetLocalizedResource() +
						"\r\n\r\n" +
						"SecuritySuggestToTakeOwnership".GetLocalizedResource();
				}
				else
				{
					ErrorMessage =
						"SecurityUnableToDisplayPermissions".GetLocalizedResource() +
						"\r\n\r\n" +
						error.ToString();
				}
			}
			else
			{
				DisplayElements = true;
				ErrorMessage = string.Empty;
			}
		}

		private async Task ExecuteChangeOwnerCommandAsync()
		{
			var sid = await FileOperationsHelpers.OpenObjectPickerAsync(FilePropertiesHelpers.GetWindowHandle(_window).ToInt64());
			if (string.IsNullOrEmpty(sid))
				return;

			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
			{
				// Set owner
				StorageSecurityService.SetOwner(_path, sid);

				// Reload
				LoadAccessControlEntry();
			});
		}

		private async Task ExecuteAddAccessControlEntryCommandAsync()
		{
			// Pick an user or a group with Object Picker UI
			var sid = await FileOperationsHelpers.OpenObjectPickerAsync(FilePropertiesHelpers.GetWindowHandle(_window).ToInt64());
			if (string.IsNullOrEmpty(sid))
				return;

			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
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
			if (SelectedAccessControlEntry is null)
				return;

			await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(() =>
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
