using Windows.Foundation.Metadata;

namespace Files.App.Utils.Terminal
{
	/// <summary>
	/// Code modified from https://github.com/felixse/FluentTerminal
	/// </summary>
	public class DefaultValueProvider
	{
		public IDictionary<string, ICollection<KeyBinding>> GetCommandKeyBindings()
		{
			var keyBindings = new Dictionary<string, ICollection<KeyBinding>>();

			foreach (Command command in Enum.GetValues(typeof(Command)))
			{
				keyBindings.Add(command.ToString(), GetDefaultKeyBindings(command));
			}
			return keyBindings;
		}

		public ICollection<KeyBinding> GetDefaultKeyBindings(Command command)
		{
			switch (command)
			{
				case Command.Copy:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.Copy),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.C
					}
				};
				case Command.Paste:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.Paste),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.V
					}
				};

				case Command.SelectAll:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.SelectAll),
						Ctrl = true,
						Shift = true,
						Key = (int)ExtendedVirtualKey.A
					}
				};
				case Command.Clear:
					return new List<KeyBinding>
				{
					new KeyBinding
					{
						Command = nameof(Command.Clear),
						Ctrl = true,
						Alt = true,
						Key = (int)ExtendedVirtualKey.L
					}
				};

				default:
					throw new InvalidOperationException($"Default keybindings for Command '{command}' are missing");
			}
		}

		public Guid GetDefaultShellProfileId()
		{
			return Guid.Parse("813f2298-210a-481a-bdbf-c17bc637a3e2");
		}

		public TerminalOptions GetDefaultTerminalOptions()
		{
			return new TerminalOptions
			{
				BellStyle = BellStyle.None,
				CursorBlink = true,
				CursorStyle = CursorStyle.Block,
				ScrollBarStyle = ScrollBarStyle.Hidden,
				FontFamily = "Consolas",
				FontSize = 13,
				FontWeight = 400,
				BackgroundOpacity = 0.8,
				Padding = 12,
				ScrollBackLimit = 1000,
				WordSeparator = " ()[]{}:;|│!&*<>@&quot;&squo;",
				UseAcrylicBackground = true
			};
		}

		public Guid GetDefaultThemeId()
		{
			return Guid.Parse("281e4352-bb50-47b7-a691-2b13830df95e");
		}

		public IEnumerable<ShellProfile> GetPreinstalledShellProfiles()
		{
			return new[]
			{
				new ShellProfile
				{
					Id = GetDefaultShellProfileId(),
					TerminalThemeId = Guid.Parse("3571ce1b-31ce-4cf7-ae15-e0bff70c3eea"),
					Name = "Powershell",
					MigrationVersion = ShellProfile.CurrentMigrationVersion,
					Arguments = string.Empty,
					Location = @"C:\windows\system32\WindowsPowerShell\v1.0\powershell.exe",
					PreInstalled = true,
					WorkingDirectory = string.Empty,
					UseConPty = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8), // Windows 10 1903+
                    UseBuffer = false,
					EnvironmentVariables = new Dictionary<string, string>
					{
						["TERM"] = "xterm-256color"
					},
					KeyBindings = new []
					{
						new KeyBinding
						{
							Command = GetDefaultShellProfileId().ToString(),
							Ctrl=true,
							Alt=true,
							Shift=false,
							Meta=false,
							Key=(int)ExtendedVirtualKey.Number1
						}
					}
				},
				new ShellProfile
				{
					Id = Guid.Parse("ab942a61-7673-4755-9bd8-765aff91d9a3"),
					TerminalThemeId = GetDefaultThemeId(),
					Name = "CMD",
					MigrationVersion = ShellProfile.CurrentMigrationVersion,
					Arguments = string.Empty,
					Location = @"C:\Windows\System32\cmd.exe",
					PreInstalled = true,
					WorkingDirectory = string.Empty,
					UseConPty = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7), // Windows 10 1809+
                    UseBuffer = true,
					EnvironmentVariables = new Dictionary<string, string>
					{
						["TERM"] = "xterm-256color"
					},
					KeyBindings = new []
					{
						new KeyBinding
						{
							Command = "ab942a61-7673-4755-9bd8-765aff91d9a3",
							Ctrl=true,
							Alt=true,
							Shift=false,
							Meta=false,
							Key=(int)ExtendedVirtualKey.Number2
						}
					}
				},
				new ShellProfile
				{
					Id= Guid.Parse("e5785ad6-584f-40cb-bdcd-d5b3b3953e7f"),
					TerminalThemeId = Guid.Parse("32519543-bf94-4db6-92a1-7bcec3966c82"),
					Name = "WSL",
					MigrationVersion = ShellProfile.CurrentMigrationVersion,
					Arguments = string.Empty,
					Location = @"C:\windows\system32\wsl.exe",
					PreInstalled = true,
					WorkingDirectory = string.Empty,
					UseConPty = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 7), // Windows 10 1809+
                    UseBuffer = true, //TODO: Set to false if the buffer causes issues with WSL.
                    EnvironmentVariables = new Dictionary<string, string>
					{
						["TERM"] = "xterm-256color"
					},
					KeyBindings = new []
					{
						new KeyBinding
						{
							Command = "e5785ad6-584f-40cb-bdcd-d5b3b3953e7f",
							Ctrl=true,
							Alt=true,
							Shift=false,
							Meta=false,
							Key=(int)ExtendedVirtualKey.Number3
						}
					}
				}
			};
		}

		public IEnumerable<TerminalTheme> GetPreInstalledThemes()
		{
			var defaultXterm = new TerminalTheme
			{
				Id = GetDefaultThemeId(),
				Author = "xterm.js",
				Name = "Xterm.js Default",
				PreInstalled = true,
				Colors = new TerminalColors
				{
					Black = "#2e3436",
					Red = "#cc0000",
					Green = "#4e9a06",
					Yellow = "#c4a000",
					Blue = "#3465a4",
					Magenta = "#75507b",
					Cyan = "#06989a",
					White = "#d3d7cf",
					BrightBlack = "#555753",
					BrightRed = "#ef2929",
					BrightGreen = "#8ae234",
					BrightYellow = "#fce94f",
					BrightBlue = "#729fcf",
					BrightMagenta = "#ad7fa8",
					BrightCyan = "#34e2e2",
					BrightWhite = "#eeeeec",
					Foreground = "#ffffff",
					Background = "#000000",
					Cursor = "#ffffff",
					CursorAccent = "#000000",
					Selection = "rgba(255, 255, 255, 0.3)"
				}
			};

			var powerShell = new TerminalTheme
			{
				Id = Guid.Parse("3571ce1b-31ce-4cf7-ae15-e0bff70c3eea"),
				Author = "Microsoft",
				Name = "PowerShell",
				PreInstalled = true,
				Colors = new TerminalColors
				{
					Black = "#000000",
					Red = "#800000",
					Green = "#008000",
					Yellow = "#EEEDF0",
					Blue = "#000080",
					Magenta = "#012456",
					Cyan = "#008080",
					White = "#c0c0c0",
					BrightBlack = "#808080",
					BrightRed = "#ff0000",
					BrightGreen = "#00ff00",
					BrightYellow = "#ffff00",
					BrightBlue = "#0000ff",
					BrightMagenta = "#ff00ff",
					BrightCyan = "#00ffff",
					BrightWhite = "#ffffff",
					Foreground = "#ffffff",
					Background = "#012456",
					Cursor = "#fedba9",
					CursorAccent = "#000000",
					Selection = "rgba(254, 219, 169, 0.3)"
				}
			};

			var homebrew = new TerminalTheme
			{
				Id = Guid.Parse("5f034210-067b-40e2-b9c9-6a25eb6fb8e6"),
				Author = "Hans Kokx",
				Name = "Homebrew",
				PreInstalled = true,
				Colors = new TerminalColors
				{
					Black = "#000000",
					Red = "#A0160B",
					Green = "#00AF21",
					Yellow = "#A1A222",
					Blue = "#192AB7",
					Magenta = "#AA2FAE",
					Cyan = "#12B1BC",
					White = "#BBB5AF",
					BrightBlack = "#747876",
					BrightRed = "#E52213",
					BrightGreen = "#00D92B",
					BrightYellow = "#E6E435",
					BrightBlue = "#283EF9",
					BrightMagenta = "#EB43E6",
					BrightCyan = "#15E7E8",
					BrightWhite = "#E9E9E9",
					Foreground = "#00D92B",
					Background = "#000000",
					Cursor = "#00D92B",
					CursorAccent = "#000000",
					Selection = "#4CFFFFFF"
				}
			};

			var tomorrow = new TerminalTheme
			{
				Id = Guid.Parse("0cbbf805-fa86-4be5-ac51-75f3054dcc3a"),
				Author = "Chris Kempson",
				Name = "Tomorrow",
				PreInstalled = true,
				Colors = new TerminalColors
				{
					Foreground = "#4D4D4C",
					Background = "#FFFFFF",
					Cursor = "#4D4D4C",
					CursorAccent = "#FFFFFF",
					Selection = "rgba(214, 214, 214, 0.5)",
					Black = "#000000",
					Red = "#C82829",
					Green = "#718C00",
					Yellow = "#EAB700",
					Blue = "#4271AE",
					Magenta = "#8959A8",
					Cyan = "#3E999F",
					White = "#FFFFFF",
					BrightBlack = "#000000",
					BrightRed = "#C82829",
					BrightGreen = "#718C00",
					BrightYellow = "#EAB700",
					BrightBlue = "#4271AE",
					BrightMagenta = "#8959A8",
					BrightCyan = "#3E999F",
					BrightWhite = "#FFFFFF"
				}
			};

			var ubuntu = new TerminalTheme
			{
				Id = Guid.Parse("32519543-bf94-4db6-92a1-7bcec3966c82"),
				Author = "Canonical",
				Name = "Ubuntu Bash",
				PreInstalled = true,
				Colors = new TerminalColors
				{
					Foreground = "#EEEEEE",
					Background = "#300A24",
					Cursor = "#CFF5DB",
					CursorAccent = "#000000",
					Selection = "#4DABEDC1",
					Black = "#300A24",
					Red = "#CC0000",
					Green = "#4E9A06",
					Yellow = "#C4A000",
					Blue = "#3465A4",
					Magenta = "#75507B",
					Cyan = "#06989A",
					White = "#D3D7CF",
					BrightBlack = "#554E53",
					BrightRed = "#EF2929",
					BrightGreen = "#8AE234",
					BrightYellow = "#FCE94F",
					BrightBlue = "#729FCF",
					BrightMagenta = "#AD7FA8",
					BrightCyan = "#34E2E2",
					BrightWhite = "#EEEEEE"
				}
			};

			return new[] { defaultXterm, powerShell, homebrew, tomorrow, ubuntu };
		}
	}
}
