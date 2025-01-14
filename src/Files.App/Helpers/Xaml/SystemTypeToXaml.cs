// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Markup;

namespace Files.App.Helpers
{
	public sealed class SystemTypeToXaml : MarkupExtension
	{
		#region Private Members

		private object parameter;

		#endregion Private Members

		#region Public Properties

		public int Int { set => parameter = value; }

		public double Double { set => parameter = value; }

		public float Float { set => parameter = value; }

		public bool Bool { set => parameter = value; }

		#endregion Public Properties

		protected override object ProvideValue()
		{
			return parameter;
		}
	}
}