// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal sealed class GenericEnumConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return ConvertInternal(value, targetType, parameter, language,
				s => s.Split(',').ToDictionary(k => System.Convert.ToInt64(k.Split('-')[0]), v => System.Convert.ToInt64(v.Split('-')[1])));
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return ConvertInternal(value, targetType, parameter, language,
				s => s.Split(',').ToDictionary(k => System.Convert.ToInt64(k.Split('-')[1]), v => System.Convert.ToInt64(v.Split('-')[0])));
		}

		private object ConvertInternal(object value, Type targetType, object parameter, string language, Func<string, Dictionary<long, long>> enumConversion)
		{
			var enumValue = System.Convert.ToInt64(value);

			if (parameter is string strParam)
			{
				// enumValue-convertedValue: 0-1,1-2
				var enumConversionValues = enumConversion(strParam);

				if (enumConversionValues.TryGetValue(enumValue, out var convertedValue))
				{
					enumValue = convertedValue;
				}
				// else.. use value from the cast above
			}

			try
			{
				if (Enum.GetName(targetType, enumValue) is string enumName)
				{
					return Enum.Parse(targetType, enumName);
				}
			}
			catch { }

			try
			{
				return System.Convert.ChangeType(enumValue, targetType);
			}
			catch { }

			return enumValue;
		}
	}
}
