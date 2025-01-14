// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Windows.Input;
using Windows.System;
using FocusManager = Microsoft.UI.Xaml.Input.FocusManager;

namespace Files.App.UserControls
{
	[DependencyProperty<bool>("IsSidebarPaneOpenToggleButtonVisible")]
	[DependencyProperty<bool>("ShowOngoingTasks")]
	[DependencyProperty<bool>("ShowSettingsButton")]
	[DependencyProperty<bool>("ShowSearchBox")]
	[DependencyProperty<AddressToolbarViewModel>("ViewModel")]
	public sealed partial class AddressToolbar : UserControl
	{
		// Dependency properties

		private readonly IUserSettingsService userSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly MainPageViewModel MainPageViewModel = Ioc.Default.GetRequiredService<MainPageViewModel>();
		public ICommandManager Commands = Ioc.Default.GetRequiredService<ICommandManager>();
		public StatusCenterViewModel? OngoingTasksViewModel { get; set; }

		// Commands

		private readonly ICommand historyItemClickedCommand;

		// Constructor

		public AddressToolbar()
		{
			InitializeComponent();

			historyItemClickedCommand = new RelayCommand<ToolbarHistoryItemModel?>(HistoryItemClicked);
		}

		private void NavToolbar_Loading(FrameworkElement _, object e)
		{
			Loading -= NavToolbar_Loading;
			if (OngoingTasksViewModel is not null)
				OngoingTasksViewModel.NewItemAdded += OngoingTasksActions_ProgressBannerPosted;
		}

		private void VisiblePath_Loaded(object _, RoutedEventArgs e)
		{
			// AutoSuggestBox won't receive focus unless it's fully loaded
			VisiblePath.Focus(FocusState.Programmatic);

			if (DependencyObjectHelpers.FindChild<TextBox>(VisiblePath) is TextBox textBox)
			{
				if (textBox.Text.StartsWith(">"))
					textBox.Select(1, textBox.Text.Length - 1);
				else
					textBox.SelectAll();
			}
		}

		private void ManualPathEntryItem_Click(object _, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType is PointerDeviceType.Mouse)
			{
				var ptrPt = e.GetCurrentPoint(NavToolbar);
				if (ptrPt.Properties.IsMiddleButtonPressed)
					return;
			}
			ViewModel.IsEditModeEnabled = true;
		}

		private async void VisiblePath_KeyDown(object _, KeyRoutedEventArgs e)
		{
			if (e.Key is VirtualKey.Escape)
				ViewModel.IsEditModeEnabled = false;

			if (e.Key is VirtualKey.Tab)
			{
				ViewModel.IsEditModeEnabled = false;
				// Delay to ensure clickable path is ready to be focused
				await Task.Delay(10);
				ClickablePath.Focus(FocusState.Keyboard);
			}
		}
		private void VisiblePath_LostFocus(object _, RoutedEventArgs e)
		{
			if (App.AppModel.IsMainWindowClosed)
				return;

			var element = FocusManager.GetFocusedElement(MainWindow.Instance.Content.XamlRoot);
			if (element is FlyoutBase or AppBarButton or Popup)
				return;

			var control = element as Control;
			if (control is null)
			{
				if (ViewModel.IsEditModeEnabled)
					ViewModel.IsEditModeEnabled = false;
				return;
			}

			if (control.FocusState is not FocusState.Programmatic and not FocusState.Keyboard)
				ViewModel.IsEditModeEnabled = false;
			else if (ViewModel.IsEditModeEnabled)
				VisiblePath.Focus(FocusState.Programmatic);
		}

