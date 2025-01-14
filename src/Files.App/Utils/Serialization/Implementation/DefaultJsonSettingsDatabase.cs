// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text.Json;

namespace Files.App.Utils.Serialization.Implementation
{
	internal class DefaultJsonSettingsDatabase : IJsonSettingsDatabase
	{
		protected ISettingsSerializer SettingsSerializer { get; }

		protected IJsonSettingsSerializer JsonSettingsSerializer { get; }

		public DefaultJsonSettingsDatabase(ISettingsSerializer settingsSerializer, IJsonSettingsSerializer jsonSettingsSerializer)
		{
			SettingsSerializer = settingsSerializer;
			JsonSettingsSerializer = jsonSettingsSerializer;
		}

		protected IDictionary<string, object?> GetFreshSettings()
		{
			string data = SettingsSerializer.ReadFromFile();

			if (string.IsNullOrWhiteSpace(data))
			{
				data = "null";
			}

			try
			{
				return JsonSettingsSerializer.DeserializeFromJson<ConcurrentDictionary<string, object?>?>(data) ?? new();
			}
			catch (Exception)
			{
				// Occurs if the settings file has invalid json
				// TODO Display prompt to notify user #710
				return JsonSettingsSerializer.DeserializeFromJson<ConcurrentDictionary<string, object?>?>("null") ?? new();
			}
		}

		protected bool SaveSettings(IDictionary<string, object?> data)
		{
			var jsonData = JsonSettingsSerializer.SerializeToJson(data);

			return SettingsSerializer.WriteToFile(jsonData);
		}

		public virtual TValue? GetValue<TValue>(string key, TValue? defaultValue = default)
		{
			var data = GetFreshSettings();

			if (data.TryGetValue(key, out var objVal))
			{
				return GetValueFromObject<TValue>(objVal) ?? defaultValue;
			}
			else
			{
				SetValue(key, defaultValue);
				return defaultValue;
			}
		}

		public virtual bool SetValue<TValue>(string key, TValue? newValue)
		{
			var data = GetFreshSettings();

			if (!data.TryAdd(key, newValue))
				data[key] = newValue;

			return SaveSettings(data);
		}

		public virtual bool RemoveKey(string key)
		{
			var data = GetFreshSettings();

			return data.Remove(key) && SaveSettings(data);
		}

		public bool FlushSettings()
		{
			// The settings are always flushed automatically, return true.
			return true;
		}

		public virtual bool ImportSettings(object? import)
		{
			try
			{
				// Try convert
				var data = (IDictionary<string, object?>?)import;
				if (data is null)
				{
					return false;
				}

				// Serialize
				var serialized = JsonSettingsSerializer.SerializeToJson(data);

				// Write to file
				return SettingsSerializer.WriteToFile(serialized);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				Debugger.Break();

				return false;
			}
		}

		public object? ExportSettings()
		{
			return GetFreshSettings();
		}

		protected static TValue? GetValueFromObject<TValue>(object? obj)
		{
			if (obj is JsonElement jElem)
			{
				return jElem.Deserialize<TValue>();
			}

			return (TValue?)obj;
		}
	}
}
