// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	public static class LayoutSizeKindHelper
	{
		private static ILayoutSettingsService LayoutSettingsService { get; } = Ioc.Default.GetRequiredService<ILayoutSettingsService>();

		/// <summary>
		/// Gets the desired icon size for the requested layout
		/// </summary>
		/// <param name="folderLayoutMode"></param>
		/// <returns></returns>
		public static uint GetIconSize(FolderLayoutModes folderLayoutMode)
		{
			return folderLayoutMode switch
			{
				// Details
				FolderLayoutModes.DetailsView when LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.Compact => Constants.ShellIconSizes.Small,
				FolderLayoutModes.DetailsView when LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.Small => Constants.ShellIconSizes.Small,
				FolderLayoutModes.DetailsView when LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.Medium => 20,
				FolderLayoutModes.DetailsView when LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.Large => 24,
				FolderLayoutModes.DetailsView when LayoutSettingsService.DetailsViewSize == DetailsViewSizeKind.ExtraLarge => Constants.ShellIconSizes.Large,

				// List
				FolderLayoutModes.ListView when LayoutSettingsService.ListViewSize == ListViewSizeKind.Compact => Constants.ShellIconSizes.Small,
				FolderLayoutModes.ListView when LayoutSettingsService.ListViewSize == ListViewSizeKind.Small => Constants.ShellIconSizes.Small,
				FolderLayoutModes.ListView when LayoutSettingsService.ListViewSize == ListViewSizeKind.Medium => 20,
				FolderLayoutModes.ListView when LayoutSettingsService.ListViewSize == ListViewSizeKind.Large => 24,
				FolderLayoutModes.ListView when LayoutSettingsService.ListViewSize == ListViewSizeKind.ExtraLarge => Constants.ShellIconSizes.Large,

				// Columns
				FolderLayoutModes.ColumnView when LayoutSettingsService.ColumnsViewSize == ColumnsViewSizeKind.Compact => Constants.ShellIconSizes.Small,
				FolderLayoutModes.ColumnView when LayoutSettingsService.ColumnsViewSize == ColumnsViewSizeKind.Small => Constants.ShellIconSizes.Small,
				FolderLayoutModes.ColumnView when LayoutSettingsService.ColumnsViewSize == ColumnsViewSizeKind.Medium => 20,
				FolderLayoutModes.ColumnView when LayoutSettingsService.ColumnsViewSize == ColumnsViewSizeKind.Large => 24,
				FolderLayoutModes.ColumnView when LayoutSettingsService.ColumnsViewSize == ColumnsViewSizeKind.ExtraLarge => Constants.ShellIconSizes.Large,

				// Card
				FolderLayoutModes.CardsView when LayoutSettingsService.CardsViewSize == CardsViewSizeKind.Small => 64,
				FolderLayoutModes.CardsView when LayoutSettingsService.CardsViewSize == CardsViewSizeKind.Medium => 64,
				FolderLayoutModes.CardsView when LayoutSettingsService.CardsViewSize == CardsViewSizeKind.Large => 80,
				FolderLayoutModes.CardsView when LayoutSettingsService.CardsViewSize == CardsViewSizeKind.ExtraLarge => 96,

				// Grid
				FolderLayoutModes.GridView when LayoutSettingsService.GridViewSize <= GridViewSizeKind.Small => 96,
				FolderLayoutModes.GridView when LayoutSettingsService.GridViewSize <= GridViewSizeKind.Large => 128,

				_ => 256,
			};
		}

		/// <summary>
		/// Gets the desired height for items in the Details View
		/// </summary>
		/// <param name="detailsViewSizeKind"></param>
		/// <returns></returns>
		public static int GetDetailsViewRowHeight(DetailsViewSizeKind detailsViewSizeKind)
		{
			switch (detailsViewSizeKind)
			{
				case DetailsViewSizeKind.Compact:
					return 28;
				case DetailsViewSizeKind.Small:
					return 36;
				case DetailsViewSizeKind.Medium:
					return 40;
				case DetailsViewSizeKind.Large:
					return 44;
				case DetailsViewSizeKind.ExtraLarge:
					return 48;
				default:
					return 36;
			}
		}

		/// <summary>
		/// Gets the desired width for items in the Grid View
		/// </summary>
		/// <param name="gridViewSizeKind"></param>
		/// <returns></returns>
		public static int GetGridViewItemWidth(GridViewSizeKind gridViewSizeKind)
		{
			switch (gridViewSizeKind)
			{
				case GridViewSizeKind.Small:
					return 80;
				case GridViewSizeKind.Medium:
					return 100;
				case GridViewSizeKind.Three:
					return 120;
				case GridViewSizeKind.Four:
					return 140;
				case GridViewSizeKind.Five:
					return 160;
				case GridViewSizeKind.Six:
					return 180;
				case GridViewSizeKind.Seven:
					return 200;
				case GridViewSizeKind.Large:
					return 220;
				case GridViewSizeKind.Nine:
					return 240;
				case GridViewSizeKind.Ten:
					return 260;
				case GridViewSizeKind.Eleven:
					return 280;
				case GridViewSizeKind.ExtraLarge:
					return 300;
				default:
					return 100;
			}
		}

		/// <summary>
		/// Gets the desired height for items in the List View
		/// </summary>
		/// <param name="listViewSizeKind"></param>
		/// <returns></returns>
		public static int GetListViewRowHeight(ListViewSizeKind listViewSizeKind)
		{
			switch (listViewSizeKind)
			{
				case ListViewSizeKind.Compact:
					return 24;
				case ListViewSizeKind.Small:
					return 32;
				case ListViewSizeKind.Medium:
					return 36;
				case ListViewSizeKind.Large:
					return 40;
				case ListViewSizeKind.ExtraLarge:
					return 44;
				default:
					return 32;
			}
		}

		/// <summary>
		/// Gets the desired height for items in the Columns View
		/// </summary>
		/// <param name="columnsViewSizeKind"></param>
		/// <returns></returns>
		public static int GetColumnsViewRowHeight(ColumnsViewSizeKind columnsViewSizeKind)
		{
			switch (columnsViewSizeKind)
			{
				case ColumnsViewSizeKind.Compact:
					return 24;
				case ColumnsViewSizeKind.Small:
					return 32;
				case ColumnsViewSizeKind.Medium:
					return 36;
				case ColumnsViewSizeKind.Large:
					return 40;
				case ColumnsViewSizeKind.ExtraLarge:
					return 44;
				default:
					return 32;
			}
		}
	}
}