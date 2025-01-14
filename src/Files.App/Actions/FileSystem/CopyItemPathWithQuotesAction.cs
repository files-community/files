// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal sealed class CopyItemPathWithQuotesAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.CopyItemPathWithQuotes.GetLocalizedResource();

		public string Description
			=> Strings.CopyItemPathWithQuotesDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new RichGlyph(themedIconStyle: "App.ThemedIcons.CopyAsPath");

		public HotKey HotKey
			=> new(Keys.C, KeyModifiers.CtrlAlt);

		public bool IsExecutable
			=> context.HasSelection;

		public CopyItemPathWithQuotesAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage?.SlimContentPage is not null)
			{
				var selectedItems = context.ShellPage.SlimContentPage.SelectedItems;
				var path = selectedItems is not null
					? string.Join("\n", selectedItems.Select(item => $"\"{item.ItemPath}\""))
					: context.ShellPage.ShellViewModel.WorkingDirectory;

				if (FtpHelpers.IsFtpPath(path))
					path = path.Replace("\\", "/", StringComparison.Ordinal);

				SafetyExtensions.IgnoreExceptions(() =>
				{
					DataPackage data = new();
					data.SetText(path);

					Clipboard.SetContent(data);
					Clipboard.Flush();
				});
			}

			return Task.CompletedTask;
		}
	}
}
