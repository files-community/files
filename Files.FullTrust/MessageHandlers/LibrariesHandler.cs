using Files.Common;
using FilesFullTrust.Helpers;
using System.Text.Json;
using System.IO.Pipes;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Foundation.Collections;

namespace FilesFullTrust.MessageHandlers
{
    public class LibrariesHandler : IMessageHandler
    {
        private NamedPipeServerStream connection;

        private FileSystemWatcher librariesWatcher;

        public void Initialize(NamedPipeServerStream connection)
        {
            this.connection = connection;

            StartLibrariesWatcher();
        }

        private void StartLibrariesWatcher()
        {
            librariesWatcher = new FileSystemWatcher
            {
                Path = ShellLibraryItem.LibrariesPath,
                Filter = "*" + ShellLibraryItem.EXTENSION,
                NotifyFilter = NotifyFilters.Attributes | NotifyFilters.LastWrite | NotifyFilters.FileName,
                IncludeSubdirectories = false,
            };

            librariesWatcher.Created += (object _, FileSystemEventArgs e) => OnLibraryChanged(e.ChangeType, e.FullPath, e.FullPath);
            librariesWatcher.Changed += (object _, FileSystemEventArgs e) => OnLibraryChanged(e.ChangeType, e.FullPath, e.FullPath);
            librariesWatcher.Deleted += (object _, FileSystemEventArgs e) => OnLibraryChanged(e.ChangeType, e.FullPath, null);
            librariesWatcher.Renamed += (object _, RenamedEventArgs e) => OnLibraryChanged(e.ChangeType, e.OldFullPath, e.FullPath);
            librariesWatcher.EnableRaisingEvents = true;
        }

        public async Task ParseArgumentsAsync(NamedPipeServerStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "ShellLibrary":
                    await HandleShellLibraryMessage(message);
                    break;
            }
        }

        private async void OnLibraryChanged(WatcherChangeTypes changeType, string oldPath, string newPath)
        {
            if (newPath != null && (!newPath.ToLower().EndsWith(ShellLibraryItem.EXTENSION) || !File.Exists(newPath)))
            {
                System.Diagnostics.Debug.WriteLine($"Ignored library event: {changeType}, {oldPath} -> {newPath}");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Library event: {changeType}, {oldPath} -> {newPath}");

            if (connection?.IsConnected ?? false)
            {
                var response = new ValueSet { { "Library", newPath ?? oldPath } };
                switch (changeType)
                {
                    case WatcherChangeTypes.Deleted:
                    case WatcherChangeTypes.Renamed:
                        response["OldPath"] = oldPath;
                        break;

                    default:
                        break;
                }
                if (!changeType.HasFlag(WatcherChangeTypes.Deleted))
                {
                    var library = ShellItem.Open(newPath) as ShellLibrary;
                    if (library == null)
                    {
                        Program.Logger.Warn($"Failed to open library after {changeType}: {newPath}");
                        return;
                    }
                    response["Item"] = JsonSerializer.Serialize(ShellFolderExtensions.GetShellLibraryItem(library, newPath), Program.IncludeFieldsOptions);
                    library.Dispose();
                }
                // Send message to UWP app to refresh items
                await Win32API.SendMessageAsync(connection, response);
            }
        }

        private async Task HandleShellLibraryMessage(Dictionary<string, object> message)
        {
            switch ((string)message["action"])
            {
                case "Enumerate":
                    // Read library information and send response to UWP
                    var enumerateResponse = await Win32API.StartSTATask(() =>
                    {
                        var response = new ValueSet();
                        try
                        {
                            var libraryItems = new List<ShellLibraryItem>();
                            // https://docs.microsoft.com/en-us/windows/win32/search/-search-win7-development-scenarios#library-descriptions
                            var libFiles = Directory.EnumerateFiles(ShellLibraryItem.LibrariesPath, "*" + ShellLibraryItem.EXTENSION);
                            foreach (var libFile in libFiles)
                            {
                                using var shellItem = ShellItem.Open(libFile);
                                if (shellItem is ShellLibrary library)
                                {
                                    libraryItems.Add(ShellFolderExtensions.GetShellLibraryItem(library, libFile));
                                }
                            }
                            response.Add("Enumerate", JsonSerializer.Serialize(libraryItems, Program.IncludeFieldsOptions));
                        }
                        catch (Exception e)
                        {
                            Program.Logger.Warn(e);
                        }
                        return response;
                    });
                    await Win32API.SendMessageAsync(connection, enumerateResponse, message.Get("RequestID", (string)null));
                    break;

                case "Create":
                    // Try create new library with the specified name and send response to UWP
                    var createResponse = await Win32API.StartSTATask(() =>
                    {
                        var response = new ValueSet();
                        try
                        {
                            using var library = new ShellLibrary((string)message["library"], Shell32.KNOWNFOLDERID.FOLDERID_Libraries, false);
                            response.Add("Create", JsonSerializer.Serialize(ShellFolderExtensions.GetShellLibraryItem(library, library.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing)), Program.IncludeFieldsOptions));
                        }
                        catch (Exception e)
                        {
                            Program.Logger.Warn(e);
                        }
                        return response;
                    });
                    await Win32API.SendMessageAsync(connection, createResponse, message.Get("RequestID", (string)null));
                    break;

                case "Update":
                    // Update details of the specified library and send response to UWP
                    var updateResponse = await Win32API.StartSTATask(() =>
                    {
                        var response = new ValueSet();
                        try
                        {
                            var folders = message.ContainsKey("folders") ? JsonSerializer.Deserialize<string[]>((string)message["folders"]) : null;
                            var defaultSaveFolder = message.Get("defaultSaveFolder", (string)null);
                            var isPinned = message.Get("isPinned", (bool?)null);

                            bool updated = false;
                            var libPath = (string)message["library"];
                            using var library = ShellItem.Open(libPath) as ShellLibrary;
                            if (folders != null)
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
                            if (defaultSaveFolder != null)
                            {
                                library.DefaultSaveFolder = ShellItem.Open(defaultSaveFolder);
                                updated = true;
                            }
                            if (isPinned != null)
                            {
                                library.PinnedToNavigationPane = isPinned == true;
                                updated = true;
                            }
                            if (updated)
                            {
                                library.Commit();
                                response.Add("Update", JsonSerializer.Serialize(ShellFolderExtensions.GetShellLibraryItem(library, libPath), Program.IncludeFieldsOptions));
                            }
                        }
                        catch (Exception e)
                        {
                            Program.Logger.Warn(e);
                        }
                        return response;
                    });
                    await Win32API.SendMessageAsync(connection, updateResponse, message.Get("RequestID", (string)null));
                    break;
            }
        }

        public void Dispose()
        {
            librariesWatcher?.Dispose();
        }
    }
}
