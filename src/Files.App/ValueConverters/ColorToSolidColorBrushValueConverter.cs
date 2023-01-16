﻿using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace Files.App.ValueConverters
{
	public class ColorToSolidColorBrushValueConverter : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, string language)
		{
			if (null == value)
				return null;

			if (value is Color)
			{
				Color color = (Color)value;
				return new SolidColorBrush(color);
			}

			Type type = value.GetType();
			throw new InvalidOperationException("Unsupported type [" + type.Name + "]");
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
