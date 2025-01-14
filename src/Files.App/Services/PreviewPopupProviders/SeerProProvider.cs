// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.DataExchange;

namespace Files.App.Services.PreviewPopupProviders
{
	public sealed class SeerProProvider : IPreviewPopupProvider
	{
		public static SeerProProvider Instance { get; } = new();

		private string? CurrentPath;

		private bool? _IsTrackSelectionSettingEnabledCache;
		private bool IsTrackSelectionSettingEnabled
		{
			get
			{
				_IsTrackSelectionSettingEnabledCache ??= DetectTrackSelectionSetting().Result;

				return _IsTrackSelectionSettingEnabledCache.Value;
			}
		}

		public unsafe async Task TogglePreviewPopupAsync(string path)
		{
			COPYDATASTRUCT data = default;
			data.dwData = 5000u;
			data.cbData = (uint)(path.Length + 1) * 2;
			data.lpData = (void*)Marshal.StringToHGlobalUni(path);

			var pData = Marshal.AllocHGlobal(Marshal.SizeOf(data));
			Marshal.StructureToPtr(data, pData, false);

			HWND hWnd = PInvoke.FindWindow("SeerWindowClass", null);
			var result = PInvoke.SendMessage(hWnd, 0x004A /*WM_COPYDATA*/, 0, pData);

			CurrentPath = PInvoke.IsWindowVisible(hWnd) ? path : null;

			Marshal.FreeHGlobal((nint)data.lpData);
			Marshal.FreeHGlobal(pData);
		}

		public async Task SwitchPreviewAsync(string path)
		{
			// Close preview window is track selection setting is disabled
			if (!IsTrackSelectionSettingEnabled && !string.IsNullOrEmpty(CurrentPath))
			{
				await TogglePreviewPopupAsync(CurrentPath);
				return;				
			}

			// Update the preview window if the path changed
			if (CurrentPath is not null && path != CurrentPath)
				await TogglePreviewPopupAsync(path);
		}

		public async Task<bool> DetectAvailability()
		{
			var hWnd = PInvoke.FindWindow("SeerWindowClass", null).Value;
			return hWnd != nint.Zero && hWnd.ToInt64() != -1;
		}

		private Task<bool> DetectTrackSelectionSetting()
		{
			bool trackSelectedFile = true;

			var keyName = @"HKEY_CURRENT_USER\Software\Corey\Seer";
			var value = Registry.GetValue(keyName, "tracking_file", null);

			if (bool.TryParse(value?.ToString(), out var result))
				return Task.FromResult(result);

			// List of possible paths for the Seer Pro settings file
			string[] paths =
			{
				Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Documents\\Seer\\uwp.ini"),
				Environment.ExpandEnvironmentVariables("%USERPROFILE%\\appdata\\Local\\Packages\\CNABA5E861-AC2A-4523-B3C1.Seer-AWindowsQuickLookTo_p7t0z30wh4868\\LocalCache\\Local\\Corey\\Seer\\uwp.ini"),
				Environment.ExpandEnvironmentVariables("%USERPROFILE%\\appdata\\Local\\Corey\\Seer\\uwp.ini"),
				Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Documents\\Seer\\config.ini")
			};

			// Find the first existing path
			foreach (var path in paths)
			{
				if (File.Exists(path))
				{
					// Read the settings file and look for the tracking_file setting
					string[] lines = File.ReadAllLines(path);

					foreach (var line in lines)
					{
						if (line.StartsWith("tracking_file", StringComparison.OrdinalIgnoreCase))
						{
							string[] keyValue = line.Split('=');
							if (keyValue.Length == 2 && bool.TryParse(keyValue[1].Trim(), out bool isTrackingFile))
							{
								trackSelectedFile = isTrackingFile;
								break;
							}
						}
					}

					break;
				}
			}

			return Task.FromResult(trackSelectedFile);
		}
	}
}
