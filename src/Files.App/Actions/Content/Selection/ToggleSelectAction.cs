// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Actions
{
	internal sealed class ToggleSelectAction : IAction
	{
		public string Label
			=> "ToggleSelect".GetLocalizedResource();

		public string Description
			=> "ToggleSelectDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Space, KeyModifiers.Ctrl);

		public bool IsExecutable
			=> GetFocusedElement() is not null;

		public Task ExecuteAsync(object? parameter = null)
		{
			if (GetFocusedElement() is SelectorItem item)
				item.IsSelected = !item.IsSelected;

			return Task.CompletedTask;
		}

		private static SelectorItem? GetFocusedElement()
		{
			return FocusManager.GetFocusedElement(MainWindow.Instance.Content.XamlRoot) as SelectorItem;
		}
	}
}
