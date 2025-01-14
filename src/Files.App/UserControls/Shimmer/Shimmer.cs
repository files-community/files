// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.UI.Animations;
using CommunityToolkit.WinUI.UI.Animations.Expressions;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Shapes;
using System.Numerics;
using Windows.UI;

namespace Files.App.UserControls
{
	/// <summary>
	/// A generic shimmer control that can be used to construct a beautiful loading effect.
	/// </summary>
	[TemplatePart(Name = PART_Shape, Type = typeof(Rectangle))]
	public partial class Shimmer : Control
	{
		private const float InitialStartPointX = -7.92f;
		private const string PART_Shape = "Shape";

		private Vector2Node? _sizeAnimation;
		private Vector2KeyFrameAnimation? _gradientStartPointAnimation;
		private Vector2KeyFrameAnimation? _gradientEndPointAnimation;
		private CompositionColorGradientStop? _gradientStop1;
		private CompositionColorGradientStop? _gradientStop2;
		private CompositionColorGradientStop? _gradientStop3;
		private CompositionColorGradientStop? _gradientStop4;
		private CompositionRoundedRectangleGeometry? _rectangleGeometry;
		private ShapeVisual? _shapeVisual;
		private CompositionLinearGradientBrush? _shimmerMaskGradient;
		private Border? _shape;

		private bool _initialized;
		private bool _animationStarted;

		public Shimmer()
		{
			DefaultStyleKey = typeof(Shimmer);
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_shape = GetTemplateChild(PART_Shape) as Border;
			if (_initialized is false && TryInitializationResource() && IsActive)
			{
				TryStartAnimation();
			}
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_initialized is false && TryInitializationResource() && IsActive)
			{
				TryStartAnimation();
			}

			ActualThemeChanged += OnActualThemeChanged;
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			ActualThemeChanged -= OnActualThemeChanged;
			StopAnimation();

			if (_initialized && _shape != null)
			{
				ElementCompositionPreview.SetElementChildVisual(_shape, null);

				_rectangleGeometry!.Dispose();
				_shapeVisual!.Dispose();
				_shimmerMaskGradient!.Dispose();
				_gradientStop1!.Dispose();
				_gradientStop2!.Dispose();
				_gradientStop3!.Dispose();
				_gradientStop4!.Dispose();

				_initialized = false;
			}
		}

		private void OnActualThemeChanged(FrameworkElement sender, object args)
		{
			if (_initialized is false)
			{
				return;
			}

			SetGradientStopColorsByTheme();
		}

		private bool TryInitializationResource()
		{
			if (_initialized)
			{
				return true;
			}

			if (_shape is null || IsLoaded is false)
			{
				return false;
			}

			var compositor = _shape.GetVisual().Compositor;

			_rectangleGeometry = compositor.CreateRoundedRectangleGeometry();
			_shapeVisual = compositor.CreateShapeVisual();
			_shimmerMaskGradient = compositor.CreateLinearGradientBrush();
			_gradientStop1 = compositor.CreateColorGradientStop();
			_gradientStop2 = compositor.CreateColorGradientStop();
			_gradientStop3 = compositor.CreateColorGradientStop();
			_gradientStop4 = compositor.CreateColorGradientStop();
			SetGradientAndStops();
			SetGradientStopColorsByTheme();
			_rectangleGeometry.CornerRadius = new Vector2((float)CornerRadius.TopLeft);
			var spriteShape = compositor.CreateSpriteShape(_rectangleGeometry);
			spriteShape.FillBrush = _shimmerMaskGradient;
			_shapeVisual.Shapes.Add(spriteShape);
			ElementCompositionPreview.SetElementChildVisual(_shape, _shapeVisual);

			_initialized = true;
			return true;
		}

