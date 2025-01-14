// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Utils.Serialization;
using Files.App.Utils.Serialization.Implementation;
using Files.App.Services.Settings;
using Files.Shared.Extensions;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;

namespace Files.App.Services.Settings
{
	internal sealed class UserSettingsService : BaseJsonSettings, IUserSettingsService
	{
		private IGeneralSettingsService _GeneralSettingsService;
		public IGeneralSettingsService GeneralSettingsService
		{
			get => GetSettingsService(ref _GeneralSettingsService);
		}

		private IFoldersSettingsService _FoldersSettingsService;
		public IFoldersSettingsService FoldersSettingsService
		{
			get => GetSettingsService(ref _FoldersSettingsService);
		}

		private IAppearanceSettingsService _AppearanceSettingsService;
		public IAppearanceSettingsService AppearanceSettingsService
		{
			get => GetSettingsService(ref _AppearanceSettingsService);
		}

		private IInfoPaneSettingsService _InfoPaneSettingsService;
		public IInfoPaneSettingsService InfoPaneSettingsService
		{
			get => GetSettingsService(ref _InfoPaneSettingsService);
		}

		private ILayoutSettingsService _LayoutSettingsService;
		public ILayoutSettingsService LayoutSettingsService
		{
			get => GetSettingsService(ref _LayoutSettingsService);
		}

		private IApplicationSettingsService _ApplicationSettingsService;
		public IApplicationSettingsService ApplicationSettingsService
		{
			get => GetSettingsService(ref _ApplicationSettingsService);
		}

		private IAppSettingsService _AppSettingsService;
		public IAppSettingsService AppSettingsService
		{
			get => GetSettingsService(ref _AppSettingsService);
		}

		public UserSettingsService()
		{
			SettingsSerializer = new DefaultSettingsSerializer();

			Initialize(Path.Combine(ApplicationData.Current.LocalFolder.Path, Constants.LocalSettings.SettingsFolderName, Constants.LocalSettings.UserSettingsFileName));

			JsonSettingsSerializer = new DefaultJsonSettingsSerializer();
			JsonSettingsDatabase = new CachingJsonSettingsDatabase(SettingsSerializer, JsonSettingsSerializer);
		}

		public override object ExportSettings()
		{
			var export = (IDictionary<string, object>)base.ExportSettings();

			// Remove session settings
			export.Remove(nameof(GeneralSettingsService.LastSessionTabList));
			export.Remove(nameof(GeneralSettingsService.LastCrashedTabList));
			export.Remove(nameof(GeneralSettingsService.PathHistoryList));

			return JsonSettingsSerializer.SerializeToJson(export);
		}

		public override bool ImportSettings(object import)
		{
			Dictionary<string, object> settingsImport = import switch
			{
				string s => JsonSettingsSerializer?.DeserializeFromJson<Dictionary<string, object>>(s) ?? [],
				Dictionary<string, object> d => d,
				_ => [],
			};

			if (!settingsImport.IsEmpty() && base.ImportSettings(settingsImport))
			{
				foreach (var item in settingsImport)
				{
					RaiseOnSettingChangedEvent(this, new SettingChangedEventArgs(item.Key, item.Value));
				}

				return true;
			}

			return false;
		}

		private TSettingsService GetSettingsService<TSettingsService>(ref TSettingsService settingsServiceMember)
			where TSettingsService : class, IBaseSettingsService
		{
			settingsServiceMember ??= Ioc.Default.GetService<TSettingsService>()!;

			return settingsServiceMember;
		}
	}
}
