// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Commands
{
	[Flags]
	public enum KeyModifiers : ushort
	{
		None = 0x0000,
		Ctrl = 0x0001,
		Alt = 0x0002,
		Shift = 0x0004,
		Win = 0x0008,
		CtrlAlt = Ctrl + Alt,
		CtrlShift = Ctrl + Shift,
		CtrlWin = Ctrl + Win,
		AltShift = Alt + Shift,
		AltWin = Alt + Win,
		ShiftWin = Shift + Win,
		CtrlAltShift = Ctrl + Alt + Shift,
		CtrlAltWin = Ctrl + Alt + Win,
		CtrlShiftWin = Ctrl + Shift + Win,
		AltShiftWin = Alt + Shift + Win,
		AltCtrlShiftWin = Ctrl + Alt + Shift + Win,
	}
}
