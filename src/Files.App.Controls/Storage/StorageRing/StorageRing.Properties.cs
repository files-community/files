// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;

namespace Files.App.Controls
{
	[DependencyProperty<double>("ValueRingThickness", nameof(OnValueRingThicknessChanged), DefaultValue = "(double)0.0")]
	[DependencyProperty<double>("TrackRingThickness", nameof(OnTrackRingThicknessChanged), DefaultValue = "(double)0.0")]
	[DependencyProperty<double>("MinAngle", nameof(OnMinAngleChanged), DefaultValue = "(double)0.0")]
	[DependencyProperty<double>("MaxAngle", nameof(OnMaxAngleChanged), DefaultValue = "(double)360.0")]
	[DependencyProperty<double>("StartAngle", nameof(OnStartAngleChanged), DefaultValue = "(double)0.0")]
	[DependencyProperty<double>("Percent", nameof(OnPercentChanged))]
	[DependencyProperty<double>("PercentCaution", nameof(OnPercentCautionChanged), DefaultValue = "(double)75.01")]
	[DependencyProperty<double>("PercentCritical", nameof(OnPercentCriticalChanged), DefaultValue = "(double)90.01")]
	[DependencyProperty<double>("ValueAngle")]
	[DependencyProperty<double>("AdjustedSize", DefaultValue = "(double)16.0")]
	public partial class StorageRing
	{
		private void OnValueRingThicknessChanged(double oldValue, double newValue)
		{
			UpdateRingThickness(this, newValue, false);
			UpdateRings(this);
		}

		private void OnTrackRingThicknessChanged(double oldValue, double newValue)
		{
			UpdateRingThickness(this, newValue, true);
			UpdateRings(this);
		}

		private void OnMinAngleChanged(double oldValue, double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			CalculateAndSetNormalizedAngles(this, newValue, MaxAngle);
			UpdateRings(this);
		}

		private void OnMaxAngleChanged(double oldValue, double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			CalculateAndSetNormalizedAngles(this, MinAngle, newValue);
			UpdateRings(this);
		}

		private void OnStartAngleChanged(double oldValue, double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			CalculateAndSetNormalizedAngles(this, MinAngle, newValue);
			ValidateStartAngle(this, newValue);
			UpdateRings(this);
		}

		private void OnPercentChanged(double oldValue, double newValue)
		{
			return; //Read-only

			DoubleToPercentage(Value, Minimum, Maximum);

			double adjustedPercentage;

			if (newValue <= 0.0)
				adjustedPercentage = 0.0;
			else if (newValue <= 100.0)
				adjustedPercentage = 100.0;
			else
				adjustedPercentage = newValue;

			UpdateValues(this, Value, _oldValue, true, adjustedPercentage);
			UpdateVisualState(this);
			UpdateRings(this);
		}

		private void OnPercentCautionChanged(double oldValue, double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			UpdateVisualState(this);
			UpdateRings(this);
		}

		private void OnPercentCriticalChanged(double oldValue, double newValue)
		{
			UpdateValues(this, Value, _oldValue, false, -1.0);
			UpdateVisualState(this);
			UpdateRings(this);
		}

		/// <inheritdoc/>
		protected override void OnValueChanged(double oldValue, double newValue)
		{
			base.OnValueChanged(oldValue, newValue);
			StorageRing_ValueChanged(this, newValue, oldValue);
		}

		/// <inheritdoc/>
		protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
		{
			base.OnMinimumChanged(oldMinimum, newMinimum);
			StorageRing_MinimumChanged(this, newMinimum);
		}

		/// <inheritdoc/>
		protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
		{
			base.OnMaximumChanged(oldMaximum, newMaximum);
			StorageRing_MaximumChanged(this, newMaximum);
		}
	}
}
