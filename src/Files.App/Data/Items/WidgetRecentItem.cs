// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents an item for recent item of File Explorer on Windows.
	/// </summary>
	public sealed class RecentItem : WidgetCardItem, IEquatable<RecentItem>, IDisposable
	{
		private BitmapImage? _Icon;
		/// <summary>
		/// Gets or sets thumbnail icon of the recent item.
		/// </summary>
		public BitmapImage? Icon
		{
			get => _Icon;
			set => SetProperty(ref _Icon, value);
		}

		/// <summary>
		/// Gets or sets name of the recent item.
		/// </summary>
		public required string Name { get; set; }

		/// <summary>
		/// Gets or sets target path of the recent item.
		/// </summary>
		public required DateTime LastModified { get; set; }

		/// <summary>
		/// Gets or initializes PIDL of the recent item.
		/// </summary>
		/// <remarks>
		/// This has to be removed in the future.
		/// </remarks>
		public unsafe required ComPtr<IShellItem> ShellItem { get; init; }

		/// <summary>
		/// Loads thumbnail icon of the recent item.
		/// </summary>
		/// <returns></returns>
		public async Task LoadRecentItemIconAsync()
		{
			var result = await FileThumbnailHelper.GetIconAsync(Path, Constants.ShellIconSizes.Small, false, IconOptions.UseCurrentScale);

			var bitmapImage = await result.ToBitmapAsync();
			if (bitmapImage is not null)
				Icon = bitmapImage;
		}

		public override int GetHashCode() => (Path, Name).GetHashCode();
		public override bool Equals(object? other) => other is RecentItem item && Equals(item);
		public bool Equals(RecentItem? other) => other is not null && other.Name == Name && other.Path == Path;

		public unsafe void Dispose()
		{
			ShellItem.Dispose();
		}
	}
}
