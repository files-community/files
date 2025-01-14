// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Files.App.UserControls.Settings
{
	[ContentProperty(Name = nameof(SettingsActionableElement))]
	public sealed partial class SettingsDisplayControl : UserControl
	{
		public FrameworkElement SettingsActionableElement { get; set; }

		public static readonly DependencyProperty AdditionalDescriptionContentProperty = DependencyProperty.Register(
		  "AdditionalDescriptionContent",
		  typeof(FrameworkElement),
		  typeof(SettingsDisplayControl),
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
		  typeof(SettingsDisplayControl),
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
		  typeof(SettingsDisplayControl),
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
		  typeof(SettingsDisplayControl),
		  new PropertyMetadata(null)
		);

		public IconElement Icon
		{
			get => (IconElement)GetValue(IconProperty);
			set => SetValue(IconProperty, value);
		}

		public SettingsDisplayControl()
		{
			InitializeComponent();
			VisualStateManager.GoToState(this, "NormalState", false);
		}

		private void MainPanel_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (e.NewSize.Width == e.PreviousSize.Width || ActionableElement is null)
				return;

			var stateToGoName = (ActionableElement.ActualWidth > e.NewSize.Width / 3) ? "CompactState" : "NormalState";
			VisualStateManager.GoToState(this, stateToGoName, false);
		}
	}
}
