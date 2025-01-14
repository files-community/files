// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class ArrangePanesHorizontallyAction : ObservableObject, IToggleAction
	{
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IMultiPanesContext MultiPanesContext = Ioc.Default.GetRequiredService<IMultiPanesContext>();

		public string Label
			=> "ArrangePanesHorizontally".GetLocalizedResource();

		public string Description
			=> "ArrangePanesHorizontallyDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Panes.Vertical");

		public bool IsOn
			=> MultiPanesContext.ShellPaneArrangement is ShellPaneArrangement.Horizontal;

		public bool IsExecutable =>
			ContentPageContext.IsMultiPaneAvailable &&
			ContentPageContext.IsMultiPaneActive;

		public ArrangePanesHorizontallyAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
			MultiPanesContext.ShellPaneArrangementChanged += MultiPanesContext_ShellPaneArrangementChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			ContentPageContext.ShellPage!.PaneHolder.ArrangePanes(ShellPaneArrangement.Horizontal);

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
