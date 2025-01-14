// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Text.Json;

namespace Files.App.Data.Parameters
{
	public sealed class TabBarItemParameter
	{
		private static readonly KnownTypesConverter _typesConverter = new();

		public Type InitialPageType { get; set; }

		public object NavigationParameter { get; set; }

		public string Serialize()
		{
			return JsonSerializer.Serialize(this, _typesConverter.Options);
		}

		public static TabBarItemParameter Deserialize(string obj)
		{
			var tabArgs = new TabBarItemParameter();

			var tempArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(obj);
			tabArgs.InitialPageType = Type.GetType(tempArgs[nameof(InitialPageType)].GetString());

			try
			{
				tabArgs.NavigationParameter = JsonSerializer.Deserialize<PaneNavigationArguments>(tempArgs[nameof(NavigationParameter)].GetRawText());
			}
			catch (JsonException)
			{
				tabArgs.NavigationParameter = tempArgs[nameof(NavigationParameter)].GetString();
			}

			return tabArgs;
		}
	}
}
