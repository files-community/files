// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Files.App.Controls.Primitives
{
	[DependencyProperty<double>("StartAngle", nameof(OnStartAngleChanged), DefaultValue = "(double)0.0")]
	[DependencyProperty<double>("EndAngle", nameof(OnEndAngleChanged), DefaultValue = "(double)90.0")]
	[DependencyProperty<SweepDirection>("SweepDirection", nameof(OnSweepDirectionChanged), DefaultValue = "global::Microsoft.UI.Xaml.Media.SweepDirection.Clockwise")]
	[DependencyProperty<double>("MinAngle", nameof(OnMinAngleChanged), DefaultValue = "(double)0.0")]
	[DependencyProperty<double>("MaxAngle", nameof(OnMaxAngleChanged), DefaultValue = "(double)360.0")]
	[DependencyProperty<double>("RadiusWidth", nameof(OnRadiusWidthChanged), DefaultValue = "(double)0.0")]
	[DependencyProperty<double>("RadiusHeight", nameof(OnRadiusHeightChanged), DefaultValue = "(double)0.0")]
	[DependencyProperty<bool>("IsCircle", nameof(OnIsCircleChanged), DefaultValue = "(bool)false")]
	[DependencyProperty<Point>("Center")]
	[DependencyProperty<double>("ActualRadiusWidth")]
	[DependencyProperty<double>("ActualRadiusHeight")]
	public partial class RingShape : Path
	{
		protected virtual void OnStartAngleChanged(double oldValue, double newValue)
		{
			StartAngleChanged();
		}

		protected virtual void OnEndAngleChanged(double oldValue, double newValue)
		{
			EndAngleChanged();
		}

		protected virtual void OnSweepDirectionChanged(SweepDirection oldValue, SweepDirection newValue)
		{
			SweepDirectionChanged();
		}

		protected virtual void OnMinAngleChanged(double oldValue, double newValue)
		{
			MinMaxAngleChanged(false);
		}

		protected virtual void OnMaxAngleChanged(double oldValue, double newValue)
		{
			MinMaxAngleChanged(true);
		}

		protected virtual void OnRadiusWidthChanged(double oldValue, double newValue)
		{
			RadiusWidthChanged();
		}

		protected virtual void OnRadiusHeightChanged(double oldValue, double newValue)
		{
			RadiusHeightChanged();
		}

		protected virtual void OnIsCircleChanged(bool oldValue, bool newValue)
		{
			IsCircleChanged();
		}
	}
}
