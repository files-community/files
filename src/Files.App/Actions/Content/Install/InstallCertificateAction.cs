// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;

namespace Files.App.Actions
{
	internal sealed class InstallCertificateAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Install".GetLocalizedResource();

		public string Description
			=> "InstallCertificateDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uEB95");

		public bool IsExecutable =>
			context.SelectedItems.Any() &&
			context.SelectedItems.All(x => FileExtensionHelpers.IsCertificateFile(x.FileExtension)) &&
			context.PageType != ContentPageTypes.RecycleBin &&
			context.PageType != ContentPageTypes.ZipFolder;

		public InstallCertificateAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			await ContextMenu.InvokeVerb("add", context.SelectedItems.Select(x => x.ItemPath).ToArray());
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IContentPageContext.SelectedItems))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
