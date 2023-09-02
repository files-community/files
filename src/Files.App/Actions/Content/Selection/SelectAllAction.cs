﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Files.App.ViewModels.UserControls;

namespace Files.App.Actions
{
	internal class SelectAllAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "SelectAll".GetLocalizedResource();

		public string Description
			=> "SelectAllDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new("\uE8B3");

		public HotKey HotKey
			=> new(Keys.A, KeyModifiers.Ctrl);

		public bool IsExecutable
		{
			get
			{
				var page = context.ShellPage;
				 
				bool isCommandPaletteOpen = page.ToolbarViewModel.IsCommandPaletteOpen;
				if (isCommandPaletteOpen)
					return true;
				if (page is null)
					return false;
				if (context.PageType is ContentPageTypes.Home && !isCommandPaletteOpen)
					return false;

				int itemCount = page.FilesystemViewModel.FilesAndFolders.Count;
				int selectedItemCount = context.SelectedItems.Count;
				if (itemCount == selectedItemCount && !isCommandPaletteOpen)
					return false;

				bool isEditing = page.ToolbarViewModel.IsEditModeEnabled;
				bool isRenaming = page.SlimContentPage.IsRenamingItem;

				return (!isEditing && !isRenaming);
			}
		}

		public SelectAllAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync()
		{
			context.ShellPage?.SlimContentPage?.ItemManipulationModel?.SelectAllItems();

			return Task.CompletedTask;
		}
	}
}
