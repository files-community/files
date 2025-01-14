// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Files.App.UserControls
{
	public class ComboBoxEx : ComboBox
	{
		double _cachedWidth;

		protected override void OnDropDownOpened(object e)
		{
			Width = _cachedWidth;

			base.OnDropDownOpened(e);
		}

		protected override void OnDropDownClosed(object e)
		{
			Width = double.NaN;

			base.OnDropDownClosed(e);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var baseSize = base.MeasureOverride(availableSize);

			if (baseSize.Width != 64)
				_cachedWidth = baseSize.Width;

			return baseSize;
		}
	}
}
