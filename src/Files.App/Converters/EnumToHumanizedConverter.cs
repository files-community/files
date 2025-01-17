// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal sealed class EnumToHumanizedConverter : IValueConverter
	{
		public string EnumTypeName { get; set; } = string.Empty;

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var stringValue = value.ToString() ?? string.Empty;

			return EnumTypeName switch
			{
				"DetailsViewSizeKind"
					=> LocalizedEnumDescriptionFactory.Get(Enum.Parse<DetailsViewSizeKind>(stringValue)),
				"ListViewSizeKind"
					=> LocalizedEnumDescriptionFactory.Get(Enum.Parse<ListViewSizeKind>(stringValue)),
				"CardsViewSizeKind"
					=> LocalizedEnumDescriptionFactory.Get(Enum.Parse<CardsViewSizeKind>(stringValue)),
				"GridViewSizeKind"
					=> LocalizedEnumDescriptionFactory.Get(Enum.Parse<GridViewSizeKind>(stringValue)),
				"ColumnsViewSizeKind"
					=> LocalizedEnumDescriptionFactory.Get(Enum.Parse<ColumnsViewSizeKind>(stringValue)),
				_ => string.Empty,
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
