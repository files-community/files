// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class OpenPropertiesAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "OpenProperties".GetLocalizedResource();

		public string Description
			=> "OpenPropertiesDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Properties");

		public HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.Alt);

		public bool IsExecutable =>
			context.PageType is not ContentPageTypes.Home &&
			!(context.PageType is ContentPageTypes.SearchResults && 
			!context.HasSelection);

		public OpenPropertiesAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var page = context.ShellPage?.SlimContentPage;

			if (page?.ItemContextMenuFlyout.IsOpen ?? false)
				page.ItemContextMenuFlyout.Closed += OpenPropertiesFromItemContextMenuFlyout;
			else if (page?.BaseContextMenuFlyout.IsOpen ?? false)
				page.BaseContextMenuFlyout.Closed += OpenPropertiesFromBaseContextMenuFlyout;
			else
				FilePropertiesHelpers.OpenPropertiesWindow(context.ShellPage!);

			return Task.CompletedTask;
		}

		private void OpenPropertiesFromItemContextMenuFlyout(object? _, object e)
		{
			var page = context.ShellPage?.SlimContentPage;
			if (page is not null)
				page.ItemContextMenuFlyout.Closed -= OpenPropertiesFromItemContextMenuFlyout;
			
			FilePropertiesHelpers.OpenPropertiesWindow(context.ShellPage!);
		}

		private void OpenPropertiesFromBaseContextMenuFlyout(object? _, object e)
		{
			var page = context.ShellPage?.SlimContentPage;
			if (page is not null)
				page.BaseContextMenuFlyout.Closed -= OpenPropertiesFromBaseContextMenuFlyout;
			
			FilePropertiesHelpers.OpenPropertiesWindow(context.ShellPage!);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.Folder):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
