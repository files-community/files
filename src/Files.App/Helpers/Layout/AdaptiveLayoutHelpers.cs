// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Previews;
using Files.Shared.Helpers;
using Windows.Storage;
using static Files.App.Constants.AdaptiveLayout;

namespace Files.App.Helpers
{
	public static class AdaptiveLayoutHelpers
	{
		private static ILayoutSettingsService LayoutSettingsService { get; } = Ioc.Default.GetRequiredService<ILayoutSettingsService>();
		private static IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public static void ApplyAdaptativeLayout(LayoutPreferencesManager folderSettings, IList<ListedItem> filesAndFolders)
		{
			if (LayoutSettingsService.SyncFolderPreferencesAcrossDirectories)
				return;
			if (folderSettings.IsLayoutModeFixed || !folderSettings.IsAdaptiveLayoutEnabled)
				return;

			var layout = GetAdaptiveLayout(filesAndFolders);
			switch (layout)
			{
				case Layouts.Detail:
					folderSettings.ToggleLayoutModeDetailsView(false);
					break;
				case Layouts.Grid:
					folderSettings.ToggleLayoutModeGridView(false);
					break;
			}
		}

		private static Layouts GetAdaptiveLayout(IList<ListedItem> filesAndFolders)
		{
			var pathLayout = GetPathLayout();
			if (pathLayout is not Layouts.None)
				return pathLayout;

			return GetContentLayout(filesAndFolders);
		}

		private static Layouts GetPathLayout()
		{
			var desktopIni = ContentPageContext.ShellPage?.ShellViewModel?.DesktopIni;
			if (desktopIni is null)
				return Layouts.None;

			var viewStateSection = desktopIni.FirstOrDefault(x => x.SectionName == "ViewState");
			if (viewStateSection is null)
				return Layouts.None;

			var viewMode = viewStateSection.Parameters.FirstOrDefault(x => x.Key == "Mode").Value;

			return viewMode switch
			{
				"Pictures" => Layouts.Grid,
				"Videos" => Layouts.Grid,
				_ => Layouts.Detail,
			};
		}

		private static Layouts GetContentLayout(IList<ListedItem> filesAndFolders)
		{
			int itemCount = filesAndFolders.Count;
			if (filesAndFolders.Count is 0)
				return Layouts.None;

			float folderPercentage = 100f * filesAndFolders.Count(IsFolder) / itemCount;
			float imagePercentage = 100f * filesAndFolders.Count(IsImage) / itemCount;
			float mediaPercentage = 100f * filesAndFolders.Count(IsMedia) / itemCount;
			float miscPercentage = 100f - (folderPercentage + imagePercentage + mediaPercentage);

			if (folderPercentage + miscPercentage > LargeThreshold)
				return Layouts.Detail;
			if (imagePercentage > ExtraLargeThreshold)
				return Layouts.Grid;
			if (imagePercentage <= MediumThreshold)
				return Layouts.Detail;
			if (100f - imagePercentage <= SmallThreshold)
				return Layouts.Detail;
			if (folderPercentage + miscPercentage <= ExtraSmallThreshold)
				return Layouts.Detail;
			return Layouts.Grid;

			static bool IsFolder(ListedItem item)
				=> item.PrimaryItemAttribute is StorageItemTypes.Folder;

			static bool IsImage(ListedItem item)
				=> !string.IsNullOrEmpty(item.FileExtension)
				&& ImagePreviewViewModel.ContainsExtension(item.FileExtension.ToLowerInvariant());

			static bool IsMedia(ListedItem item)
				=> !string.IsNullOrEmpty(item.FileExtension)
				&& (FileExtensionHelpers.IsAudioFile(item.FileExtension) 
				|| FileExtensionHelpers.IsVideoFile(item.FileExtension));
		}

		private enum Layouts
		{
			None, // Don't decide. Another function to decide can be called afterwards if available.
			Detail, // Apply the layout Detail.
			Grid, // Apply the layout Grid.
		}
	}
}
