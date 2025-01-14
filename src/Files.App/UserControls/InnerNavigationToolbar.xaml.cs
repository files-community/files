// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;

namespace Files.App.UserControls
{
	public sealed partial class InnerNavigationToolbar : UserControl
	{
		public InnerNavigationToolbar()
		{
			InitializeComponent();
		}

		public IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();
		public ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();
		public IModifiableCommandManager ModifiableCommands { get; } = Ioc.Default.GetRequiredService<IModifiableCommandManager>();

		private readonly IAddItemService addItemService = Ioc.Default.GetRequiredService<IAddItemService>();

		public AppModel AppModel => App.AppModel;

		public AddressToolbarViewModel? ViewModel
		{
			get => (AddressToolbarViewModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		// Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(AddressToolbarViewModel), typeof(InnerNavigationToolbar), new PropertyMetadata(null));

		public bool ShowViewControlButton
		{
			get { return (bool)GetValue(ShowViewControlButtonProperty); }
			set { SetValue(ShowViewControlButtonProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ShowViewControlButton.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowViewControlButtonProperty =
			DependencyProperty.Register("ShowViewControlButton", typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));

		public bool ShowPreviewPaneButton
		{
			get { return (bool)GetValue(ShowPreviewPaneButtonProperty); }
			set { SetValue(ShowPreviewPaneButtonProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ShowPreviewPaneButton.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ShowPreviewPaneButtonProperty =
			DependencyProperty.Register("ShowPreviewPaneButton", typeof(bool), typeof(AddressToolbar), new PropertyMetadata(null));
		private void NewEmptySpace_Opening(object sender, object e)
		{
			var shell = NewEmptySpace.Items.Where(x => (x.Tag as string) == "CreateNewFile").Reverse().ToList();
			shell.ForEach(x => NewEmptySpace.Items.Remove(x));
			if (!ViewModel.InstanceViewModel.CanCreateFileInPage)
				return;

			var cachedNewContextMenuEntries = addItemService.GetEntries();
			if (cachedNewContextMenuEntries is null)
				return;

			var separatorIndex = NewEmptySpace.Items.IndexOf(NewEmptySpace.Items.Single(x => x.Name == "NewMenuFileFolderSeparator"));

			ushort key = 0;
			string keyFormat = $"D{cachedNewContextMenuEntries.Count.ToString().Length}";

			foreach (var newEntry in Enumerable.Reverse(cachedNewContextMenuEntries))
			{
				MenuFlyoutItem menuLayoutItem;
				if (!string.IsNullOrEmpty(newEntry.IconBase64))
				{
					byte[] bitmapData = Convert.FromBase64String(newEntry.IconBase64);
					using var ms = new MemoryStream(bitmapData);
					var image = new BitmapImage();
					_ = image.SetSourceAsync(ms.AsRandomAccessStream());
					menuLayoutItem = new MenuFlyoutItemWithImage()
					{
						Text = newEntry.Name,
						BitmapIcon = image,
						Tag = "CreateNewFile"
					};
				}
				else
				{
					menuLayoutItem = new MenuFlyoutItem()
					{
						Text = newEntry.Name,
						Icon = new FontIcon
						{
							Glyph = "\xE7C3"
						},
						Tag = "CreateNewFile"
					};
				}
				menuLayoutItem.AccessKey = (cachedNewContextMenuEntries.Count + 1 - (++key)).ToString(keyFormat);
				menuLayoutItem.Command = ViewModel.CreateNewFileCommand;
				menuLayoutItem.CommandParameter = newEntry;
				NewEmptySpace.Items.Insert(separatorIndex + 1, menuLayoutItem);
			}
		}

		private void SortGroup_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			if (sender is MenuFlyoutSubItem menu)
			{
				var items = menu.Items
					.TakeWhile(item => item is not MenuFlyoutSeparator)
					.Where(item => item.IsEnabled)
					.ToList();

				string format = $"D{items.Count.ToString().Length}";

				for (ushort index = 0; index < items.Count; ++index)
				{
					items[index].AccessKey = (index+1).ToString(format);
				}
			}

		}

		private void AppBarButton_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			// Suppress access key invocation if any dialog is open
			if (VisualTreeHelper.GetOpenPopupsForXamlRoot(MainWindow.Instance.Content.XamlRoot).Any())
				args.Handled = true;
		}
	}
}