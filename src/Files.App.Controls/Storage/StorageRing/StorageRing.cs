// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls.Primitives;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;
using WinRT;

namespace Files.App.Controls
{
	/// <summary>
	/// Represents percentage bar islands.
	/// </summary>
	public partial class StorageRing : RangeBase
	{
		// Fields

		private double _containerSize;          // Size of the inner container after padding
		private double _containerCenter;        // Center X and Y value of the inner container
		private double _sharedRadius;           // Radius to be shared by both rings (smaller of the two)

		private double _oldValue;               // Stores the previous Value
		private double _oldValueAngle;          // Stored the old ValueAngle

		private double _valueRingThickness;     // The stored value ring thickness
		private double _trackRingThickness;     // The stored track ring thickness
		private ThicknessCheck _thicknessCheck; // Determines how the two ring thicknesses compare
		private double _largerThickness;        // The larger of the two ring thicknesses
		private double _smallerThickness;       // The smaller of the two ring thicknesses

		private Grid? _containerGrid;           // Reference to the container Grid
		private RingShape? _valueRingShape;     // Reference to the Value RingShape
		private RingShape? _trackRingShape;     // Reference to the Track RingShape

		private RectangleGeometry? _clipRect;   // Clipping RectangleGeometry for the canvas

		private double _normalizedMinAngle;     // Stores the normalized Minimum Angle
		private double _normalizedMaxAngle;     // Stores the normalized Maximum Angle
		private double _gapAngle;               // Stores the angle to be used to separate Value and Track rings
		private double _validStartAngle;        // The validated StartAngle

		#region  Private Setters

		/// <summary>
		/// Sets the Container size to the smaller of control's Height and Width.
		/// </summary>
		private void SetContainerSize(double controlWidth, double controlHeight, Thickness padding)
		{
			double correctedWidth = controlWidth - (padding.Left + padding.Right);
			double correctedHeight = controlHeight - (padding.Top + padding.Bottom);

			double check = Math.Min(correctedWidth, correctedHeight);

			_containerSize = check < minSize ? minSize : check;
		}

		/// <summary>
		/// Sets the private Container center X and Y value
		/// </summary>
		private void SetContainerCenter(double containerSize)
		{
			_containerCenter = (containerSize / 2);
		}

		/// <summary>
		/// Sets the shared Radius by passing in containerSize and thickness.
		/// </summary>
		private void SetSharedRadius(double containerSize, double thickness)
		{
			double check = (containerSize / 2) - (thickness / 2);
			double minSize = 4;

			_sharedRadius = check <= minSize ? minSize : check;
		}

		/// <summary>
		/// Sets the private ThicknessCheck enum value
		/// </summary>
		private void SetThicknessCheck(double valueThickness, double trackThickness)
		{
			if (valueThickness > trackThickness)
				_thicknessCheck = ThicknessCheck.Value;
			else if (valueThickness < trackThickness)
				_thicknessCheck = ThicknessCheck.Track;
			else
				_thicknessCheck = ThicknessCheck.Equal;
		}

		#endregion

		// Constructor

		/// <summary>
		/// Initializes an instance of <see cref="StorageRing"/> class.
		/// </summary>
		public StorageRing()
		{
			DefaultStyleKey = typeof(StorageRing);

			SizeChanged += StorageRing_SizeChanged;
			Unloaded += StorageRing_Unloaded;
			IsEnabledChanged += StorageRing_IsEnabledChanged;
			Loaded += StorageRing_Loaded;
		}

		protected override void OnApplyTemplate()
		{
			InitializeParts();

			base.OnApplyTemplate();
		}

		/// <summary>
		/// Initializes the Parts and properties of a PercentageRing control.
		/// </summary>
		private void InitializeParts()
		{
			// Retrieve references to visual elements
			_containerGrid = GetTemplateChild(ContainerPartName) as Grid;
			_valueRingShape = GetTemplateChild(ValueRingShapePartName) as RingShape;
			_trackRingShape = GetTemplateChild(TrackRingShapePartName) as RingShape;

			CalculateAndSetNormalizedAngles(this, MinAngle, MaxAngle);

			UpdateValues(this, Value, 0.0, false, -1.0);
		}

