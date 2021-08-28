using Files.Common;
using FilesFullTrust.Helpers;
using System.IO.Pipes;
using System.Text;
using Vanara.PInvoke;
using Windows.Foundation.Collections;
using Windows.Storage;
using System.Text.Json;

namespace FilesFullTrust.MessageHandlers
{
    public class ContextMenuHandler : IMessageHandler
    {
        private DisposableDictionary handleTable;

        public ContextMenuHandler()
        {
            // Create handle table to store context menu references
            handleTable = new DisposableDictionary();
        }

        public void Initialize(NamedPipeServerStream connection)
        {
            // Preload context menu for better performance
            // We query the context menu for the app's local folder
            var preloadPath = ApplicationData.Current.LocalFolder.Path;
            using var _ = ContextMenu.GetContextMenuForFiles(new string[] { preloadPath }, Shell32.CMF.CMF_NORMAL | Shell32.CMF.CMF_SYNCCASCADEMENU, FilterMenuItems(false));
        }

        public async Task ParseArgumentsAsync(NamedPipeServerStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "LoadContextMenu":
                    var contextMenuResponse = new ValueSet();
                    var loadThreadWithMessageQueue = new ThreadWithMessageQueue<Dictionary<string, object>>(HandleMenuMessage);
                    var cMenuLoad = await loadThreadWithMessageQueue.PostMessageAsync<ContextMenu>(message);
                    contextMenuResponse.Add("Handle", handleTable.AddValue(loadThreadWithMessageQueue));
                    contextMenuResponse.Add("ContextMenu", JsonSerializer.Serialize(cMenuLoad, Program.IncludeFieldsOptions));
                    var serializedCm = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(contextMenuResponse, Program.IncludeFieldsOptions));
                    await Win32API.SendMessageAsync(connection, contextMenuResponse, message.Get("RequestID", (string)null));
                    break;

                case "ExecAndCloseContextMenu":
                    var menuKey = (string)message["Handle"];
                    var execThreadWithMessageQueue = handleTable.GetValue<ThreadWithMessageQueue<Dictionary<string, object>>>(menuKey);
                    if (execThreadWithMessageQueue != null)
                    {
                        await execThreadWithMessageQueue.PostMessage(message);
                    }
                    // The following line is needed to cleanup resources when menu is closed.
                    // Unfortunately if you uncomment it some menu items will randomly stop working.
                    // Resource cleanup is currently done on app closing,
                    // if we find a solution for the issue above, we should cleanup as soon as a menu is closed.
                    //handleTable.RemoveValue(menuKey);
                    break;

                case "InvokeVerb":
                    var filePath = (string)message["FilePath"];
                    var split = filePath.Split('|').Where(x => !string.IsNullOrWhiteSpace(x));
                    var verb = (string)message["Verb"];
                    using (var cMenu = ContextMenu.GetContextMenuForFiles(split.ToArray(), Shell32.CMF.CMF_DEFAULTONLY))
                    {
                        var result = cMenu?.InvokeVerb(verb);
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Success", result } }, message.Get("RequestID", (string)null));
                    }
                    break;
            }
        }

        private object HandleMenuMessage(Dictionary<string, object> message, DisposableDictionary table)
        {
            switch (message.Get("Arguments", ""))
            {
                case "LoadContextMenu":
                    var contextMenuResponse = new ValueSet();
                    var filePath = (string)message["FilePath"];
                    var extendedMenu = (bool)message["ExtendedMenu"];
                    var showOpenMenu = (bool)message["ShowOpenMenu"];
                    var split = filePath.Split('|').Where(x => !string.IsNullOrWhiteSpace(x));
                    var cMenuLoad = ContextMenu.GetContextMenuForFiles(split.ToArray(),
                        (extendedMenu ? Shell32.CMF.CMF_EXTENDEDVERBS : Shell32.CMF.CMF_NORMAL) | Shell32.CMF.CMF_SYNCCASCADEMENU, FilterMenuItems(showOpenMenu));
                    table.SetValue("MENU", cMenuLoad);
                    return cMenuLoad;

                case "ExecAndCloseContextMenu":
                    var cMenuExec = table.GetValue<ContextMenu>("MENU");
                    if (message.TryGetValue("ItemID", out var menuId))
                    {
                        switch (message.Get("CommandString", (string)null))
                        {
                            case "mount":
                                var vhdPath = cMenuExec.ItemsPath.First();
                                Win32API.MountVhdDisk(vhdPath);
                                break;

                            case "format":
                                var drivePath = cMenuExec.ItemsPath.First();
                                Win32API.OpenFormatDriveDialog(drivePath);
                                break;

                            default:
                                cMenuExec?.InvokeItem((int)(long)menuId);
                                break;
                        }
                    }
                    // The following line is needed to cleanup resources when menu is closed.
                    // Unfortunately if you uncomment it some menu items will randomly stop working.
                    // Resource cleanup is currently done on app closing,
                    // if we find a solution for the issue above, we should cleanup as soon as a menu is closed.
                    //table.RemoveValue("MENU");
                    return null;

                default:
                    return null;
            }
        }

        private Func<string, bool> FilterMenuItems(bool showOpenMenu)
        {
            var knownItems = new List<string>()
            {
                "opennew", "opencontaining", "opennewprocess",
                "runas", "runasuser", "pintohome", "PinToStartScreen",
                "cut", "copy", "paste", "delete", "properties", "link",
                "Windows.ModernShare", "Windows.Share", "setdesktopwallpaper",
                "eject", "rename", "explore", "openinfiles", "extract",
                "copyaspath", "undelete", "empty",
                Win32API.ExtractStringFromDLL("shell32.dll", 30312), // SendTo menu
                Win32API.ExtractStringFromDLL("shell32.dll", 34593), // Add to collection
            };

            bool filterMenuItemsImpl(string menuItem)
            {
                return string.IsNullOrEmpty(menuItem) ? false : knownItems.Contains(menuItem)
                    || (!showOpenMenu && menuItem.Equals("open", StringComparison.OrdinalIgnoreCase));
            }

            return filterMenuItemsImpl;
        }

        public void Dispose()
        {
            handleTable?.Dispose();
        }
    }
}
