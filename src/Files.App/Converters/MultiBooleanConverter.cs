// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Converters
{
	internal sealed class MultiBooleanConverter
	{
		public static Boolean OrConvert(bool a, bool b)
			=> (a || b);

		public static Boolean AndConvert(bool a, bool b)
			=> (a && b);

		public static Boolean AndNotConvert(bool a, bool b)
			=> (a && !b);

		public static Boolean OrAndConvert(bool a, bool b, bool c)
			=> (a || b) && c;

		public static Visibility OrConvertToVisibility(bool a, bool b)
			=> (a || b) ? Visibility.Visible : Visibility.Collapsed;

		public static Visibility AndConvertToVisibility(bool a, bool b)
			=> (a && b) ? Visibility.Visible : Visibility.Collapsed;

		public static Visibility NorConvertToVisibility(bool a, bool b)
			=> !(a || b) ? Visibility.Visible : Visibility.Collapsed;

		public static Visibility OrNotConvertToVisibility(bool a, bool b)
			=> OrConvertToVisibility(a, !b);
	}
}
