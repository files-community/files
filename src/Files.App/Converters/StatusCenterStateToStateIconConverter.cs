// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Files.App.Converters
{
	class StatusCenterStateToStateIconConverter : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is StatusCenterItemIconKind state)
			{
				var pathMarkup = state switch
				{
					StatusCenterItemIconKind.Copy =>       Application.Current.Resources["App.Theme.PathIcon.ActionCopy"] as string,
					StatusCenterItemIconKind.Move =>       Application.Current.Resources["App.Theme.PathIcon.ActionMove"] as string,
					StatusCenterItemIconKind.Delete =>     Application.Current.Resources["App.Theme.PathIcon.ActionDelete"] as string,
					StatusCenterItemIconKind.Recycle =>    Application.Current.Resources["App.Theme.PathIcon.ActionDelete"] as string,
					StatusCenterItemIconKind.Extract =>    Application.Current.Resources["App.Theme.PathIcon.ActionExtract"] as string,
					StatusCenterItemIconKind.Compress =>   Application.Current.Resources["App.Theme.PathIcon.ActionExtract"] as string,
					StatusCenterItemIconKind.Successful => Application.Current.Resources["App.Theme.PathIcon.ActionSuccess"] as string,
					StatusCenterItemIconKind.Error =>      Application.Current.Resources["App.Theme.PathIcon.ActionInfo"] as string,
					_ => ""
				};

				string xaml = @$"<Path xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""><Path.Data>{pathMarkup}</Path.Data></Path>";

				if (XamlReader.Load(xaml) is not Path path)
					return null;

				// Initialize a new instance
				Geometry geometry = path.Data;

				// Destroy
				path.Data = null;

				return geometry;
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
