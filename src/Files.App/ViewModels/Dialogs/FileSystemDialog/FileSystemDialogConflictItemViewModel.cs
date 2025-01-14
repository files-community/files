// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Enums;
using System.IO;

namespace Files.App.ViewModels.Dialogs.FileSystemDialog
{
	public sealed class FileSystemDialogConflictItemViewModel : BaseFileSystemDialogItemViewModel, IFileSystemDialogConflictItemViewModel
	{
		private string? _DestinationDisplayName;
		public string? DestinationDisplayName
		{
			get => _DestinationDisplayName;
			set => SetProperty(ref _DestinationDisplayName, value);
		}

		private string? _CustomName;
		public string? CustomName
		{
			get => _CustomName;
			set
			{
				if (SetProperty(ref _CustomName, value))
				{
					DestinationDisplayName = value;
					_DestinationPath = Path.Combine(Path.GetDirectoryName(DestinationPath), value);
				}
			}
		}

		private string? _DestinationPath;
		public string? DestinationPath
		{
			get => _DestinationPath;
			set
			{
				if (SetProperty(ref _DestinationPath, value))
					OnPropertyChanged(nameof(DestinationDirectoryDisplayName));
			}
		}

		private bool _IsTextBoxVisible;
		public bool IsTextBoxVisible
		{
			get => _IsTextBoxVisible;
			set => SetProperty(ref _IsTextBoxVisible, value);
		}

		public string DestinationDirectoryDisplayName
			=> Path.GetFileName(Path.GetDirectoryName(DestinationPath));

		public bool IsConflict
			=> ConflictResolveOption != FileNameConflictResolveOptionType.None;

		private FileNameConflictResolveOptionType _ConflictResolveOption;
		public FileNameConflictResolveOptionType ConflictResolveOption
		{
			get => _ConflictResolveOption;
			set
			{
				if (SetProperty(ref _ConflictResolveOption, value))
					Messenger?.Send(new FileSystemDialogOptionChangedMessage(this));
			}
		}
	}
}
