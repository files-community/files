// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Contracts
{
	public interface IFoldersSettingsService : IBaseSettingsService, INotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets a value indicating whether or not hidden items should be visible.
		/// </summary>
		bool ShowHiddenItems { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not protected system files should be visible.
		/// </summary>
		bool ShowProtectedSystemFiles { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not alternate data streams should be visible.
		/// </summary>
		bool AreAlternateStreamsVisible { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to display dot files.
		/// </summary>
		bool ShowDotFiles { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not items should open with one click.
		/// </summary>
		bool OpenItemsWithOneClick { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not folders should open with two clicks in ColumnsLayout.
		/// </summary>
		bool ColumnLayoutOpenFoldersWithOneClick { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to open folders in new tab.
		/// </summary>
		bool OpenFoldersInNewTab { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to show folder size.
		/// </summary>
		bool CalculateFolderSizes { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to scroll to the parent folder when navigating up.
		/// </summary>
		bool ScrollToPreviousFolderWhenNavigatingUp { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if file extensions should be displayed.
		/// </summary>
		bool ShowFileExtensions { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if media thumbnails should be displayed.
		/// </summary>
		bool ShowThumbnails { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to show the delete confirmation dialog when deleting items.
		/// </summary>
		DeleteConfirmationPolicies DeleteConfirmationPolicy { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to select files and folders when hovering them.
		/// </summary>
		bool SelectFilesOnHover { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if double clicking a blank space should go up a directory.
		/// </summary>
		bool DoubleClickToGoUp { get; set; }

		/// <summary>
		/// Gets or sets a value indicating if a warning dialog show be shown when changing file extensions.
		/// </summary>
		bool ShowFileExtensionWarning { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not to show checkboxes when selecting items.
		/// </summary>
		bool ShowCheckboxesWhenSelectingItems { get; set; }
	}
}
