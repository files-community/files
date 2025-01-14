// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.Foundation;

namespace Files.App.Controls.Primitives
{
	/// <summary>
	/// Represents primitive Path shape for drawing a circular or elliptical ring.
	/// </summary>
	public partial class RingShape : Path
	{
		#region Fields and Constructors

		// Fields

		private bool			_isUpdating;             // Is True when path is updating
		private bool			_isCircle;               // When True, Width and Height are equalized
		private Size			_equalSize;              // Calculated where Width and Height are equal
		private double			_equalRadius;            // Calculated where RadiusWidth and RadiusHeight are equal
		private Point			_centerPoint;            // Center Point within Width and Height bounds
		private double			_normalizedMinAngle;     // Normalized MinAngle between -180 and 540
		private double			_normalizedMaxAngle;     // Normalized MaxAngle between 0 and 360
		private double			_validStartAngle;        // The validated StartAngle
		private double			_validEndAngle;          // The validated EndAngle
		private double			_radiusWidth;            // The radius Width
		private double			_radiusHeight;           // The radius Height
		private SweepDirection	_sweepDirection;         // The SweepDirection

		// Constants

		private const double DegreesToRadians = Math.PI / 180;

		// Constructor

		/// <summary>
		/// Initializes an instance of the <see cref="RingShape" /> class.
		/// </summary>
		public RingShape()
		{
			SizeChanged += RingShape_SizeChanged;
			RegisterPropertyChangedCallback(StrokeThicknessProperty, OnStrokeThicknessChanged);
		}

		#endregion

		#region PropertyChanged Events

		private void StartAngleChanged()
		{
			BeginUpdate();
			ValidateAngle(this, StartAngle, true);
			EndUpdate();
		}

		private void EndAngleChanged()
		{
			BeginUpdate();
			ValidateAngle(this, EndAngle, false);
			EndUpdate();
		}

		private void IsCircleChanged()
		{
			BeginUpdate();
			_isCircle = IsCircle;
			EndUpdate();
		}

		private void RadiusWidthChanged()
		{
			BeginUpdate();
			AdjustRadiusWidth(this, RadiusWidth, StrokeThickness);
			EndUpdate();
		}

		private void RadiusHeightChanged()
		{
			BeginUpdate();
			AdjustRadiusHeight(this, RadiusHeight, StrokeThickness);
			EndUpdate();
		}

		private void RingShape_SizeChanged(object obj, SizeChangedEventArgs e)
		{
			BeginUpdate();
			EndUpdate();
		}

		private void OnStrokeThicknessChanged(DependencyObject d, DependencyProperty dp)
		{
			BeginUpdate();
			EndUpdate();
		}

		private void MinMaxAngleChanged(bool isMax)
		{
			BeginUpdate();
			CalculateAndSetNormalizedAngles(this, MinAngle, MaxAngle);
			EndUpdate();
		}

		private void SweepDirectionChanged()
		{
			BeginUpdate();
			_sweepDirection = SweepDirection;
			EndUpdate();
		}

		#endregion

		#region RingShape Path Updates

		/// <summary>
		/// Suspends path updates until EndUpdate is called
		/// </summary>
		public void BeginUpdate()
		{
			_isUpdating = true;
		}

		/// <summary>
		/// Resumes immediate path updates every time a component property value changes. Updates the path
		/// </summary>
		public void EndUpdate()
		{
			_isUpdating = false;
			UpdatePath();
		}

		private void UpdatePath()
		{
			if (_isUpdating ||
				ActualWidth <= 0 || ActualHeight <= 0 ||
				_radiusWidth <= 0 || _radiusHeight <= 0)
				return;

			UpdateSizeAndStroke(this);

			var startAngle = _validStartAngle;
			var endAngle = _validEndAngle;

			// If the ring is closed and complete
			if (endAngle >= startAngle + 360)
			{
				Data = DrawEllipse(_isCircle, _centerPoint, _equalRadius, _radiusWidth, _radiusHeight);
			}
			else
			{
				this.InvalidateArrange();
				this.Data = DrawArc(this, _sweepDirection, _isCircle, _centerPoint, startAngle, endAngle, _equalRadius, _radiusWidth, _radiusHeight);
			}
		}

		#endregion

		#region Drawing Updates

		/// <summary>
		/// Updates sizes, center point and radii
		/// </summary>
		/// <param name="d">The DependencyObject calling the function</param>
		public void UpdateSizeAndStroke(DependencyObject d)
		{
			RingShape ringShape = (RingShape)d;

			AdjustRadiusWidth( ringShape , ringShape.RadiusWidth , ringShape.StrokeThickness );
			AdjustRadiusHeight( ringShape , ringShape.RadiusHeight , ringShape.StrokeThickness );

			_equalSize = CalculateEqualSize( new Size( ringShape.Width , ringShape.Height ) , ringShape.StrokeThickness );
			_equalRadius = CalculateEqualRadius( ringShape , ringShape.RadiusWidth , ringShape.RadiusHeight , ringShape.StrokeThickness );

			_centerPoint = new Point( ringShape.Width / 2 , ringShape.Height / 2 );
			ringShape.Center = _centerPoint;

			CalculateAndSetNormalizedAngles( ringShape , ringShape.MinAngle , ringShape.MaxAngle );

			ValidateAngle( ringShape , ringShape.StartAngle , true );
			ValidateAngle( ringShape , ringShape.EndAngle , false );
		}

