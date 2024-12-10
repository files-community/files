// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.Extensions.Logging;
using Microsoft.Win32.SafeHandles;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;
using Vanara.PInvoke;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using static Vanara.PInvoke.Kernel32;
using COMPRESSION_FORMAT = Windows.Win32.Storage.FileSystem.COMPRESSION_FORMAT;
using HRESULT = Vanara.PInvoke.HRESULT;
using HWND = Vanara.PInvoke.HWND;

namespace Files.App.Helpers
{
	/// <summary>
	/// Provides static helper for Win32.
	/// </summary>
	public static partial class Win32Helper
	{
		public static Task StartSTATask(Func<Task> func)
		{
			var taskCompletionSource = new TaskCompletionSource();
			Thread thread = new Thread(async () =>
			{
				Ole32.OleInitialize();

				try
				{
					await func();
					taskCompletionSource.SetResult();
				}
				catch (Exception ex)
				{
					taskCompletionSource.SetResult();
					App.Logger.LogWarning(ex, ex.Message);
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

			return taskCompletionSource.Task;
		}

		public static Task StartSTATask(Action action)
		{
			var taskCompletionSource = new TaskCompletionSource();
			Thread thread = new Thread(() =>
			{
				Ole32.OleInitialize();

				try
				{
					action();
					taskCompletionSource.SetResult();
				}
				catch (Exception ex)
				{
					taskCompletionSource.SetResult();
					App.Logger.LogWarning(ex, ex.Message);
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

			return taskCompletionSource.Task;
		}

		public static Task<T?> StartSTATask<T>(Func<T> func)
		{
			var taskCompletionSource = new TaskCompletionSource<T?>();

			Thread thread = new Thread(() =>
			{
				Ole32.OleInitialize();

				try
				{
					taskCompletionSource.SetResult(func());
				}
				catch (Exception ex)
				{
					taskCompletionSource.SetResult(default);
					App.Logger.LogWarning(ex, ex.Message);
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

			return taskCompletionSource.Task;
		}

		public static Task<T?> StartSTATask<T>(Func<Task<T>> func)
		{
			var taskCompletionSource = new TaskCompletionSource<T?>();

			Thread thread = new Thread(async () =>
			{
				Ole32.OleInitialize();
				try
				{
					taskCompletionSource.SetResult(await func());
				}
				catch (Exception ex)
				{
					taskCompletionSource.SetResult(default);
					App.Logger.LogInformation(ex, ex.Message);
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

			return taskCompletionSource.Task;
		}

		public static async Task<string?> GetFileAssociationAsync(string filename, bool checkDesktopFirst = false)
		{
			// Find UWP apps
			async Task<string?> GetUwpAssoc()
			{
				var uwpApps = await Launcher.FindFileHandlersAsync(Path.GetExtension(filename));
				return uwpApps.Any() ? uwpApps[0].PackageFamilyName : null;
			}

			// Find desktop apps
			string? GetDesktopAssoc()
			{
				var lpResult = new StringBuilder(2048);
				var hResult = Shell32.FindExecutable(filename, null, lpResult);

				return hResult.ToInt64() > 32 ? lpResult.ToString() : null;
			}

			if (checkDesktopFirst)
				return GetDesktopAssoc() ?? await GetUwpAssoc();

			return await GetUwpAssoc() ?? GetDesktopAssoc();
		}

		public static string ExtractStringFromDLL(string file, int number)
		{
			var lib = Kernel32.LoadLibrary(file);
			StringBuilder result = new StringBuilder(2048);

			_ = User32.LoadString(lib, number, result, result.Capacity);
			Kernel32.FreeLibrary(lib);

			return result.ToString();
		}

		public static string?[] CommandLineToArgs(string commandLine)
		{
			if (string.IsNullOrEmpty(commandLine))
				return [];

			var argv = Shell32.CommandLineToArgvW(commandLine, out int argc);
			if (argv == IntPtr.Zero)
				throw new Win32Exception();

			try
			{
				var args = new string?[argc];
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

		private static readonly object _iconOverlayLock = new object();

		/// <summary>
		/// Returns overlay for given file or folder
		/// </summary>
		/// <param name="path"></param>
		/// <param name="isDirectory"></param>
		/// <returns></returns>
		public static byte[]? GetIconOverlay(string path, bool isDirectory)
		{
			var shFileInfo = new Shell32.SHFILEINFO();
			const Shell32.SHGFI flags = Shell32.SHGFI.SHGFI_OVERLAYINDEX | Shell32.SHGFI.SHGFI_ICON | Shell32.SHGFI.SHGFI_SYSICONINDEX | Shell32.SHGFI.SHGFI_ICONLOCATION;
			byte[]? overlayData = null;

			try
			{
				IntPtr result = Shell32.SHGetFileInfo(path, isDirectory ? FileAttributes.Directory : 0, ref shFileInfo, Shell32.SHFILEINFO.Size, flags);
				if (result == IntPtr.Zero)
					return null;

				User32.DestroyIcon(shFileInfo.hIcon);

				lock (_iconOverlayLock)
				{
					if (!Shell32.SHGetImageList(Shell32.SHIL.SHIL_LARGE, typeof(ComCtl32.IImageList).GUID, out var imageListOut).Succeeded)
						return null;

					var imageList = (ComCtl32.IImageList)imageListOut;

					var overlayIdx = shFileInfo.iIcon >> 24;
					if (overlayIdx != 0)
					{
						var overlayImage = imageList.GetOverlayImage(overlayIdx);

						using var hOverlay = imageList.GetIcon(overlayImage, ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT);

						if (!hOverlay.IsNull && !hOverlay.IsInvalid)
						{
							using var icon = hOverlay.ToIcon();
							using var image = icon.ToBitmap();

							overlayData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
						}
					}

					Marshal.ReleaseComObject(imageList);
				}
			}
			catch (Exception)
			{
				return null;
			}

			return overlayData;
		}

		private static readonly object _iconLock = new object();

		/// <summary>
		/// Returns an icon if returnIconOnly is true, otherwise a thumbnail will be returned if available.
		/// </summary>
		/// <param name="path"></param>
		/// <param name="size"></param>
		/// <param name="isFolder"></param>
		/// <param name="iconOptions"></param>
		/// <returns></returns>
		public static byte[]? GetIcon(
			string path,
			int size,
			bool isFolder,
			IconOptions iconOptions)
		{
			byte[]? iconData = null;

			try
			{
				// Attempt to get file icon/thumbnail using IShellItemImageFactory GetImage
				using var shellItem = SafetyExtensions.IgnoreExceptions(()
					=> ShellFolderExtensions.GetShellItemFromPathOrPIDL(path));

				if (shellItem is not null && shellItem.IShellItem is Shell32.IShellItemImageFactory shellFactory)
				{
					var flags = Shell32.SIIGBF.SIIGBF_BIGGERSIZEOK;

					if (iconOptions.HasFlag(IconOptions.ReturnIconOnly))
						flags |= Shell32.SIIGBF.SIIGBF_ICONONLY;

					if (iconOptions.HasFlag(IconOptions.ReturnThumbnailOnly))
						flags |= Shell32.SIIGBF.SIIGBF_THUMBNAILONLY;

					if (iconOptions.HasFlag(IconOptions.ReturnOnlyIfCached))
						flags |= Shell32.SIIGBF.SIIGBF_INCACHEONLY;

					var hres = shellFactory.GetImage(new SIZE(size, size), flags, out var hbitmap);
					if (hres == HRESULT.S_OK)
					{
						using var image = GetBitmapFromHBitmap(hbitmap);
						if (image is not null)
							iconData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
					}

					Marshal.ReleaseComObject(shellFactory);
				}

				if (iconData is not null || iconOptions.HasFlag(IconOptions.ReturnThumbnailOnly))
					return iconData;
				else
				{
					var shfi = new Shell32.SHFILEINFO();
					const Shell32.SHGFI flags = Shell32.SHGFI.SHGFI_OVERLAYINDEX | Shell32.SHGFI.SHGFI_ICON | Shell32.SHGFI.SHGFI_SYSICONINDEX | Shell32.SHGFI.SHGFI_ICONLOCATION | Shell32.SHGFI.SHGFI_USEFILEATTRIBUTES;

					// Cannot access file, use file attributes
					var useFileAttibutes = iconData is null;

					var ret = Shell32.SHGetFileInfo(path, isFolder ? FileAttributes.Directory : 0, ref shfi, Shell32.SHFILEINFO.Size, flags);
					if (ret == IntPtr.Zero)
						return iconData;

					User32.DestroyIcon(shfi.hIcon);

					var imageListSize = size switch
					{
						<= 16 => Shell32.SHIL.SHIL_SMALL,
						<= 32 => Shell32.SHIL.SHIL_LARGE,
						<= 48 => Shell32.SHIL.SHIL_EXTRALARGE,
						_ => Shell32.SHIL.SHIL_JUMBO,
					};

					lock (_iconLock)
					{
						if (!Shell32.SHGetImageList(imageListSize, typeof(ComCtl32.IImageList).GUID, out var imageListOut).Succeeded)
							return iconData;

						var imageList = (ComCtl32.IImageList)imageListOut;

						if (iconData is null)
						{
							var iconIdx = shfi.iIcon & 0xFFFFFF;
							if (iconIdx != 0)
							{
								// Could not fetch thumbnail, load simple icon
								using var hIcon = imageList.GetIcon(iconIdx, ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT);
								if (!hIcon.IsNull && !hIcon.IsInvalid)
								{
									using (var icon = hIcon.ToIcon())
									using (var image = icon.ToBitmap())
									{
										iconData = (byte[]?)new ImageConverter().ConvertTo(image, typeof(byte[]));
									}
								}
							}
							else if (isFolder)
							{
								// Could not icon, load generic icon
								var icons = ExtractSelectedIconsFromDLL(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "imageres.dll"), [2], size);
								var generic = icons.SingleOrDefault(x => x.Index == 2);
								iconData = generic?.IconData;
							}
							else
							{
								// Could not icon, load generic icon
								var icons = ExtractSelectedIconsFromDLL(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll"), [1], size);
								var generic = icons.SingleOrDefault(x => x.Index == 1);
								iconData = generic?.IconData;
							}
						}

						Marshal.ReleaseComObject(imageList);
					}

					return iconData;
				}
			}
			finally
			{

			}
		}

		public static async Task<bool> RunPowershellCommandAsync(string command, PowerShellExecutionOptions options)
		{
			using Process process = CreatePowershellProcess(command, options);
			using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(30 * 1000));

			try
			{
				process.Start();
				await process.WaitForExitAsync(cts.Token);
				return process.ExitCode == 0;
			}
			catch (OperationCanceledException)
			{
				return false;
			}
			catch (InvalidOperationException ex)
			{
				App.Logger.LogWarning(ex, ex.Message);
				return false;
			}
			catch (Win32Exception)
			{
				// If user cancels UAC
				return false;
			}
		}

		public static bool RunPowershellCommand(string command, PowerShellExecutionOptions options)
		{
			try
			{
				using Process process = CreatePowershellProcess(command, options);

				process.Start();

				if (process.WaitForExit(30 * 1000))
					return process.ExitCode == 0;

				return false;
			}
			catch (Win32Exception)
			{
				// If user cancels UAC
				return false;
			}
		}

		private static readonly ConcurrentDictionary<(string File, int Index, int Size), IconFileInfo> _iconCache = new();

		public static IList<IconFileInfo> ExtractSelectedIconsFromDLL(string file, IList<int> indexes, int iconSize = 48)
		{
			var iconsList = new List<IconFileInfo>();

			foreach (int index in indexes)
			{
				if (_iconCache.TryGetValue((file, index, iconSize), out var iconInfo))
				{
					iconsList.Add(iconInfo);
				}
				else
				{
					// This is merely to pass into the function and is unneeded otherwise
					if (Shell32.SHDefExtractIcon(file, -1 * index, 0, out User32.SafeHICON icon, out User32.SafeHICON hIcon2, Convert.ToUInt32(iconSize)) == HRESULT.S_OK)
					{
						using var image = icon.ToBitmap();
						byte[] bitmapData = (byte[])(new ImageConverter().ConvertTo(image, typeof(byte[])) ?? Array.Empty<byte>());
						iconInfo = new IconFileInfo(bitmapData, index);
						_iconCache[(file, index, iconSize)] = iconInfo;
						iconsList.Add(iconInfo);
						User32.DestroyIcon(icon);
						User32.DestroyIcon(hIcon2);
					}
				}
			}

			return iconsList;
		}

		public static IList<IconFileInfo>? ExtractIconsFromDLL(string file)
		{
			var iconsList = new List<IconFileInfo>();
			using var currentProc = Process.GetCurrentProcess();

			using var icoCnt = Shell32.ExtractIcon(currentProc.Handle, file, -1);
			if (icoCnt is null)
				return null;

			int count = icoCnt.DangerousGetHandle().ToInt32();
			if (count <= 0)
				return null;

			for (int i = 0; i < count; i++)
			{
				if (_iconCache.TryGetValue((file, i, -1), out var iconInfo))
				{
					iconsList.Add(iconInfo);
				}
				else
				{
					using var icon = Shell32.ExtractIcon(currentProc.Handle, file, i);
					using var image = icon.ToBitmap();

					byte[] bitmapData = (byte[])(new ImageConverter().ConvertTo(image, typeof(byte[])) ?? Array.Empty<byte>());
					iconInfo = new IconFileInfo(bitmapData, i);
					_iconCache[(file, i, -1)] = iconInfo;
					iconsList.Add(iconInfo);
				}
			}

			return iconsList;
		}

		public static bool SetCustomDirectoryIcon(string? folderPath, string? iconFile, int iconIndex = 0)
		{
			if (folderPath is null)
				return false;

			var fcs = new Shell32.SHFOLDERCUSTOMSETTINGS()
			{
				dwMask = Shell32.FOLDERCUSTOMSETTINGSMASK.FCSM_ICONFILE,
				pszIconFile = iconFile,
				cchIconFile = 0,
				iIconIndex = iconIndex,
			};

			fcs.dwSize = (uint)Marshal.SizeOf(fcs);

			var success = Shell32.SHGetSetFolderCustomSettings(ref fcs, folderPath, Shell32.FCS.FCS_FORCEWRITE).Succeeded;

			return success;
		}

		public static bool SetCustomFileIcon(string? filePath, string? iconFile, int iconIndex = 0)
		{
			if (filePath is null)
				return false;

			var success = FileOperationsHelpers.SetLinkIcon(filePath, iconFile, iconIndex);

			return success;
		}

		public static Task OpenFormatDriveDialog(string drive)
		{
			// Format requires elevation
			int driveIndex = drive.ToUpperInvariant()[0] - 'A';
			return RunPowershellCommandAsync($"-command \"$Signature = '[DllImport(\\\"shell32.dll\\\", SetLastError = false)]public static extern uint SHFormatDrive(IntPtr hwnd, uint drive, uint fmtID, uint options);'; $SHFormatDrive = Add-Type -MemberDefinition $Signature -Name \"Win32SHFormatDrive\" -Namespace Win32Functions -PassThru; $SHFormatDrive::SHFormatDrive(0, {driveIndex}, 0xFFFF, 0x0001)\"", PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
		}

		public static void SetVolumeLabel(string drivePath, string newLabel)
		{
			// Rename requires elevation
			RunPowershellCommand($"-command \"$Signature = '[DllImport(\\\"kernel32.dll\\\", SetLastError = false)]public static extern bool SetVolumeLabel(string lpRootPathName, string lpVolumeName);'; $SetVolumeLabel = Add-Type -MemberDefinition $Signature -Name \"Win32SetVolumeLabel\" -Namespace Win32Functions -PassThru; $SetVolumeLabel::SetVolumeLabel('{drivePath}', '{newLabel}')\"", PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
		}

		public static void SetNetworkDriveLabel(string driveName, string newLabel)
		{
			RunPowershellCommand($"-command \"(New-Object -ComObject Shell.Application).NameSpace('{driveName}').Self.Name='{newLabel}'\"", PowerShellExecutionOptions.Hidden);
		}

		public static Task<bool> MountVhdDisk(string vhdPath)
		{
			// Mounting requires elevation
			return RunPowershellCommandAsync($"-command \"Mount-DiskImage -ImagePath '{vhdPath}'\"", PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
		}

		public static Bitmap? GetBitmapFromHBitmap(HBITMAP hBitmap)
		{
			try
			{
				Bitmap bmp = Image.FromHbitmap((IntPtr)hBitmap);
				if (Image.GetPixelFormatSize(bmp.PixelFormat) < 32)
					return bmp;

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

		public static Shell32.ITaskbarList4? CreateTaskbarObject()
		{
			try
			{
				var taskbar2 = new Shell32.ITaskbarList2();
				taskbar2.HrInit();
				return taskbar2 as Shell32.ITaskbarList4;
			}
			catch (Exception)
			{
				// explorer.exe is not running as a shell
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
						return true;
				}
			}

			return false;
		}

		public static IEnumerable<HWND> GetDesktopWindows()
		{
			HWND prevHwnd = HWND.NULL;
			var windowsList = new List<HWND>();

			while (true)
			{
				prevHwnd = User32.FindWindowEx(HWND.NULL, prevHwnd, null, null);
				if (prevHwnd == HWND.NULL)
					break;

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
							User32.SetWindowPos(
								newWindow,
								User32.SpecialWindowHandles.HWND_TOPMOST,
								0, 0, 0, 0,
								User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);

							User32.SetWindowPos(
								newWindow,
								User32.SpecialWindowHandles.HWND_NOTOPMOST,
								0, 0, 0, 0,
								User32.SetWindowPosFlags.SWP_SHOWWINDOW | User32.SetWindowPosFlags.SWP_NOSIZE | User32.SetWindowPosFlags.SWP_NOMOVE);
						}

						break;
					}
				}
			});
		}

		/// <summary>
		/// Gets file path from file FRN
		/// </summary>
		/// <param name="frn">File reference number</param>
		/// <param name="volumeHint">Drive containing the file (e.g. "C:\")</param>
		/// <returns>File path or null</returns>
		public static string? PathFromFileId(ulong frn, string volumeHint)
		{
			string? volumePath = Path.GetPathRoot(volumeHint);

			using var volumeHandle = Kernel32.CreateFile(volumePath, Kernel32.FileAccess.GENERIC_READ, FileShare.Read, null, FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
			if (volumeHandle.IsInvalid)
				return null;

			var fileId = new Kernel32.FILE_ID_DESCRIPTOR() { Type = 0, Id = new Kernel32.FILE_ID_DESCRIPTOR.DUMMYUNIONNAME() { FileId = (long)frn } };
			fileId.dwSize = (uint)Marshal.SizeOf(fileId);

			using var hFile = Kernel32.OpenFileById(volumeHandle, fileId, Kernel32.FileAccess.GENERIC_READ, FileShare.Read, null, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
			if (hFile.IsInvalid)
				return null;

			var sb = new StringBuilder(4096);
			var ret = Kernel32.GetFinalPathNameByHandle(hFile, sb, 4095, 0);

			return (ret != 0) ? sb.ToString() : null;
		}

		public sealed class Win32Window : IWin32Window
		{
			public IntPtr Handle { get; set; }

			public static Win32Window FromLong(long hwnd)
				=> new Win32Window() { Handle = new IntPtr(hwnd) };
		}

		public static void OpenFolderInExistingShellWindow(string folderPath)
		{
			var opened = false;

			if (Ole32.CoCreateInstance(typeof(Shell32.ShellWindows).GUID, null, Ole32.CLSCTX.CLSCTX_LOCAL_SERVER, typeof(Shell32.IShellWindows).GUID, out var shellWindowsUnk).Succeeded)
			{
				var shellWindows = (Shell32.IShellWindows)shellWindowsUnk;

				using var controlPanelCategoryView = new Vanara.Windows.Shell.ShellItem("::{26EE0668-A00A-44D7-9371-BEB064C98683}");

				for (int i = 0; i < shellWindows.Count; i++)
				{
					var item = shellWindows.Item(i);

					var serv = (Shell32.IServiceProvider)item;
					if (serv is not null)
					{
						if (serv.QueryService(Shell32.SID_STopLevelBrowser, typeof(Shell32.IShellBrowser).GUID, out var ppv).Succeeded)
						{
							var pUnk = Marshal.GetObjectForIUnknown(ppv);
							var shellBrowser = (Shell32.IShellBrowser)pUnk;

							using var targetFolder = SafetyExtensions.IgnoreExceptions(() => new Vanara.Windows.Shell.ShellItem(folderPath));
							if (targetFolder is not null)
							{
								if (shellBrowser.QueryActiveShellView(out var shellView).Succeeded)
								{
									var folderView = (Shell32.IFolderView)shellView;
									var folder = folderView.GetFolder<Shell32.IPersistFolder2>();
									var folderPidl = new Shell32.PIDL(IntPtr.Zero);

									if (folder.GetCurFolder(ref folderPidl).Succeeded)
									{
										if (folderPidl.IsParentOf(targetFolder.PIDL.DangerousGetHandle(), true) ||
											folderPidl.Equals(controlPanelCategoryView.PIDL))
										{
											if (shellBrowser.BrowseObject(targetFolder.PIDL.DangerousGetHandle(), Shell32.SBSP.SBSP_SAMEBROWSER | Shell32.SBSP.SBSP_ABSOLUTE).Succeeded)
											{
												opened = true;

												break;
											}
										}
									}

									folderPidl.Dispose();

									Marshal.ReleaseComObject(folder);
									Marshal.ReleaseComObject(folderView);
									Marshal.ReleaseComObject(shellView);
								}
							}

							Marshal.ReleaseComObject(shellBrowser);
							Marshal.ReleaseComObject(pUnk);
						}

						Marshal.ReleaseComObject(serv);
					}

					Marshal.ReleaseComObject(item);
				}

				Marshal.ReleaseComObject(shellWindows);
				Marshal.ReleaseComObject(shellWindowsUnk);
			}

			if (!opened)
			{
				Shell32.ShellExecute(
					HWND.NULL,
					"open",
					Environment.ExpandEnvironmentVariables("%windir%\\explorer.exe"),
					folderPath,
					null,
					ShowWindowCommand.SW_SHOWNORMAL);
			}
		}

		public static async Task<bool> InstallInf(string filePath)
		{
			try
			{
				var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(30 * 1000));

				using Process process = new();
				process.StartInfo.FileName = "InfDefaultInstall.exe";
				process.StartInfo.Verb = "runas";
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.Arguments = $"\"{filePath}\"";
				process.Start();

				await process.WaitForExitAsync(cts.Token);

				return true;
			}
			catch
			{
				return false;
			}
		}

		public static async Task InstallFontsAsync(string[] fontFilePaths, bool forAllUsers)
		{
			string fontDirectory = forAllUsers
				? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts")
				: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Fonts");

			string registryKey = forAllUsers
				? "HKLM:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts"
				: "HKCU:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Fonts";

			var psCommand = new StringBuilder("-command \"");

			foreach (string fontFilePath in fontFilePaths)
			{
				var destinationPath = Path.Combine(fontDirectory, Path.GetFileName(fontFilePath));
				var appendCommand = $"Copy-Item '{fontFilePath}' '{fontDirectory}'; New-ItemProperty -Name '{Path.GetFileNameWithoutExtension(fontFilePath)}' -Path '{registryKey}' -PropertyType string -Value '{destinationPath}';";

				if (psCommand.Length + appendCommand.Length > 32766)
				{
					// The command is too long to run at once, so run the command once up to this point.
					await RunPowershellCommandAsync(psCommand.Append("\"").ToString(), PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
					psCommand.Clear().Append("-command \"");
				}

				psCommand.Append(appendCommand);
			}

			await RunPowershellCommandAsync(psCommand.Append("\"").ToString(), PowerShellExecutionOptions.Elevated | PowerShellExecutionOptions.Hidden);
		}

		private static Process CreatePowershellProcess(string command, PowerShellExecutionOptions options)
		{
			Process process = new();

			process.StartInfo.FileName = "powershell.exe";
			if (options.HasFlag(PowerShellExecutionOptions.Elevated))
			{
				process.StartInfo.UseShellExecute = true;
				process.StartInfo.Verb = "runas";
			}

			if (options.HasFlag(PowerShellExecutionOptions.Hidden))
			{
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			}
			process.StartInfo.Arguments = command;

			return process;
		}

		public static SafeFileHandle CreateFileForWrite(string filePath, bool overwrite = true)
		{
			return new SafeFileHandle(Win32PInvoke.CreateFileFromApp(filePath,
				(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE, 0, IntPtr.Zero, overwrite ? Win32PInvoke.CREATE_ALWAYS : Win32PInvoke.OPEN_ALWAYS, (uint)Win32PInvoke.File_Attributes.BackupSemantics, IntPtr.Zero), true);
		}

		public static SafeFileHandle OpenFileForRead(string filePath, bool readWrite = false, uint flags = 0)
		{
			return new SafeFileHandle(Win32PInvoke.CreateFileFromApp(filePath,
				(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | (uint)(readWrite ? FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE : 0u), (uint)(Win32PInvoke.FILE_SHARE_READ | (readWrite ? 0 : Win32PInvoke.FILE_SHARE_WRITE)), IntPtr.Zero, Win32PInvoke.OPEN_EXISTING, (uint)Win32PInvoke.File_Attributes.BackupSemantics | flags, IntPtr.Zero), true);
		}

		public static bool GetFileDateModified(string filePath, out FILETIME dateModified)
		{
			using var hFile = new SafeFileHandle(Win32PInvoke.CreateFileFromApp(filePath, (uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ, Win32PInvoke.FILE_SHARE_READ, IntPtr.Zero, Win32PInvoke.OPEN_EXISTING, (uint)Win32PInvoke.File_Attributes.BackupSemantics, IntPtr.Zero), true);
			return Win32PInvoke.GetFileTime(hFile.DangerousGetHandle(), out _, out _, out dateModified);
		}

		public static bool SetFileDateModified(string filePath, FILETIME dateModified)
		{
			using var hFile = new SafeFileHandle(Win32PInvoke.CreateFileFromApp(filePath, (uint)FILE_ACCESS_RIGHTS.FILE_WRITE_ATTRIBUTES, 0, IntPtr.Zero, Win32PInvoke.OPEN_EXISTING, (uint)Win32PInvoke.File_Attributes.BackupSemantics, IntPtr.Zero), true);
			return Win32PInvoke.SetFileTime(hFile.DangerousGetHandle(), new(), new(), dateModified);
		}

		public static bool HasFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			if (Win32PInvoke.GetFileAttributesExFromApp(
				lpFileName, Win32PInvoke.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo))
			{
				return (lpFileInfo.dwFileAttributes & dwAttrs) == dwAttrs;
			}
			return false;
		}

		public static bool SetFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			if (!Win32PInvoke.GetFileAttributesExFromApp(
				lpFileName, Win32PInvoke.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo))
			{
				return false;
			}
			return Win32PInvoke.SetFileAttributesFromApp(lpFileName, lpFileInfo.dwFileAttributes | dwAttrs);
		}

		public static bool UnsetFileAttribute(string lpFileName, FileAttributes dwAttrs)
		{
			if (!Win32PInvoke.GetFileAttributesExFromApp(
				lpFileName, Win32PInvoke.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, out var lpFileInfo))
			{
				return false;
			}
			return Win32PInvoke.SetFileAttributesFromApp(lpFileName, lpFileInfo.dwFileAttributes & ~dwAttrs);
		}

		public static unsafe bool CanCompressContent(string path)
		{
			path = Path.GetPathRoot(path) ?? string.Empty;
			uint dwFileSystemFlags = 0;

			var success = PInvoke.GetVolumeInformation(
				path,
				null,
				0u,
				null,
				null,
				&dwFileSystemFlags,
				null,
				0u);

			if (!success)
				return false;

			return (dwFileSystemFlags & PInvoke.FILE_FILE_COMPRESSION) != 0;
		}

		public static unsafe bool SetCompressionAttributeIoctl(string lpFileName, bool isCompressed)
		{
			// GENERIC_READ | GENERIC_WRITE flags are needed here
			// FILE_FLAG_BACKUP_SEMANTICS is used to open directories
			using var hFile = PInvoke.CreateFile(
				lpFileName,
				(uint)(FILE_ACCESS_RIGHTS.FILE_GENERIC_READ | FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE | FILE_ACCESS_RIGHTS.FILE_WRITE_ATTRIBUTES),
				FILE_SHARE_MODE.FILE_SHARE_READ | FILE_SHARE_MODE.FILE_SHARE_WRITE,
				lpSecurityAttributes: null,
				FILE_CREATION_DISPOSITION.OPEN_EXISTING,
				FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL | FILE_FLAGS_AND_ATTRIBUTES.FILE_FLAG_BACKUP_SEMANTICS,
				hTemplateFile: null);

			if (hFile.IsInvalid)
				return false;

			var bytesReturned = 0u;
			var compressionFormat = isCompressed
				? COMPRESSION_FORMAT.COMPRESSION_FORMAT_DEFAULT
				: COMPRESSION_FORMAT.COMPRESSION_FORMAT_NONE;

			var result = PInvoke.DeviceIoControl(
				new(hFile.DangerousGetHandle()),
				PInvoke.FSCTL_SET_COMPRESSION,
				&compressionFormat,
				sizeof(ushort),
				null,
				0u,
				&bytesReturned);

			return result;
		}

		public static string ReadStringFromFile(string filePath)
		{
			IntPtr hFile = Win32PInvoke.CreateFileFromApp(filePath,
				(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_READ,
				Win32PInvoke.FILE_SHARE_READ,
				IntPtr.Zero,
				Win32PInvoke.OPEN_EXISTING,
				(uint)Win32PInvoke.File_Attributes.BackupSemantics,
				IntPtr.Zero);

			if (hFile.ToInt64() == -1)
			{
				return null;
			}

			const int BUFFER_LENGTH = 4096;
			byte[] buffer = new byte[BUFFER_LENGTH];
			int dwBytesRead;
			string szRead = string.Empty;

			unsafe
			{
				using (MemoryStream ms = new MemoryStream())
				using (StreamReader reader = new StreamReader(ms, true))
				{
					while (true)
					{
						fixed (byte* pBuffer = buffer)
						{
							if (Win32PInvoke.ReadFile(hFile, pBuffer, BUFFER_LENGTH - 1, &dwBytesRead, IntPtr.Zero) && dwBytesRead > 0)
							{
								ms.Write(buffer, 0, dwBytesRead);
							}
							else
							{
								break;
							}
						}
					}
					ms.Position = 0;
					szRead = reader.ReadToEnd();
				}
			}

			Win32PInvoke.CloseHandle(hFile);

			return szRead;
		}

		public static bool WriteStringToFile(string filePath, string str, Win32PInvoke.File_Attributes flags = 0)
		{
			IntPtr hStream = Win32PInvoke.CreateFileFromApp(filePath,
				(uint)FILE_ACCESS_RIGHTS.FILE_GENERIC_WRITE, 0, IntPtr.Zero, Win32PInvoke.CREATE_ALWAYS, (uint)(Win32PInvoke.File_Attributes.BackupSemantics | flags), IntPtr.Zero);
			if (hStream.ToInt64() == -1)
			{
				return false;
			}
			byte[] buff = Encoding.UTF8.GetBytes(str);
			int dwBytesWritten;
			unsafe
			{
				fixed (byte* pBuff = buff)
				{
					Win32PInvoke.WriteFile(hStream, pBuff, buff.Length, &dwBytesWritten, IntPtr.Zero);
				}
			}
			Win32PInvoke.CloseHandle(hStream);
			return true;
		}

		public static bool WriteBufferToFileWithProgress(string filePath, byte[] buffer, Win32PInvoke.LPOVERLAPPED_COMPLETION_ROUTINE callback)
		{
			using var hFile = CreateFileForWrite(filePath);

			if (hFile.IsInvalid)
			{
				return false;
			}

			NativeOverlapped nativeOverlapped = new NativeOverlapped();
			bool result = Win32PInvoke.WriteFileEx(hFile.DangerousGetHandle(), buffer, (uint)buffer.LongLength, ref nativeOverlapped, callback);

			if (!result)
			{
				System.Diagnostics.Debug.WriteLine(Marshal.GetLastWin32Error());
			}

			return result;
		}

		// https://www.pinvoke.net/default.aspx/kernel32/GetFileInformationByHandleEx.html
		public static ulong? GetFolderFRN(string folderPath)
		{
			using var handle = OpenFileForRead(folderPath);
			if (!handle.IsInvalid)
			{
				var fileStruct = new Win32PInvoke.FILE_ID_BOTH_DIR_INFO();
				if (Win32PInvoke.GetFileInformationByHandleEx(handle.DangerousGetHandle(), Win32PInvoke.FILE_INFO_BY_HANDLE_CLASS.FileIdBothDirectoryInfo, out fileStruct, (uint)Marshal.SizeOf(fileStruct)))
				{
					return (ulong)fileStruct.FileId;
				}
			}
			return null;
		}

		public static ulong? GetFileFRN(string filePath)
		{
			using var handle = OpenFileForRead(filePath);
			if (!handle.IsInvalid)
			{
				try
				{
					var fileID = Kernel32.GetFileInformationByHandleEx<Kernel32.FILE_ID_INFO>(handle, Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileIdInfo);
					return BitConverter.ToUInt64(fileID.FileId.Identifier, 0);
				}
				catch { }
			}
			return null;
		}

		public static long? GetFileSizeOnDisk(string filePath)
		{
			using var handle = OpenFileForRead(filePath);
			if (!handle.IsInvalid)
			{
				try
				{
					var fileAllocationInfo = Kernel32.GetFileInformationByHandleEx<Kernel32.FILE_STANDARD_INFO>(handle, Kernel32.FILE_INFO_BY_HANDLE_CLASS.FileStandardInfo);
					return fileAllocationInfo.AllocationSize;
				}
				catch { }
			}
			return null;
		}

		// https://github.com/rad1oactive/BetterExplorer/blob/master/Windows%20API%20Code%20Pack%201.1/source/WindowsAPICodePack/Shell/ReparsePoint.cs
		public static string ParseSymLink(string path)
		{
			using var handle = OpenFileForRead(path, false, 0x00200000);
			if (!handle.IsInvalid)
			{
				if (Win32PInvoke.DeviceIoControl(handle.DangerousGetHandle(), Win32PInvoke.FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0, out Win32PInvoke.REPARSE_DATA_BUFFER buffer, Win32PInvoke.MAXIMUM_REPARSE_DATA_BUFFER_SIZE, out _, IntPtr.Zero))
				{
					var subsString = new string(buffer.PathBuffer, ((buffer.SubsNameOffset / 2) + 2), buffer.SubsNameLength / 2);
					var printString = new string(buffer.PathBuffer, ((buffer.PrintNameOffset / 2) + 2), buffer.PrintNameLength / 2);
					var normalisedTarget = printString ?? subsString;
					if (string.IsNullOrEmpty(normalisedTarget))
					{
						normalisedTarget = subsString;
						if (normalisedTarget.StartsWith(@"\??\", StringComparison.Ordinal))
						{
							normalisedTarget = normalisedTarget.Substring(4);
						}
					}
					if (buffer.ReparseTag == Win32PInvoke.IO_REPARSE_TAG_SYMLINK && (normalisedTarget.Length < 2 || normalisedTarget[1] != ':'))
					{
						// Target is relative, get the absolute path
						normalisedTarget = normalisedTarget.TrimStart(Path.DirectorySeparatorChar);
						path = path.TrimEnd(Path.DirectorySeparatorChar);
						normalisedTarget = Path.GetFullPath(Path.Combine(path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar)), normalisedTarget));
					}
					return normalisedTarget;
				}
			}
			return null;
		}

		// https://stackoverflow.com/a/7988352
		public static IEnumerable<(string Name, long Size)> GetAlternateStreams(string path)
		{
			Win32PInvoke.WIN32_FIND_STREAM_DATA findStreamData = new Win32PInvoke.WIN32_FIND_STREAM_DATA();
			IntPtr hFile = Win32PInvoke.FindFirstStreamW(path, Win32PInvoke.StreamInfoLevels.FindStreamInfoStandard, findStreamData, 0);

			if (hFile.ToInt64() != -1)
			{
				do
				{
					// The documentation for FindFirstStreamW says that it is always a ::$DATA
					// stream type, but FindNextStreamW doesn't guarantee that for subsequent
					// streams so we check to make sure
					if (findStreamData.cStreamName.EndsWith(":$DATA") && findStreamData.cStreamName != "::$DATA")
					{
						yield return (findStreamData.cStreamName, findStreamData.StreamSize);
					}
				}
				while (Win32PInvoke.FindNextStreamW(hFile, findStreamData));

				Win32PInvoke.FindClose(hFile);
			}
		}

		public static bool GetWin32FindDataForPath(string targetPath, out Win32PInvoke.WIN32_FIND_DATA findData)
		{
			Win32PInvoke.FINDEX_INFO_LEVELS findInfoLevel = Win32PInvoke.FINDEX_INFO_LEVELS.FindExInfoBasic;

			int additionalFlags = Win32PInvoke.FIND_FIRST_EX_LARGE_FETCH;

			IntPtr hFile = Win32PInvoke.FindFirstFileExFromApp(
				targetPath,
				findInfoLevel,
				out findData,
				Win32PInvoke.FINDEX_SEARCH_OPS.FindExSearchNameMatch,
				IntPtr.Zero,
				additionalFlags);

			if (hFile.ToInt64() != -1)
			{
				Win32PInvoke.FindClose(hFile);

				return true;
			}

			return false;
		}
	}
}
