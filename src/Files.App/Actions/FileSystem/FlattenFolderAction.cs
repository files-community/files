// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using Windows.Foundation.Metadata;
using Windows.Storage;

namespace Files.App.Actions
{
	internal sealed class FlattenFolderAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;
		private readonly IGeneralSettingsService GeneralSettingsService = Ioc.Default.GetRequiredService<IGeneralSettingsService>();

		public string Label
			=> "FlattenFolder".GetLocalizedResource();

		public string Description
			=> "FlattenFolderDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Folder");

		public bool IsExecutable =>
			GeneralSettingsService.ShowFlattenOptions &&
			context.ShellPage is not null &&
			context.HasSelection &&
			context.SelectedItems.Count is 1 &&
			context.SelectedItem is not null &&
			context.SelectedItem.PrimaryItemAttribute is StorageItemTypes.Folder;

		public FlattenFolderAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
			GeneralSettingsService.PropertyChanged += GeneralSettingsService_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			if (context.SelectedItem is null)
				return;

			var optionsDialog = new ContentDialog()
			{
				Title = "FlattenFolder".GetLocalizedResource(),
				Content = "FlattenFolderDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "Flatten".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				optionsDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			var result = await optionsDialog.TryShowAsync();
			if (result != ContentDialogResult.Primary)
				return;

			FlattenFolder(context.SelectedItem.ItemPath);
		}

		private void FlattenFolder(string path)
		{
			var containedFolders = Directory.GetDirectories(path);
			var containedFiles = Directory.GetFiles(path);

			foreach (var containedFolder in containedFolders)
			{
				FlattenFolder(containedFolder);

				var folderName = Path.GetFileName(containedFolder);
				var destinationPath = Path.Combine(context?.SelectedItem?.ItemPath ?? string.Empty, folderName);

				if (Directory.Exists(destinationPath))
					continue;

				try
				{
					Directory.Move(containedFolder, destinationPath);
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex.Message, $"Folder '{folderName}' already exists in the destination folder.");
				}
			}

			foreach (var containedFile in containedFiles)
			{
				var fileName = Path.GetFileName(containedFile);
				var destinationPath = Path.Combine(context?.SelectedItem?.ItemPath ?? string.Empty, fileName);

				if (File.Exists(destinationPath))
					continue;

				try
				{
					File.Move(containedFile, destinationPath);
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex.Message, $"Failed to move file '{fileName}'.");
				}
			}

			if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
			{
				try
				{
					Directory.Delete(path);
				}
				catch (Exception ex)
				{
					App.Logger.LogWarning(ex.Message, $"Failed to delete folder '{path}'.");
				}
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.HasSelection):
				case nameof(IContentPageContext.SelectedItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private void GeneralSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IGeneralSettingsService.ShowFlattenOptions))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
