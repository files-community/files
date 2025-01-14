// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
    sealed class GitInitAction : ObservableObject, IAction
    {
		private readonly IContentPageContext _context;

		public string Label
			=> "InitRepo".GetLocalizedResource();

		public string Description
			=> "InitRepoDescription".GetLocalizedResource();

		public bool IsExecutable => 
			_context.Folder is not null &&
			_context.Folder.ItemPath != SystemIO.Path.GetPathRoot(_context.Folder.ItemPath) &&
			!_context.IsGitRepository;

		public GitInitAction()
		{
			_context = Ioc.Default.GetRequiredService<IContentPageContext>();

			_context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return GitHelpers.InitializeRepositoryAsync(_context.Folder?.ItemPath);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.Folder):
				case nameof(IContentPageContext.IsGitRepository):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
