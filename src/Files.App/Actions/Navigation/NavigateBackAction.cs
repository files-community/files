// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class NavigateBackAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Back".GetLocalizedResource();

		public string Description
			=> "NavigateBackDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Left, KeyModifiers.Alt);

		public HotKey SecondHotKey
			=> new(Keys.Back);

		public HotKey ThirdHotKey
			=> new(Keys.Mouse4);

		public HotKey MediaHotKey
			=> new(Keys.GoBack, KeyModifiers.None, false);

		public RichGlyph Glyph
			=> new("\uE72B");

		public bool IsExecutable
			=> context.CanGoBack;

		public NavigateBackAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context.ShellPage!.Back_Click();

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanGoBack):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
