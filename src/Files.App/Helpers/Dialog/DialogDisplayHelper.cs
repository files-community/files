// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Dialogs;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.Helpers
{
	internal sealed class DialogDisplayHelper
	{
		/// <summary>
		/// Standard dialog, to ensure consistency.
		/// The secondaryText can be un-assigned to hide its respective button.
		/// Result is true if the user presses primary text button
		/// </summary>
		/// <param name="title">
		/// The title of this dialog
		/// </param>
		/// <param name="message">
		/// THe main body message displayed within the dialog
		/// </param>
		/// <param name="primaryText">
		/// Text to be displayed on the primary button (which returns true when pressed).
		/// If not set, defaults to 'OK'
		/// </param>
		/// <param name="secondaryText">
		/// The (optional) secondary button text.
		/// If not set, it won't be presented to the user at all.
		/// </param>
		public static async Task<bool> ShowDialogAsync(string title, string message, string primaryText = "OK", string secondaryText = null)
		{
			var dialog = new DynamicDialog(new DynamicDialogViewModel()
			{
				TitleText = title,
				SubtitleText = message, // We can use subtitle here as our actual message and skip DisplayControl
				PrimaryButtonText = primaryText,
				SecondaryButtonText = secondaryText,
				DynamicButtons = DynamicDialogButtons.Primary | DynamicDialogButtons.Secondary
			});

			return await ShowDialogAsync(dialog) == DynamicDialogResult.Primary;
		}

		public static async Task<DynamicDialogResult> ShowDialogAsync(DynamicDialog dialog)
		{
			try
			{
				if (MainWindow.Instance.Content is Frame rootFrame)
				{
					await dialog.ShowAsync();
					return dialog.DynamicResult;
				}
			}
			catch (Exception)
			{
			}

			return DynamicDialogResult.Cancel;
		}
	}
}