// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Files.App.ViewModels.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System.IO;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.System;
using Visibility = Microsoft.UI.Xaml.Visibility;

namespace Files.App.Utils.Library
{
	public sealed class LibraryManager : IDisposable
	{
		public EventHandler<NotifyCollectionChangedEventArgs>? DataChanged;

		private FileSystemWatcher librariesWatcher;
		private readonly List<LibraryLocationItem> libraries = [];
		private static readonly Lazy<LibraryManager> lazy = new(() => new LibraryManager());

		public static LibraryManager Default
			=> lazy.Value;

		public IReadOnlyList<LibraryLocationItem> Libraries
		{
			get
			{
				lock (libraries)
				{
					return libraries.ToList().AsReadOnly();
				}
			}
		}

		public LibraryManager()
		{
			InitializeWatcher();
		}

		private void InitializeWatcher()
		{
			if (librariesWatcher is not null)
				return;

			librariesWatcher = new FileSystemWatcher
			{
				Path = ShellLibraryItem.LibrariesPath,
				Filter = "*" + ShellLibraryItem.EXTENSION,
				NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.FileName,
				IncludeSubdirectories = false,
			};

			librariesWatcher.Created += OnLibraryChanged;
			librariesWatcher.Changed += OnLibraryChanged;
			librariesWatcher.Deleted += OnLibraryChanged;
			librariesWatcher.Renamed += OnLibraryRenamed;

			librariesWatcher.EnableRaisingEvents = true;
		}

		/// <summary>
		/// Get libraries of the current user with the help of the FullTrust process.
		/// </summary>
		/// <returns>List of library items</returns>
		public static async Task<List<LibraryLocationItem>> ListUserLibraries()
		{
			var libraries = await Win32Helper.StartSTATask(() =>
			{
				try
				{
					var libraryItems = new List<ShellLibraryItem>();
					// https://learn.microsoft.com/windows/win32/search/-search-win7-development-scenarios#library-descriptions
					var libFiles = Directory.EnumerateFiles(ShellLibraryItem.LibrariesPath, "*" + ShellLibraryItem.EXTENSION);
					foreach (var libFile in libFiles)
					{
						using var shellItem = new ShellLibraryEx(Shell32.ShellUtil.GetShellItemForPath(libFile), true);
						if (shellItem is ShellLibraryEx library)
						{
							libraryItems.Add(ShellFolderExtensions.GetShellLibraryItem(library, libFile));
						}
					}
					return libraryItems;
				}
				catch (Exception e)
				{
					App.Logger.LogWarning(e, null);
				}

				return [];
			});

			return libraries.Select(lib => new LibraryLocationItem(lib)).ToList();
		}

		public async Task UpdateLibrariesAsync()
		{
			lock (libraries)
			{
				libraries.Clear();
			}
			var libs = await ListUserLibraries();
			if (libs is not null)
			{
				libs.Sort();
				lock (libraries)
				{
					libraries.AddRange(libs);
				}
			}
			DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public bool TryGetLibrary(string path, out LibraryLocationItem library)
		{
			if (string.IsNullOrWhiteSpace(path) || !path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase))
			{
				library = null;
				return false;
			}
			library = Libraries.FirstOrDefault(l => string.Equals(path, l.Path, StringComparison.OrdinalIgnoreCase));
			return library is not null;
		}

		/// <summary>
		/// Create a new library with the specified name.
		/// </summary>
		/// <param name="name">The name of the new library (must be unique)</param>
		/// <returns>The new library if successfully created</returns>
		public async Task<bool> CreateNewLibrary(string name)
		{
			if (string.IsNullOrWhiteSpace(name) || !CanCreateLibrary(name).result)
				return false;

			var newLib = new LibraryLocationItem(await Win32Helper.StartSTATask(() =>
			{
				try
				{
					using var library = new ShellLibraryEx(name, Shell32.KNOWNFOLDERID.FOLDERID_Libraries, false);
					library.Folders.Add(ShellItem.Open(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))); // Add default folder so it's not empty
					library.Commit();
					library.Reload();
					return Task.FromResult(ShellFolderExtensions.GetShellLibraryItem(library, library.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing)));
				}
				catch (Exception e)
				{
					App.Logger.LogWarning(e, null);
				}

				return Task.FromResult<ShellLibraryItem>(null);
			}));

