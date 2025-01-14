// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.UserControls.TabBar
{
	/// <summary>
	/// Represents item for <see cref="TabBar"/>.
	/// </summary>
	public sealed class TabBarItem : ObservableObject, ITabBarItem, IDisposable
	{
		public Frame ContentFrame { get; private set; }

		public event EventHandler<TabBarItemParameter> ContentChanged;

		private IconSource _IconSource;
		public IconSource IconSource
		{
			get => _IconSource;
			set => SetProperty(ref _IconSource, value);
		}

		private string? _Header;
		public string? Header
		{
			get => _Header;
			set => SetProperty(ref _Header, value);
		}

		private string? _Description = null;
		public string? Description
		{
			get => _Description;
			set => SetProperty(ref _Description, value);
		}

		private string? _ToolTipText;
		public string? ToolTipText
		{
			get => _ToolTipText;
			set => SetProperty(ref _ToolTipText, value);
		}

		private bool _AllowStorageItemDrop;
		public bool AllowStorageItemDrop
		{
			get => _AllowStorageItemDrop;
			set => SetProperty(ref _AllowStorageItemDrop, value);
		}

		private TabBarItemParameter _NavigationArguments;
		public TabBarItemParameter NavigationParameter
		{
			get => _NavigationArguments;
			set
			{
				if (value != _NavigationArguments)
				{
					_NavigationArguments = value;
					if (_NavigationArguments is not null)
					{
						ContentFrame.Navigate(_NavigationArguments.InitialPageType, _NavigationArguments.NavigationParameter);
					}
					else
					{
						ContentFrame.Content = null;
					}
				}
			}
		}

		public ITabBarItemContent TabItemContent
			=> ContentFrame?.Content as ITabBarItemContent;

		public TabBarItem()
		{
			ContentFrame = new()
			{
				CacheSize = 0,
				IsNavigationStackEnabled = false,
			};

			ContentFrame.Navigated += ContentFrame_Navigated;
		}

		public void Unload()
		{
			ContentChanged -= NavigationHelpers.Control_ContentChanged;

			Dispose();
		}

		private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
		{
			if (TabItemContent is not null)
				TabItemContent.ContentChanged += TabItemContent_ContentChanged;
		}

		private void TabItemContent_ContentChanged(object? sender, TabBarItemParameter e)
		{
			_NavigationArguments = e;
			ContentChanged?.Invoke(this, e);
		}

		public void Dispose()
		{
			if (TabItemContent is IDisposable disposableContent)
				disposableContent?.Dispose();

			ContentFrame.Content = null;
		}
	}
}
