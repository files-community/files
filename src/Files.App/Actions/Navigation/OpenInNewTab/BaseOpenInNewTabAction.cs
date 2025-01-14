// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal abstract class BaseOpenInNewTabAction : ObservableObject, IAction
	{
		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		protected IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		protected ISidebarContext SidebarContext { get; } = Ioc.Default.GetRequiredService<ISidebarContext>();

		public string Label
			=> "OpenInNewTab".GetLocalizedResource();

		public string Description
			=> "OpenDirectoryInNewTabDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.OpenInTab");

		public virtual bool IsAccessibleGlobally
			=> true;

		public virtual bool IsExecutable =>
			ContentPageContext.ShellPage is not null &&
			ContentPageContext.ShellPage.SlimContentPage is not null &&
			ContentPageContext.SelectedItems.Count is not 0 &&
			ContentPageContext.SelectedItems.Count <= 5 &&
			ContentPageContext.SelectedItems.Count(x => x.IsFolder) == ContentPageContext.SelectedItems.Count;

		public BaseOpenInNewTabAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public virtual async Task ExecuteAsync(object? parameter = null)
		{
			foreach (ListedItem listedItem in ContentPageContext.SelectedItems)
			{
				await MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					await NavigationHelpers.AddNewTabByPathAsync(
						typeof(ShellPanesPage),
						(listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath,
						false);
				},
				Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
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
