// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal sealed class EditInNotepadAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "EditInNotepad".GetLocalizedResource();

		public string Description
			=> "EditInNotepadDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE70F");

		public bool IsExecutable =>
			context.SelectedItems.Any() &&
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.ZipFolder &&
			context.SelectedItems.All(x => FileExtensionHelpers.IsBatchFile(x.FileExtension) || FileExtensionHelpers.IsAhkFile(x.FileExtension) || FileExtensionHelpers.IsCmdFile(x.FileExtension));

		public EditInNotepadAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			return Task.WhenAll(context.SelectedItems.Select(item => Win32Helper.RunPowershellCommandAsync($"notepad '{item.ItemPath}\'", PowerShellExecutionOptions.Hidden)));
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}