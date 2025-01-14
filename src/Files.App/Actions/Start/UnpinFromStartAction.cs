// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class UnpinFromStartAction : IAction
	{
		private IStorageService StorageService { get; } = Ioc.Default.GetRequiredService<IStorageService>();

		private IStartMenuService StartMenuService { get; } = Ioc.Default.GetRequiredService<IStartMenuService>();

		public IContentPageContext context;

		public string Label
			=> "UnpinItemFromStart/Text".GetLocalizedResource();

		public string Description
			=> "UnpinFromStartDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.FavoritePinRemove");

		public UnpinFromStartAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItems.Count > 0)
			{
				foreach (ListedItem listedItem in context.ShellPage?.SlimContentPage.SelectedItems)
				{
					await SafetyExtensions.IgnoreExceptions(async () =>
					{
						IStorable storable = listedItem.IsFolder switch
						{
							true => await StorageService.GetFolderAsync(listedItem.ItemPath),
							_ => await StorageService.GetFileAsync((listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath)
						};
						await StartMenuService.UnpinAsync(storable);
					});
				}
			}
			else
			{
				await SafetyExtensions.IgnoreExceptions(async () =>
				{
					var currentFolder = context.ShellPage.ShellViewModel.CurrentFolder;
					var folder = await StorageService.GetFolderAsync(currentFolder.ItemPath);

					await StartMenuService.UnpinAsync(folder);
				});
			}
		}
	}
}
