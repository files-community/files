// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using Windows.Foundation;

namespace Files.App.Controls
{
	/// <summary>
	/// Represents percentage ring islands.
	/// </summary>
	public partial class StorageBar : RangeBase
	{
		// Fields

		double _oldValue;                // Stores the previous value

		double _valueBarMaxWidth;        // The maximum width for the Value Bar
		double _trackBarMaxWidth;        // The maximum width for the Track Bar

		Grid? _containerGrid;            // Reference to the container Grid
		Size? _containerSize;            // Reference to the container Size

		ColumnDefinition? _valueColumn;  // Reference to the ValueBar Column
		ColumnDefinition? _trackColumn;  // Reference to the TrackBar Column
		ColumnDefinition? _gapColumn;    // Reference to the Gap Column

		Border? _valueBarBorder;         // Reference to the Value Bar Border
		Border? _trackBarBorder;         // Reference to the Track Bar Border

		BarShapes _barShape;             // Reference to the BarShape

		double _gapWidth;                // Stores the Gap between Value and Track Bars
		double _smallerHeight;           // Stores the smaller between Value and Track Bars

		// Constructor

		/// <summary>
		/// Initializes an instance of <see cref="StorageBar"/> class.
		/// </summary>
		public StorageBar()
		{
			DefaultStyleKey = typeof(StorageBar);

			SizeChanged -= StorageBar_SizeChanged;
			Unloaded -= StorageBar_Unloaded;
			IsEnabledChanged -= StorageBar_IsEnabledChanged;

			SizeChanged += StorageBar_SizeChanged;
			Unloaded += StorageBar_Unloaded;
			IsEnabledChanged += StorageBar_IsEnabledChanged;
		}

		/// <inheritdoc/>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			UpdateInitialLayout(this);
		}

		#region Handle Property Changes

