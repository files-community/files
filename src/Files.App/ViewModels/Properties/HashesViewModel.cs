﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Files.App.Filesystem;
using Files.Backend.Models;
using Files.Shared.Helpers;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using System;
using System.Threading;

namespace Files.App.ViewModels.Properties
{
	public class HashesViewModel : ObservableObject, IDisposable
	{
		public HashesViewModel(ListedItem item)
		{
			Item = item;

			Hashes = new();

			CanAccessFile = true;

			LoadFileContent = new(ExecuteLoadFileContent);

			CancellationTokenSource = new CancellationTokenSource();

			if (LoadFileContent.CanExecute(CancellationTokenSource.Token))
				LoadFileContent.Execute(CancellationTokenSource.Token);
		}

		public ListedItem Item { get; }

		private bool _canAccessFile;
		public bool CanAccessFile
		{
			get => _canAccessFile;
			set => SetProperty(ref _canAccessFile, value);
		}

		private HashInfoItem _selectedItem;
		public HashInfoItem SelectedItem
		{
			get => _selectedItem;
			set => SetProperty(ref _selectedItem, value);
		}

		public ObservableCollection<HashInfoItem> Hashes { get; set; }

		private byte[] _fileData;

		public AsyncRelayCommand LoadFileContent { get; set; }

		public CancellationTokenSource CancellationTokenSource { get; set; }

		private bool _isLoading;
		public bool IsLoading
		{
			get => _isLoading;
			set => SetProperty(ref _isLoading, value);
		}

		private void GetHashes()
		{
			Hashes.Add(new()
			{
				Algorithm = "MD5",
				HashValue = ChecksumHelpers.CreateMD5(_fileData),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA1",
				HashValue = ChecksumHelpers.CreateSHA1(_fileData),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA256",
				HashValue = ChecksumHelpers.CreateSHA256(_fileData),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA384",
				HashValue = ChecksumHelpers.CreateSHA384(_fileData),
			});
			Hashes.Add(new()
			{
				Algorithm = "SHA512",
				HashValue = ChecksumHelpers.CreateSHA512(_fileData),
			});
		}

		private async Task ExecuteLoadFileContent(CancellationToken cancellationToken)
		{
			try
			{
				IsLoading = true;

				await App.Window.DispatcherQueue.EnqueueAsync(async () =>
				{
					_fileData = await File.ReadAllBytesAsync(Item.ItemPath);
				});

				CanAccessFile = true;
				GetHashes();
			}
			catch (OperationCanceledException)
			{
				CanAccessFile = false;
			}
			catch (Exception)
			{
				CanAccessFile = false;
			}
			finally
			{
				IsLoading = false;
			}
		}

		public void Dispose()
		{
			CancellationTokenSource.Cancel();
		}
	}
}
