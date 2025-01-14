// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.System;

namespace Files.App.Actions
{
	internal abstract class BaseOpenInNewWindowAction : ObservableObject, IAction
	{
		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		protected IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		protected ISidebarContext SidebarContext { get; } = Ioc.Default.GetRequiredService<ISidebarContext>();

		public string Label
			=> "OpenInNewWindow".GetLocalizedResource();

		public string Description
			=> "OpenInNewWindowDescription".GetLocalizedResource();

		public virtual HotKey HotKey
			=> new(Keys.Enter, KeyModifiers.CtrlAlt);

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.OpenInWindow");

		public virtual bool IsAccessibleGlobally
			=> true;

		public virtual bool IsExecutable =>
			ContentPageContext.ShellPage is not null &&
			ContentPageContext.ShellPage.SlimContentPage is not null &&
			ContentPageContext.SelectedItems.Count is not 0 &&
			ContentPageContext.SelectedItems.Count <= 5 &&
			ContentPageContext.SelectedItems.Count(x => x.IsFolder) == ContentPageContext.SelectedItems.Count;

		public BaseOpenInNewWindowAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public virtual async Task ExecuteAsync(object? parameter = null)
		{
			if (ContentPageContext.ShellPage?.SlimContentPage?.SelectedItems is null)
				return;

			List<ListedItem> items = ContentPageContext.ShellPage.SlimContentPage.SelectedItems;

			foreach (ListedItem listedItem in items)
			{
				var selectedItemPath = (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath;
				var folderUri = new Uri($"files-dev:?folder={@selectedItemPath}");

				await Launcher.LaunchUriAsync(folderUri);
			}
		}

		protected virtual void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
