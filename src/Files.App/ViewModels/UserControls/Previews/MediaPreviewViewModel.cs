// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Windows.Media.Core;

namespace Files.App.ViewModels.Previews
{
	public sealed class MediaPreviewViewModel : BasePreviewModel
	{
		public event EventHandler TogglePlaybackRequested;

		private MediaSource source;
		public MediaSource Source
		{
			get => source;
			private set => SetProperty(ref source, value);
		}

		public MediaPreviewViewModel(ListedItem item) : base(item) { }

		public void TogglePlayback()
			=> TogglePlaybackRequested?.Invoke(this, null);

		public override Task<List<FileProperty>> LoadPreviewAndDetailsAsync()
		{
			Source = MediaSource.CreateFromStorageFile(Item.ItemFile);

			return Task.FromResult(new List<FileProperty>());
		}

		public override void PreviewControlBase_Unloaded(object sender, RoutedEventArgs e)
		{
			Source = null;

			base.PreviewControlBase_Unloaded(sender, e);
		}
	}
}
