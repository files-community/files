// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Files.App.Views.Properties
{
	public sealed partial class LibraryPage : BasePropertiesPage, INotifyPropertyChanged
	{
		private ICommonDialogService CommonDialogService { get; } = Ioc.Default.GetRequiredService<ICommonDialogService>();

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public ObservableCollection<LibraryFolder> Folders { get; } = [];

		public bool IsLibraryEmpty => Folders.Count == 0;

		private int selectedFolderIndex = -1;
		public int SelectedFolderIndex
		{
			get => selectedFolderIndex;
			set
			{
				if (selectedFolderIndex != value)
				{
					selectedFolderIndex = value;
					NotifyPropertyChanged(nameof(SelectedFolderIndex));
					NotifyPropertyChanged(nameof(IsNotDefaultFolderSelected));
				}
			}
		}

		public bool IsNotDefaultFolderSelected => selectedFolderIndex >= 0 && !Folders[selectedFolderIndex].IsDefault;

		private bool isPinned;
		public bool IsPinned
		{
			get => isPinned;
			set
			{
				if (isPinned != value)
				{
					isPinned = value;
					NotifyPropertyChanged(nameof(IsPinned));
				}
			}
		}

		public ICommand AddLocationCommand { get; }
		public ICommand SetDefaultLocationCommand { get; }
		public ICommand RemoveLocationCommand { get; }

		public LibraryPage()
		{
			InitializeComponent();

			AddLocationCommand = new AsyncRelayCommand(AddLocationAsync);
			SetDefaultLocationCommand = new RelayCommand(SetDefaultLocation);
			RemoveLocationCommand = new RelayCommand(RemoveLocation);
		}

		protected override void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			base.Properties_Loaded(sender, e);

			if (BaseProperties is LibraryProperties props)
			{
				Folders.Clear();
				if (!props.Library.IsEmpty)
				{
					foreach (var path in props.Library.Folders)
					{
						Folders.Add(new LibraryFolder
						{
							Path = path,
							IsDefault = string.Equals(path, props.Library.DefaultSaveFolder, StringComparison.OrdinalIgnoreCase),
						});
					}
					NotifyPropertyChanged(nameof(IsLibraryEmpty));
				}
			}
		}

		private async Task AddLocationAsync()
		{
			var result = CommonDialogService.Open_FileOpenDialog(MainWindow.Instance.WindowHandle, true, [], Environment.SpecialFolder.Desktop, out var filePath);
			if (!result)
				return;

			var folder = await StorageHelpers.ToStorageItem<BaseStorageFolder>(filePath);
			if (folder is not null && !Folders.Any((f) => string.Equals(folder.Path, f.Path, StringComparison.OrdinalIgnoreCase)))
			{
				bool isDefault = Folders.Count == 0;
				Folders.Add(new LibraryFolder { Path = folder.Path, IsDefault = isDefault });
				if (isDefault)
				{
					NotifyPropertyChanged(nameof(IsLibraryEmpty));
				}
			}
		}

		private void SetDefaultLocation()
		{
			int index = SelectedFolderIndex;
			if (index >= 0)
			{
				foreach (var f in Folders)
				{
					f.IsDefault = false;
				}
				Folders[index].IsDefault = true;
			}
		}

		private void RemoveLocation()
		{
			int index = SelectedFolderIndex;
			if (index >= 0)
			{
				Folders.RemoveAt(index);
				if (index > 0)
				{
					SelectedFolderIndex = index - 1;
				}
				else if (Folders.Count > 0)
				{
					SelectedFolderIndex = 0;
				}
			}
		}

		private bool IsChanged(LibraryItem lib, out string newDefaultSaveFolder, out string[] newFolders, out bool? newIsPinned)
		{
			bool isChanged = false;

			newDefaultSaveFolder = null;
			newFolders = null;
			newIsPinned = null;

			var defaultSaveFolderPath = Folders.FirstOrDefault(f => f.IsDefault)?.Path;
			if (!string.Equals(defaultSaveFolderPath, lib.DefaultSaveFolder, StringComparison.OrdinalIgnoreCase))
			{
				newDefaultSaveFolder = defaultSaveFolderPath;
				isChanged = true;
			}

			if ((lib.Folders?.Count ?? 0) != Folders.Count || lib.Folders?.SequenceEqual(Folders.Select(f => f.Path), StringComparer.OrdinalIgnoreCase) != true)
			{
				newFolders = Folders.Select(f => f.Path).ToArray();
				isChanged = true;
			}

			if (isPinned != lib.IsPinned)
			{
				newIsPinned = isPinned;
				isChanged = true;
			}

			return isChanged;
		}

		public override async Task<bool> SaveChangesAsync()
		{
			if (BaseProperties is LibraryProperties props)
			{
				// Skip checks / updates and close dialog when the library is empty
				if (IsLibraryEmpty)
					return true;

				// Skip updates and close dialog when nothing changed
				if (!IsChanged(props.Library, out string newDefaultSaveFolder, out string[] newFolders, out bool? newIsPinned))
					return true;

				while (true)
				{
					using var dialog = DynamicDialogFactory.GetFor_PropertySaveErrorDialog();
					try
					{
						var library = await Task.Run(() => App.LibraryManager.UpdateLibrary(props.Library.ItemPath, newDefaultSaveFolder, newFolders, newIsPinned));
						if (library is not null)
						{
							props.UpdateLibrary(new LibraryItem(library));
							return true;
						}
						// TODO: show / throw error about the failure?
						return false;
					}
					catch
					{
						await dialog.TryShowAsync();
						switch (dialog.DynamicResult)
						{
							case DynamicDialogResult.Primary:
								break;
							case DynamicDialogResult.Secondary:
								return true;
							case DynamicDialogResult.Cancel:
								return false;
						}
					}
				}
			}
			return false;
		}

		public override void Dispose()
		{
		}

	}

	public sealed class LibraryFolder : ObservableObject
	{
		public string Path { get; set; }

		private bool isDefault;
		public bool IsDefault
		{
			get => isDefault;
			set => SetProperty(ref isDefault, value);
		}
	}
}
