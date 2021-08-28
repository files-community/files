using Files.Common;
using System.Text.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Vanara.PInvoke;
using Windows.Foundation.Collections;
using Windows.System;

namespace FilesFullTrust
{
    internal class Win32API
    {
        public static Task<T> StartSTATask<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Thread thread = new Thread(() =>
            {
                Ole32.OleInitialize();
                try
                {
                    tcs.SetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.SetResult(default);
                    Program.Logger.Info(ex, ex.Message);
                    //tcs.SetException(e);
                }
                finally
                {
                    Ole32.OleUninitialize();
                }
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.Normal
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public static Task<T> StartSTATask<T>(Func<Task<T>> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Thread thread = new Thread(async () =>
            {
                Ole32.OleInitialize();
                try
                {
                    tcs.SetResult(await func());
                }
                catch (Exception ex)
                {
                    tcs.SetResult(default);
                    Program.Logger.Info(ex, ex.Message);
                    //tcs.SetException(e);
                }
                finally
                {
                    Ole32.OleUninitialize();
                }
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.Normal
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        public static async Task<string> GetFileAssociationAsync(string filename)
        {
            // Find UWP apps
            var uwpApps = await Launcher.FindFileHandlersAsync(Path.GetExtension(filename));
            if (uwpApps.Any())
            {
                return uwpApps.First().PackageFamilyName;
            }

            // Find desktop apps
            var lpResult = new StringBuilder(2048);
            var hResult = Shell32.FindExecutable(filename, null, lpResult);
            if (hResult.ToInt64() > 32)
            {
                return lpResult.ToString();
            }

            return null;
        }

        public static string ExtractStringFromDLL(string file, int number)
        {
            var lib = Kernel32.LoadLibrary(file);
            StringBuilder result = new StringBuilder(2048);
            User32.LoadString(lib, number, result, result.Capacity);
            Kernel32.FreeLibrary(lib);
            return result.ToString();
        }

        public static string[] CommandLineToArgs(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine))
            {
                return Array.Empty<string>();
            }

            var argv = Shell32.CommandLineToArgvW(commandLine, out int argc);
            if (argv == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        public static (string icon, string overlay) GetFileIconAndOverlay(string path, int thumbnailSize, bool getOverlay = true, bool onlyGetOverlay = false)
        {
            string iconStr = null, overlayStr = null;

            if (!onlyGetOverlay)
            {
                using var shellItem = new Vanara.Windows.Shell.ShellItem(path);
                if (shellItem.IShellItem is Shell32.IShellItemImageFactory fctry)
                {
                    var flags = Shell32.SIIGBF.SIIGBF_BIGGERSIZEOK;
                    if (thumbnailSize < 80) flags |= Shell32.SIIGBF.SIIGBF_ICONONLY;
                    var hres = fctry.GetImage(new SIZE(thumbnailSize, thumbnailSize), flags, out var hbitmap);
                    if (hres == HRESULT.S_OK)
                    {
                        using var image = GetBitmapFromHBitmap(hbitmap);
                        if (image != null)
                        {
                            byte[] bitmapData = (byte[])new ImageConverter().ConvertTo(image, typeof(byte[]));
                            iconStr = Convert.ToBase64String(bitmapData, 0, bitmapData.Length);
                        }
                    }
                    //Marshal.ReleaseComObject(fctry);
                }
            }

            if (getOverlay)
            {
                var shfi = new Shell32.SHFILEINFO();
                var ret = Shell32.SHGetFileInfo(
                    path,
                    0,
                    ref shfi,
                    Shell32.SHFILEINFO.Size,
                    Shell32.SHGFI.SHGFI_OVERLAYINDEX | Shell32.SHGFI.SHGFI_ICON | Shell32.SHGFI.SHGFI_SYSICONINDEX | Shell32.SHGFI.SHGFI_ICONLOCATION);
                if (ret == IntPtr.Zero)
                {
                    return (iconStr, null);
                }

                User32.DestroyIcon(shfi.hIcon);
                Shell32.SHGetImageList(Shell32.SHIL.SHIL_LARGE, typeof(ComCtl32.IImageList).GUID, out var tmp);
                using var imageList = ComCtl32.SafeHIMAGELIST.FromIImageList(tmp);
                if (imageList.IsNull || imageList.IsInvalid)
                {
                    return (iconStr, null);
                }

                var overlayIdx = shfi.iIcon >> 24;
                if (overlayIdx != 0)
                {
                    var overlayImage = imageList.Interface.GetOverlayImage(overlayIdx);
                    using var hOverlay = imageList.Interface.GetIcon(overlayImage, ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT);
                    if (!hOverlay.IsNull && !hOverlay.IsInvalid)
                    {
                        using var image = hOverlay.ToIcon().ToBitmap();
                        byte[] bitmapData = (byte[])new ImageConverter().ConvertTo(image, typeof(byte[]));
                        overlayStr = Convert.ToBase64String(bitmapData, 0, bitmapData.Length);
                    }
                }

                return (iconStr, overlayStr);
            }
            else
            {
                return (iconStr, null);
            }
        }

        public static bool RunPowershellCommand(string command, bool runAsAdmin)
        {
            try
            {
                using Process process = new Process();
                if (runAsAdmin)
                {
                    process.StartInfo.UseShellExecute = true;
                    process.StartInfo.Verb = "runas";
                }
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.Arguments = command;
                process.Start();
                if (process.WaitForExit(30 * 1000))
                {
                    return process.ExitCode == 0;
                }
                return false;
            }
            catch (Win32Exception)
            {
                // If user cancels UAC
                return false;
            }
        }

        public static IList<IconFileInfo> ExtractSelectedIconsFromDLL(string file, IList<int> indexes, int iconSize = 48)
        {
            var iconsList = new List<IconFileInfo>();

            foreach (int index in indexes)
            {
                User32.SafeHICON icon;
                User32.SafeHICON hIcon2;    // This is merely to pass into the function and is unneeded otherwise
                if (Shell32.SHDefExtractIcon(file, -1 * index, 0, out icon, out hIcon2, Convert.ToUInt32(iconSize)) == HRESULT.S_OK)
                {
                    using var image = icon.ToBitmap();
                    byte[] bitmapData = (byte[])new ImageConverter().ConvertTo(image, typeof(byte[]));
                    var icoStr = Convert.ToBase64String(bitmapData, 0, bitmapData.Length);
                    iconsList.Add(new IconFileInfo(icoStr, index));
                    User32.DestroyIcon(icon);
                    User32.DestroyIcon(hIcon2);
                }
            }
            return iconsList;
        }

        public static void UnlockBitlockerDrive(string drive, string password)
        {
            RunPowershellCommand($"-command \"$SecureString = ConvertTo-SecureString '{password}' -AsPlainText -Force; Unlock-BitLocker -MountPoint '{drive}' -Password $SecureString\"", true);
        }

        public static void OpenFormatDriveDialog(string drive)
        {
            // format requires elevation
            int driveIndex = drive.ToUpperInvariant()[0] - 'A';
            RunPowershellCommand($"-command \"$Signature = '[DllImport(\\\"shell32.dll\\\", SetLastError = false)]public static extern uint SHFormatDrive(IntPtr hwnd, uint drive, uint fmtID, uint options);'; $SHFormatDrive = Add-Type -MemberDefinition $Signature -Name \"Win32SHFormatDrive\" -Namespace Win32Functions -PassThru; $SHFormatDrive::SHFormatDrive(0, {driveIndex}, 0xFFFF, 0x0001)\"", true);
        }

        public static void SetVolumeLabel(string driveName, string newLabel)
        {
            // rename requires elevation
            RunPowershellCommand($"-command \"$Signature = '[DllImport(\\\"kernel32.dll\\\", SetLastError = false)]public static extern bool SetVolumeLabel(string lpRootPathName, string lpVolumeName);'; $SetVolumeLabel = Add-Type -MemberDefinition $Signature -Name \"Win32SetVolumeLabel\" -Namespace Win32Functions -PassThru; $SetVolumeLabel::SetVolumeLabel('{driveName}', '{newLabel}')\"", true);
        }

        public static void MountVhdDisk(string vhdPath)
        {
            // mounting requires elevation
            RunPowershellCommand($"-command \"Mount-DiskImage -ImagePath '{vhdPath}'\"", true);
        }

        public static Bitmap GetBitmapFromHBitmap(HBITMAP hBitmap)
        {
            try
            {
                Bitmap bmp = Image.FromHbitmap((IntPtr)hBitmap);
                if (Image.GetPixelFormatSize(bmp.PixelFormat) < 32)
                {
                    return bmp;
                }

                Rectangle bmBounds = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var bmpData = bmp.LockBits(bmBounds, ImageLockMode.ReadOnly, bmp.PixelFormat);
                if (IsAlphaBitmap(bmpData))
                {
                    var alpha = GetAlphaBitmapFromBitmapData(bmpData);
                    bmp.UnlockBits(bmpData);
                    bmp.Dispose();
                    return alpha;
                }

                bmp.UnlockBits(bmpData);
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private static Bitmap GetAlphaBitmapFromBitmapData(BitmapData bmpData)
        {
            using var tmp = new Bitmap(bmpData.Width, bmpData.Height, bmpData.Stride, PixelFormat.Format32bppArgb, bmpData.Scan0);
            Bitmap clone = new Bitmap(tmp.Width, tmp.Height, tmp.PixelFormat);
            using (Graphics gr = Graphics.FromImage(clone))
            {
                gr.DrawImage(tmp, new Rectangle(0, 0, clone.Width, clone.Height));
            }
            return clone;
        }

        private static bool IsAlphaBitmap(BitmapData bmpData)
        {
            for (int y = 0; y <= bmpData.Height - 1; y++)
            {
                for (int x = 0; x <= bmpData.Width - 1; x++)
                {
                    Color pixelColor = Color.FromArgb(
                        Marshal.ReadInt32(bmpData.Scan0, (bmpData.Stride * y) + (4 * x)));

                    if (pixelColor.A > 0 & pixelColor.A < 255)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static async Task SendMessageAsync(NamedPipeServerStream pipe, ValueSet valueSet, string requestID = null)
        {
            var message = new Dictionary<string, object>(valueSet);
            message.Add("RequestID", requestID);
            var serialized = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            await pipe.WriteAsync(serialized, 0, serialized.Length);
        }

        // There is usually no need to define Win32 COM interfaces/P-Invoke methods here.
        // The Vanara library contains the definitions for all members of Shell32.dll, User32.dll and more
        // The ones below are due to bugs in the current version of the library and can be removed once fixed
        // Structure used by SHQueryRecycleBin.
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        public static IEnumerable<HWND> GetDesktopWindows()
        {
            HWND prevHwnd = HWND.NULL;
            var windowsList = new List<HWND>();
            while (true)
            {
                prevHwnd = User32.FindWindowEx(HWND.NULL, prevHwnd, null, null);
                if (prevHwnd == null || prevHwnd == HWND.NULL)
                {
                    break;
                }
                windowsList.Add(prevHwnd);
            }
            return windowsList;
        }

        public static void BringToForeground(IEnumerable<HWND> currentWindows)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(5 * 1000);

            Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(500);

                    var newWindows = GetDesktopWindows().Except(currentWindows).Where(x => User32.IsWindowVisible(x) && !User32.IsIconic(x));
                    if (newWindows.Any())
                    {
                        foreach (var newWindow in newWindows)
                        {
                            User32.SetWindowPos(newWindow, User32.SpecialWindowHandles.HWND_TOPMOST,
                                    0, 0, 0, 0, User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);
                            User32.SetWindowPos(newWindow, User32.SpecialWindowHandles.HWND_NOTOPMOST,
                                0, 0, 0, 0, User32.SetWindowPosFlags.SWP_SHOWWINDOW | User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);
                        }
                        break;
                    }
                }
            });
        }

        public static string GenerateUniquePath(string path)
        {
            string uniquePath = path;

            if (File.Exists(path))
            {
                string nameWithoutExt = Path.GetFileNameWithoutExtension(path);
                string extension = Path.GetExtension(path);
                string directory = Path.GetDirectoryName(path);

                for (ushort count = 1; File.Exists(uniquePath); count++)
                {
                    if (Regex.IsMatch(nameWithoutExt, @".*\(\d+\)"))
                    {
                        uniquePath = Path.Combine(directory, $"{nameWithoutExt.Substring(0, nameWithoutExt.LastIndexOf("(", StringComparison.InvariantCultureIgnoreCase))}({count}){extension}");
                    }
                    else
                    {
                        uniquePath = Path.Combine(directory, $"{nameWithoutExt} ({count}){extension}");
                    }
                }
            }
            else if (Directory.Exists(path))
            {
                string directory = Path.GetDirectoryName(path);
                string Name = Path.GetFileName(path);

                for (ushort Count = 1; Directory.Exists(uniquePath); Count++)
                {
                    if (Regex.IsMatch(Name, @".*\(\d+\)"))
                    {
                        uniquePath = Path.Combine(directory, $"{Name.Substring(0, Name.LastIndexOf("(", StringComparison.InvariantCultureIgnoreCase))}({Count})");
                    }
                    else
                    {
                        uniquePath = Path.Combine(directory, $"{Name} ({Count})");
                    }
                }
            }

            return uniquePath;
        }

        /// <summary>
        /// Gets file path from file FRN
        /// </summary>
        /// <param name="frn">File reference number</param>
        /// <param name="volumeHint">Drive containing the file (e.g. "C:\")</param>
        /// <returns>File path or null</returns>
        public static string PathFromFileId(ulong frn, string volumeHint)
        {
            string volumePath = Path.GetPathRoot(volumeHint);
            using var volumeHandle = Kernel32.CreateFile(volumePath, Kernel32.FileAccess.GENERIC_READ, FileShare.Read, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
            if (volumeHandle.IsInvalid) return null;
            var fileId = new Kernel32.FILE_ID_DESCRIPTOR() { Type = 0, Id = new Kernel32.FILE_ID_DESCRIPTOR.DUMMYUNIONNAME() { FileId = (long)frn } };
            fileId.dwSize = (uint)Marshal.SizeOf(fileId);
            using var hFile = Kernel32.OpenFileById(volumeHandle, fileId, Kernel32.FileAccess.GENERIC_READ, FileShare.Read, null, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
            if (hFile.IsInvalid) return null;
            var sb = new StringBuilder(4096);
            var ret = Kernel32.GetFinalPathNameByHandle(hFile, sb, 4095, 0);
            return (ret != 0) ? sb.ToString() : null;
        }

        public class Win32Window : IWin32Window
        {
            public IntPtr Handle { get; set; }

            public static Win32Window FromLong(long hwnd)
            {
                return new Win32Window() { Handle = new IntPtr(hwnd) };
            }
        }

        // Get information from recycle bin.
        [DllImport(Lib.Shell32, SetLastError = false, CharSet = CharSet.Auto)]
        public static extern int SHQueryRecycleBin(string pszRootPath,
            ref SHQUERYRBINFO pSHQueryRBInfo);
    }
}