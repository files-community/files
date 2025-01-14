// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Text;
using Windows.Storage;

namespace Files.App.Actions
{
	internal class OpenTerminalAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context;

		public virtual string Label
			=> "OpenTerminal".GetLocalizedResource();

		public virtual string Description
			=> "OpenTerminalDescription".GetLocalizedResource();

		public virtual HotKey HotKey
			=> new(Keys.Oem3, KeyModifiers.Ctrl);

		public RichGlyph Glyph
			=> new("\uE756");

		public virtual bool IsExecutable
			=> GetIsExecutable();

		public virtual bool IsAccessibleGlobally
			=> true;

		public OpenTerminalAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			var terminalStartInfo = GetProcessStartInfo();
			if (terminalStartInfo is not null)
			{
				MainWindow.Instance.DispatcherQueue.TryEnqueue(() =>
				{
					try
					{
						Process.Start(terminalStartInfo);
					}
					catch (Win32Exception)
					{
					}
				});
			}

			return Task.CompletedTask;
		}

		protected virtual ProcessStartInfo? GetProcessStartInfo()
		{
			var paths = GetPaths();
			if (paths.Length is 0)
				return null;

			var path = paths[0] + (paths[0].EndsWith('\\') ? "\\" : "");

			var args = new StringBuilder($"-d \"{path}\"");
			for (int i = 1; i < paths.Length; i++)
			{
				path = paths[i] + (paths[i].EndsWith('\\') ? "\\" : "");
				args.Append($" ; nt -d \"{path}\"");
			}

			return new()
			{
				FileName = "wt.exe",
				Arguments = args.ToString()
			};
		}

		protected virtual string[] GetPaths()
		{
			if (context.HasSelection)
			{
				return context.SelectedItems!
					.Where(item => item.PrimaryItemAttribute is StorageItemTypes.Folder)
					.Select(item => item.ItemPath)
					.ToArray();
			}
			else if (context.Folder is not null)
			{
				return [context.Folder.ItemPath];
			}

			return [];
		}

		private bool GetIsExecutable()
		{
			if (context.PageType is ContentPageTypes.None or ContentPageTypes.Home or ContentPageTypes.RecycleBin or ContentPageTypes.ZipFolder)
				return false;

			var isFolderNull = context.Folder is null;

			if (!context.HasSelection && isFolderNull)
				return false;

			if (context.SelectedItems.Count > Constants.Actions.MaxSelectedItems)
				return false;

			return context.SelectedItems.Any(item => item.PrimaryItemAttribute is StorageItemTypes.Folder) || !isFolderNull;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.Folder):
				case nameof(IContentPageContext.SelectedItems):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
