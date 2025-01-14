// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Services;

namespace Files.App.Extensions
{
	/// <summary>
	/// Provides static extension for localization.
	/// </summary>
	public static class LocalizationExtensions
	{
		private static ILocalizationService? FallbackLocalizationService;

		public static string ToLocalized(this string resourceKey, ILocalizationService? localizationService = null)
		{
			if (localizationService is null)
			{
				FallbackLocalizationService ??= Ioc.Default.GetService<ILocalizationService>();

				return FallbackLocalizationService?.LocalizeFromResourceKey(resourceKey) ?? string.Empty;
			}

			return localizationService.LocalizeFromResourceKey(resourceKey);
		}
	}
}
