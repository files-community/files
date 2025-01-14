// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;

namespace Files.App.Extensions
{
	public static class KeyboardAcceleratorExtensions
	{
		public static bool CheckIsPressed(this KeyboardAccelerator keyboardAccelerator)
		{
			return
				// Check if the main key is pressed
				Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(keyboardAccelerator.Key).HasFlag(CoreVirtualKeyStates.Down) &&
				// Check if menu (alt) key is a modifier, and if so check if it's pressed
				(!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu) || Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down)) &&
				// Check if shift key is a modifier, and if so check if it's pressed
				(!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift) || Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)) &&
				// Check if ctrl key is a modifier, and if so check if it's pressed
				(!keyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control) || Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down));
		}
	}
}
