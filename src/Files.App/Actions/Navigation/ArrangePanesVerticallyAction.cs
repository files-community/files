// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class ArrangePanesVerticallyAction : ObservableObject, IToggleAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IMultiPanesContext MultiPanesContext = Ioc.Default.GetRequiredService<IMultiPanesContext>();

		public string Label
			=> "ArrangePanesVertically".GetLocalizedResource();

		public string Description
			=> "ArrangePanesVerticallyDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Panes.Horizontal");

		public bool IsOn
			=> MultiPanesContext.ShellPaneArrangement is ShellPaneArrangement.Vertical;

		public bool IsExecutable =>
			ContentPageContext.IsMultiPaneAvailable &&
			ContentPageContext.IsMultiPaneActive;

		public ArrangePanesVerticallyAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
			MultiPanesContext.ShellPaneArrangementChanged += MultiPanesContext_ShellPaneArrangementChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			ContentPageContext.ShellPage!.PaneHolder.ArrangePanes(ShellPaneArrangement.Vertical);

			return Task.CompletedTask;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.IsMultiPaneAvailable):
				case nameof(IContentPageContext.IsMultiPaneActive):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private void MultiPanesContext_ShellPaneArrangementChanged(object? sender, EventArgs e)
		{
			OnPropertyChanged(nameof(IsOn));
		}
	}
}