		private static EllipseGeometry DrawEllipse(bool IsCircle, Point Center, double EqualRadius, double RadiusWidth, double RadiusHeight)
		{
			EllipseGeometry eg;

			if ( IsCircle == true )
			{
				eg = new EllipseGeometry
				{
					Center = Center ,
					RadiusX = EqualRadius ,
					RadiusY = EqualRadius ,
				};
			}
			else
			{
				eg = new EllipseGeometry
				{
					Center = Center ,
					RadiusX = RadiusWidth ,
					RadiusY = RadiusHeight ,
				};
			}

			return eg;
		}

		private static PathGeometry DrawArc(RingShape RingShape, SweepDirection SweepDirection, bool IsCircle, Point Center, double StartAngle, double EndAngle, double EqualRadius, double RadiusWidth, double RadiusHeight)
		{
			var pathGeometry = new PathGeometry();
			var pathFigure = new PathFigure();
			pathFigure.IsClosed = false;
			pathFigure.IsFilled = false;

			var newCenter = Center;

			var arcSegment = new ArcSegment();

			if ( IsCircle == true )
			{
				var radius = EqualRadius;

				RingShape.ActualRadiusWidth = radius;
				RingShape.ActualRadiusHeight = radius;

				// Start Point
				pathFigure.StartPoint = ArcStartPoint( SweepDirection , newCenter, StartAngle, radius, radius);


				// Arc Segment and End Point
				arcSegment = CreateArcSegment( SweepDirection , newCenter , StartAngle , EndAngle , radius , radius );
			}
			else
			{
				var radiusWidth = RadiusWidth;
				var radiusHeight = RadiusHeight;

				RingShape.ActualRadiusWidth = radiusWidth;
				RingShape.ActualRadiusHeight = radiusHeight;

				// Start Point
				pathFigure.StartPoint = ArcStartPoint( SweepDirection , newCenter , StartAngle , radiusWidth , radiusHeight );


				// Arc Segment and End Point
				arcSegment = CreateArcSegment( SweepDirection , newCenter , StartAngle , EndAngle , radiusWidth , radiusHeight );
			}

			pathFigure.Segments.Add( arcSegment );
			pathGeometry.Figures.Add( pathFigure );

			return pathGeometry;
		}

		private static Point ArcStartPoint(SweepDirection SweepDirection , Point Center, double StartAngle , double RadiusWidth, double RadiusHeight)
		{
			var finalPoint = new Point();

			// Counterclockwise
			if ( SweepDirection == SweepDirection.Counterclockwise )
			{
				finalPoint =
				new Point(
					Center.X - Math.Sin( StartAngle * DegreesToRadians ) * RadiusWidth ,
					Center.Y - Math.Cos( StartAngle * DegreesToRadians ) * RadiusHeight );
			}
			// Clockwise
			else
			{
				finalPoint =
				new Point(
					Center.X + Math.Sin( StartAngle * DegreesToRadians ) * RadiusWidth ,
					Center.Y - Math.Cos( StartAngle * DegreesToRadians ) * RadiusHeight );
			}

			return finalPoint;
		}

		private static ArcSegment CreateArcSegment(SweepDirection SweepDirection , Point Center , double StartAngle, double EndAngle , double RadiusWidth , double RadiusHeight)
		{ 
			var finalArcSegment = new ArcSegment();

			// Counterclockwise
			if ( SweepDirection == SweepDirection.Counterclockwise )
			{
				finalArcSegment.Point =
					new Point(
						Center.X - Math.Sin( EndAngle * DegreesToRadians ) * RadiusWidth ,
						Center.Y - Math.Cos( EndAngle * DegreesToRadians ) * RadiusHeight );

				if ( EndAngle < StartAngle )
				{
					finalArcSegment.IsLargeArc = ( EndAngle - StartAngle ) <= -180.0;
					finalArcSegment.SweepDirection = SweepDirection.Clockwise;
				}
				else
				{
					finalArcSegment.IsLargeArc = ( EndAngle - StartAngle ) >= 180.0;
					finalArcSegment.SweepDirection = SweepDirection.Counterclockwise;
				}
			}
			// Clockwise
			else
			{
				finalArcSegment.Point =
					new Point(
						Center.X + Math.Sin( EndAngle * DegreesToRadians ) * RadiusWidth ,
						Center.Y - Math.Cos( EndAngle * DegreesToRadians ) * RadiusHeight );
				//ArcSegment.IsLargeArc = ( EndAngle - StartAngle ) >= 180.0;
				if ( EndAngle < StartAngle )
				{
					finalArcSegment.IsLargeArc = ( EndAngle - StartAngle ) <= -180.0;
					finalArcSegment.SweepDirection = SweepDirection.Counterclockwise;
				}
				else
				{
					finalArcSegment.IsLargeArc = ( EndAngle - StartAngle ) >= 180.0;
					finalArcSegment.SweepDirection = SweepDirection.Clockwise;
				}
			}
			finalArcSegment.Size = new Size( RadiusWidth , RadiusHeight );

			return finalArcSegment;
		}