		#region Property Change Events

		/// <summary>
		/// Occurs when either the ActualHeight or the ActualWidth property changes value,
		/// </summary>
		/// <param name="sender">The object that triggered the event.</param>
		/// <param name="e">Provides data related to the SizeChanged event.</param>
		private void StorageRing_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Size minSize;

			if ( DesiredSize.Width < MinWidth || DesiredSize.Height < MinHeight ||
				e.NewSize.Width < MinWidth || e.NewSize.Height < MinHeight)
			{
				Width = MinWidth;
				Height = MinHeight;

				minSize = new Size( MinWidth , MinHeight );
			}
			else
			{
				minSize = e.NewSize;
			}

			UpdateContainerCenterAndSizes( this , minSize );
			
			UpdateRings(this);
		}

		/// <summary>
		/// Occurs when this object is no longer connected to the value object tree.
		/// </summary>
		/// <param name="sender">The object that triggered the event.</param>
		/// <param name="e">Provides data related to the Unloaded event.</param>
		private void StorageRing_Unloaded(object sender, RoutedEventArgs e)
		{
			SizeChanged -= StorageRing_SizeChanged;
			Unloaded -= StorageRing_Unloaded;
			IsEnabledChanged -= StorageRing_IsEnabledChanged;
		}

		/// <summary>
		/// Occurs when this object is loaded into the value object tree
		/// </summary>
		/// <param name="sender">The object that triggered the event.</param>
		/// <param name="e">Provides data related to the Unloaded event.</param>
		private void StorageRing_Loaded(object sender, RoutedEventArgs e)
		{
		}

