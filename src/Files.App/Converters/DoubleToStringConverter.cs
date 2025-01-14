// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal sealed class DoubleToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is not null)
			{
				return value.ToString();
			}

			return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			try
			{
				return Double.Parse(value as string);
			}
			catch (FormatException)
			{
				return null;
			}
		}
	}
}
