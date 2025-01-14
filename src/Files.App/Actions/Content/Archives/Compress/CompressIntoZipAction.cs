// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class CompressIntoZipAction : BaseCompressArchiveAction
	{
		public override string Label
			=> string.Format("CreateNamedArchive".GetLocalizedResource(), $"{StorageArchiveService.GenerateArchiveNameFromItems(context.SelectedItems)}.zip");

		public override string Description
			=> "CompressIntoZipDescription".GetLocalizedResource();

		public CompressIntoZipAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage is null)
				return Task.CompletedTask;

			GetDestination(out var sources, out var directory, out var fileName);

			ICompressArchiveModel compressionModel = new CompressArchiveModel(
				sources,
				directory,
				fileName,
				Environment.ProcessorCount,
				fileFormat: ArchiveFormats.Zip);

			return StorageArchiveService.CompressAsync(compressionModel);
		}
	}
}
