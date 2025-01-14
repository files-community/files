// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Reflection;

namespace Files.App.Extensions
{
	public static class EnumExtensions
	{
		public static TEnum GetEnum<TEnum>(string text) where TEnum : struct
		{
			if (!typeof(TEnum).GetTypeInfo().IsEnum)
			{
				throw new InvalidOperationException("Generic parameter 'TEnum' must be an enum.");
			}

			return (TEnum)Enum.Parse(typeof(TEnum), text);
		}
	}
}
