// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Utils;
using System.Windows.Input;

namespace Files.App.Data.Items
{
	public sealed partial class WidgetFileTagsContainerItem : ObservableObject, IAsyncInitialize
	{
		// Fields

		private readonly IFileTagsService FileTagsService = Ioc.Default.GetRequiredService<IFileTagsService>();
		private readonly IImageService ImageService = Ioc.Default.GetRequiredService<IImageService>();
		private readonly ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		private readonly string _tagUid;

		// Properties

		public ObservableCollection<WidgetFileTagCardItem> Tags { get; }

		private string? _Color;
		public string? Color
		{
			get => _Color;
			set => SetProperty(ref _Color, value);
		}

		private string? _Name;
		public string? Name
		{
			get => _Name;
			set => SetProperty(ref _Name, value);
		}

		// Events

		public delegate void SelectedTagChangedEventHandler(object sender, SelectedTagChangedEventArgs e);
		public static event SelectedTagChangedEventHandler? SelectedTagChanged;

		// Commands

		public ICommand ViewMoreCommand { get; }
		public ICommand OpenAllCommand { get; }

		public WidgetFileTagsContainerItem(string tagUid)
		{
			_tagUid = tagUid;
			Tags = new();

			ViewMoreCommand = new AsyncRelayCommand(ViewMore);
			OpenAllCommand = new AsyncRelayCommand(OpenAll);
		}

		/// <inheritdoc/>
		public async Task InitAsync(CancellationToken cancellationToken = default)
		{
			await foreach (var item in FileTagsService.GetItemsForTagAsync(_tagUid))
			{
				var icon = await ImageService.GetIconAsync(item.Storable, default);
				Tags.Add(new(item.Storable, icon));
			}
		}

		private Task<bool> ViewMore()
		{
			return NavigationHelpers.OpenPath($"tag:{Name}", ContentPageContext.ShellPage!);
		}

		private Task OpenAll()
		{
			SelectedTagChanged?.Invoke(this, new(Tags.Select(tag => (tag.Path, tag.IsFolder))));

			return Commands.OpenAllTaggedItems.ExecuteAsync();
		}
	}
}
