using Files.Common;
using System.Text.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.RegularExpressions;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace FilesFullTrust.MessageHandlers
{
    public class ApplicationLaunchHandler : IMessageHandler
    {
        public void Initialize(NamedPipeServerStream connection)
        {
        }

        public Task ParseArgumentsAsync(NamedPipeServerStream connection, Dictionary<string, object> message, string arguments)
        {
            switch (arguments)
            {
                case "LaunchApp":
                    if (message.ContainsKey("Application"))
                    {
                        var application = (string)message["Application"];
                        HandleApplicationLaunch(application, message);
                    }
                    else if (message.ContainsKey("ApplicationList"))
                    {
                        var applicationList = JsonSerializer.Deserialize<IEnumerable<string>>((string)message["ApplicationList"]);
                        HandleApplicationsLaunch(applicationList, message);
                    }
                    break;
            }
            return Task.CompletedTask;
        }

        private void HandleApplicationsLaunch(IEnumerable<string> applications, Dictionary<string, object> message)
        {
            foreach (var application in applications)
            {
                HandleApplicationLaunch(application, message);
            }
        }

        private async void HandleApplicationLaunch(string application, Dictionary<string, object> message)
        {
            var arguments = message.Get("Parameters", "");
            var workingDirectory = message.Get("WorkingDirectory", "");
            var currentWindows = Win32API.GetDesktopWindows();

            if (new[] { ".vhd", ".vhdx" }.Contains(Path.GetExtension(application).ToLower()))
            {
                // Use powershell to mount vhds as this requires admin rights
                Win32API.MountVhdDisk(application);
                return;
            }

            try
            {
                using Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = application;
                // Show window if workingDirectory (opening terminal)
                process.StartInfo.CreateNoWindow = string.IsNullOrEmpty(workingDirectory);
                if (arguments == "runas")
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runas";
                    if (Path.GetExtension(application).ToLower() == ".msi")
                    {
                        process.StartInfo.FileName = "msiexec.exe";
                        process.StartInfo.Arguments = $"/a \"{application}\"";
                    }
                }
                else if (arguments == "runasuser")
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runasuser";
                    if (Path.GetExtension(application).ToLower() == ".msi")
                    {
                        process.StartInfo.FileName = "msiexec.exe";
                        process.StartInfo.Arguments = $"/i \"{application}\"";
                    }
                }
                else
                {
                    process.StartInfo.Arguments = arguments;
                }
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.Start();
                Win32API.BringToForeground(currentWindows);
            }
            catch (Win32Exception)
            {
                using Process process = new Process();
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = application;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = workingDirectory;
                try
                {
                    process.Start();
                    Win32API.BringToForeground(currentWindows);
                }
                catch (Win32Exception)
                {
                    try
                    {
                        await Win32API.StartSTATask(() =>
                        {
                            var split = application.Split('|').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => GetMtpPath(x));
                            if (split.Count() == 1)
                            {
                                Process.Start(split.First());
                                Win32API.BringToForeground(currentWindows);
                            }
                            else
                            {
                                var groups = split.GroupBy(x => new
                                {
                                    Dir = Path.GetDirectoryName(x),
                                    Prog = Win32API.GetFileAssociationAsync(x).Result ?? Path.GetExtension(x)
                                });
                                foreach (var group in groups)
                                {
                                    if (!group.Any())
                                    {
                                        continue;
                                    }
                                    using var cMenu = ContextMenu.GetContextMenuForFiles(group.ToArray(), Shell32.CMF.CMF_DEFAULTONLY);
                                    cMenu?.InvokeVerb(Shell32.CMDSTR_OPEN);
                                }
                            }
                            return true;
                        });
                    }
                    catch (Win32Exception)
                    {
                        // Cannot open file (e.g DLL)
                    }
                    catch (ArgumentException)
                    {
                        // Cannot open file (e.g DLL)
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Invalid file path
            }
        }

        private string GetMtpPath(string executable)
        {
            if (executable.StartsWith("\\\\?\\"))
            {
                using var computer = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_ComputerFolder);
                using var device = computer.FirstOrDefault(i => executable.Replace("\\\\?\\", "").StartsWith(i.Name));
                var deviceId = device?.ParsingName;
                var itemPath = Regex.Replace(executable, @"^\\\\\?\\[^\\]*\\?", "");
                return deviceId != null ? Path.Combine(deviceId, itemPath) : executable;
            }
            return executable;
        }

        public void Dispose()
        {
        }
    }
}
