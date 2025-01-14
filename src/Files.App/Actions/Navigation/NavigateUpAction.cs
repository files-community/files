// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class NavigateUpAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Up".GetLocalizedResource();

		public string Description
			=> "NavigateUpDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Up, KeyModifiers.Alt);

		public RichGlyph Glyph
			=> new("\uE74A");

		public bool IsExecutable
			=> context.CanNavigateToParent;

		public NavigateUpAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.ShellPage!.Up_Click();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanNavigateToParent):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
