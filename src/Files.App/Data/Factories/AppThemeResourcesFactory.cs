// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Factories
{
	public static class AppThemeResourceFactory
	{
		public static ObservableCollection<AppThemeResourceItem> AppThemeResources { get; } =
		[
			new AppThemeResourceItem
			{
				BackgroundColor = "#00000000", /* Transparent */
				Name = "Default".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32FFB900", /* #FFB900 */
				Name = "YellowGold".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32F7630C", /* #F7630C */
				Name = "OrangeBright".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32D13438", /* #D13438 */
				Name = "BrickRed".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32FF4343", /* #FF4343 */
				Name = "ModRed".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32EA005E", /* #EA005E */
				Name = "Red".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32EA005E", /* #EA005E */
				Name = "RoseBright".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#320078D7", /* #0078D7 */
				Name = "Blue".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#328764B8", /* #8764B8 */
				Name = "IrisPastel".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32B146C2", /* #B146C2 */
				Name = "VioletRedLight".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#320099BC", /* #0099BC */
				Name = "CoolBlueBright".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#3200B7C3", /* #00B7C3 */
				Name = "Seafoam".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#3200B294", /* #00B294 */
				Name = "MintLight".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#327A7574", /* #7A7574 */
				Name = "Gray".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32107C10", /* #107C10 */
				Name = "Green".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#32767676", /* #767676 */
				Name = "Overcast".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#324C4A48", /* #4C4A48 */
				Name = "Storm".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#3269797E", /* #69797E */
				Name = "BlueGray".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#324A5459", /* #4A5459 */
				Name = "GrayDark".GetLocalizedResource()
			},
			new AppThemeResourceItem
			{
				BackgroundColor = "#327E735F", /* #7E735F */
				Name = "Camouflage".GetLocalizedResource()
			}
		];
	}
}
