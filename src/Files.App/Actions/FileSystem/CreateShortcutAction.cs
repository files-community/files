﻿using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using System.IO;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CreateShortcutAction : IAction
	{
		public IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label { get; } = "BaseLayoutItemContextFlyoutShortcut/Text".GetLocalizedResource();

		public RichGlyph Glyph { get; } = new RichGlyph(opacityStyle: "ColorIconShortcut");

		public async Task ExecuteAsync()
		{
			var currentPath = context.ShellPage?.FilesystemViewModel.WorkingDirectory;

			if (App.LibraryManager.TryGetLibrary(currentPath ?? string.Empty, out var library) && !library.IsEmpty)
			{
				currentPath = library.DefaultSaveFolder;
			}

			foreach (ListedItem selectedItem in context.SelectedItems)
			{
				var fileName = string.Format("ShortcutCreateNewSuffix".GetLocalizedResource(), selectedItem.Name) + ".lnk";
				var filePath = Path.Combine(currentPath ?? string.Empty, fileName);

				if (!await FileOperationsHelpers.CreateOrUpdateLinkAsync(filePath, selectedItem.ItemPath))
					await UIFilesystemHelpers.HandleShortcutCannotBeCreated(fileName, selectedItem.ItemPath);
			}
		}
	}
}
