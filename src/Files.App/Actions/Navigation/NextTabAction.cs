// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Actions
{
	internal sealed class NextTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext multitaskingContext;

		public string Label
			=> "NextTab".GetLocalizedResource();

		public string Description
			=> "NextTabDescription".GetLocalizedResource();

		public bool IsExecutable
			=> multitaskingContext.TabCount > 1;

		public HotKey HotKey
			=> new(Keys.Tab, KeyModifiers.Ctrl);

		public NextTabAction()
		{
			multitaskingContext = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			multitaskingContext.PropertyChanged += MultitaskingContext_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			App.AppModel.TabStripSelectedIndex = (App.AppModel.TabStripSelectedIndex + 1) % multitaskingContext.TabCount;

			// Small delay for the UI to load
			await Task.Delay(500);

			// Refocus on the file list
			(multitaskingContext.CurrentTabItem.TabItemContent as Control)?.Focus(FocusState.Programmatic);
		}

		private void MultitaskingContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMultitaskingContext.TabCount))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
