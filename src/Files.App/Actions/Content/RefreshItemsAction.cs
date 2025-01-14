// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class RefreshItemsAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Refresh".GetLocalizedResource();

		public string Description
			=> "RefreshItemsDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE72C");

		public HotKey HotKey
			=> new(Keys.R, KeyModifiers.Ctrl);

		public HotKey SecondHotKey
			=> new(Keys.F5);

		public bool IsExecutable
			=> context.CanRefresh;

		public RefreshItemsAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return;

			await context.ShellPage.Refresh_Click();
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.CanRefresh):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
