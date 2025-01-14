// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Utils;

namespace Files.App.Data.Items
{
	[Bindable(true)]
	public sealed partial class ShelfItem : ObservableObject, IWrapper<IStorable>, IAsyncInitialize
	{
		private readonly IImageService _imageService;
		private readonly ICollection<ShelfItem> _sourceCollection;

		[ObservableProperty] private IImage? _Icon;
		[ObservableProperty] private string? _Name;
		[ObservableProperty] private string? _Path;

		/// <inheritdoc/>
		public IStorable Inner { get; }

		public ShelfItem(IStorable storable, ICollection<ShelfItem> sourceCollection, IImage? icon = null)
		{
			_imageService = Ioc.Default.GetRequiredService<IImageService>();
			_sourceCollection = sourceCollection;
			Inner = storable;
			Icon = icon;
			Name = storable.Name;
			Path = storable.Id;
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			Icon = await _imageService.GetIconAsync(Inner, cancellationToken);
		}

		[RelayCommand]
		private void Remove()
		{
			_sourceCollection.Remove(this);
		}
	}
}
