// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Windows.Storage;
using static Files.App.Constants;

namespace Files.App.Utils
{
	public static class WSLDistroManager
	{
		public static EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

		private static readonly List<WslDistroItem> distros = [];
		public static IReadOnlyList<WslDistroItem> Distros
		{
			get
			{
				lock (distros)
				{
					return distros.ToList().AsReadOnly();
				}
			}
		}

		public static async Task UpdateDrivesAsync()
		{
			try
			{
				// Check if WSL is installed
				const string WslRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Lxss\MSI";
				using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(WslRegistryPath))
				{
					if (key is null || key.GetValue("InstallLocation") is null)
						return;
				}

				var distroFolder = await StorageFolder.GetFolderFromPathAsync(@"\\wsl$\");
				foreach (StorageFolder folder in await distroFolder.GetFoldersAsync())
				{
					Uri logoURI = GetLogoUri(folder.DisplayName);

					var distro = new WslDistroItem
					{
						Text = folder.DisplayName,
						Path = folder.Path,
						Icon = logoURI,
						MenuOptions = new ContextMenuOptions { IsLocationItem = true },
					};

					lock (distros)
					{
						if (distros.Any(x => x.Path == folder.Path))
						{
							continue;
						}
						distros.Add(distro);
					}
					DataChanged?.Invoke(SectionType.WSL, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, distro));
				}
			}
			catch (Exception)
			{
				// WSL Not Supported/Enabled
			}
		}

		public static bool TryGetDistro(string path, [NotNullWhen(true)] out WslDistroItem? distro)
		{
			var normalizedPath = PathNormalization.NormalizePath(path);
			distro = Distros.FirstOrDefault(x => normalizedPath.StartsWith(PathNormalization.NormalizePath(x.Path), StringComparison.OrdinalIgnoreCase));

			return distro is not null;
		}

		private static Uri GetLogoUri(string displayName)
		{
			if (Contains(displayName, "ubuntu"))
			{
				return new Uri(WslIconsPaths.UbuntuIcon);
			}
			if (Contains(displayName, "kali"))
			{
				return new Uri(WslIconsPaths.KaliIcon);
			}
			if (Contains(displayName, "debian"))
			{
				return new Uri(WslIconsPaths.DebianIcon);
			}
			if (Contains(displayName, "opensuse"))
			{
				return new Uri(WslIconsPaths.OpenSuse);
			}
			return Contains(displayName, "alpine") ? new Uri(WslIconsPaths.Alpine) : new Uri(WslIconsPaths.GenericIcon);

			static bool Contains(string displayName, string distroName)
				=> displayName.Contains(distroName, StringComparison.OrdinalIgnoreCase);
		}
	}
}