// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Utils;
using System.IO;

namespace Files.App.ViewModels.Dialogs.FileSystemDialog
{
	public abstract class BaseFileSystemDialogItemViewModel : ObservableObject
	{
		public IMessenger? Messenger { get; set; }

		private string? _SourcePath;
		public virtual string? SourcePath
		{
			get => _SourcePath;
			set
			{
				if (SetProperty(ref _SourcePath, value))
				{
					OnPropertyChanged(nameof(SourceDirectoryDisplayName));
					DisplayName = Path.GetFileName(value);
				}
			}
		}

		private string? _DisplayName;
		public virtual string? DisplayName
		{
			get => _DisplayName;
			set => SetProperty(ref _DisplayName, value);
		}

		private IImage? _ItemIcon;
		public IImage? ItemIcon
		{
			get => _ItemIcon;
			set => SetProperty(ref _ItemIcon, value);
		}

		public virtual string? SourceDirectoryDisplayName
			=> Path.GetFileName(Path.GetDirectoryName(SourcePath));
	}
}
