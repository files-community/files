// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class NavigateHomeAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Home".GetLocalizedResource();

		public string Description
			=> "NavigateHomeDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE80F");

		public bool IsExecutable
			=> context.PageType is not ContentPageTypes.Home;

		public NavigateHomeAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.ShellPage!.NavigateHome();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
