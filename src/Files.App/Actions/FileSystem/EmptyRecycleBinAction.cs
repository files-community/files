// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Windows.Foundation.Metadata;

namespace Files.App.Actions
{
	internal sealed class EmptyRecycleBinAction : BaseUIAction, IAction
	{
		private readonly IStorageTrashBinService StorageTrashBinService = Ioc.Default.GetRequiredService<IStorageTrashBinService>();
		private readonly StatusCenterViewModel StatusCenterViewModel = Ioc.Default.GetRequiredService<StatusCenterViewModel>();
		private readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IContentPageContext context;

		public string Label
			=> "EmptyRecycleBin".GetLocalizedResource();

		public string Description
			=> "EmptyRecycleBinDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.Delete");

		public override bool IsExecutable =>
			UIHelpers.CanShowDialog &&
			((context.PageType == ContentPageTypes.RecycleBin && context.HasItem) ||
			StorageTrashBinService.HasItems());

		public EmptyRecycleBinAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync(object? parameter = null)
		{
			// TODO: Use AppDialogService
			var confirmationDialog = new ContentDialog()
			{
				Title = "ConfirmEmptyBinDialogTitle".GetLocalizedResource(),
				Content = "ConfirmEmptyBinDialogContent".GetLocalizedResource(),
				PrimaryButtonText = "Yes".GetLocalizedResource(),
				SecondaryButtonText = "Cancel".GetLocalizedResource(),
				DefaultButton = ContentDialogButton.Primary
			};

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				confirmationDialog.XamlRoot = MainWindow.Instance.Content.XamlRoot;

			if (UserSettingsService.FoldersSettingsService.DeleteConfirmationPolicy is DeleteConfirmationPolicies.Never ||
				await confirmationDialog.TryShowAsync() is ContentDialogResult.Primary)
			{
				var banner = StatusCenterHelper.AddCard_EmptyRecycleBin(ReturnResult.InProgress);

				bool result = await Task.Run(StorageTrashBinService.EmptyTrashBin);

				StatusCenterViewModel.RemoveItem(banner);

				// Post a status based on the result
				StatusCenterHelper.AddCard_EmptyRecycleBin(result ? ReturnResult.Success : ReturnResult.Failed);
			}
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
				case nameof(IContentPageContext.HasItem):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
