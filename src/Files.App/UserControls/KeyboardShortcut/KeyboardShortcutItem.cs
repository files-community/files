// Copyright (c) 2023 Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.KeyboardShortcut
{
	public sealed partial class KeyboardShortcutItem : Control
	{
		internal const string SmallState = "Small";
		internal const string MediumState = "Medium";
		internal const string LargeState = "Large";

		internal const string MainTextTextBlock = "PART_MainTextTextBlock";

		public KeyboardShortcutItem()
		{
			DefaultStyleKey = typeof(KeyboardShortcutItem);
		}

		private void OnItemTypeChanged()
		{
		}

		private void OnSizeChanged()
		{
			switch (Size)
			{
				case KeyboardShortcutItemSize.Small:
					VisualStateManager.GoToState(this, SmallState, true);
					break;
				case KeyboardShortcutItemSize.Medium:
					VisualStateManager.GoToState(this, MediumState, true);
					break;
				case KeyboardShortcutItemSize.Large:
					VisualStateManager.GoToState(this, LargeState, true);
					break;
			}
		}

		private void OnTextChanged()
		{
		}
	}
}
