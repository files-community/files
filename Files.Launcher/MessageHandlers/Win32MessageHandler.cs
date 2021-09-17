﻿using Files.Common;
using FilesFullTrust.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Vanara.Windows.Shell;
using Windows.ApplicationModel;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace FilesFullTrust.MessageHandlers
{
    public class Win32MessageHandler : IMessageHandler
    {
        public void Initialize(NamedPipeServerStream connection)
        {
        }

        public async Task ParseArgumentsAsync(NamedPipeServerStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "Bitlocker":
                    var bitlockerAction = (string)message["action"];
                    if (bitlockerAction == "Unlock")
                    {
                        var drive = (string)message["drive"];
                        var password = (string)message["password"];
                        Win32API.UnlockBitlockerDrive(drive, password);
                        await Win32API.SendMessageAsync(connection, new ValueSet() { { "Bitlocker", "Unlock" } }, message.Get("RequestID", (string)null));
                    }
                    break;

                case "SetVolumeLabel":
                    var driveName = (string)message["drivename"];
                    var newLabel = (string)message["newlabel"];
                    Win32API.SetVolumeLabel(driveName, newLabel);
                    await Win32API.SendMessageAsync(connection, new ValueSet() { { "SetVolumeLabel", driveName } }, message.Get("RequestID", (string)null));
                    break;

                case "GetIconOverlay":
                    var fileIconPath = (string)message["filePath"];
                    var thumbnailSize = (int)(long)message["thumbnailSize"];
                    var isOverlayOnly = (bool)message["isOverlayOnly"];
                    var iconOverlay = Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(fileIconPath, thumbnailSize, true, isOverlayOnly)).Result;
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "Icon", iconOverlay.icon },
                        { "Overlay", iconOverlay.overlay }
                    }, message.Get("RequestID", (string)null));
                    break;

                case "GetIconWithoutOverlay":
                    var fileIconPath2 = (string)message["filePath"];
                    var thumbnailSize2 = (int)(long)message["thumbnailSize"];
                    var icon2 = Win32API.StartSTATask(() => Win32API.GetFileIconAndOverlay(fileIconPath2, thumbnailSize2, false)).Result;
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "Icon", icon2.icon },
                    }, message.Get("RequestID", (string)null));
                    break;

                case "ShellFolder":
                    // Enumerate shell folder contents and send response to UWP
                    var folderPath = (string)message["folder"];
                    var responseEnum = new ValueSet();
                    var folderContentsList = await Win32API.StartSTATask(() =>
                    {
                        var flc = new List<ShellFileItem>();
                        try
                        {
                            using (var shellFolder = new ShellFolder(folderPath))
                            {
                                foreach (var folderItem in shellFolder)
                                {
                                    try
                                    {
                                        var shellFileItem = ShellFolderExtensions.GetShellFileItem(folderItem);
                                        flc.Add(shellFileItem);
                                    }
                                    catch (FileNotFoundException)
                                    {
                                        // Happens if files are being deleted
                                    }
                                    finally
                                    {
                                        folderItem.Dispose();
                                    }
                                }
                            }
                        }
                        catch
                        {
                        }
                        return flc;
                    });
                    responseEnum.Add("Enumerate", JsonConvert.SerializeObject(folderContentsList));
                    await Win32API.SendMessageAsync(connection, responseEnum, message.Get("RequestID", (string)null));
                    break;

                case "GetSelectedIconsFromDLL":
                    var selectedIconInfos = Win32API.ExtractSelectedIconsFromDLL((string)message["iconFile"], JsonConvert.DeserializeObject<List<int>>((string)message["iconIndexes"]), Convert.ToInt32(message["requestedIconSize"]));
                    await Win32API.SendMessageAsync(connection, new ValueSet()
                    {
                        { "IconInfos", JsonConvert.SerializeObject(selectedIconInfos) },
                    }, message.Get("RequestID", (string)null));
                    break;

                case "SetAsDefaultExplorer":
                    {
                        var enable = (bool)message["Value"];
                        var scriptPath = Path.Combine(Package.Current.InstalledPath, "Assets", "FilesOpenDialog");
                        try
                        {
                            using var regeditProcess = Process.Start("regedit.exe", Path.Combine(scriptPath, enable ? "SetFilesAsDefault.reg" : "UnsetFilesAsDefault"));
                            regeditProcess.WaitForExit();
                        }
                        catch
                        {
                            // Canceled UAC
                        }
                    }
                    break;

                case "SetAsOpenFileDialog":
                    {
                        var enable = (bool)message["Value"];
                        var destFolder = Path.Combine(ApplicationData.Current.LocalFolder.Path, "FilesOpenDialog");
                        if (enable)
                        {
                            foreach (var file in Directory.GetFiles(Path.Combine(Package.Current.InstalledPath, "Assets", "FilesOpenDialog")))
                            {
                                File.Copy(file, Path.Combine(destFolder, Path.GetFileName(file)), true);
                            }

                            using var regProc32 = Process.Start("regsvr32.exe", @$"/s /n /i:user {Path.Combine(destFolder, "CustomOpenDialog32.dll")}");
                            regProc32.WaitForExit();
                            using var regProc64 = Process.Start("regsvr32.exe", @$"/s /n /i:user {Path.Combine(destFolder, "CustomOpenDialog64.dll")}");
                            regProc64.WaitForExit();
                        }
                        else
                        {
                            using var regProc32 = Process.Start("regsvr32.exe", @$"/s /n /u /i:user {Path.Combine(destFolder, "CustomOpenDialog32.dll")}");
                            regProc32.WaitForExit();
                            using var regProc64 = Process.Start("regsvr32.exe", @$"/s /n /u /i:user {Path.Combine(destFolder, "CustomOpenDialog64.dll")}");
                            regProc64.WaitForExit();
                        }
                    }
                    break;
            }
        }

        public void Dispose()
        {
        }
    }
}
