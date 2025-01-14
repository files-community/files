// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Data.Commands
{
	public readonly struct RichGlyph
	{
		public static RichGlyph None { get; } = new(string.Empty);

		public bool IsNone { get; }

		public string BaseGlyph { get; }
		public string FontFamily { get; }
		public string ThemedIconStyle { get; }

		public RichGlyph(string baseGlyph = "", string fontFamily = "", string themedIconStyle = "")
		{
			BaseGlyph = baseGlyph;
			FontFamily = fontFamily;
			ThemedIconStyle = themedIconStyle;

			IsNone = string.IsNullOrEmpty(baseGlyph) && string.IsNullOrEmpty(fontFamily) && string.IsNullOrEmpty(themedIconStyle);
		}

		public void Deconstruct(out string baseGlyph, out string fontFamily, out string themedIconStyle)
		{
			baseGlyph = BaseGlyph;
			fontFamily = FontFamily;
			themedIconStyle = ThemedIconStyle;
		}

		public object? ToIcon()
		{
			return (object?)ToThemedIcon() ?? ToFontIcon();
		}

		public FontIcon? ToFontIcon()
		{
			if (IsNone)
				return null;

			var fontIcon = new FontIcon
			{
				Glyph = BaseGlyph
			};

			if (!string.IsNullOrEmpty(FontFamily))
				fontIcon.FontFamily = (FontFamily)Application.Current.Resources[FontFamily];

			return fontIcon;
		}

		public ThemedIcon? ToThemedIcon()
		{
			if (string.IsNullOrEmpty(ThemedIconStyle))
				return null;

			return new()
			{
				Style = (Style)Application.Current.Resources[ThemedIconStyle]
			};
		}

		public Style? ToThemedIconStyle()
		{
			if (string.IsNullOrEmpty(ThemedIconStyle))
				return null;
			return (Style)Application.Current.Resources[ThemedIconStyle];
		}
	}
}
