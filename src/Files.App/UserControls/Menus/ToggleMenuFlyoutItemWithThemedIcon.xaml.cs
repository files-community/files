// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control element template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls
{
	public sealed partial class ToggleMenuFlyoutItemWithThemedIcon: ToggleMenuFlyoutItem
	{
		public Style ThemedIconStyle
		{
			get { return (Style)GetValue(ThemedIconStyleProperty); }
			set { SetValue(ThemedIconStyleProperty, value); }
		}

		public static readonly DependencyProperty ThemedIconStyleProperty =
			DependencyProperty.Register("ThemedIconStyle", typeof(Style), typeof(ToggleMenuFlyoutItemWithThemedIcon), new PropertyMetadata(null, OnThemedIconStyleChanged));

		private static void OnThemedIconStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			(d as ToggleMenuFlyoutItem).Icon = e.NewValue is not null ? new IconSourceElement() : null;
		}

		public ToggleMenuFlyoutItemWithThemedIcon()
		{
			InitializeComponent();
		}
	}
}