		/// <summary>
		/// Handles the IsEnabledChanged event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void StorageBar_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			UpdateControl(this);
		}

		/// <summary>
		/// Handles the Unloaded event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void StorageBar_Unloaded(object sender, RoutedEventArgs e)
		{
			SizeChanged -= StorageBar_SizeChanged;
			Unloaded -= StorageBar_Unloaded;
			IsEnabledChanged -= StorageBar_IsEnabledChanged;
		}

		/// <summary>
		/// Handles the SizeChanged event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void StorageBar_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Size minSize;

			if ( DesiredSize.Width < MinWidth || DesiredSize.Height < MinHeight ||
				e.NewSize.Width < MinWidth || e.NewSize.Height < MinHeight )
			{
				Width = MinWidth;
				Height = MinHeight;

				minSize = new Size( MinWidth , MinHeight );
			}
			else
			{
				minSize = e.NewSize;
			}

			UpdateContainer(this, minSize );
			UpdateControl(this);
		}

		#endregion

		#region Update functions

		/// <summary>
		/// Updates the initial layout of the StorageBar control
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		private void UpdateInitialLayout(DependencyObject d)
		{
			// Retrieve references to visual elements
			_containerGrid = GetTemplateChild(ContainerPartName) as Grid;

			_valueColumn = GetTemplateChild(ValueColumnPartName) as ColumnDefinition;
			_trackColumn = GetTemplateChild(TrackColumnPartName) as ColumnDefinition;
			_gapColumn = GetTemplateChild(GapColumnPartName) as ColumnDefinition;

			_valueBarBorder = GetTemplateChild(ValueBorderPartName) as Border;
			_trackBarBorder = GetTemplateChild(TrackBorderPartName) as Border;

			_barShape = BarShape;

			ValueBarHeight = ValueBarHeight;
			TrackBarHeight = TrackBarHeight;
		}

		/// <summary>
		/// Updates the StorageBar control.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		private void UpdateControl(DependencyObject d)
		{
			// 1. Update the Bar Heights
			UpdateContainerHeightsAndCorners(this, ValueBarHeight, TrackBarHeight);

			// 2. Set the 3 Column Widths
			UpdateColumnWidths(this, Value, Minimum, Maximum);

			// 3. Update the control's VisualState
			UpdateVisualState(this);
		}

		/// <summary>
		/// Updates the StorageBar Values.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="newValue">The new Value</param>
		/// <param name="oldValue">The old Value</param>
		/// <param name="isPercent">Checks if the Percent value is being changed</param>
		/// <param name="newPercent">The new Percent value</param>
		private void UpdateValue(DependencyObject d, double newValue, double oldValue, bool isPercent, double newPercent)
		{
			_oldValue = oldValue;

			var adjustedValue = isPercent ? PercentageToValue(newPercent, Minimum, Maximum) : newValue;
			Percent = DoubleToPercentage(adjustedValue, Minimum, Maximum);

			UpdateControl(this);
		}

		/// <summary>
		/// Updates Container Heights and Bar Corner Radii
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="valueBarHeight">The ValueBar Height</param>
		/// <param name="trackBarHeight">The TrackBar Height</param>
		private void UpdateContainerHeightsAndCorners(DependencyObject d, double valueBarHeight, double trackBarHeight)
		{
			// Finds the larger of the two height values
			double calculatedLargerHeight = Math.Max(valueBarHeight, trackBarHeight);
			double calculatedSmallerHeight = Math.Min(valueBarHeight, trackBarHeight);

			if (_valueBarBorder != null || _trackBarBorder != null || _containerGrid != null)
			{
				_valueBarBorder.Height = valueBarHeight;
				_trackBarBorder.Height = trackBarHeight;

				if (_barShape == BarShapes.Round)
				{
					_valueBarBorder.CornerRadius = new(valueBarHeight / 2);
					_trackBarBorder.CornerRadius = new(trackBarHeight / 2);
				}
				else if (_barShape == BarShapes.Soft)
				{
					_valueBarBorder.CornerRadius = new(valueBarHeight / 4);
					_trackBarBorder.CornerRadius = new(trackBarHeight / 4);
				}
				else
				{
					_valueBarBorder.CornerRadius = new(0);
					_trackBarBorder.CornerRadius = new(0);
				}

				_containerGrid.Height = calculatedLargerHeight;
			}

			_gapWidth = calculatedLargerHeight;
			_smallerHeight = calculatedSmallerHeight;

		}

		/// <summary>
		/// Updates Column Widths and Bar Column assignments
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="value">The Value</param>
		/// <param name="minValue">The Minimum value</param>
		/// <param name="maxValue">The Maximum value</param>
		private void UpdateColumnWidths(DependencyObject d, double value, double minValue, double maxValue)
		{
			if (_gapColumn != null || _valueColumn != null || _trackColumn != null || _valueBarBorder != null || _trackBarBorder != null)
			{
				if (_containerSize is not Size containerSize)
					return;

				if (containerSize.Width > TrackBarHeight || containerSize.Width > ValueBarHeight)
				{
					double valuePercent = DoubleToPercentage(Value, Minimum, Maximum);
					double minPercent = DoubleToPercentage(Minimum, Minimum, Maximum);
					double maxPercent = DoubleToPercentage(Maximum, Minimum, Maximum);

					if (valuePercent <= minPercent)  // Value is <= Minimum
					{
						_gapColumn.Width = new(1, GridUnitType.Star);
						_valueColumn.Width = new(1, GridUnitType.Star);
						_trackColumn.Width = new(1, GridUnitType.Star);

						Grid.SetColumnSpan(_trackBarBorder, 3);
						Grid.SetColumn(_trackBarBorder, 0);

						_valueBarBorder.Visibility = Visibility.Collapsed;
						_trackBarBorder.Visibility = Visibility.Visible;
					}
					else if (valuePercent >= maxPercent)  // Value is >= Maximum
					{
						_gapColumn.Width = new(1, GridUnitType.Star);
						_valueColumn.Width = new(1, GridUnitType.Star);
						_trackColumn.Width = new(1, GridUnitType.Star);

						Grid.SetColumnSpan(_valueBarBorder, 3);
						Grid.SetColumn(_valueBarBorder, 0);

						_valueBarBorder.Visibility = Visibility.Visible;
						_trackBarBorder.Visibility = Visibility.Collapsed;
					}
					else
					{
						Grid.SetColumnSpan(_valueBarBorder, 1);
						Grid.SetColumn(_valueBarBorder, 0);

						Grid.SetColumnSpan(_trackBarBorder, 1);
						Grid.SetColumn(_trackBarBorder, 2);


						_valueBarBorder.Visibility = Visibility.Visible;
						_trackBarBorder.Visibility = Visibility.Visible;


						var valueBarHeight = ValueBarHeight;
						var trackBarHeight = TrackBarHeight;
						var gapWidth = _gapWidth;

						_valueColumn.MaxWidth = containerSize.Width;
						_trackColumn.MaxWidth = containerSize.Width;

						var valueLarger = valueBarHeight > trackBarHeight;

						if (valuePercent > minPercent && valuePercent <= minPercent + 2.0)  // Between 0% and 2%
						{
							var interpolatedValueBarHeight = CalculateInterpolatedValue(
								this,
								minPercent,
								Percent,
								minPercent + 2.0,
								0.0,
								valueBarHeight,
								true);

							var interpolatedTrackBarHeight = CalculateInterpolatedValue(
								this,
								minPercent,
								Percent,
								minPercent + 2.0,
								0.0,
								trackBarHeight,
								true);

							var interpolatedGapWidth = gapWidth;

							if (valueLarger == true)
							{
								interpolatedGapWidth = CalculateInterpolatedValue(
									this,
									minPercent,
									Percent,
									minPercent + 2.0,
									0.0,
									gapWidth,
									true);
							}
							else
							{
								interpolatedGapWidth = CalculateInterpolatedValue(
									this,
									minPercent,
									Percent,
									minPercent + 2.0,
									0.0,
									_smallerHeight,
									true);
							}

							_valueColumn.MinWidth = interpolatedValueBarHeight;
							_trackColumn.MinWidth = interpolatedTrackBarHeight;

							_valueBarBorder.Height = interpolatedValueBarHeight;
							_trackBarBorder.Height = trackBarHeight;

							var calculatedValueWidth = (_valueColumn.MaxWidth / 100) * valuePercent;

							_valueColumn.Width = new(calculatedValueWidth);
							_gapColumn.Width = new(interpolatedGapWidth);
							_trackColumn.Width = new(1, GridUnitType.Star);
						}
						else if (valuePercent >= maxPercent - 1.0 && valuePercent < maxPercent)   // Between 98% and 100%
						{
							var interpolatedValueBarHeight = CalculateInterpolatedValue(
								this,
								maxPercent - 2.0,
								Percent,
								maxPercent,
								valueBarHeight,
								0.0,
								true);

							var interpolatedTrackBarHeight = CalculateInterpolatedValue(
								this,
								maxPercent - 2.0,
								Percent,
								maxPercent,
								trackBarHeight,
								0.0,
								true);

							var interpolatedGapWidth = gapWidth;

							if (valueLarger == true)
							{
								interpolatedGapWidth = CalculateInterpolatedValue(
									this,
									maxPercent - 2.0,
									Percent,
									maxPercent,
									0.0,
									_smallerHeight,
									true);
							}
							else
							{
								interpolatedGapWidth = CalculateInterpolatedValue(
									this,
									maxPercent - 2.0,
									Percent,
									maxPercent,
									0.0,
									gapWidth,
									true);
							}

							_valueColumn.MinWidth = interpolatedValueBarHeight;
							_trackColumn.MinWidth = interpolatedTrackBarHeight;

							var calculatedValueWidth = (_valueColumn.MaxWidth / 100) * valuePercent;

							_valueColumn.Width = new(calculatedValueWidth);
							_trackColumn.Width = new(1, GridUnitType.Star);
							_gapColumn.Width = new(interpolatedGapWidth);

							_valueBarBorder.Height = valueBarHeight;
							_trackBarBorder.Height = interpolatedTrackBarHeight;
						}
						else  // Between 2% and 98%
						{
							_valueColumn.MinWidth = valueBarHeight;
							_trackColumn.MinWidth = trackBarHeight;

							double calculatedValueWidth = (_valueColumn.MaxWidth / 100) * valuePercent;

							_valueColumn.Width = new(calculatedValueWidth);
							_trackColumn.Width = new(1, GridUnitType.Star);

							var interpolatedGapWidth = gapWidth;

							if (valueLarger == true)
							{
								interpolatedGapWidth = CalculateInterpolatedValue(
									this,
									minPercent + 2.0,
									Percent,
									maxPercent - 2.0,
									gapWidth,
									_smallerHeight,
									true);
							}
							else
							{
								interpolatedGapWidth = CalculateInterpolatedValue(
									this,
									minPercent + 2.0,
									Percent,
									maxPercent - 2.0,
									_smallerHeight,
									gapWidth,
									true);
							}

							_gapColumn.Width = new(interpolatedGapWidth);

							_valueBarBorder.Height = valueBarHeight;
							_trackBarBorder.Height = trackBarHeight;
						}
					}
				}
			}
		}

		/// <summary>
		/// Updates the ContainerSize taking in control Padding
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="newSize">The new Size</param>
		private void UpdateContainer(DependencyObject d, Size newSize)
		{
			double containerWidth = newSize.Width - (Padding.Left + Padding.Right);
			double containerHeight = newSize.Height - (Padding.Top + Padding.Bottom);

			_containerSize = new(containerWidth, containerHeight);
		}

		/// <summary>
		/// Update the control's VisualState
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		private void UpdateVisualState(DependencyObject d)
		{
			// First is the control is Disabled
			if (IsEnabled == false)
			{
				VisualStateManager.GoToState(this, DisabledStateName, true);
			}
			// Then the control is Enabled
			else
			{
				// Is the Percent value equal to or above the PercentCritical value
				if (Percent >= PercentCritical)
				{
					VisualStateManager.GoToState(this, CriticalStateName, true);
				}
				// Is the Percent value equal to or above the PercentCaution value
				else if (Percent >= PercentCaution)
				{
					VisualStateManager.GoToState(this, CautionStateName, true);
				}
				// Else we use the Safe State
				else
				{
					VisualStateManager.GoToState(this, SafeStateName, true);
				}
			}
		}

		#endregion

		#region Conversion return functions

		/// <summary>
		/// Converts a value within a specified range to a percentage.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="minValue">The minimum value of the input range.</param>
		/// <param name="maxValue">The maximum value of the input range.</param>
		/// <returns>The percentage value (between 0 and 100).</returns>
		private double DoubleToPercentage(double value, double minValue, double maxValue)
		{
			// Ensure value is within the specified range
			if (value < minValue)
			{
				return 0.0; // Below the range
			}
			else if (value > maxValue)
			{
				return 100.0; // Above the range
			}
			else
			{
				// Calculate the normalized value
				var normalizedValue = (value - minValue) / (maxValue - minValue);

				// Convert to percentage
				var percentage = normalizedValue * 100.0;

				double roundedPercentage = Math.Round(percentage, 2, MidpointRounding.ToEven);

				return roundedPercentage;
			}
		}

		/// <summary>
		/// Converts a percentage within a specified range to a value.
		/// </summary>
		/// <param name="percentage">The percentage to convert.</param>
		/// <param name="minValue">The minimum value of the input range.</param>
		/// <param name="maxValue">The maximum value of the input range.</param>
		/// <returns>The percentage value (between 0 and 100).</returns>
		private double PercentageToValue(double percentage, double minValue, double maxValue)
		{
			double convertedValue = percentage * (maxValue - minValue) / 100.0;

			// Ensure the converted value stays within the specified range
			if (convertedValue < minValue)
				convertedValue = minValue;
			else if (convertedValue > maxValue)
				convertedValue = maxValue;

			return convertedValue;
		}

		/// <summary>
		/// Calculates an interpolated value based on the provided parameters.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="startValue">The starting value for interpolation.</param>
		/// <param name="value">The current value to interpolate.</param>
		/// <param name="endValue">The ending value for interpolation.</param>
		/// <param name="startOutput">The starting Output value.</param>
		/// <param name="endOutput">The ending Output value.</param>
		/// <param name="useEasing">Indicates whether to apply an easing function.</param>
		/// <returns>The interpolated thickness value.</returns>
		private double CalculateInterpolatedValue(DependencyObject d, double startValue, double value, double endValue, double startOutput, double endOutput, bool useEasing)
		{
			// Ensure that value is within the range [startValue, endValue]
			value = Math.Max(startValue, Math.Min(endValue, value));

			// Calculate the interpolation factor (t) between 0 and 1
			var t = (value - startValue) / (endValue - startValue);

			double interpolatedOutput;

			if (useEasing)
			{
				// Apply an easing function (e.g., quadratic ease-in-out)
				//var easedT = EaseInOutFunction(t);
				var easedT = EaseOutCubic(t);

				// Interpolate the thickness
				interpolatedOutput = startOutput + easedT * (endOutput - startOutput);
			}
			else
			{
				// Interpolate the thickness
				interpolatedOutput = startOutput + t * (endOutput - startOutput);
			}

			return interpolatedOutput;
		}

		/// <summary>
		/// Example quadratic ease-in-out function
		/// </summary>
		private double EasingInOutFunction(double t)
		{
			return t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
		}

		/// <summary>
		/// Example ease-out cubic function
		/// </summary>
		static double EaseOutCubic(double t)
		{
			return 1.0 - Math.Pow(1.0 - t, 3.0);
		}

		#endregion
	}
}
