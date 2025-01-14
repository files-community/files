// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Services.SizeProvider;

namespace Files.App.Services
{
	public sealed class UserSizeProvider : ISizeProvider
	{
		private readonly IFoldersSettingsService folderPreferences
			= Ioc.Default.GetRequiredService<IFoldersSettingsService>();

		private ISizeProvider provider;

		public event EventHandler<SizeChangedEventArgs> SizeChanged;

		public UserSizeProvider()
		{
			provider = GetProvider();
			provider.SizeChanged += Provider_SizeChanged;

			folderPreferences.PropertyChanged += FolderPreferences_PropertyChanged;
		}

		public Task CleanAsync()
			=> provider.CleanAsync();

		public async Task ClearAsync()
			=> await provider.ClearAsync();

		public Task UpdateAsync(string path, CancellationToken cancellationToken)
			=> provider.UpdateAsync(path, cancellationToken);

		public bool TryGetSize(string path, out ulong size)
			=> provider.TryGetSize(path, out size);

		public void Dispose()
		{
			provider.Dispose();
			folderPreferences.PropertyChanged -= FolderPreferences_PropertyChanged;
		}

		private ISizeProvider GetProvider()
			=> folderPreferences.CalculateFolderSizes ? new DrivesSizeProvider() : new NoSizeProvider();

		private async void FolderPreferences_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IFoldersSettingsService.CalculateFolderSizes))
			{
				await provider.ClearAsync();
				provider.SizeChanged -= Provider_SizeChanged;
				provider = GetProvider();
				provider.SizeChanged += Provider_SizeChanged;
			}
		}

		private void Provider_SizeChanged(object sender, SizeChangedEventArgs e)
			=> SizeChanged?.Invoke(this, e);
	}
}
