// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal sealed class CopyItemPathAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.CopyPath.GetLocalizedResource();

		public string Description
			=> Strings.CopyItemPathDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new RichGlyph(themedIconStyle: "App.ThemedIcons.CopyAsPath");

		public HotKey HotKey
			=> new(Keys.C, KeyModifiers.CtrlShift);

		public bool IsExecutable
			=> context.HasSelection;

		public CopyItemPathAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage?.SlimContentPage is not null)
			{
				var path = context.ShellPage.SlimContentPage.SelectedItems is not null
				? context.ShellPage.SlimContentPage.SelectedItems.Select(x => x.ItemPath).Aggregate((accum, current) => accum + "\n" + current)
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