// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using System;
using System.Windows.Input;

namespace Files.App.UserControls.Settings
{
	[ContentProperty(Name = nameof(SettingsActionableElement))]
	public sealed partial class SettingsBlockControl : UserControl
	{
		public FrameworkElement SettingsActionableElement { get; set; }

		public ICommand ButtonCommand
		{
			get { return (ICommand)GetValue(ButtonCommandProperty); }
			set { SetValue(ButtonCommandProperty, value); }
		}
		public static readonly DependencyProperty ButtonCommandProperty =
			DependencyProperty.Register("ButtonCommand", typeof(ICommand), typeof(SettingsBlockControl), new PropertyMetadata(null));

		public static readonly DependencyProperty ExpandableContentProperty = DependencyProperty.Register(
		  "ExpandableContent",
		  typeof(FrameworkElement),
		  typeof(SettingsBlockControl),
		  new PropertyMetadata(null)
		);

		public FrameworkElement ExpandableContent
		{
			get => (FrameworkElement)GetValue(ExpandableContentProperty);
			set => SetValue(ExpandableContentProperty, value);
		}

		public static readonly DependencyProperty AdditionalDescriptionContentProperty = DependencyProperty.Register(
		  "AdditionalDescriptionContent",
		  typeof(FrameworkElement),
		  typeof(SettingsBlockControl),
		  new PropertyMetadata(null)
		);

		public FrameworkElement AdditionalDescriptionContent
		{
			get => (FrameworkElement)GetValue(AdditionalDescriptionContentProperty);
			set => SetValue(AdditionalDescriptionContentProperty, value);
		}

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
		  "Title",
		  typeof(string),
		  typeof(SettingsBlockControl),
		  new PropertyMetadata(null)
		);

		public string Title
		{
			get => (string)GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}

		public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
		  "Description",
		  typeof(string),
		  typeof(SettingsBlockControl),
		  new PropertyMetadata(null)
		);

		public string Description
		{
			get => (string)GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
		  "Icon",
		  typeof(IconElement),
		  typeof(SettingsBlockControl),
		  new PropertyMetadata(null)
		);

		public IconElement Icon
		{
			get => (IconElement)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public static readonly DependencyProperty IsClickableProperty = DependencyProperty.Register(
		  "IsClickable",
		  typeof(bool),
		  typeof(SettingsBlockControl),
		  new PropertyMetadata(false)
		);

		public bool IsClickable
		{
			get => (bool)GetValue(IsClickableProperty);
			set => SetValue(IsClickableProperty, value);
		}

		public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
		  "IsExpanded",
		  typeof(bool),
		  typeof(SettingsBlockControl),
		  new PropertyMetadata(false)
		);

		public bool IsExpanded
		{
			get => (bool)GetValue(IsExpandedProperty);
			set => SetValue(IsExpandedProperty, value);
		}

		//
		// Summary:
		//     Occurs when a button control is clicked.
		public event EventHandler<bool> Click;

		public SettingsBlockControl()
		{
			InitializeComponent();
			Loaded += SettingsBlockControl_Loaded;
		}

		private void SettingsBlockControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (ActionableButton is not null)
			{
				AutomationProperties.SetName(ActionableButton, Title);
			}
		}

		private void Expander_Expanding(Microsoft.UI.Xaml.Controls.Expander sender, Microsoft.UI.Xaml.Controls.ExpanderExpandingEventArgs args)
		{
			Click?.Invoke(this, true);
		}

		private void Expander_Collapsed(Microsoft.UI.Xaml.Controls.Expander sender, Microsoft.UI.Xaml.Controls.ExpanderCollapsedEventArgs args)
		{
			Click?.Invoke(this, false);
		}
	}
}