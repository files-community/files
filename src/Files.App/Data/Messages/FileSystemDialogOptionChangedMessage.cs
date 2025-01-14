// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Messaging.Messages;
using Files.App.ViewModels.Dialogs.FileSystemDialog;

namespace Files.App.Data.Messages
{
	/// <summary>
	/// Represents a messenger for FileSystemDialog option changed.
	/// </summary>
	public sealed class FileSystemDialogOptionChangedMessage
		: ValueChangedMessage<FileSystemDialogConflictItemViewModel>
	{
		/// <summary>
		/// Initializes a class.
		/// </summary>
		public FileSystemDialogOptionChangedMessage(FileSystemDialogConflictItemViewModel value)
			: base(value)
		{
		}
	}
}
