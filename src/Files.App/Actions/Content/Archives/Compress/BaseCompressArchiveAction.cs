// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal abstract class BaseCompressArchiveAction : BaseUIAction, IAction
	{
		protected readonly IContentPageContext context;
		protected IStorageArchiveService StorageArchiveService { get; } = Ioc.Default.GetRequiredService<IStorageArchiveService>();

		public abstract string Label { get; }

		public abstract string Description { get; }

		public override bool IsExecutable =>
			IsContextPageTypeAdaptedToCommand() &&
			StorageArchiveService.CanCompress(context.SelectedItems) &&
			UIHelpers.CanShowDialog;

		public BaseCompressArchiveAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public abstract Task ExecuteAsync(object? parameter = null);

		protected void GetDestination(out string[] sources, out string directory, out string fileName)
		{
			sources = context.SelectedItems.Select(item => item.ItemPath).ToArray();
			directory = string.Empty;
			fileName = string.Empty;

			if (sources.Length is not 0)
			{
				// Get the current directory path
				directory = context.ShellPage.ShellViewModel.WorkingDirectory.Normalize();

				// Get the library save folder if the folder is library item
				if (App.LibraryManager.TryGetLibrary(directory, out var library) && !library.IsEmpty)
					directory = library.DefaultSaveFolder;

				// Gets the file name from the directory path
				fileName = SystemIO.Path.GetFileName(sources.Length is 1 ? sources[0] : directory);
			}
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return
				context.PageType != ContentPageTypes.RecycleBin &&
				context.PageType != ContentPageTypes.ZipFolder &&
				context.PageType != ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					if (IsContextPageTypeAdaptedToCommand())
						OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
