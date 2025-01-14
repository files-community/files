// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Files.Shared.Extensions
{
	public static class ComponentModelExtensions
	{
		public static string GetDescription<T>(this T enumValue) where T : Enum
		{
			var description = enumValue.ToString();
			var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

			if (fieldInfo is not null)
			{
				var attrs = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
				if (attrs is not null && attrs.Length > 0)
				{
					description = ((DescriptionAttribute)attrs[0]).Description;
				}
			}

			return description;
		}

		public static T? GetValueFromDescription<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string description) where T : Enum
		{
			foreach (var field in typeof(T).GetFields())
			{
				if (Attribute.GetCustomAttribute(field,
					typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
				{
					if (attribute.Description == description)
					{
						return (T?)field.GetValue(null);
					}
				}
				else
				{
					if (field.Name == description)
					{
						return (T?)field.GetValue(null);
					}
				}
			}

			return default;
		}
	}
}
