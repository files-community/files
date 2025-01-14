// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Immutable;

namespace Files.App.Data.Contexts
{
	sealed class TagsContext : ITagsContext
    {
		private static readonly IReadOnlyList<(string path, bool isFolder)> _emptyTaggedItemsList
			= Enumerable.Empty<(string path, bool isFolder)>().ToImmutableList();

		public event PropertyChangedEventHandler? PropertyChanged;

		private IEnumerable<(string path, bool isFolder)> _TaggedItems = _emptyTaggedItemsList;
		public IEnumerable<(string path, bool isFolder)> TaggedItems
		{
			get => _TaggedItems;
			set
			{
				_TaggedItems = value is not null
					? value
					: _emptyTaggedItemsList;

				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TaggedItems)));
			}
		}

		public TagsContext()
		{
			WidgetFileTagsContainerItem.SelectedTagChanged += SelectedTagsChanged;
			SidebarViewModel.SelectedTagChanged += SelectedTagsChanged;
		}

		private void SelectedTagsChanged(object _, SelectedTagChangedEventArgs e)
		{
			TaggedItems = e.Items;
		}
	}
}
