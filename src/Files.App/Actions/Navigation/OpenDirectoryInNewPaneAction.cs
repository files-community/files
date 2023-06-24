﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.Contexts;

namespace Files.App.Actions
{
	internal class OpenDirectoryInNewPaneAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "OpenDirectoryInNewPane".GetLocalizedResource();

		public string Description
			=> "OpenDirectoryInNewPaneDescription".GetLocalizedResource();

		public bool IsExecutable =>
			context.PageType == ContentPageTypes.SearchResults &&
			context.PageType == ContentPageTypes.Ftp &&
			context.PageType == ContentPageTypes.ZipFolder;

		public OpenDirectoryInNewPaneAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			NavigationHelpers.OpenInSecondaryPane(
				context.ShellPage,
				context.ShellPage.SlimContentPage.SelectedItems.FirstOrDefault());

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
