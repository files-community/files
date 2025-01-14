// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Data;

namespace Files.App.Converters
{
	internal sealed class ConflictResolveOptionToIndexConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (FileNameConflictResolveOptionType)value switch
			{
				FileNameConflictResolveOptionType.None => -1,
				FileNameConflictResolveOptionType.GenerateNewName => 0,
				FileNameConflictResolveOptionType.ReplaceExisting => 1,
				FileNameConflictResolveOptionType.Skip => 2,
				_ => -1
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return value switch
			{
				0 => FileNameConflictResolveOptionType.GenerateNewName,
				1 => FileNameConflictResolveOptionType.ReplaceExisting,
				2 => FileNameConflictResolveOptionType.Skip,
				_ => FileNameConflictResolveOptionType.None
			};
		}
	}
}
