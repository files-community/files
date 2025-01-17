// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Shared.Helpers;
using System.IO;
using System.Windows.Input;

namespace Files.App.ViewModels.Properties
{
	public sealed class HashesViewModel : ObservableObject, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetService<IUserSettingsService>()!;

		private HashInfoItem _selectedItem;
		public HashInfoItem SelectedItem
		{
			get => _selectedItem;
			set => SetProperty(ref _selectedItem, value);
		}

		public ObservableCollection<HashInfoItem> Hashes { get; set; }

		public Dictionary<string, bool> ShowHashes { get; private set; }

		public ICommand ToggleIsEnabledCommand { get; private set; }

		private ListedItem _item;

		private CancellationTokenSource _cancellationTokenSource;

		public HashesViewModel(ListedItem item)
		{
			ToggleIsEnabledCommand = new RelayCommand<string>(ToggleIsEnabled);

			_item = item;
			_cancellationTokenSource = new();

			Hashes =
			[
				new() { Algorithm = "CRC32" },
				new() { Algorithm = "MD5" },
				new() { Algorithm = "SHA1" },
				new() { Algorithm = "SHA256" },
				new() { Algorithm = "SHA384" },
				new() { Algorithm = "SHA512" },
			];

			ShowHashes = UserSettingsService.GeneralSettingsService.ShowHashesDictionary ?? [];
			// Default settings
			ShowHashes.TryAdd("CRC32", true);
			ShowHashes.TryAdd("MD5", true);
			ShowHashes.TryAdd("SHA1", true);
			ShowHashes.TryAdd("SHA256", true);
			ShowHashes.TryAdd("SHA384", false);
			ShowHashes.TryAdd("SHA512", false);

			Hashes.Where(x => ShowHashes[x.Algorithm]).ForEach(x => ToggleIsEnabledCommand.Execute(x.Algorithm));
		}

		private void ToggleIsEnabled(string? algorithm)
		{
			var hashInfoItem = Hashes.First(x => x.Algorithm == algorithm);
			hashInfoItem.IsEnabled = !hashInfoItem.IsEnabled;

			if (ShowHashes[hashInfoItem.Algorithm] != hashInfoItem.IsEnabled)
			{
				ShowHashes[hashInfoItem.Algorithm] = hashInfoItem.IsEnabled;
				UserSettingsService.GeneralSettingsService.ShowHashesDictionary = ShowHashes;
			}

			// Don't calculate hashes for online files
			if (_item.SyncStatusUI.SyncStatus is CloudDriveSyncStatus.FileOnline or CloudDriveSyncStatus.FolderOnline)
			{
				hashInfoItem.HashValue = "CalculationOnlineFileHashError".GetLocalizedResource();
				return;
			}

			if (hashInfoItem.HashValue is null && hashInfoItem.IsEnabled)
			{
				hashInfoItem.IsCalculating = true;

				MainWindow.Instance.DispatcherQueue.EnqueueOrInvokeAsync(async () =>
				{
					try
					{
						await using (var stream = File.OpenRead(_item.ItemPath))
						{
							hashInfoItem.HashValue = hashInfoItem.Algorithm switch
							{
								"CRC32" => await ChecksumHelpers.CreateCRC32(stream, _cancellationTokenSource.Token),
								"MD5" => await ChecksumHelpers.CreateMD5(stream, _cancellationTokenSource.Token),
								"SHA1" => await ChecksumHelpers.CreateSHA1(stream, _cancellationTokenSource.Token),
								"SHA256" => await ChecksumHelpers.CreateSHA256(stream, _cancellationTokenSource.Token),
								"SHA384" => await ChecksumHelpers.CreateSHA384(stream, _cancellationTokenSource.Token),
								"SHA512" => await ChecksumHelpers.CreateSHA512(stream, _cancellationTokenSource.Token),
								_ => throw new InvalidOperationException()
							};
						}

						hashInfoItem.IsCalculated = true;
					}
					catch (OperationCanceledException)
					{
						// not an error
					}
					catch (IOException)
					{
						// File is currently open
						hashInfoItem.HashValue = "CalculationErrorFileIsOpen".GetLocalizedResource();
					}
					catch (Exception)
					{
						hashInfoItem.HashValue = "CalculationError".GetLocalizedResource();
					}
					finally
					{
						hashInfoItem.IsCalculating = false;
					}
				});
			}
		}

		public void Dispose()
		{
			_cancellationTokenSource.Cancel();
		}
	}
}
