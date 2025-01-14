// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.UserControls.Sidebar
{
	public sealed partial class SidebarView
	{
		public SidebarDisplayMode DisplayMode
		{
			get { return (SidebarDisplayMode)GetValue(DisplayModeProperty); }
			set { SetValue(DisplayModeProperty, value); }
		}
		public static readonly DependencyProperty DisplayModeProperty =
			DependencyProperty.Register(nameof(DisplayMode), typeof(SidebarDisplayMode), typeof(SidebarView), new PropertyMetadata(SidebarDisplayMode.Expanded, OnPropertyChanged));

		public UIElement InnerContent
		{
			get { return (UIElement)GetValue(InnerContentProperty); }
			set { SetValue(InnerContentProperty, value); }
		}
		public static readonly DependencyProperty InnerContentProperty =
			DependencyProperty.Register(nameof(InnerContent), typeof(UIElement), typeof(SidebarView), new PropertyMetadata(null));

		public UIElement Footer
		{
			get { return (UIElement)GetValue(FooterProperty); }
			set { SetValue(FooterProperty, value); }
		}
		public static readonly DependencyProperty FooterProperty =
			DependencyProperty.Register("Footer", typeof(UIElement), typeof(SidebarView), new PropertyMetadata(null));

		public bool IsPaneOpen
		{
			get { return (bool)GetValue(IsPaneOpenProperty); }
			set { SetValue(IsPaneOpenProperty, value); }
		}
		public static readonly DependencyProperty IsPaneOpenProperty =
			DependencyProperty.Register(nameof(IsPaneOpen), typeof(bool), typeof(SidebarView), new PropertyMetadata(false, OnPropertyChanged));

		public double OpenPaneLength
		{
			get { return (double)GetValue(OpenPaneLengthProperty); }
			set
			{
				SetValue(OpenPaneLengthProperty, value);
				NegativeOpenPaneLength = -value;
			}
		}
		public static readonly DependencyProperty OpenPaneLengthProperty =
			DependencyProperty.Register(nameof(OpenPaneLength), typeof(double), typeof(SidebarView), new PropertyMetadata(240d, OnPropertyChanged));

		public double NegativeOpenPaneLength
		{
			get { return (double)GetValue(NegativeOpenPaneLengthProperty); }
			set { SetValue(NegativeOpenPaneLengthProperty, value); }
		}
		public static readonly DependencyProperty NegativeOpenPaneLengthProperty =
			DependencyProperty.Register(nameof(NegativeOpenPaneLength), typeof(double), typeof(SidebarView), new PropertyMetadata(null));

		public ISidebarViewModel ViewModel
		{
			get => (ISidebarViewModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ISidebarViewModel), typeof(SidebarView), new PropertyMetadata(null));

		public ISidebarItemModel SelectedItem
		{
			get => (ISidebarItemModel)GetValue(SelectedItemProperty);
			set
			{
				SetValue(SelectedItemProperty, value);
			}
		}
		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.Register(nameof(SelectedItem), typeof(ISidebarItemModel), typeof(SidebarView), new PropertyMetadata(null));

		public object MenuItemsSource
		{
			get => (object)GetValue(MenuItemsSourceProperty);
			set => SetValue(MenuItemsSourceProperty, value);
		}
		public static readonly DependencyProperty MenuItemsSourceProperty =
			DependencyProperty.Register(nameof(MenuItemsSource), typeof(object), typeof(SidebarView), new PropertyMetadata(null));

		public static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is not SidebarView control) return;

			if (e.Property == OpenPaneLengthProperty)
			{
				control.UpdateOpenPaneLengthColumn();
			}
			else if (e.Property == DisplayModeProperty)
			{
				control.UpdateDisplayMode();
			}
			else if (e.Property == IsPaneOpenProperty)
			{
				control.UpdateMinimalMode();
			}
		}
	}
}