		/// <summary>
		/// Occurs when the IsEnabled property changes.
		/// </summary>
		/// <param name="sender">The object that triggered the event.</param>
		/// <param name="e">Provides data for a PropertyChangedCallback implementation that is invoked 
		/// when a dependency property changes its value. </param>
		private void StorageRing_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			UpdateVisualState(this);
		}

		#endregion

		#region Update functions

		/// <summary>
		/// Updates Values used by the control
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="newValue">The new Value</param>
		/// <param name="oldValue">The old Value</param>
		/// <param name="percentChanged">Checks if Percent value is being changed</param>
		/// <param name="newPercent">The new Percent value</param>
		private void UpdateValues(DependencyObject d, double newValue, double oldValue, bool percentChanged, double newPercent)
		{
			CalculateAndSetNormalizedAngles(this, MinAngle, MaxAngle);

			double adjustedValue;

			if (percentChanged)
			{
				var percentToValue = PercentageToValue(newPercent, Minimum, Maximum);
				adjustedValue = percentToValue;
			}
			else
			{
				adjustedValue = newValue;
			}

			ValueAngle = DoubleToAngle(adjustedValue, Minimum, Maximum, _normalizedMinAngle, _normalizedMaxAngle);
			Percent = DoubleToPercentage(adjustedValue, Minimum, Maximum);

			_oldValue = oldValue;
			_oldValueAngle = DoubleToAngle(oldValue, Minimum, Maximum, _normalizedMinAngle, _normalizedMaxAngle);
		}

		/// <summary>
		/// Updates Container Center point and Sizes
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="newSize">The new Size</param>
		private void UpdateContainerCenterAndSizes(DependencyObject d, Size newSize)
		{
			var borderThickness = BorderThickness;

			var borderWidth = borderThickness.Left + borderThickness.Right;
			var borderHeight = borderThickness.Top + borderThickness.Bottom;

			// Set Container Size
			SetContainerSize(Width - (borderWidth * 2), Height - (borderHeight * 2), Padding);
			AdjustedSize = _containerSize;

			// Set Container Center
			SetContainerCenter(AdjustedSize);

			// Set Clipping Rectangle
			RectangleGeometry rectGeo = new() { Rect = new(0 - borderWidth, 0 - borderHeight, AdjustedSize + borderWidth, AdjustedSize + borderHeight) };
			_clipRect = rectGeo;

			// Get Container
			var container = _containerGrid;

			// If the Container is not null.
			if (container != null)
			{
				// Set the _container width and height to the adjusted size (AdjustedSize).
				container.Width = AdjustedSize;
				container.Height = AdjustedSize;
				container.Clip = _clipRect;
			}
		}

		/// <summary>
		/// Updates the Radii of both Rings
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="newRadius">The new Radius</param>
		/// <param name="isTrack">Checks if the Track is currently being updated</param>
		private void UpdateRadii(DependencyObject d, double newRadius, bool isTrack)
		{
			double valueRingThickness = _valueRingThickness;
			double trackRingThickness = _trackRingThickness;

			// We want to limit the Thickness values to no more than 1/5 of the container size
			if (isTrack == false)
			{
				if (newRadius > (AdjustedSize / 5))
					valueRingThickness = (AdjustedSize / 5);
				else
					valueRingThickness = newRadius;
			}
			else
			{
				if (newRadius > (AdjustedSize / 5))
					trackRingThickness = (AdjustedSize / 5);
				else
					trackRingThickness = newRadius;
			}

			// We check if both Rings have Equal thickness
			if (_thicknessCheck == ThicknessCheck.Equal)
			{
				SetSharedRadius(AdjustedSize, 0);
			}
			// Else we use the larger thickness to adjust the size
			else
			{
				SetSharedRadius(AdjustedSize, _largerThickness);
			}
		}

		/// <summary>
		/// Updates the Thickness for both Rings
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="newThickness">The new Thickness</param>
		/// <param name="isTrack">Checks if the TrackRing Thickness is being updated</param>
		private void UpdateRingThickness(DependencyObject d, double newThickness, bool isTrack)
		{
			if (isTrack)
				_trackRingThickness = newThickness;
			else
				_valueRingThickness = newThickness;

			SetThicknessCheck(_valueRingThickness, _trackRingThickness);

			if (_thicknessCheck is ThicknessCheck.Value)
			{
				_largerThickness = _valueRingThickness;
				_smallerThickness = _trackRingThickness;
			}
			else if (_thicknessCheck is ThicknessCheck.Track)
			{
				_largerThickness = _trackRingThickness;
				_smallerThickness = _valueRingThickness;
			}
			else // ThicknessCheck == Equal
			{
				_largerThickness = _valueRingThickness;
				_smallerThickness = _valueRingThickness;
			}
		}

		/// <summary>
		/// Updates the GapAngle separating both Rings.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="newRadius">The new Radius</param>
		/// <param name="isTrack">Checks if the TrackRing is being updated</param>
		private void UpdateGapAngle(DependencyObject d, double newRadius, bool isTrack)
		{
			double angle = GapThicknessToAngle(_sharedRadius, (_largerThickness * 0.75));
			_gapAngle = angle;
		}

		/// <summary>
		/// Updates the control's VisualState
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		private void UpdateVisualState(DependencyObject d)
		{
			// First is the control is Disabled
			if (!IsEnabled)
			{
				VisualStateManager.GoToState(this, DisabledStateName, true);
			}
			// Then the control is Enabled
			else
			{
				// Is the Percent value equal to or above the PercentCritical value
				if (Percent >= PercentCritical)
					VisualStateManager.GoToState(this, CriticalStateName, true);
				// Is the Percent value equal to or above the PercentCaution value
				else if (Percent >= PercentCaution)
					VisualStateManager.GoToState(this, CautionStateName, true);
				// Else we use the Safe State
				else
					VisualStateManager.GoToState(this, SafeStateName, true);
			}
		}

		#endregion

		#region Update Rings

		/// <summary>
		/// Updates both Rings
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		private void UpdateRings(DependencyObject d)
		{
			if (_valueRingShape == null || _trackRingShape == null)
				return;

			UpdateContainerCenterAndSizes(this, DesiredSize);
			CalculateAndSetNormalizedAngles(this, MinAngle, MaxAngle);
			UpdateRingSizes(d, _valueRingShape, _trackRingShape);
			UpdateRadii(d, _sharedRadius, false);
			UpdateRadii(d, _sharedRadius, true);
			UpdateGapAngle(d, _sharedRadius, false);
			UpdateRingLayouts(d, _valueRingShape, _trackRingShape);
			UpdateRingAngles(d, _valueRingShape, _trackRingShape);
			UpdateRingStrokes(d, _valueRingShape, _trackRingShape);
			UpdateVisualState(d);
		}

		/// <summary>
		/// Updates the Ring Sizes
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="valueRingShape">The reference to the ValueRing RingShape TemplatePart</param>
		/// <param name="trackRingShape">The reference to the TrackRing RingShape TemplatePart</param>
		private void UpdateRingSizes(DependencyObject d, RingShape valueRingShape, RingShape trackRingShape)
		{
			if (valueRingShape is null || trackRingShape is null)
				return;

			// Set sizes for the rings as needed
			if (_thicknessCheck is ThicknessCheck.Value)
			{
				valueRingShape.Width = _containerSize;
				valueRingShape.Height = _containerSize;

				trackRingShape.Width = _containerSize - (_largerThickness / 2);
				trackRingShape.Height = _containerSize - (_largerThickness / 2);
			}
			else if (_thicknessCheck is ThicknessCheck.Track)
			{
				valueRingShape.Width = _containerSize - (_largerThickness / 2);
				valueRingShape.Height = _containerSize - (_largerThickness / 2);

				trackRingShape.Width = _containerSize;
				trackRingShape.Height = _containerSize;
			}
			else // ThicknessCheck == Equal
			{
				valueRingShape.Width = _containerSize;
				valueRingShape.Height = _containerSize;

				trackRingShape.Width = _containerSize;
				trackRingShape.Height = _containerSize;
			}

			valueRingShape.UpdateLayout();
			trackRingShape.UpdateLayout();
		}

		/// <summary>
		/// Updates the Start and End Angles for both Rings
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="valueRingShape">The reference to the ValueRing RingShape TemplatePart</param>
		/// <param name="trackRingShape">The reference to the TrackRing RingShape TemplatePart</param>
		private void UpdateRingAngles(DependencyObject d, RingShape valueRingShape, RingShape trackRingShape)
		{
			if (valueRingShape == null || trackRingShape == null)
				return;

			double ValueStartAngle = _normalizedMinAngle;
			double ValueEndAngle;
			double TrackStartAngle = _normalizedMaxAngle;
			double TrackEndAngle;

			//
			// We get percentage values to use for manipulating how we draw the rings.
			var minPercent = DoubleToPercentage(Minimum, Minimum, Maximum);
			var maxPercent = DoubleToPercentage(Maximum, Minimum, Maximum);
			var percent = Percent;

			//
			// Percent is below or at its Minimum
			if (percent <= minPercent)
			{
				ValueEndAngle = _normalizedMinAngle;

				TrackStartAngle = _normalizedMaxAngle - 0.01;
				TrackEndAngle = _normalizedMinAngle;
			}
			//
			// Percent is between it's Minimum and its Minimum + 2 (between 0% and 2%)
			else if (percent > minPercent && percent < minPercent + 2.0)
			{
				ValueEndAngle = ValueAngle;

				double interpolatedStartTo;
				double interpolatedEndTo;

				//
				// We need to interpolate the track start and end angles between pRing.Minimum and pRing.Minimum + 0.75
				interpolatedStartTo = GetAdjustedAngle(
					this,
					minPercent,
					percent,
					minPercent + 2.0,
					_normalizedMinAngle,
					_normalizedMinAngle + _gapAngle,
					ValueAngle,
					true);

				if (IsFullCircle(_normalizedMinAngle, _normalizedMaxAngle) == true)
				{
					interpolatedEndTo = GetAdjustedAngle(
						this,
						minPercent,
						percent,
						minPercent + 2.0,
						_normalizedMaxAngle,
						_normalizedMaxAngle - (_gapAngle + ValueAngle),
						ValueAngle,
						true);
				}
				else
				{
					interpolatedEndTo = _normalizedMaxAngle;
				}

				TrackStartAngle = interpolatedEndTo;
				TrackEndAngle = interpolatedStartTo;
			}
			//
			// Percent is at or above its Maximum value
			else if (percent >= maxPercent)
			{
				ValueEndAngle = _normalizedMaxAngle;

				TrackStartAngle = _normalizedMaxAngle;
				TrackEndAngle = _normalizedMinAngle;
			}
			//
			// Any value between the Minimum and the Maximum value
			else
			{
				ValueEndAngle = ValueAngle;

				if (IsFullCircle(MinAngle, MaxAngle) == true)
				{
					TrackStartAngle = _normalizedMaxAngle - _gapAngle;

					//
					// When the trackRing's EndAngle meets or exceeds its adjusted StartAngle
					if (ValueAngle > (_normalizedMaxAngle - (_gapAngle * 2)))
					{
						TrackEndAngle = _normalizedMaxAngle - (_gapAngle - 0.0001);
					}
					else
					{
						// We take the MaxAngle - the GapAngle, then minus the ValueAngle from it
						TrackEndAngle = (_normalizedMinAngle + _gapAngle) - (_normalizedMinAngle - ValueAngle);
					}
				}
				else
				{
					TrackStartAngle = _normalizedMaxAngle;

					//
					// When the trackRing's EndAngle meets or exceeds its adjusted StartAngle
					if (ValueAngle > (_normalizedMaxAngle - (_gapAngle / 20)))
					{
						TrackEndAngle = (_normalizedMaxAngle - 0.0001);
					}
					else
					{
						// We take the MaxAngle - the GapAngle, then minus the ValueAngle from it
						TrackEndAngle = (_normalizedMinAngle + (_gapAngle - (_normalizedMinAngle - ValueAngle)));
					}
				}
			}

			valueRingShape.StartAngle = ValueStartAngle;
			trackRingShape.StartAngle = TrackStartAngle;

			valueRingShape.EndAngle = ValueEndAngle;
			trackRingShape.EndAngle = TrackEndAngle;
		}

		/// <summary>
		/// Updates the Layout for both Rings
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="valueRingShape">The reference to the ValueRing RingShape TemplatePart</param>
		/// <param name="trackRingShape">The reference to the TrackRing RingShape TemplatePart</param>
		private void UpdateRingLayouts(DependencyObject d, RingShape valueRingShape, RingShape trackRingShape)
		{
			StorageRing storageRing = (StorageRing)d;

			if (valueRingShape == null || trackRingShape == null)
				return;

			valueRingShape.RadiusWidth = _sharedRadius;
			valueRingShape.RadiusHeight = _sharedRadius;

			trackRingShape.RadiusWidth = _sharedRadius;
			trackRingShape.RadiusHeight = _sharedRadius;

			// Apply Width and Heights
			valueRingShape.Width = AdjustedSize;
			valueRingShape.Height = AdjustedSize;

			trackRingShape.Width = AdjustedSize;
			trackRingShape.Height = AdjustedSize;
		}

		/// <summary>
		/// Updates the Strokes for both Rings
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="valueRingShape">The reference to the ValueRing RingShape TemplatePart</param>
		/// <param name="trackRingShape">The reference to the TrackRing RingShape TemplatePart</param>
		private void UpdateRingStrokes(DependencyObject d, RingShape valueRingShape, RingShape trackRingShape)
		{
			if (valueRingShape == null || trackRingShape == null)
				return;

			var normalizedMinAngle = _normalizedMinAngle;
			var normalizedMaxAngle = _normalizedMaxAngle;

			// We get percentage values to use for manipulating how we draw the rings.
			var minPercent = DoubleToPercentage(Minimum, Minimum, Maximum);
			var maxPercent = DoubleToPercentage(Maximum, Minimum, Maximum);
			var percent = Percent;

			// Percent is below or at its Minimum
			if (percent <= minPercent)
			{
				valueRingShape.StrokeThickness = 0;
				trackRingShape.StrokeThickness = _trackRingThickness;
			}
			// Percent is between it's Minimum and its Minimum + 2.0 (between 0% and 2%)
			else if (percent > minPercent && percent < minPercent + 2.0)
			{
				valueRingShape.StrokeThickness = GetThicknessTransition(
					this,
					minPercent,
					percent,
					minPercent + 2.0,
					0.0,
					_valueRingThickness,
					true);

				trackRingShape.StrokeThickness = _trackRingThickness;
			}
			//
			// Percent is at or above its Maximum value
			else if (percent >= maxPercent)
			{
				valueRingShape.StrokeThickness = _valueRingThickness;
				trackRingShape.StrokeThickness = 0;
			}
			//
			// Any percent value between the Minimum + 2 and the Maximum value
			else
			{
				valueRingShape.StrokeThickness = ValueRingThickness;

				if (IsFullCircle(normalizedMinAngle, normalizedMaxAngle) == true)
				{
					if (ValueAngle > (normalizedMaxAngle + 1.0) - (_gapAngle * 2))
					{
						valueRingShape.StrokeThickness = _valueRingThickness;

						trackRingShape.StrokeThickness = GetThicknessTransition(
							this,
							(normalizedMaxAngle + 0.1) - (_gapAngle * 2),
							ValueAngle, (normalizedMaxAngle) - (_gapAngle),
							_trackRingThickness,
							0.0,
							true);
					}
					else
					{
						valueRingShape.StrokeThickness = _valueRingThickness;
						trackRingShape.StrokeThickness = _trackRingThickness;
					}
				}
				else
				{
					if (ValueAngle > (normalizedMaxAngle - _gapAngle))
					{
						valueRingShape.StrokeThickness = _valueRingThickness;
						trackRingShape.StrokeThickness = GetThicknessTransition(
							this,
							(normalizedMaxAngle + 0.1) - (_gapAngle / 2),
							ValueAngle, (normalizedMaxAngle) - (_gapAngle / 2),
							_trackRingThickness,
							0.0,
							true);
					}
					else
					{
						valueRingShape.StrokeThickness = _valueRingThickness;
						trackRingShape.StrokeThickness = _trackRingThickness;
					}
				}
			};
		}

		#endregion

		#region RangeBase Events

		/// <summary>
		/// Occurs when the range value changes.
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="newValue">The new Value</param>
		/// <param name="oldValue">The previous Value</param>
		private void StorageRing_ValueChanged(DependencyObject d, double newValue, double oldValue)
		{
			UpdateValues(d, newValue, oldValue, false, -1.0);
			UpdateRings(d);
		}

		/// <summary>
		/// Occurs when the Minimum value changes.
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="newValue">The new Minimum value</param>
		private void StorageRing_MinimumChanged(DependencyObject d, double newMinimum)
		{
			UpdateRings(d);
		}

		/// <summary>
		/// Occurs when the Maximum value changes.
		/// </summary>
		/// <param name="d">The DependencyObject which holds the DependencyProperty</param>
		/// <param name="newValue">The new Maximum value</param>
		private void StorageRing_MaximumChanged(DependencyObject d, double newMaximum)
		{
			UpdateRings(d);
		}

		#endregion

		#region Conversion methods

		/// <summary>
		/// Calculates and Sets the normalized Min and Max Angles
		/// </summary>
		/// <param name="d">The DependencyObject calling the function</param>
		/// <param name="minAngle">MinAngle in the range from -180 to 180.</param>
		/// <param name="maxAngle">MaxAngle, in the range from -180 to 540.</param>
		private void CalculateAndSetNormalizedAngles(DependencyObject d, double minAngle, double maxAngle)
		{
			StorageRing storageRing = (StorageRing)d;

			var result = CalculateModulus(minAngle, 360);

			if (result >= 180)
				result = result - 360;

			_normalizedMinAngle = result;

			result = CalculateModulus(maxAngle, 360);

			if (result < 180)
				result = result + 360;

			if (result > _normalizedMinAngle + 360)
				result = result - 360;


			_normalizedMaxAngle = result;
		}

		/// <summary>
		/// Calculates the modulus of a number with respect to a divider.
		/// The result is always positive or zero, regardless of the input values.
		/// </summary>
		/// <param name="number">The input number.</param>
		/// <param name="divider">The divider (non-zero).</param>
		/// <returns>The positive modulus result.</returns>
		private static double CalculateModulus(double number, double divider)
		{
			// Calculate the modulus
			var result = number % divider;

			// Ensure the result is positive or zero
			result = result < 0 ? result + divider : result;

			return result;
		}

		/// <summary>
		/// Validates the StartAngle
		/// </summary>
		/// <param name="d">The DependencyObject calling the function</param>
		/// <param name="startAngle">The StartAngle to validate</param>
		private void ValidateStartAngle(DependencyObject d, double startAngle)
		{
			if (startAngle >= _normalizedMaxAngle)
				_validStartAngle = _normalizedMaxAngle;
			else if (startAngle <= _normalizedMinAngle)
				_validStartAngle = _normalizedMinAngle;
			else
				_validStartAngle = startAngle;
		}

		/// <summary>
		/// Calculates an interpolated thickness value based on the provided parameters.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="startValue">The starting value for interpolation.</param>
		/// <param name="value">The current value to interpolate.</param>
		/// <param name="endValue">The ending value for interpolation.</param>
		/// <param name="startThickness">The starting thickness value.</param>
		/// <param name="endThickness">The ending thickness value.</param>
		/// <param name="useEasing">Indicates whether to apply an easing function.</param>
		/// <returns>The interpolated thickness value.</returns>
		private double GetThicknessTransition(DependencyObject d, double startValue, double value, double endValue, double startThickness, double endThickness, bool useEasing)
		{
			// Ensure that value is within the range [startValue, endValue]
			value = Math.Max(startValue, Math.Min(endValue, value));

			// Calculate the interpolation factor (t) between 0 and 1
			var t = (value - startValue) / (endValue - startValue);

			double interpolatedThickness;

			if (useEasing)
			{
				// Apply an easing function (e.g., quadratic ease-in-out)
				var easedT = EaseOutCubic(t);

				// Interpolate the thickness
				interpolatedThickness = startThickness + easedT * (endThickness - startThickness);
			}
			else
			{
				// Interpolate the thickness
				interpolatedThickness = startThickness + t * (endThickness - startThickness);
			}

			return interpolatedThickness;
		}

		/// <summary>
		/// Calculates an interpolated angle based on the provided parameters.
		/// </summary>
		/// <param name="d">The DependencyObject representing the control.</param>
		/// <param name="startValue">The starting value for interpolation.</param>
		/// <param name="value">The current value to interpolate.</param>
		/// <param name="endValue">The ending value for interpolation.</param>
		/// <param name="startAngle">The starting angle value.</param>
		/// <param name="endAngle">The ending angle value.</param>
		/// <param name="valueAngle">The angle corresponding to the current value.</param>
		/// <param name="useEasing">Indicates whether to apply an easing function.</param>
		/// <returns>The interpolated angle value.</returns>
		private double GetAdjustedAngle(DependencyObject d, double startValue, double value, double endValue, double startAngle, double endAngle, double valueAngle, bool useEasing)
		{
			// Ensure that value is within the range [startValue, endValue]
			value = Math.Max(startValue, Math.Min(endValue, value));

			// Calculate the interpolation factor (t) between 0 and 1
			var t = (value - startValue) / (endValue - startValue);

			double interpolatedAngle;

			if (useEasing)
			{
				// Apply an easing function
				var easedT = EaseOutCubic(t);

				// Interpolate the angle
				interpolatedAngle = startAngle + easedT * (endAngle - startAngle);
			}
			else
			{
				// Interpolate the angle
				interpolatedAngle = startAngle + t * (endAngle - startAngle);
			}

			return interpolatedAngle;
		}

		/// <summary>
		/// Converts a value within a specified range to an angle within another specified range.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="minValue">The minimum value of the input range.</param>
		/// <param name="maxValue">The maximum value of the input range.</param>
		/// <param name="minAngle">The minimum angle of the output range (in degrees).</param>
		/// <param name="maxAngle">The maximum angle of the output range (in degrees).</param>
		/// <returns>The converted angle.</returns>
		private double DoubleToAngle(double value, double minValue, double maxValue, double minAngle, double maxAngle)
		{
			// If value is below the Minimum set
			if (value < minValue)
				return minAngle;

			// If value is above the Maximum set
			if (value > maxValue)
				return maxAngle;

			// Calculate the normalized value
			double normalizedValue = (value - minValue) / (maxValue - minValue);

			// Determine the angle range
			double angleRange = MaxAngle - MinAngle;

			// Calculate the actual angle
			double angle = MinAngle + (normalizedValue * angleRange);

			return angle;
		}

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
		/// Calculates the total angle needed to accommodate a gap between two strokes around a circle.
		/// </summary>
		/// <param name="thickness">The Thickness radius to measure.</param>
		/// <param name="radius">The radius of the rings.</param>
		/// <returns>The gap angle (sum of angles for the larger and smaller strokes).</returns>
		private double GapThicknessToAngle(double radius, double thickness)
		{
			if (radius > 0 && thickness > 0)
			{
				// Calculate the maximum number of circles
				double n = Math.PI * (radius / thickness);

				// Calculate the angle between each small circle
				double angle = 360.0 / n;

				return angle;
			}

			return 0;
		}

		/// <summary>
		/// Calculates an adjusted angle using linear interpolation (lerp) between the start and end angles.
		/// </summary>
		/// <param name="startAngle">The initial angle.</param>
		/// <param name="endAngle">The final angle.</param>
		/// <param name="valueAngle">A value between 0 and 1 representing the interpolation factor.</param>
		/// <returns>The adjusted angle based on linear interpolation.</returns>
		private static double GetInterpolatedAngle(double startAngle, double endAngle, double valueAngle)
		{
			// Linear interpolation formula (lerp): GetInterpolatedAngle = (startAngle + valueAngle) * (endAngle - startAngle)
			return (startAngle + valueAngle) * (endAngle - startAngle);
		}

		/// <summary>
		/// Example quadratic ease-in-out function
		/// </summary>
		private double EaseInOutFunction(double t)
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

		/// <summary>
		/// Checks True if the Angle range is a Full Circle
		/// </summary>
		/// <param name="MinAngle"></param>
		/// <param name="MaxAngle"></param>
		/// <returns></returns>
		public static bool IsFullCircle(double MinAngle, double MaxAngle)
		{
			// Calculate the absolute difference between angles
			double angleDifference = Math.Abs(MaxAngle - MinAngle);

			// Check if the angle difference is equal to 360 degrees
			//return angleDifference == 360;

			// Changed to this as suggested by Marcel
			return Math.Abs( angleDifference - 360 ) < Double.Epsilon;
		}

		#endregion
	}
}
