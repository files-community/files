// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class PinToStartAction : IAction
	{
		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		private IStartMenuService StartMenuService { get; } = Ioc.Default.GetRequiredService<IStartMenuService>();

		public IContentPageContext context;

		public string Label
			=> "PinItemToStart/Text".GetLocalizedResource();

		public string Description
			=> "PinToStartDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.FavoritePin");

		public bool IsExecutable =>
			context.ShellPage is not null;

		public PinToStartAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItems.Count > 0 && context.ShellPage?.SlimContentPage?.SelectedItems is not null)
			{
				foreach (ListedItem listedItem in context.ShellPage.SlimContentPage.SelectedItems)
				{
					await SafetyExtensions.IgnoreExceptions(async () =>
					{
						IStorable storable = listedItem.IsFolder switch
						{
							true => await StorageService.GetFolderAsync(listedItem.ItemPath),
							_ => await StorageService.GetFileAsync((listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath)
						};
						await StartMenuService.PinAsync(storable, listedItem.Name);
					});
				}
			}
			else if (context.ShellPage?.ShellViewModel?.CurrentFolder is not null)
			{
				await SafetyExtensions.IgnoreExceptions(async () =>
				{
					var currentFolder = context.ShellPage.ShellViewModel.CurrentFolder;
					var folder = await StorageService.GetFolderAsync(currentFolder.ItemPath);

					await StartMenuService.PinAsync(folder, currentFolder.Name);
				});
			}
		}
	}
}
