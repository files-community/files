// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;

namespace Files.App.ViewModels.Properties
{
	internal sealed class LibraryProperties : BaseProperties
	{
		public LibraryItem Library { get; private set; }

		public LibraryProperties(SelectedItemsPropertiesViewModel viewModel, CancellationTokenSource tokenSource,
			DispatcherQueue coreDispatcher, LibraryItem item, IShellPage instance)
		{
			ViewModel = viewModel;
			TokenSource = tokenSource;
			Dispatcher = coreDispatcher;
			Library = item;
			AppInstance = instance;

			GetBaseProperties();
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		}

		public void UpdateLibrary(LibraryItem library)
		{
			Library = library;
			GetBaseProperties();
			GetSpecialPropertiesAsync();
		}

		public override void GetBaseProperties()
		{
			if (Library is not null)
			{
				ViewModel.ItemName = Library.Name;
				ViewModel.OriginalItemName = Library.Name;
				ViewModel.ItemType = Library.ItemType;
				ViewModel.LoadCustomIcon = Library.LoadCustomIcon;
				ViewModel.CustomIconSource = Library.CustomIconSource;
				ViewModel.LoadFileIcon = Library.LoadFileIcon;
				ViewModel.ContainsFilesOrFolders = false;
			}
		}

		public async override Task GetSpecialPropertiesAsync()
		{
			ViewModel.IsReadOnly = Win32Helper.HasFileAttribute(Library.ItemPath, System.IO.FileAttributes.ReadOnly);
			ViewModel.IsHidden = Win32Helper.HasFileAttribute(Library.ItemPath, System.IO.FileAttributes.Hidden);
			ViewModel.CanCompressContent = false;

			var result = await FileThumbnailHelper.GetIconAsync(
				Library.ItemPath,
				Constants.ShellIconSizes.ExtraLarge,
				true,
				IconOptions.UseCurrentScale);
			
			if (result is not null)
			{
				ViewModel.IconData = result;
				ViewModel.LoadCustomIcon = false;
				ViewModel.LoadFileIcon = true;
			}

			BaseStorageFile libraryFile = await AppInstance.ShellViewModel.GetFileFromPathAsync(Library.ItemPath);
			if (libraryFile is not null)
			{
				ViewModel.ItemCreatedTimestampReal = libraryFile.DateCreated;
				if (libraryFile.Properties is not null)
				{
					GetOtherPropertiesAsync(libraryFile.Properties);
				}
			}

			var storageFolders = new List<BaseStorageFolder>();
			if (Library.Folders is not null)
			{
				try
				{
					foreach (var path in Library.Folders)
					{
						BaseStorageFolder folder = await AppInstance.ShellViewModel.GetFolderFromPathAsync(path);
						if (!string.IsNullOrEmpty(folder.Path))
						{
							storageFolders.Add(folder);
						}
					}
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex, ex.Message);
				}
			}

			if (storageFolders.Count > 0)
			{
				ViewModel.ContainsFilesOrFolders = true;
				ViewModel.LocationsCount = storageFolders.Count;
				GetLibrarySizeAsync(storageFolders, TokenSource.Token);
			}
			else
			{
				ViewModel.FilesAndFoldersCountString = "LibraryNoLocations/Text".GetLocalizedResource();
			}
		}

		private async Task GetLibrarySizeAsync(List<BaseStorageFolder> storageFolders, CancellationToken token)
		{
			ViewModel.ItemSizeVisibility = true;
			ViewModel.ItemSizeProgressVisibility = true;
			ViewModel.ItemSizeOnDiskProgressVisibility = true;

			try
			{
				long librarySize = 0;
				long librarySizeOnDisk = 0;
				foreach (var folder in storageFolders)
				{
					var foldersSize = await Task.Run(async () => await CalculateFolderSizeAsync(folder.Path, token));
					librarySize += foldersSize.size;
					librarySizeOnDisk += foldersSize.sizeOnDisk;
				}
				ViewModel.ItemSizeBytes = librarySize;
				ViewModel.ItemSize = librarySize.ToLongSizeString();
				ViewModel.ItemSizeOnDiskBytes = librarySize;
				ViewModel.ItemSizeOnDisk = librarySize.ToLongSizeString();
			}
			catch (Exception ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
			}

			ViewModel.ItemSizeProgressVisibility = false;
			ViewModel.ItemSizeOnDiskProgressVisibility = false;

			SetItemsCountString();
		}

		private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "IsReadOnly":
					if (ViewModel.IsReadOnly is not null)
					{
						if ((bool)ViewModel.IsReadOnly)
							Win32Helper.SetFileAttribute(Library.ItemPath, System.IO.FileAttributes.ReadOnly);
						else
							Win32Helper.UnsetFileAttribute(Library.ItemPath, System.IO.FileAttributes.ReadOnly);
					}

					break;

				case "IsHidden":
					if (ViewModel.IsHidden is not null)
					{
						if ((bool)ViewModel.IsHidden)
							Win32Helper.SetFileAttribute(Library.ItemPath, System.IO.FileAttributes.Hidden);
						else
							Win32Helper.UnsetFileAttribute(Library.ItemPath, System.IO.FileAttributes.Hidden);
					}

					break;
			}
		}
	}
}