			if (newLib is not null)
			{
				lock (libraries)
				{
					libraries.Add(newLib);
				}
				DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newLib));
				return true;
			}
			return false;
		}

		/// <summary>
		/// Update library details.
		/// </summary>
		/// <param name="libraryFilePath">Library file path</param>
		/// <param name="defaultSaveFolder">Update the default save folder or null to keep current</param>
		/// <param name="folders">Update the library folders or null to keep current</param>
		/// <param name="isPinned">Update the library pinned status or null to keep current</param>
		/// <returns>The new library if successfully updated</returns>
		public async Task<LibraryLocationItem> UpdateLibrary(string libraryPath, string defaultSaveFolder = null, string[] folders = null, bool? isPinned = null)
		{
			if (string.IsNullOrWhiteSpace(libraryPath) || (defaultSaveFolder is null && folders is null && isPinned is null))
				// Nothing to update
				return null;

			var item = await Win32Helper.StartSTATask(() =>
			{
				try
				{
					bool updated = false;
					using var library = new ShellLibraryEx(Shell32.ShellUtil.GetShellItemForPath(libraryPath), false);
					if (folders is not null)
					{
						if (folders.Length > 0)
						{
							var foldersToRemove = library.Folders.Where(f => !folders.Any(folderPath => string.Equals(folderPath, f.FileSystemPath, StringComparison.OrdinalIgnoreCase)));
							foreach (var toRemove in foldersToRemove)
							{
								library.Folders.Remove(toRemove);
								updated = true;
							}
							var foldersToAdd = folders.Distinct(StringComparer.OrdinalIgnoreCase)
													  .Where(folderPath => !library.Folders.Any(f => string.Equals(folderPath, f.FileSystemPath, StringComparison.OrdinalIgnoreCase)))
													  .Select(ShellItem.Open);
							foreach (var toAdd in foldersToAdd)
							{
								library.Folders.Add(toAdd);
								updated = true;
							}
							foreach (var toAdd in foldersToAdd)
							{
								toAdd.Dispose();
							}
						}
					}
					if (defaultSaveFolder is not null)
					{
						library.DefaultSaveFolder = ShellItem.Open(defaultSaveFolder);
						updated = true;
					}
					if (isPinned is not null)
					{
						library.PinnedToNavigationPane = isPinned == true;
						updated = true;
					}
					if (updated)
					{
						library.Commit();
						library.Reload(); // Reload folders list
						return Task.FromResult(ShellFolderExtensions.GetShellLibraryItem(library, libraryPath));
					}
				}
				catch (Exception e)
				{
					App.Logger.LogWarning(e, null);
				}

				return Task.FromResult<ShellLibraryItem>(null);
			});

			var newLib = item is not null ? new LibraryLocationItem(item) : null;
			if (newLib is not null)
			{
				var libItem = Libraries.FirstOrDefault(l => string.Equals(l.Path, libraryPath, StringComparison.OrdinalIgnoreCase));
				if (libItem is not null)
				{
					lock (libraries)
					{
						libraries[libraries.IndexOf(libItem)] = newLib;
					}
					DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newLib, libItem));
				}
				return newLib;
			}
			return null;
		}

		public (bool result, string reason) CanCreateLibrary(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				return (false, "ErrorInputEmpty".GetLocalizedResource());
			}
			if (FilesystemHelpers.ContainsRestrictedCharacters(name))
			{
				return (false, "ErrorNameInputRestrictedCharacters".GetLocalizedResource());
			}
			if (FilesystemHelpers.ContainsRestrictedFileName(name))
			{
				return (false, "ErrorNameInputRestricted".GetLocalizedResource());
			}
			if (Libraries.Any((item) => string.Equals(name, item.Text, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(name, Path.GetFileNameWithoutExtension(item.Path), StringComparison.OrdinalIgnoreCase)))
			{
				return (false, "CreateLibraryErrorAlreadyExists".GetLocalizedResource());
			}
			return (true, string.Empty);
		}

		public static async Task ShowRestoreDefaultLibrariesDialogAsync()
		{
			var dialog = new DynamicDialog(new DynamicDialogViewModel
			{
				TitleText = "DialogRestoreLibrariesTitleText".GetLocalizedResource(),
				SubtitleText = "DialogRestoreLibrariesSubtitleText".GetLocalizedResource(),
				PrimaryButtonText = "Restore".GetLocalizedResource(),
				CloseButtonText = "Cancel".GetLocalizedResource(),
				PrimaryButtonAction = async (vm, e) =>
				{
					await ContextMenu.InvokeVerb("restorelibraries", ShellLibraryItem.LibrariesPath);
					await App.LibraryManager.UpdateLibrariesAsync();
				},
				CloseButtonAction = (vm, e) => vm.HideDialog(),
				KeyDownAction = (vm, e) =>
				{
					if (e.Key == VirtualKey.Escape)
					{
						vm.HideDialog();
					}
				},
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
			});
			await dialog.ShowAsync();
		}

		public static async Task ShowCreateNewLibraryDialogAsync()
		{
			var inputText = new TextBox
			{
				PlaceholderText = "FolderWidgetCreateNewLibraryInputPlaceholderText".GetLocalizedResource()
			};
			var tipText = new TextBlock
			{
				Text = string.Empty,
				Visibility = Visibility.Collapsed
			};

			var dialog = new DynamicDialog(new DynamicDialogViewModel
			{
				DisplayControl = new Grid
				{
					Children =
					{
						new StackPanel
						{
							Spacing = 4d,
							Children =
							{
								inputText,
								tipText
							}
						}
					}
				},
				TitleText = "FolderWidgetCreateNewLibraryDialogTitleText".GetLocalizedResource(),
				SubtitleText = "SideBarCreateNewLibrary/Text".GetLocalizedResource(),
				PrimaryButtonText = "Create".GetLocalizedResource(),
				CloseButtonText = "Cancel".GetLocalizedResource(),
				PrimaryButtonAction = async (vm, e) =>
				{
					var (result, reason) = App.LibraryManager.CanCreateLibrary(inputText.Text);
					tipText.Text = reason;
					tipText.Visibility = result ? Visibility.Collapsed : Visibility.Visible;
					if (!result)
					{
						e.Cancel = true;
						return;
					}
					await App.LibraryManager.CreateNewLibrary(inputText.Text);
				},
				CloseButtonAction = (vm, e) =>
				{
					vm.HideDialog();
				},
				KeyDownAction = async (vm, e) =>
				{
					if (e.Key == VirtualKey.Enter)
					{
						await App.LibraryManager.CreateNewLibrary(inputText.Text);
					}
					else if (e.Key == VirtualKey.Escape)
					{
						vm.HideDialog();
					}
				},
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Cancel
			});
			await dialog.ShowAsync();
		}

		private void OnLibraryChanged(WatcherChangeTypes changeType, string oldPath, string newPath)
		{
			if (newPath is not null && (!newPath.ToLowerInvariant().EndsWith(ShellLibraryItem.EXTENSION, StringComparison.Ordinal) || !File.Exists(newPath)))
			{
				System.Diagnostics.Debug.WriteLine($"Ignored library event: {changeType}, {oldPath} -> {newPath}");
				return;
			}

			System.Diagnostics.Debug.WriteLine($"Library event: {changeType}, {oldPath} -> {newPath}");

			if (!changeType.HasFlag(WatcherChangeTypes.Deleted))
			{
				var library = SafetyExtensions.IgnoreExceptions(() => new ShellLibraryEx(Shell32.ShellUtil.GetShellItemForPath(newPath), true));
				if (library is null)
				{
					App.Logger.LogWarning($"Failed to open library after {changeType}: {newPath}");
					return;
				}

				var library1 = ShellFolderExtensions.GetShellLibraryItem(library, newPath);

				string? path = oldPath;
				if (string.IsNullOrEmpty(oldPath))
				{
					path = library1?.FullPath;
				}
				var changedLibrary = Libraries.FirstOrDefault(l => string.Equals(l.Path, path, StringComparison.OrdinalIgnoreCase));
				if (changedLibrary is not null)
				{
					lock (libraries)
					{
						libraries.Remove(changedLibrary);
					}
					DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, changedLibrary));
				}
				// library is null in case it was deleted
				if (library is not null && !Libraries.Any(x => x.Path == library1?.FullPath))
				{
					var libItem = new LibraryLocationItem(library1);
					lock (libraries)
					{
						libraries.Add(libItem);
					}
					DataChanged?.Invoke(SectionType.Library, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, libItem));
				}

				library?.Dispose();
			}
		}

		private void OnLibraryChanged(object sender, FileSystemEventArgs e)
		{
			switch (e.ChangeType)
			{
				case WatcherChangeTypes.Created:
				case WatcherChangeTypes.Changed:
					OnLibraryChanged(e.ChangeType, e.FullPath, e.FullPath);
					break;
				case WatcherChangeTypes.Deleted:
					OnLibraryChanged(e.ChangeType, e.FullPath, null);
					break;
			}
		}

		private void OnLibraryRenamed(object sender, RenamedEventArgs e)
			=> OnLibraryChanged(e.ChangeType, e.OldFullPath, e.FullPath);

		public static bool IsLibraryPath(string path)
			=> !string.IsNullOrEmpty(path) && path.EndsWith(ShellLibraryItem.EXTENSION, StringComparison.OrdinalIgnoreCase);

		public void Dispose()
			=> librariesWatcher?.Dispose();
	}
}