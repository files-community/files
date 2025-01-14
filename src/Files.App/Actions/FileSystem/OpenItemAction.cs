// Copyright (c) Files Community
// Licensed under the MIT License.

using System.IO;
using Windows.Storage;

namespace Files.App.Actions
{
	internal sealed class OpenItemAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "Open".GetLocalizedResource();

		public string Description
			=> "OpenItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.OpenFile");

		public HotKey HotKey
			=> new(Keys.Enter);


		public bool IsExecutable =>
			context.HasSelection &&
			!(context.ShellPage is ColumnShellPage &&
			context.SelectedItem?.PrimaryItemAttribute == StorageItemTypes.Folder);

		public OpenItemAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is not null)
				return NavigationHelpers.OpenSelectedItemsAsync(context.ShellPage);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}

	internal sealed class OpenItemWithApplicationPickerAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "OpenWith".GetLocalizedResource();

		public string Description
			=> "OpenItemWithApplicationPickerDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.OpenWith");

		public bool IsExecutable =>
			context.HasSelection &&
			context.SelectedItems.All(i =>
				(i.PrimaryItemAttribute == StorageItemTypes.File && !i.IsShortcut && !i.IsExecutable) ||
				(i.PrimaryItemAttribute == StorageItemTypes.Folder && i.IsArchive));

		public OpenItemWithApplicationPickerAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return Task.CompletedTask;

			return NavigationHelpers.OpenSelectedItemsAsync(context.ShellPage, true);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}

	internal sealed class OpenParentFolderAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> "BaseLayoutItemContextFlyoutOpenParentFolder/Text".GetLocalizedResource();

		public string Description
			=> "OpenParentFolderDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(baseGlyph: "\uE197");

		public bool IsExecutable =>
			context.HasSelection &&
			context.ShellPage is not null &&
			context.ShellPage.InstanceViewModel.IsPageTypeSearchResults;

		public OpenParentFolderAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return;

			var item = context.SelectedItem;
			var folderPath = Path.GetDirectoryName(item?.ItemPath.TrimEnd('\\'));

			if (folderPath is null || item is null)
				return;

			context.ShellPage.NavigateWithArguments(context.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
			{
				NavPathParam = folderPath,
				SelectItems = [item.ItemNameRaw],
				AssociatedTabInstance = context.ShellPage
			});
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
