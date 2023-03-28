﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Filesystem.Archive;
using Files.App.Helpers;
using Microsoft.UI.Xaml.Input;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CompressIntoZipAction : XamlUICommand
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => string.Format("CreateNamedArchive".GetLocalizedResource(), $"{ArchiveHelpers.DetermineArchiveNameFromSelection(context.SelectedItems)}.zip");

		public string Description => "TODO: Need to be described.";

		public bool CanExecute => IsContextPageTypeAdaptedToCommand()
									&& ArchiveHelpers.CanCompress(context.SelectedItems);

		public CompressIntoZipAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var (sources, directory, fileName) = ArchiveHelpers.GetCompressDestination(context.ShellPage);

			IArchiveCreator creator = new ArchiveCreator
			{
				Sources = sources,
				Directory = directory,
				FileName = fileName,
				FileFormat = ArchiveFormats.Zip,
			};

			await ArchiveHelpers.CompressArchiveAsync(creator);
		}

		private bool IsContextPageTypeAdaptedToCommand()
		{
			return context.PageType is not ContentPageTypes.RecycleBin
				and not ContentPageTypes.ZipFolder
				and not ContentPageTypes.None;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
					if (IsContextPageTypeAdaptedToCommand())
						NotifyCanExecuteChanged();
					break;
			}
		}
	}
}