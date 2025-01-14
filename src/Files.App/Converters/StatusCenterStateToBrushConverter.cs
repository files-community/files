// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Converters
{
	public sealed class StatusCenterStateToBrushConverter : DependencyObject, IValueConverter
	{
		public static readonly DependencyProperty InProgressBackgroundBrushProperty =
			DependencyProperty.Register(nameof(InProgressBackgroundBrush), typeof(SolidColorBrush), typeof(StatusCenterStateToBrushConverter), new PropertyMetadata(null));

		public static readonly DependencyProperty InProgressForegroundBrushProperty =
			DependencyProperty.Register(nameof(InProgressForegroundBrush), typeof(SolidColorBrush), typeof(StatusCenterStateToBrushConverter), new PropertyMetadata(null));

		public static readonly DependencyProperty SuccessfulBackgroundBrushProperty =
			DependencyProperty.Register(nameof(SuccessfulBackgroundBrush), typeof(SolidColorBrush), typeof(StatusCenterStateToBrushConverter), new PropertyMetadata(null));

		public static readonly DependencyProperty SuccessfulForegroundBrushProperty =
			DependencyProperty.Register(nameof(SuccessfulForegroundBrush), typeof(SolidColorBrush), typeof(StatusCenterStateToBrushConverter), new PropertyMetadata(null));

		public static readonly DependencyProperty ErrorBackgroundBrushProperty =
			DependencyProperty.Register(nameof(ErrorBackgroundBrush), typeof(SolidColorBrush), typeof(StatusCenterStateToBrushConverter), new PropertyMetadata(null));

		public static readonly DependencyProperty ErrorForegroundBrushProperty =
			DependencyProperty.Register(nameof(ErrorForegroundBrush), typeof(SolidColorBrush), typeof(StatusCenterStateToBrushConverter), new PropertyMetadata(null));

		public static readonly DependencyProperty CanceledBackgroundBrushProperty =
			DependencyProperty.Register(nameof(CanceledBackgroundBrush), typeof(SolidColorBrush), typeof(StatusCenterStateToBrushConverter), new PropertyMetadata(null));

		public static readonly DependencyProperty CanceledForegroundBrushProperty =
			DependencyProperty.Register(nameof(CanceledForegroundBrush), typeof(SolidColorBrush), typeof(StatusCenterStateToBrushConverter), new PropertyMetadata(null));

		public SolidColorBrush InProgressBackgroundBrush
		{
			get => (SolidColorBrush)GetValue(InProgressBackgroundBrushProperty);
			set => SetValue(InProgressBackgroundBrushProperty, value);
		}

		public SolidColorBrush InProgressForegroundBrush
		{
			get => (SolidColorBrush)GetValue(InProgressForegroundBrushProperty);
			set => SetValue(InProgressForegroundBrushProperty, value);
		}

		public SolidColorBrush SuccessfulBackgroundBrush
		{
			get => (SolidColorBrush)GetValue(SuccessfulBackgroundBrushProperty);
			set => SetValue(SuccessfulBackgroundBrushProperty, value);
		}

		public SolidColorBrush SuccessfulForegroundBrush
		{
			get => (SolidColorBrush)GetValue(SuccessfulForegroundBrushProperty);
			set => SetValue(SuccessfulForegroundBrushProperty, value);
		}

		public SolidColorBrush ErrorBackgroundBrush
		{
			get => (SolidColorBrush)GetValue(ErrorBackgroundBrushProperty);
			set => SetValue(ErrorBackgroundBrushProperty, value);
		}

		public SolidColorBrush ErrorForegroundBrush
		{
			get => (SolidColorBrush)GetValue(ErrorForegroundBrushProperty);
			set => SetValue(ErrorForegroundBrushProperty, value);
		}

		public SolidColorBrush CanceledBackgroundBrush
		{
			get => (SolidColorBrush)GetValue(CanceledBackgroundBrushProperty);
			set => SetValue(CanceledBackgroundBrushProperty, value);
		}

		public SolidColorBrush CanceledForegroundBrush
		{
			get => (SolidColorBrush)GetValue(CanceledForegroundBrushProperty);
			set => SetValue(CanceledForegroundBrushProperty, value);
		}

		public object? Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is StatusCenterItemKind state)
			{	
				if (bool.TryParse(parameter?.ToString(), out var isBackground) && isBackground)
				{
					return state switch
					{
						StatusCenterItemKind.InProgress => InProgressBackgroundBrush,
						StatusCenterItemKind.Successful => SuccessfulBackgroundBrush,
						StatusCenterItemKind.Error => ErrorBackgroundBrush,
						StatusCenterItemKind.Canceled => CanceledBackgroundBrush,
						_ => CanceledBackgroundBrush
					};
				}
				else
				{
					return state switch
					{
						StatusCenterItemKind.InProgress => InProgressForegroundBrush,
						StatusCenterItemKind.Successful => SuccessfulForegroundBrush,
						StatusCenterItemKind.Error => ErrorForegroundBrush,
						StatusCenterItemKind.Canceled => CanceledForegroundBrush,
						_ => CanceledForegroundBrush
					};
				}
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