		private void SearchRegion_OnGotFocus(object sender, RoutedEventArgs e) => ViewModel.SearchRegion_GotFocus(sender, e);
		private void SearchRegion_LostFocus(object sender, RoutedEventArgs e) => ViewModel.SearchRegion_LostFocus(sender, e);
		private void SearchRegion_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			// Suppress access key invocation if any dialog is open
			if (VisualTreeHelper.GetOpenPopupsForXamlRoot(MainWindow.Instance.Content.XamlRoot).Any())
				args.Handled = true;
			else
				sender.Focus(FocusState.Keyboard);
		}

		private void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
			=> ViewModel.VisiblePath_QuerySubmitted(sender, args);

		private void OngoingTasksActions_ProgressBannerPosted(object? _, StatusCenterItem e)
		{
			if (OngoingTasksViewModel is not null)
				OngoingTasksViewModel.NewItemAdded -= OngoingTasksActions_ProgressBannerPosted;

			// Displays a teaching tip the first time a banner is posted
			if (userSettingsService.AppSettingsService.ShowStatusCenterTeachingTip)
			{
				StatusCenterTeachingTip.IsOpen = true;
				userSettingsService.AppSettingsService.ShowStatusCenterTeachingTip = false;
			}
		}

		private void Button_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			// Suppress access key invocation if any dialog is open
			if (VisualTreeHelper.GetOpenPopupsForXamlRoot(MainWindow.Instance.Content.XamlRoot).Any())
				args.Handled = true;
		}

		private async void BackHistoryFlyout_Opening(object? sender, object e)
		{
			var shellPage = Ioc.Default.GetRequiredService<IContentPageContext>().ShellPage;
			if (shellPage is null)
				return;

			await AddHistoryItemsAsync(shellPage.BackwardStack, BackHistoryFlyout.Items, true);
		}

		private async void ForwardHistoryFlyout_Opening(object? sender, object e)
		{
			var shellPage = Ioc.Default.GetRequiredService<IContentPageContext>().ShellPage;
			if (shellPage is null)
				return;

			await AddHistoryItemsAsync(shellPage.ForwardStack, ForwardHistoryFlyout.Items, false);
		}

		private async Task AddHistoryItemsAsync(IEnumerable<PageStackEntry> items, IList<MenuFlyoutItemBase> flyoutItems, bool isBackMode)
		{
			// This may not seem performant, however it's the most viable trade-off to make.
			// Instead of constantly keeping track of back/forward stack and performing lookups
			// (which may degrade performance), we only add items in bulk when it's needed.
			// There's also a high chance the user might not use the feature at all in which case
			// the former approach would just waste extra performance gain

			flyoutItems.Clear();
			foreach (var item in items.Reverse())
			{
				if (item.Parameter is not NavigationArguments args || args.NavPathParam is null)
					continue;

				var fileName = SystemIO.Path.GetFileName(args.NavPathParam);

				// The fileName is empty if the path is (root) drive path
				if (string.IsNullOrEmpty(fileName))
					fileName = args.NavPathParam;

				var flyoutItem = new MenuFlyoutItem
				{
					Icon = new FontIcon { Glyph = "\uE8B7" }, // Use font icon as placeholder
					Text = fileName,
					Command = historyItemClickedCommand,
					CommandParameter = new ToolbarHistoryItemModel(item, isBackMode)
				};

				flyoutItems?.Add(flyoutItem);

				// Start loading the thumbnail in the background
				_ = LoadFlyoutItemIconAsync(flyoutItem, args.NavPathParam);
			}
		}

		private async Task LoadFlyoutItemIconAsync(MenuFlyoutItem flyoutItem, string path)
		{
			var imageSource = await NavigationHelpers.GetIconForPathAsync(path);

			if (imageSource is not null)
				flyoutItem.Icon = new ImageIcon { Source = imageSource };
		}

		private void HistoryItemClicked(ToolbarHistoryItemModel? itemModel)
		{
			if (itemModel is null)
				return;

			var shellPage = Ioc.Default.GetRequiredService<IContentPageContext>().ShellPage;
			if (shellPage is null)
				return;

			if (itemModel.IsBackMode)
			{
				// Remove all entries after the target entry in the BackwardStack
				while (shellPage.BackwardStack.Last() != itemModel.PageStackEntry)
				{
					shellPage.BackwardStack.RemoveAt(shellPage.BackwardStack.Count - 1);
				}

				// Navigate back
				shellPage.Back_Click();
			}
			else
			{
				// Remove all entries before the target entry in the ForwardStack
				while (shellPage.ForwardStack.First() != itemModel.PageStackEntry)
				{
					shellPage.ForwardStack.RemoveAt(0);
				}

				// Navigate forward
				shellPage.Forward_Click();
			}
		}

		private void ClickablePath_GettingFocus(UIElement sender, GettingFocusEventArgs args)
		{
			if (args.InputDevice != FocusInputDeviceKind.Keyboard)
				return;

			var previousControl = args.OldFocusedElement as FrameworkElement;
			if (previousControl?.Name == nameof(HomeButton) || previousControl?.Name == nameof(Refresh))
				ViewModel.IsEditModeEnabled = true;
		}
	}
}
