// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Data.Models
{
	public class AppModel : ObservableObject
	{
		public AppModel()
		{
			Clipboard.ContentChanged += Clipboard_ContentChanged;
		}

		// TODO: Refactor this method
		public void Clipboard_ContentChanged(object sender, object e)
		{
			try
			{
				DataPackageView packageView = Clipboard.GetContent();
				IsPasteEnabled = packageView.Contains(StandardDataFormats.StorageItems) || packageView.Contains(StandardDataFormats.Bitmap);
			}
			catch
			{
				IsPasteEnabled = false;
			}
		}

		private int tabStripSelectedIndex = 0;
		public int TabStripSelectedIndex
		{
			get => tabStripSelectedIndex;
			set
			{
				SetProperty(ref tabStripSelectedIndex, value);

				if (value >= 0 && value < MainPageViewModel.AppInstances.Count)
				{
					Frame rootFrame = (Frame)MainWindow.Instance.Content;
					var mainView = (MainPage)rootFrame.Content;
					mainView.ViewModel.SelectedTabItem = MainPageViewModel.AppInstances[value];
				}
			}
		}

		private bool isAppElevated = false;
		public bool IsAppElevated
		{
			get => isAppElevated;
			set => SetProperty(ref isAppElevated, value);
		}

		private bool isPasteEnabled = false;
		public bool IsPasteEnabled
		{
			get => isPasteEnabled;
			set => SetProperty(ref isPasteEnabled, value);
		}

		private volatile int isMainWindowClosed = 0;
		public bool IsMainWindowClosed
		{
			get => isMainWindowClosed == 1;
			set
			{
				int orig = Interlocked.Exchange(ref isMainWindowClosed, value ? 1 : 0);
				if (isMainWindowClosed != orig)
					OnPropertyChanged();
			}
		}

		private int propertiesWindowCount = 0;
		public int PropertiesWindowCount
		{
			get => propertiesWindowCount;
		}

		public int IncrementPropertiesWindowCount()
		{
			var result = Interlocked.Increment(ref propertiesWindowCount);
			OnPropertyChanged(nameof(PropertiesWindowCount));
			return result;
		}

		public int DecrementPropertiesWindowCount()
		{
			var result = Interlocked.Decrement(ref propertiesWindowCount);
			OnPropertyChanged(nameof(PropertiesWindowCount));
			return result;
		}

		private bool forceProcessTermination = false;
		public bool ForceProcessTermination
		{
			get => forceProcessTermination;
			set => SetProperty(ref forceProcessTermination, value);
		}

		private string googleDrivePath = string.Empty;
		/// <summary>
		/// Gets or sets a value indicating the path for Google Drive.
		/// </summary>
		public string GoogleDrivePath
		{
			get => googleDrivePath;
			set => SetProperty(ref googleDrivePath, value);
		}

		private string pCloudDrivePath = string.Empty;
		/// <summary>
		/// Gets or sets a value indicating the path for pCloud Drive.
		/// </summary>
		public string PCloudDrivePath
		{
			get => pCloudDrivePath;
			set => SetProperty(ref pCloudDrivePath, value);
		}

		/// <summary>
		/// Gets or sets a value indicating the AppWindow DPI.
		/// </summary>
		public float AppWindowDpi
		{
			get => InteropHelpers.GetDpiForWindow(MainWindow.Instance.WindowHandle) / 96f;
		}
	}
}
