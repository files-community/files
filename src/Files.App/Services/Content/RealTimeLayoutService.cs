// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using System.Globalization;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT.Interop;

namespace Files.App.Services.Content
{
	/// <summary>
	/// Provides a service to manage real-time layout updates, including content layout and title bar updates,
	/// while supporting flow direction based on the current culture.
	/// </summary>
	internal sealed class RealTimeLayoutService : IRealTimeLayoutService
	{
		/// <summary>
		/// List of weak references to target objects and their associated callbacks.
		/// </summary>
		private readonly List<(WeakReference<object> Reference, Action Callback)> _callbacks = [];

		/// <inheritdoc/>
		public event EventHandler<FlowDirection>? FlowDirectionChanged;

		/// <inheritdoc/>
		public FlowDirection FlowDirection => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

		/// /// <inheritdoc/>
		public void UpdateCulture(CultureInfo culture)
		{
			FlowDirection tmp = FlowDirection;

			CultureInfo.DefaultThreadCurrentUICulture = culture;
			CultureInfo.CurrentUICulture = culture;
// TODO: Remove this after work RealTime string resources change work
#if DEBUG
			if (tmp != FlowDirection)
				InvokeCallbacks();
#endif
		}

		/// <inheritdoc/>
		public void AddCallback(Window target, Action callback)
		{
// TODO: Remove this after work RealTime string resources change work
#if DEBUG
			var weakReference = new WeakReference<object>(target);
			_callbacks.Add((weakReference, callback));

			if (!IsExistTarget(target))
				target.Closed += (sender, args) => RemoveCallback(target);
#endif
		}

		/// <inheritdoc/>
		public void AddCallback(FrameworkElement target, Action callback)
		{
// TODO: Remove this after work RealTime string resources change work
#if DEBUG
			var weakReference = new WeakReference<object>(target);
			_callbacks.Add((weakReference, callback));

			if (!IsExistTarget(target))
				target.Unloaded += (sender, args) => RemoveCallback(target);
#endif
		}

		/// <inheritdoc/>
		public bool UpdateTitleBar(Window window)
		{
			try
			{
				var hwnd = new HWND(WindowNative.GetWindowHandle(window));
				var exStyle = PInvoke.GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

				exStyle = FlowDirection is FlowDirection.RightToLeft
					? new((uint)exStyle | (uint)WINDOW_EX_STYLE.WS_EX_LAYOUTRTL) // Set RTL layout
					: new((uint)exStyle.ToInt64() & ~(uint)WINDOW_EX_STYLE.WS_EX_LAYOUTRTL); // Set LTR layout

				if (PInvoke.SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle) == 0)
					return false;
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}

		/// <inheritdoc/>
		public void UpdateContent(Window window)
		{
			if (window.Content is FrameworkElement frameworkElement)
				frameworkElement.FlowDirection = FlowDirection;
		}

		/// <inheritdoc/>
		public void UpdateContent(FrameworkElement frameworkElement)
		{
			frameworkElement.FlowDirection = FlowDirection;
		}

		private bool IsExistTarget(object target)
			=> _callbacks.FindIndex(item => item.Reference.TryGetTarget(out var targetObject) && targetObject == target) >= 0;

		/// <summary>
		/// Removes the callback associated with the specified target.
		/// </summary>
		/// <param name="target">The target object whose callback needs to be removed.</param>
		private void RemoveCallback(object target)
		{
			_callbacks.RemoveAll(item =>
				item.Reference.TryGetTarget(out var targetObject) && targetObject == target);
		}

		/// <summary>
		/// Invokes all registered callbacks for targets that are still valid.
		/// </summary>
		private void InvokeCallbacks()
		{
			_callbacks.Where(item =>
				item.Reference.TryGetTarget(out var targetObject) && targetObject != null)
				.Select(item => item.Callback).ForEach(callback => callback());
			_callbacks.RemoveAll(item => !item.Reference.TryGetTarget(out _));

			FlowDirectionChanged?.Invoke(this, FlowDirection);
		}
	}
}
