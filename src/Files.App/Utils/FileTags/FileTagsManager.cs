// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Specialized;

namespace Files.App.Utils.FileTags
{
	public sealed class FileTagsManager
	{
		private readonly ILogger logger = Ioc.Default.GetRequiredService<ILogger<App>>();
		private readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetService<IFileTagsSettingsService>();

		public EventHandler<NotifyCollectionChangedEventArgs> DataChanged;

		private readonly List<FileTagItem> fileTags = [];
		public IReadOnlyList<FileTagItem> FileTags
		{
			get
			{
				lock (fileTags)
				{
					return fileTags.ToList().AsReadOnly();
				}
			}
		}

		public FileTagsManager()
		{
			fileTagsSettingsService.OnTagsUpdated += TagsUpdatedAsync;
		}

		private async void TagsUpdatedAsync(object? _, EventArgs e)
		{
			lock (fileTags)
				fileTags.Clear();
			DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			await UpdateFileTagsAsync();
		}

		public Task UpdateFileTagsAsync()
		{
			try
			{
				foreach (var tag in fileTagsSettingsService.FileTagList)
				{
					var tagItem = new FileTagItem
					{
						Text = tag.Name,
						Path = $"tag:{tag.Name}",
						FileTag = tag,
						MenuOptions = new ContextMenuOptions { IsLocationItem = true },
					};

					lock (fileTags)
					{
						if (fileTags.Any(x => x.Path == $"tag:{tag.Name}"))
						{
							continue;
						}
						fileTags.Add(tagItem);
					}
					DataChanged?.Invoke(SectionType.FileTag, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, tagItem));
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Error loading tags section.");
			}

			return Task.CompletedTask;
		}
	}
}
