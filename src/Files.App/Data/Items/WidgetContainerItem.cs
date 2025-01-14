// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;

namespace Files.App.Data.Items
{
	/// <summary>
	/// Represents an item of Files widget container.
	/// </summary>
	public sealed class WidgetContainerItem : ObservableObject, IDisposable
	{
		// Fields

		private readonly Action<bool> _expanderValueChangedCallback;
		private readonly Func<bool> _expanderValueRequestedCallback;

		// Properties

		public IWidgetViewModel _WidgetItemModel;
		public IWidgetViewModel WidgetItemModel
		{
			get => _WidgetItemModel;
			set => SetProperty(ref _WidgetItemModel, value);
		}

		public string WidgetAutomationProperties
			=> WidgetItemModel.AutomationProperties;

		public bool ShowMenuFlyout
			=> WidgetItemModel.ShowMenuFlyout;

		public MenuFlyoutItem MenuFlyoutItem
			=> WidgetItemModel.MenuFlyoutItem;

		private object _WidgetControl;
		public object WidgetControl
		{
			get => _WidgetControl;
			set => SetProperty(ref _WidgetControl, value);
		}

		public bool IsExpanded
		{
			get => _expanderValueRequestedCallback?.Invoke() ?? true;
			set
			{
				_expanderValueChangedCallback?.Invoke(value);
				OnPropertyChanged();
			}
		}

		// Constructor

		public WidgetContainerItem(object widgetControl, IWidgetViewModel widgetItemModel, Action<bool> expanderValueChangedCallback, Func<bool> expanderValueRequestedCallback)
		{
			_expanderValueChangedCallback = expanderValueChangedCallback;
			_expanderValueRequestedCallback = expanderValueRequestedCallback;

			WidgetItemModel = widgetItemModel;
			WidgetControl = widgetControl;
		}

		// Disposer

		public void Dispose()
		{
			(WidgetControl as IDisposable)?.Dispose();
		}
	}
}
