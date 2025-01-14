// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal abstract class BaseOpenInNewPaneAction : ObservableObject, IAction
	{
		protected IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		protected IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();
		protected IHomePageContext HomePageContext { get; } = Ioc.Default.GetRequiredService<IHomePageContext>();
		protected ISidebarContext SidebarContext { get; } = Ioc.Default.GetRequiredService<ISidebarContext>();

		public string Label
			=> "OpenInNewPane".GetLocalizedResource();

		public string Description
			=> "OpenDirectoryInNewPaneDescription".GetLocalizedResource();

		public virtual bool IsExecutable =>
			ContentPageContext.SelectedItem is not null &&
			ContentPageContext.SelectedItem.IsFolder;

		public virtual bool IsAccessibleGlobally
			=> true;

		public BaseOpenInNewPaneAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public virtual Task ExecuteAsync(object? parameter = null)
		{
			NavigationHelpers.OpenInSecondaryPane(
				ContentPageContext.ShellPage,
				ContentPageContext.ShellPage.SlimContentPage.SelectedItems.FirstOrDefault());

			return Task.CompletedTask;
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
