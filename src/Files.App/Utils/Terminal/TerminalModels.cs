using System.Text.RegularExpressions;

namespace Files.App.Utils.Terminal
{
	/// <summary>
	/// Code modified from https://github.com/felixse/FluentTerminal
	/// </summary>
	public class KeyBinding
	{
		public KeyBinding()
		{
		}

		public KeyBinding(KeyBinding other)
		{
			Command = other.Command;
			Key = other.Key;
			Ctrl = other.Ctrl;
			Alt = other.Alt;
			Shift = other.Shift;
			Meta = other.Meta;
		}

		public string Command { get; set; }
		public int Key { get; set; }
		public bool Ctrl { get; set; }
		public bool Alt { get; set; }
		public bool Shift { get; set; }
		public bool Meta { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is KeyBinding other)
			{
				return other.Command.Equals(Command)
					&& other.Key == Key
					&& other.Ctrl == Ctrl
					&& other.Alt == Alt
					&& other.Shift == Shift
					&& other.Meta == Meta;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Command, Key, Ctrl, Alt, Shift, Meta);
		}
	}

	public class TerminalTheme
	{
		public TerminalTheme()
		{
		}

		public TerminalTheme(TerminalTheme other)
		{
			Id = other.Id;
			Name = other.Name;
			Author = other.Author;
			PreInstalled = other.PreInstalled;
			Colors = new TerminalColors(other.Colors);
		}

		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Author { get; set; }
		public bool PreInstalled { get; set; }
		public TerminalColors Colors { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is TerminalTheme other)
			{
				return Equals(other.Name, Name)
					&& Equals(other.Author, Author)
					&& Equals(other.Colors, Colors);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Name, Author, Colors);
		}
	}

	public class TerminalColors
	{
		public string Foreground { get; set; }
		public string Background { get; set; }
		public string Cursor { get; set; }
		public string CursorAccent { get; set; }
		public string Selection { get; set; }
		public string SelectionForeground { get; set; }
		public string SelectionBackground { get; set; }

		public string Black { get; set; }
		public string Red { get; set; }
		public string Green { get; set; }
		public string Yellow { get; set; }
		public string Blue { get; set; }
		public string Magenta { get; set; }
		public string Cyan { get; set; }
		public string White { get; set; }

		public string BrightBlack { get; set; }
		public string BrightRed { get; set; }
		public string BrightGreen { get; set; }
		public string BrightYellow { get; set; }
		public string BrightBlue { get; set; }
		public string BrightMagenta { get; set; }
		public string BrightCyan { get; set; }
		public string BrightWhite { get; set; }

		public TerminalColors()
		{
		}

		public TerminalColors(TerminalColors other)
		{
			Foreground = other.Foreground;
			Background = other.Background;
			Cursor = other.Cursor;
			CursorAccent = other.CursorAccent;
			Selection = other.Selection;

			Black = other.Black;
			Red = other.Red;
			Green = other.Green;
			Yellow = other.Yellow;
			Blue = other.Blue;
			Magenta = other.Magenta;
			Cyan = other.Cyan;
			White = other.White;

			BrightBlack = other.BrightBlack;
			BrightRed = other.BrightRed;
			BrightGreen = other.BrightGreen;
			BrightYellow = other.BrightYellow;
			BrightBlue = other.BrightBlue;
			BrightMagenta = other.BrightMagenta;
			BrightCyan = other.BrightCyan;
			BrightWhite = other.BrightWhite;
		}

		public override bool Equals(object obj)
		{
			if (obj is TerminalColors other)
			{
				return other.Foreground.Equals(Foreground)
					&& other.Background.Equals(Background)
					&& other.Cursor.Equals(Cursor)
					&& other.CursorAccent.Equals(CursorAccent)
					&& other.Selection.Equals(Selection)
					&& other.Black.Equals(Black)
					&& other.Red.Equals(Red)
					&& other.Green.Equals(Green)
					&& other.Yellow.Equals(Yellow)
					&& other.Blue.Equals(Blue)
					&& other.Magenta.Equals(Magenta)
					&& other.Cyan.Equals(Cyan)
					&& other.White.Equals(White)
					&& other.BrightBlack.Equals(BrightBlack)
					&& other.BrightRed.Equals(BrightRed)
					&& other.BrightGreen.Equals(BrightGreen)
					&& other.BrightYellow.Equals(BrightYellow)
					&& other.BrightBlue.Equals(BrightBlue)
					&& other.BrightMagenta.Equals(BrightMagenta)
					&& other.BrightCyan.Equals(BrightCyan)
					&& other.BrightWhite.Equals(BrightWhite);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(
				HashCode.Combine(Foreground, Background, Cursor, CursorAccent, Selection),
				HashCode.Combine(Black, Red, Green, Yellow, Blue, Magenta, Cyan, White),
				HashCode.Combine(BrightBlack, BrightRed, BrightGreen, BrightYellow, BrightBlue, BrightMagenta, BrightCyan, BrightWhite)
			);
		}
	}

	public enum ExtendedVirtualKey
	{
		None = 0,
		LeftButton = 1,
		RightButton = 2,
		Cancel = 3,
		MiddleButton = 4,
		XButton1 = 5,
		XButton2 = 6,
		Back = 8,
		Tab = 9,
		Clear = 12,
		Enter = 13,
		Shift = 16,
		Control = 17,
		Menu = 18,
		Pause = 19,
		CapitalLock = 20,
		Kana = 21,
		Hangul = 21,
		Junja = 23,
		Final = 24,
		Hanja = 25,
		Kanji = 25,
		Escape = 27,
		Convert = 28,
		NonConvert = 29,
		Accept = 30,
		ModeChange = 31,
		Space = 32,
		PageUp = 33,
		PageDown = 34,
		End = 35,
		Home = 36,
		Left = 37,
		Up = 38,
		Right = 39,
		Down = 40,
		Select = 41,
		Print = 42,
		Execute = 43,
		Snapshot = 44,
		Insert = 45,
		Delete = 46,
		Help = 47,

		[Description("0")]
		Number0 = 48,

		[Description("1")]
		Number1 = 49,

		[Description("2")]
		Number2 = 50,

		[Description("3")]
		Number3 = 51,

		[Description("4")]
		Number4 = 52,

		[Description("5")]
		Number5 = 53,

		[Description("6")]
		Number6 = 54,

		[Description("7")]
		Number7 = 55,

		[Description("8")]
		Number8 = 56,

		[Description("9")]
		Number9 = 57,

		A = 65,
		B = 66,
		C = 67,
		D = 68,
		E = 69,
		F = 70,
		G = 71,
		H = 72,
		I = 73,
		J = 74,
		K = 75,
		L = 76,
		M = 77,
		N = 78,
		O = 79,
		P = 80,
		Q = 81,
		R = 82,
		S = 83,
		T = 84,
		U = 85,
		V = 86,
		W = 87,
		X = 88,
		Y = 89,
		Z = 90,
		LeftWindows = 91,
		RightWindows = 92,
		Application = 93,
		Sleep = 95,
		NumberPad0 = 96,
		NumberPad1 = 97,
		NumberPad2 = 98,
		NumberPad3 = 99,
		NumberPad4 = 100,
		NumberPad5 = 101,
		NumberPad6 = 102,
		NumberPad7 = 103,
		NumberPad8 = 104,
		NumberPad9 = 105,
		Multiply = 106,
		Add = 107,
		Separator = 108,
		Subtract = 109,
		Decimal = 110,
		Divide = 111,
		F1 = 112,
		F2 = 113,
		F3 = 114,
		F4 = 115,
		F5 = 116,
		F6 = 117,
		F7 = 118,
		F8 = 119,
		F9 = 120,
		F10 = 121,
		F11 = 122,
		F12 = 123,
		F13 = 124,
		F14 = 125,
		F15 = 126,
		F16 = 127,
		F17 = 128,
		F18 = 129,
		F19 = 130,
		F20 = 131,
		F21 = 132,
		F22 = 133,
		F23 = 134,
		F24 = 135,
		NavigationView = 136,
		NavigationMenu = 137,
		NavigationUp = 138,
		NavigationDown = 139,
		NavigationLeft = 140,
		NavigationRight = 141,
		NavigationAccept = 142,
		NavigationCancel = 143,
		NumberKeyLock = 144,
		Scroll = 145,
		LeftShift = 160,
		RightShift = 161,
		LeftControl = 162,
		RightControl = 163,
		LeftMenu = 164,
		RightMenu = 165,
		GoBack = 166,
		GoForward = 167,
		Refresh = 168,
		Stop = 169,
		Search = 170,
		Favorites = 171,
		GoHome = 172,
		VolumeMute = 173,
		VolumeDown = 174,
		VolumeUp = 175,
		MediaNext = 176,
		MediaPrevious = 177,
		MediaStop = 178,
		MediaPlayPause = 179,

		[Description(";")]
		OEM_1 = 186,

		[Description("+")]
		Plus = 187,

		[Description(",")]
		Comma = 188,

		[Description("-")]
		Minus = 189,

		[Description(".")]
		Period = 190,

		[Description("/")]
		OEM_2 = 191,

		[Description("`")]
		OEM_3 = 192,

		GamepadA = 195,
		GamepadB = 196,
		GamepadX = 197,
		GamepadY = 198,
		GamepadRightShoulder = 199,
		GamepadLeftShoulder = 200,
		GamepadLeftTrigger = 201,
		GamepadRightTrigger = 202,
		GamepadDPadUp = 203,
		GamepadDPadDown = 204,
		GamepadDPadLeft = 205,
		GamepadDPadRight = 206,
		GamepadMenu = 207,
		GamepadView = 208,
		GamepadLeftThumbstickButton = 209,
		GamepadRightThumbstickButton = 210,
		GamepadLeftThumbstickUp = 211,
		GamepadLeftThumbstickDown = 212,
		GamepadLeftThumbstickRight = 213,
		GamepadLeftThumbstickLeft = 214,
		GamepadRightThumbstickUp = 215,
		GamepadRightThumbstickDown = 216,
		GamepadRightThumbstickRight = 217,
		GamepadRightThumbstickLeft = 218,

		[Description("[")]
		OEM_4 = 219,

		[Description(@"\")]
		OEM_5 = 220,

		[Description("]")]
		OEM_6 = 221,

		[Description("'")]
		OEM_7 = 222,

		[Description("<")]
		OEM_102 = 226
	}

	public class ShellProfile
	{
		/// <summary>
		/// Replace all instances of anything resembling a newline, treating pairs of \r\n in either order as a single linebreak.
		/// </summary>
		public static readonly Regex NewlinePattern = new Regex(@"\n\r|\r\n|\r|\n", RegexOptions.Compiled);
		public const int CurrentMigrationVersion = 1;

		public ShellProfile()
		{
		}

		protected ShellProfile(ShellProfile other)
		{
			Id = other.Id;
			PreInstalled = other.PreInstalled;
			Name = other.Name;
			Arguments = other.Arguments;
			Location = other.Location;
			WorkingDirectory = other.WorkingDirectory;
			TabThemeId = other.TabThemeId;
			TerminalThemeId = other.TerminalThemeId;
			UseConPty = other.UseConPty;
			UseBuffer = other.UseBuffer;
			KeyBindings = other.KeyBindings.Select(x => new KeyBinding(x)).ToList();
		}

		public Guid Id { get; set; }
		public bool PreInstalled { get; set; }
		public string Name { get; set; }
		public string Arguments { get; set; }
		public string Location { get; set; }
		public string WorkingDirectory { get; set; }
		public int TabThemeId { get; set; }
		public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
		public bool UseConPty { get; set; }
		public bool UseBuffer { get; set; } = true;

		public int MigrationVersion { get; set; } = CurrentMigrationVersion;

		/// <summary>
		/// For attaching a data to the profile. This property doesn't get serialized nor cloned.
		/// </summary>
		[JsonIgnore]
		public object Tag { get; set; }

		public Guid TerminalThemeId { get; set; }
		public ICollection<KeyBinding> KeyBindings { get; set; } = new List<KeyBinding>();

		public virtual bool EqualTo(ShellProfile other)
		{
			if (other == null)
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return other.Id.Equals(Id)
				   && other.PreInstalled.Equals(PreInstalled)
				   && other.Name.NullableEqualTo(Name)
				   && other.Arguments.NullableEqualTo(Arguments)
				   && other.Location.NullableEqualTo(Location)
				   && other.WorkingDirectory.NullableEqualTo(WorkingDirectory)
				   && other.TabThemeId.Equals(TabThemeId)
				   && other.TerminalThemeId.Equals(TerminalThemeId)
				   && other.UseConPty == UseConPty
				   && other.UseBuffer == UseBuffer
				   && other.KeyBindings.SequenceEqual(KeyBindings);
		}

		public virtual ShellProfile Clone() => new ShellProfile(this);
	}

	public class TerminalOptions
	{
		public string FontFamily { get; set; }
		public int FontSize { get; set; }

		public int FontWeight { get; set; }

		public CursorStyle CursorStyle { get; set; }

		public bool CursorBlink { get; set; }

		public BellStyle BellStyle { get; set; }

		public ScrollBarStyle ScrollBarStyle { get; set; }

		public double BackgroundOpacity { get; set; }

		public bool UseAcrylicBackground { get; set; }

		public int Padding { get; set; }

		public uint ScrollBackLimit { get; set; }

		public string WordSeparator { get; set; }
	}

	public enum CursorStyle
	{
		Block,
		Underline,
		Bar
	}

	public enum BellStyle
	{
		None,
		Sound
	}

	public enum ScrollBarStyle
	{
		Hidden,
		AutoHiding,
		Visible
	}

	public enum Command
	{
		Copy,
		Paste,
		SelectAll,
		Clear
	}

	public class TerminalSize
	{
		public int Columns { get; set; }

		public int Rows { get; set; }

		public bool EquivalentTo(TerminalSize other) => ReferenceEquals(this, other) ||
														other != null && Columns.Equals(other.Columns) &&
														Rows.Equals(other.Rows);
	}

	public enum SessionType
	{
		Unknown,
		WinPty,
		ConPty
	}

	public enum MouseAction
	{
		None,
		ContextMenu,
		Paste,
		CopySelectionOrPaste
	}

	public enum MouseButton
	{
		Left,
		Middle,
		Right
	}
}
