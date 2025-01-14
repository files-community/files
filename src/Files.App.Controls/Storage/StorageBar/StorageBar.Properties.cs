// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Controls
{
	[DependencyProperty<double>("ValueBarHeight", nameof(OnValueBarHeightChanged), DefaultValue = "(double)4.0")]
	[DependencyProperty<double>("TrackBarHeight", nameof(OnTrackBarHeightChanged), DefaultValue = "(double)2.0")]
	[DependencyProperty<BarShapes>("BarShape", nameof(OnBarShapeChanged), DefaultValue = "BarShapes.Round")]
	[DependencyProperty<double>("Percent", nameof(OnPercentChanged), DefaultValue = "(double)0.0")]
	[DependencyProperty<double>("PercentCaution", nameof(OnPercentCautionChanged), DefaultValue = "(double)75.1")]
	[DependencyProperty<double>("PercentCritical", nameof(OnPercentCriticalChanged), DefaultValue = "(double)89.9")]
	public partial class StorageBar
	{
		private void OnValueBarHeightChanged(double oldValue, double newValue)
		{
			UpdateControl(this);
		}

		private void OnTrackBarHeightChanged(double oldValue, double newValue)
		{
			UpdateControl(this);
		}

		private void OnBarShapeChanged(BarShapes oldValue, BarShapes newValue)
		{
			UpdateControl(this);
		}

		private void OnPercentChanged(double oldValue, double newValue)
		{
			return; //Read-only

			DoubleToPercentage(Value, Minimum, Maximum);
			UpdateControl(this);
		}

		private void OnPercentCautionChanged(double oldValue, double newValue)
		{
			UpdateControl(this);
		}

		private void OnPercentCriticalChanged(double oldValue, double newValue)
		{
			UpdateControl(this);
		}

		/// <inheritdoc/>
		protected override void OnValueChanged(double oldValue, double newValue)
		{
			_oldValue = oldValue;
			base.OnValueChanged(oldValue, newValue);
			UpdateValue(this, Value, _oldValue, false, -1.0);
		}

		/// <inheritdoc/>
		protected override void OnMaximumChanged(double oldValue, double newValue)
		{
			base.OnMaximumChanged(oldValue, newValue);
			UpdateValue(this, oldValue, newValue, false, -1.0);
		}

		/// <inheritdoc/>
		protected override void OnMinimumChanged(double oldValue, double newValue)
		{
			base.OnMinimumChanged(oldValue, newValue);
			UpdateValue(this, oldValue, newValue, false, -1.0);
		}
	}
}
