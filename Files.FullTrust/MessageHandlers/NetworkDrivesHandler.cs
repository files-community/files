using Files.Common;
using Microsoft.Win32;
using System.IO.Pipes;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using Windows.Foundation.Collections;

namespace FilesFullTrust.MessageHandlers
{
    public class NetworkDrivesHandler : IMessageHandler
    {
        public void Initialize(NamedPipeServerStream connection)
        {
        }

        public async Task ParseArgumentsAsync(NamedPipeServerStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "NetworkDriveOperation":
                    await ParseNetworkDriveOperationAsync(connection, message);
                    break;

                case "GetOneDriveAccounts":
                    try
                    {
                        var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts", false);

                        if (oneDriveAccountsKey == null)
                        {
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Count", 0 } }, message.Get("RequestID", (string)null));
                            return;
                        }

                        var oneDriveAccounts = new ValueSet();
                        foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
                        {
                            var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
                            var displayName = (string)Registry.GetValue(accountKeyName, "DisplayName", null);
                            var userFolder = (string)Registry.GetValue(accountKeyName, "UserFolder", null);
                            var accountName = string.IsNullOrWhiteSpace(displayName) ? "OneDrive" : $"OneDrive - {displayName}";
                            if (!string.IsNullOrWhiteSpace(userFolder) && !oneDriveAccounts.ContainsKey(accountName))
                            {
                                oneDriveAccounts.Add(accountName, userFolder);
                            }
                        }
                        oneDriveAccounts.Add("Count", oneDriveAccounts.Count);
                        await Win32API.SendMessageAsync(connection, oneDriveAccounts, message.Get("RequestID", (string)null));
                    }
                    catch
                    {
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Count", 0 } }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "GetSharePointSyncLocationsFromOneDrive":
                    try
                    {
                        using var oneDriveAccountsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\OneDrive\Accounts", false);

                        if (oneDriveAccountsKey == null)
                        {
                            await Win32API.SendMessageAsync(connection, new ValueSet() { { "Count", 0 } }, message.Get("RequestID", (string)null));
                            return;
                        }

                        var sharepointAccounts = new ValueSet();

                        foreach (var account in oneDriveAccountsKey.GetSubKeyNames())
                        {
                            var accountKeyName = @$"{oneDriveAccountsKey.Name}\{account}";
                            var displayName = (string)Registry.GetValue(accountKeyName, "DisplayName", null);
                            var userFolderToExcludeFromResults = (string)Registry.GetValue(accountKeyName, "UserFolder", null);
                            var accountName = string.IsNullOrWhiteSpace(displayName) ? "SharePoint" : $"SharePoint - {displayName}";

                            var sharePointSyncFolders = new List<string>();
                            var mountPointKeyName = @$"SOFTWARE\Microsoft\OneDrive\Accounts\{account}\ScopeIdToMountPointPathCache";
                            using (var mountPointsKey = Registry.CurrentUser.OpenSubKey(mountPointKeyName))
                            {
                                if (mountPointsKey == null)
                                {
                                    continue;
                                }

                                var valueNames = mountPointsKey.GetValueNames();
                                foreach (var valueName in valueNames)
                                {
                                    var value = (string)Registry.GetValue(@$"HKEY_CURRENT_USER\{mountPointKeyName}", valueName, null);
                                    if (!string.Equals(value, userFolderToExcludeFromResults, StringComparison.OrdinalIgnoreCase))
                                    {
                                        sharePointSyncFolders.Add(value);
                                    }
                                }
                            }

                            foreach (var sharePointSyncFolder in sharePointSyncFolders.OrderBy(o => o))
                            {
                                var parentFolder = Directory.GetParent(sharePointSyncFolder)?.FullName ?? string.Empty;
                                if (!sharepointAccounts.Any(acc => string.Equals(acc.Key, accountName, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrWhiteSpace(parentFolder))
                                {
                                    sharepointAccounts.Add(accountName, parentFolder);
                                }
                            }
                        }

                        sharepointAccounts.Add("Count", sharepointAccounts.Count);
                        await Win32API.SendMessageAsync(connection, sharepointAccounts, message.Get("RequestID", (string)null));
                    }
                    catch
                    {
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Count", 0 } }, message.Get("RequestID", (string)null));
                    }
                    break;
            }
        }

        private async Task ParseNetworkDriveOperationAsync(NamedPipeServerStream connection, Dictionary<string, object> message)
        {
            switch (message.Get("netdriveop", ""))
            {
                case "GetNetworkLocations":
                    var networkLocations = await Win32API.StartSTATask(() =>
                    {
                        var netl = new ValueSet();
                        using (var nethood = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_NetHood))
                        {
                            foreach (var link in nethood)
                            {
                                var linkPath = (string)link.Properties["System.Link.TargetParsingPath"];
                                if (linkPath != null)
                                {
                                    netl.Add(link.Name, linkPath);
                                }
                            }
                        }
                        return netl;
                    });
                    networkLocations ??= new ValueSet();
                    networkLocations.Add("Count", networkLocations.Count);
                    await Win32API.SendMessageAsync(connection, networkLocations, message.Get("RequestID", (string)null));
                    break;

                case "OpenMapNetworkDriveDialog":
                    var hwnd = (long)message["HWND"];
                    _ = NetworkDrivesAPI.OpenMapNetworkDriveDialog(hwnd);
                    break;

                case "DisconnectNetworkDrive":
                    var drivePath = (string)message["drive"];
                    _ = NetworkDrivesAPI.DisconnectNetworkDrive(drivePath);
                    break;
            }
        }

        public void Dispose()
        {
        }
    }
}
