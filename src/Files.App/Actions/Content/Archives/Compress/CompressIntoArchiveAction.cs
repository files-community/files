// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace Files.App.Actions
{
	internal sealed class CompressIntoArchiveAction : BaseCompressArchiveAction
	{
		public override string Label
			=> "CreateArchive".GetLocalizedResource();

		public override string Description
			=> "CompressIntoArchiveDescription".GetLocalizedResource();

		public CompressIntoArchiveAction()
		{
		}

		public override async Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return;

			GetDestination(out var sources, out var directory, out var fileName);

			var dialog = new CreateArchiveDialog() { FileName = fileName };

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				dialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			var result = await dialog.TryShowAsync();

			if (!dialog.CanCreate || result != ContentDialogResult.Primary)
				return;

			ICompressArchiveModel compressionModel = new CompressArchiveModel(
				sources,
				directory,
				dialog.FileName,
				dialog.CPUThreads,
				dialog.Password,
				dialog.FileFormat,
				dialog.CompressionLevel,
				dialog.SplittingSize);

			await StorageArchiveService.CompressAsync(compressionModel);
		}
	}
}
