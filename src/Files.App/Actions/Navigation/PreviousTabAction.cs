// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Actions
{
	internal sealed class PreviousTabAction : ObservableObject, IAction
	{
		private readonly IMultitaskingContext multitaskingContext;

		public string Label
			=> "PreviousTab".GetLocalizedResource();

		public string Description
			=> "PreviousTabDescription".GetLocalizedResource();

		public bool IsExecutable
			=> multitaskingContext.TabCount > 1;

		public HotKey HotKey
			=> new(Keys.Tab, KeyModifiers.CtrlShift);

		public PreviousTabAction()
		{
			multitaskingContext = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			multitaskingContext.PropertyChanged += MultitaskingContext_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (App.AppModel.TabStripSelectedIndex is 0)
				App.AppModel.TabStripSelectedIndex = multitaskingContext.TabCount - 1;
			else
				App.AppModel.TabStripSelectedIndex--;

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
