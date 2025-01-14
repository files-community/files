// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Markup;
using Windows.ApplicationModel.Resources;

namespace Files.App.Helpers
{
	[MarkupExtensionReturnType(ReturnType = typeof(string))]
	public sealed class ResourceString : MarkupExtension
	{
		private static readonly ResourceLoader resourceLoader = new();

		public string Name { get; set; } = string.Empty;

		protected override object ProvideValue() => resourceLoader.GetString(Name);
	}
}
