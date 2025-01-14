// Copyright (c) Files Community
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace Files.App.Utils.Serialization
{
	/// <summary>
	/// A base class to easily manage all application's settings.
	/// </summary>
	internal abstract class BaseJsonSettings : ISettingsSharingContext
	{
		private ISettingsSharingContext? _settingsSharingContext;

		public bool IsAvailable { get; protected set; }

		private ISettingsSerializer? _SettingsSerializer;
		protected ISettingsSerializer? SettingsSerializer
		{
			get => _settingsSharingContext?.Instance?.SettingsSerializer ?? _SettingsSerializer;
			set => _SettingsSerializer = value;
		}

		private IJsonSettingsSerializer? _JsonSettingsSerializer;
		protected IJsonSettingsSerializer? JsonSettingsSerializer
		{
			get => _settingsSharingContext?.Instance?.JsonSettingsSerializer ?? _JsonSettingsSerializer;
			set => _JsonSettingsSerializer = value;
		}

		private IJsonSettingsDatabase? _JsonSettingsDatabase;
		protected IJsonSettingsDatabase? JsonSettingsDatabase
		{
			get => _settingsSharingContext?.Instance?.JsonSettingsDatabase ?? _JsonSettingsDatabase;
			set => _JsonSettingsDatabase = value;
		}

		BaseJsonSettings ISettingsSharingContext.Instance => this;

		public event EventHandler<SettingChangedEventArgs>? OnSettingChangedEvent;

		public virtual bool FlushSettings()
		{
			return JsonSettingsDatabase?.FlushSettings() ?? false;
		}

		public virtual object ExportSettings()
		{
			return JsonSettingsDatabase?.ExportSettings() ?? false;
		}

		public virtual bool ImportSettings(object import)
		{
			return JsonSettingsDatabase?.ImportSettings(import) ?? false;
		}

		public bool RegisterSettingsContext(ISettingsSharingContext settingsSharingContext)
		{
			if (_settingsSharingContext is null)
			{
				// Can register only once
				_settingsSharingContext = settingsSharingContext;
				IsAvailable = settingsSharingContext.Instance.IsAvailable;
				return true;
			}

			return false;
		}

		public ISettingsSharingContext GetSharingContext()
		{
			return _settingsSharingContext ?? this;
		}

		protected virtual void Initialize(string filePath)
		{
			IsAvailable = SettingsSerializer?.CreateFile(filePath) ?? false;
		}

		protected virtual TValue? Get<TValue>(TValue? defaultValue, [CallerMemberName] string propertyName = "")
		{
			if (string.IsNullOrEmpty(propertyName))
			{
				return defaultValue;
			}

			return JsonSettingsDatabase is null ? defaultValue : JsonSettingsDatabase.GetValue(propertyName, defaultValue) ?? defaultValue;
		}

		protected virtual bool Set<TValue>(TValue? value, [CallerMemberName] string propertyName = "")
		{
			if (string.IsNullOrEmpty(propertyName))
			{
				return false;
			}

			if (JsonSettingsDatabase is not null &&
				(!JsonSettingsDatabase.GetValue<TValue>(propertyName)?.Equals(value) ?? true) &&
				JsonSettingsDatabase.SetValue(propertyName, value))
			{
				RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(propertyName, value));
				return true;
			}

			return false;
		}

		protected virtual void RaiseOnSettingChangedEvent(object sender, SettingChangedEventArgs e)
		{
			OnSettingChangedEvent?.Invoke(sender, e);
			_settingsSharingContext?.Instance.RaiseOnSettingChangedEvent(sender, e);
		}
	}
}
