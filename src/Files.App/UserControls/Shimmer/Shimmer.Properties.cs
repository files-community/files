// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls
{
	public partial class Shimmer : Control
	{
		/// <summary>
		/// Identifies the <see cref="Duration"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty DurationProperty =
			DependencyProperty.Register(
				nameof(Duration),
				typeof(object),
				typeof(Shimmer),
				new PropertyMetadata(defaultValue: TimeSpan.FromMilliseconds(1600), PropertyChanged));

		/// <summary>
		/// Identifies the <see cref="IsActive"/> dependency property.
		/// </summary>
		public static readonly DependencyProperty IsActiveProperty =
			DependencyProperty.Register(
				nameof(IsActive),
				typeof(bool),
				typeof(Shimmer),
				new PropertyMetadata(defaultValue: true, PropertyChanged));

		/// <summary>
		/// Gets or sets the animation duration
		/// </summary>
		public TimeSpan Duration
		{
			get => (TimeSpan)GetValue(DurationProperty);
			set => SetValue(DurationProperty, value);
		}

		/// <summary>
		/// Gets or sets if the animation is playing
		/// </summary>
		public bool IsActive
		{
			get => (bool)GetValue(IsActiveProperty);
			set => SetValue(IsActiveProperty, value);
		}

		private static void PropertyChanged(DependencyObject s, DependencyPropertyChangedEventArgs e)
		{
			var self = (Shimmer)s;
			if (self.IsActive)
			{
				self.StopAnimation();
				self.TryStartAnimation();
			}
			else
			{
				self.StopAnimation();
			}
		}
	}
}