		private void SetGradientAndStops()
		{
			_shimmerMaskGradient!.StartPoint = new Vector2(InitialStartPointX, 0.0f);
			_shimmerMaskGradient.EndPoint = new Vector2(0.0f, 1.0f); //Vector2.One

			_gradientStop1!.Offset = 0.273f;
			_gradientStop2!.Offset = 0.436f;
			_gradientStop3!.Offset = 0.482f;
			_gradientStop4!.Offset = 0.643f;

			_shimmerMaskGradient.ColorStops.Add(_gradientStop1);
			_shimmerMaskGradient.ColorStops.Add(_gradientStop2);
			_shimmerMaskGradient.ColorStops.Add(_gradientStop3);
			_shimmerMaskGradient.ColorStops.Add(_gradientStop4);
		}

		private void SetGradientStopColorsByTheme()
		{
			switch (ActualTheme)
			{
				case ElementTheme.Default:
				case ElementTheme.Dark:
					_gradientStop1!.Color = Color.FromArgb((byte)(255 * 6.05 / 100), 255, 255, 255);
					_gradientStop2!.Color = Color.FromArgb((byte)(255 * 3.26 / 100), 255, 255, 255);
					_gradientStop3!.Color = Color.FromArgb((byte)(255 * 3.26 / 100), 255, 255, 255);
					_gradientStop4!.Color = Color.FromArgb((byte)(255 * 6.05 / 100), 255, 255, 255);
					break;
				case ElementTheme.Light:
					_gradientStop1!.Color = Color.FromArgb((byte)(255 * 5.37 / 100), 0, 0, 0);
					_gradientStop2!.Color = Color.FromArgb((byte)(255 * 2.89 / 100), 0, 0, 0);
					_gradientStop3!.Color = Color.FromArgb((byte)(255 * 2.89 / 100), 0, 0, 0);
					_gradientStop4!.Color = Color.FromArgb((byte)(255 * 5.37 / 100), 0, 0, 0);
					break;
			}
		}

		private void TryStartAnimation()
		{
			if (_animationStarted || _initialized is false || _shape is null || _shapeVisual is null || _rectangleGeometry is null)
			{
				return;
			}

			var rootVisual = _shape.GetVisual();
			_sizeAnimation = rootVisual.GetReference().Size;
			_shapeVisual.StartAnimation(nameof(ShapeVisual.Size), _sizeAnimation);
			_rectangleGeometry.StartAnimation(nameof(CompositionRoundedRectangleGeometry.Size), _sizeAnimation);

			_gradientStartPointAnimation = rootVisual.Compositor.CreateVector2KeyFrameAnimation();
			_gradientStartPointAnimation.Duration = Duration;
			_gradientStartPointAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
			_gradientStartPointAnimation.InsertKeyFrame(0.0f, new Vector2(InitialStartPointX, 0.0f));
			_gradientStartPointAnimation.InsertKeyFrame(1.0f, Vector2.Zero);
			_shimmerMaskGradient!.StartAnimation(nameof(CompositionLinearGradientBrush.StartPoint), _gradientStartPointAnimation);

			_gradientEndPointAnimation = rootVisual.Compositor.CreateVector2KeyFrameAnimation();
			_gradientEndPointAnimation.Duration = Duration;
			_gradientEndPointAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
			_gradientEndPointAnimation.InsertKeyFrame(0.0f, new Vector2(1.0f, 0.0f)); //Vector2.One
			_gradientEndPointAnimation.InsertKeyFrame(1.0f, new Vector2(-InitialStartPointX, 1.0f));
			_shimmerMaskGradient.StartAnimation(nameof(CompositionLinearGradientBrush.EndPoint), _gradientEndPointAnimation);

			_animationStarted = true;
		}

		private void StopAnimation()
		{
			if (_animationStarted is false)
			{
				return;
			}

			_shapeVisual!.StopAnimation(nameof(ShapeVisual.Size));
			_rectangleGeometry!.StopAnimation(nameof(CompositionRoundedRectangleGeometry.Size));
			_shimmerMaskGradient!.StopAnimation(nameof(CompositionLinearGradientBrush.StartPoint));
			_shimmerMaskGradient.StopAnimation(nameof(CompositionLinearGradientBrush.EndPoint));

			_sizeAnimation!.Dispose();
			_gradientStartPointAnimation!.Dispose();
			_gradientEndPointAnimation!.Dispose();
			_animationStarted = false;
		}
	}
}
