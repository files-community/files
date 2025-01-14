// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;

namespace Files.App.Data.Models
{
	public sealed class AppModel : ObservableObject
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

		private int _TabStripSelectedIndex = 0;
		public int TabStripSelectedIndex
		{
			get => _TabStripSelectedIndex;
			set
			{
				SetProperty(ref _TabStripSelectedIndex, value);

				try
				{
					if (value >= 0 && value < MainPageViewModel.AppInstances.Count)
					{
						var rootFrame = (Frame)MainWindow.Instance.Content;
						var mainView = (MainPage)rootFrame.Content;
						mainView.ViewModel.SelectedTabItem = MainPageViewModel.AppInstances[value];
					}
				}
				catch (COMException)
				{

				}
			}
		}

		private bool _IsAppElevated = false;
		public bool IsAppElevated
		{
			get => _IsAppElevated;
			set => SetProperty(ref _IsAppElevated, value);
		}

		private bool _IsPasteEnabled = false;
		public bool IsPasteEnabled
		{
			get => _IsPasteEnabled;
			set => SetProperty(ref _IsPasteEnabled, value);
		}

		private volatile int _IsMainWindowClosed = 0;
		public bool IsMainWindowClosed
		{
			get => _IsMainWindowClosed == 1;
			set
			{
				int orig = Interlocked.Exchange(ref _IsMainWindowClosed, value ? 1 : 0);
				if (_IsMainWindowClosed != orig)
					OnPropertyChanged();
			}
		}

		private int _PropertiesWindowCount = 0;
		public int PropertiesWindowCount
		{
			get => _PropertiesWindowCount;
		}

		public int IncrementPropertiesWindowCount()
		{
			var result = Interlocked.Increment(ref _PropertiesWindowCount);
			OnPropertyChanged(nameof(PropertiesWindowCount));
			return result;
		}

		public int DecrementPropertiesWindowCount()
		{
			var result = Interlocked.Decrement(ref _PropertiesWindowCount);
			OnPropertyChanged(nameof(PropertiesWindowCount));
			return result;
		}

		private bool _ForceProcessTermination = false;
		public bool ForceProcessTermination
		{
			get => _ForceProcessTermination;
			set => SetProperty(ref _ForceProcessTermination, value);
		}

		private string _GoogleDrivePath = string.Empty;
		/// <summary>
		/// Gets or sets a value indicating the path for Google Drive.
		/// </summary>
		public string GoogleDrivePath
		{
			get => _GoogleDrivePath;
			set => SetProperty(ref _GoogleDrivePath, value);
		}

		private string _PCloudDrivePath = string.Empty;
		/// <summary>
		/// Gets or sets a value indicating the path for pCloud Drive.
		/// </summary>
		public string PCloudDrivePath
		{
			get => _PCloudDrivePath;
			set => SetProperty(ref _PCloudDrivePath, value);
		}

		/// <summary>
		/// Gets or sets a value indicating the AppWindow DPI.
		/// TODO update value if the DPI changes
		/// </summary>
		private float _AppWindowDPI = Win32PInvoke.GetDpiForWindow(MainWindow.Instance.WindowHandle) / 96f;
		public float AppWindowDPI
		{
			get => _AppWindowDPI;
			set => SetProperty(ref _AppWindowDPI, value);
		}
	}
}