		#endregion

		#region Value Calculations

		private static Size CalculateEqualSize(Size size, double strokeThickness)
		{
			double adjWidth = size.Width;
			double adjHeight = size.Height;

			var smaller = Math.Min(adjWidth, adjHeight);

			if (smaller > strokeThickness * 2)
			{
				return new Size(smaller, smaller);
			}
			else
			{
				return new Size(strokeThickness * 2, strokeThickness * 2);
			}
		}

		private static double CalculateEqualRadius(DependencyObject d, double radiusWidth, double radiusHeight, double strokeThickness)
		{
			RingShape ringShape = (RingShape)d;

			double adjWidth = radiusWidth;
			double adjHeight = radiusHeight;

			var smaller = Math.Min(adjWidth, adjHeight);

			if (smaller <= strokeThickness)
			{
				return strokeThickness;
			}
			else if (smaller >= ((ringShape._equalSize.Width / 2) - (strokeThickness / 2)))
			{
				return (ringShape._equalSize.Width / 2) - (strokeThickness / 2);
			}
			else
			{
				return smaller;
			}
		}

		private static Point CalculateCenter(double Width, double Height)
		{
			Point calculatedCenter = new Point((Width / 2.0), (Height / 2.0));

			return calculatedCenter;
		}

		private static void CalculateAndSetNormalizedAngles(DependencyObject d, double minAngle, double maxAngle)
		{
			RingShape ringShape = (RingShape)d;

			var result = CalculateModulus(minAngle, 360);

			if (result >= 180)
				result = result - 360;

			ringShape._normalizedMinAngle = result;

			result = CalculateModulus(maxAngle, 360);

			if (result < 180)
				result = result + 360;

			if (result > ringShape._normalizedMinAngle + 360)
				result = result - 360;


			ringShape._normalizedMaxAngle = result;
		}

		private static double CalculateModulus(double number, double divider)
		{
			// Calculate the modulus
			var result = number % divider;

			// Ensure the result is positive or zero
			result = result < 0 ? result + divider : result;

			return result;
		}

		private void ValidateAngle(DependencyObject d, double angle, bool isStart)
		{
			RingShape ringShape = (RingShape)d;

			if (angle >= _normalizedMaxAngle)
			{
				if (isStart == true)
				{
					_validStartAngle = _normalizedMaxAngle;
				}
				else
				{
					_validEndAngle = _normalizedMaxAngle;
				}
			}
			else if (angle <= _normalizedMinAngle)
			{
				if (isStart == true)
				{
					_validStartAngle = _normalizedMinAngle;
				}
				else
				{
					_validEndAngle = _normalizedMinAngle;
				}
			}
			else
			{
				if (isStart == true)
				{
					_validStartAngle = angle;
				}
				else
				{
					_validEndAngle = angle;
				}
			}
		}

		private void AdjustRadiusWidth(DependencyObject d, double radiusWidth, double strokeThickness)
		{
			RingShape ringShape = (RingShape)d;

			var maxValue = (ringShape.Width / 2) - (ringShape.StrokeThickness / 2);
			var threshold = strokeThickness;

			if (radiusWidth >= maxValue)
			{
				ringShape._radiusWidth = maxValue;
			}
			else if (radiusWidth <= maxValue && radiusWidth >= threshold)
			{
				ringShape._radiusWidth = radiusWidth;
			}
			else
			{
				ringShape._radiusWidth = threshold;
			}
		}

		private void AdjustRadiusHeight(DependencyObject d, double radiusHeight, double strokeThickness)
		{
			RingShape ringShape = (RingShape)d;

			var maxValue = (ringShape.Height / 2) - (ringShape.StrokeThickness / 2);
			var threshold = strokeThickness;

			if (radiusHeight >= maxValue)
			{
				ringShape._radiusHeight = maxValue;
			}
			else if (radiusHeight <= maxValue && radiusHeight >= threshold)
			{
				ringShape._radiusHeight = radiusHeight;
			}
			else
			{
				ringShape._radiusHeight = threshold;
			}
		}

		#endregion
	}
}
