// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Actions
{
	internal sealed class CopyPathAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.CopyPath.GetLocalizedResource();

		public string Description
			=> Strings.CopyPathDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new RichGlyph(themedIconStyle: "App.ThemedIcons.CopyAsPath");

		public bool IsExecutable
			=> context.PageType != ContentPageTypes.Home && context.PageType != ContentPageTypes.RecycleBin;

		public CopyPathAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			if (context.ShellPage?.SlimContentPage is not null)
			{
				var path = context.ShellPage.ShellViewModel.WorkingDirectory;

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
