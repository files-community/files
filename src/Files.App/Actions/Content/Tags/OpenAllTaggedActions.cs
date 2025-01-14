// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
    sealed class OpenAllTaggedActions: ObservableObject, IAction
    {
		private readonly IContentPageContext _pageContext;

		private readonly ITagsContext _tagsContext;

		public string Label
			=> "OpenAllTaggedItems".GetLocalizedResource();

		public string Description
			=> "OpenAllTaggedItemsDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE71D");

		public bool IsExecutable => 
			_pageContext.ShellPage is not null &&
			_tagsContext.TaggedItems.Any();

		public OpenAllTaggedActions()
		{
			_pageContext = Ioc.Default.GetRequiredService<IContentPageContext>();
			_tagsContext = Ioc.Default.GetRequiredService<ITagsContext>();

			_pageContext.PropertyChanged += Context_PropertyChanged;
			_tagsContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			var filePaths = _tagsContext.TaggedItems
				.Where(item => !item.isFolder)
				.Select(f => f.path)
				.ToList();
			
			var folderPaths = _tagsContext
				.TaggedItems
				.Where(item => item.isFolder)
				.Select(f => f.path)
				.ToList();
			
			await Task.WhenAll(filePaths.Select(path => NavigationHelpers.OpenPath(path, _pageContext.ShellPage!)));

			foreach (var path in folderPaths) 
				await NavigationHelpers.OpenPathInNewTab(path);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.ShellPage):
				case nameof(ITagsContext.TaggedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
