// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Files.App.Helpers
{
	internal sealed class NavigationInteractionTracker : IDisposable
	{
		private static IRealTimeLayoutService RealTimeLayoutService => Ioc.Default.GetRequiredService<IRealTimeLayoutService>();

		private static int RePos => RealTimeLayoutService.FlowDirection is FlowDirection.LeftToRight ? 1 : -1;
		private static (float Min, float Max) RePosB => RealTimeLayoutService.FlowDirection is FlowDirection.LeftToRight ? (-96f, 0f) : (0f, 96f);
		private static (float Min, float Max) RePosF => RealTimeLayoutService.FlowDirection is FlowDirection.LeftToRight ? (0f, 96f) : (-96f, 0f);

		public bool CanNavigateForward
		{
			get
			{
				_props.TryGetBoolean(nameof(CanNavigateForward), out bool val);
				return val;
			}
			set
			{
				if (!_disposed)
				{
					_props.InsertBoolean(nameof(CanNavigateForward), value);
					if (RePos == 1)
						_tracker.MaxPosition = new(value ? 96 : 0);
					else
						_tracker.MinPosition = new(value ? -96 : 0);

				}
			}
		}

		public bool CanNavigateBackward
		{
			get
			{
				_props.TryGetBoolean(nameof(CanNavigateBackward), out bool val);
				return val;
			}
			set
			{
				if (!_disposed)
				{
					_props.InsertBoolean(nameof(CanNavigateBackward), value);
					if (RePos == 1)
						_tracker.MinPosition = new(value ? -96 : 0);
					else
						_tracker.MaxPosition = new(value ? 96 : 0);
				}
			}
		}

		private UIElement _rootElement;
		private UIElement _backIcon;
		private UIElement _forwardIcon;

		private PointerEventHandler _pointerPressedHandler;

		private Visual _rootVisual;
		private Visual _backVisual;
		private Visual _forwardVisual;

		private InteractionTracker _tracker;
		private VisualInteractionSource _source;
		private InteractionTrackerOwner _trackerOwner;
		private CompositionPropertySet _props;

		public event EventHandler<OverscrollNavigationEventArgs>? NavigationRequested;

		private bool _disposed;

		public NavigationInteractionTracker(UIElement rootElement, UIElement backIcon, UIElement forwardIcon)
		{
			_rootElement = rootElement;
			_backIcon = backIcon;
			_forwardIcon = forwardIcon;

			ElementCompositionPreview.SetIsTranslationEnabled(_backIcon, true);
			ElementCompositionPreview.SetIsTranslationEnabled(_forwardIcon, true);
			_rootVisual = ElementCompositionPreview.GetElementVisual(_rootElement);
			_backVisual = ElementCompositionPreview.GetElementVisual(_backIcon);
			_forwardVisual = ElementCompositionPreview.GetElementVisual(_forwardIcon);

			SetupInteractionTracker();

			_props = _rootVisual.Compositor.CreatePropertySet();
			CanNavigateBackward = false;
			CanNavigateForward = false;

			SetupAnimations();

			_pointerPressedHandler = new(PointerPressed);
			_rootElement.AddHandler(UIElement.PointerPressedEvent, _pointerPressedHandler, true);

			RealTimeLayoutService.FlowDirectionChanged += RealTimeLayoutService_FlowDirectionChanged;
		}

		private void RealTimeLayoutService_FlowDirectionChanged(object? sender, FlowDirection e)
		{
			var canBack = CanNavigateBackward;
			var canForward = CanNavigateForward;

			CanNavigateBackward = canBack;
			CanNavigateForward = canForward;

			SetupAnimations();
		}

		[MemberNotNull(nameof(_tracker), nameof(_source), nameof(_trackerOwner))]
		private void SetupInteractionTracker()
		{
			var compositor = _rootVisual.Compositor;

			_trackerOwner = new(this);
			_tracker = InteractionTracker.CreateWithOwner(compositor, _trackerOwner);
			_tracker.MinPosition = new Vector3(-96f);
			_tracker.MaxPosition = new Vector3(96f);

			_source = VisualInteractionSource.Create(_rootVisual);
			_source.ManipulationRedirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadOnly;
			_source.PositionXSourceMode = InteractionSourceMode.EnabledWithoutInertia;
			_source.PositionXChainingMode = InteractionChainingMode.Always;
			_source.PositionYSourceMode = InteractionSourceMode.Disabled;
			_tracker.InteractionSources.Add(_source);
		}

		private void SetupAnimations()
		{
			var compositor = _rootVisual.Compositor;

			var backResistance = CreateResistanceCondition(RePosB.Min, RePosB.Max);
			var forwardResistance = CreateResistanceCondition(RePosF.Min, RePosF.Max);
			List<CompositionConditionalValue> conditionalValues = [backResistance, forwardResistance];
			_source.ConfigureDeltaPositionXModifiers(conditionalValues);

			var backAnim = compositor.CreateExpressionAnimation($"(clamp(tracker.Position.X, {RePosB.Min}, {RePosB.Max}) * 2 * ({-RePos})) - 48");
			backAnim.SetReferenceParameter("tracker", _tracker);
			backAnim.SetReferenceParameter("props", _props);
			_backVisual.StartAnimation("Translation.X", backAnim);

			var forwardAnim = compositor.CreateExpressionAnimation($"(clamp(tracker.Position.X, {RePosF.Min}, {RePosF.Max}) * 2 * ({-RePos})) + 48");
			forwardAnim.SetReferenceParameter("tracker", _tracker);
			forwardAnim.SetReferenceParameter("props", _props);
			_forwardVisual.StartAnimation("Translation.X", forwardAnim);
		}

		private void PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				_source.TryRedirectForManipulation(e.GetCurrentPoint(_rootElement));
			}
		}

		private CompositionConditionalValue CreateResistanceCondition(float minValue, float maxValue)
		{
			var compositor = _rootVisual.Compositor;

			var resistance = CompositionConditionalValue.Create(compositor);
			var resistanceCondition = compositor.CreateExpressionAnimation($"tracker.Position.X > {minValue} && tracker.Position.X < {maxValue}");
			resistanceCondition.SetReferenceParameter("tracker", _tracker);
			var resistanceValue = compositor.CreateExpressionAnimation($"source.DeltaPosition.X * (1 - sqrt(1 - square((tracker.Position.X / {minValue + maxValue}) - 1)))");
			resistanceValue.SetReferenceParameter("source", _source);
			resistanceValue.SetReferenceParameter("tracker", _tracker);
			resistance.Condition = resistanceCondition;
			resistance.Value = resistanceValue;

			return resistance;
		}

		~NavigationInteractionTracker()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_disposed = true;

			_rootElement.RemoveHandler(UIElement.PointerPressedEvent, _pointerPressedHandler);
			_backVisual.StopAnimation("Translation.X");
			_forwardVisual.StopAnimation("Translation.X");
			_tracker.Dispose();
			_source.Dispose();
			_props.Dispose();
			RealTimeLayoutService.FlowDirectionChanged -= RealTimeLayoutService_FlowDirectionChanged;

			GC.SuppressFinalize(this);
		}

		private sealed class InteractionTrackerOwner : IInteractionTrackerOwner
		{
			private NavigationInteractionTracker _parent;
			private bool _shouldBounceBack;
			private bool _shouldAnimate = true;
			private Vector3KeyFrameAnimation _scaleAnimation;
			private SpringVector3NaturalMotionAnimation _returnAnimation;

			public InteractionTrackerOwner(NavigationInteractionTracker parent)
			{
				_parent = parent;

				var compositor = _parent._rootVisual.Compositor;
				_scaleAnimation = compositor.CreateVector3KeyFrameAnimation();
				_scaleAnimation.InsertKeyFrame(0.5f, new(1.3f));
				_scaleAnimation.InsertKeyFrame(1f, new(1f));
				_scaleAnimation.Duration = TimeSpan.FromMilliseconds(275);

				_returnAnimation = compositor.CreateSpringVector3Animation();
				_returnAnimation.FinalValue = new(0f);
				_returnAnimation.DampingRatio = 1f;
			}

			public void IdleStateEntered(InteractionTracker sender, InteractionTrackerIdleStateEnteredArgs args)
			{
				if (!_shouldBounceBack)
					return;

				if (Math.Abs(sender.Position.X) > 64)
				{
					_parent._tracker.TryUpdatePosition(new(0f));

					EventHandler<OverscrollNavigationEventArgs>? navEvent = _parent.NavigationRequested;
					if (navEvent is not null)
					{
						if (sender.Position.X * RePos > 0 && _parent.CanNavigateForward)
						{
							navEvent(_parent, OverscrollNavigationEventArgs.Forward);
						}
						else if (sender.Position.X * RePos < 0 && _parent.CanNavigateBackward)
						{
							navEvent(_parent, OverscrollNavigationEventArgs.Back);
						}
					}
				}
				else
				{					
					_parent._tracker.TryUpdatePositionWithAnimation(_returnAnimation);
				}
				_shouldBounceBack = false;
				_shouldAnimate = true;
			}

			public void InteractingStateEntered(InteractionTracker sender, InteractionTrackerInteractingStateEnteredArgs args)
			{
				_shouldBounceBack = true;
			}

			public void ValuesChanged(InteractionTracker sender, InteractionTrackerValuesChangedArgs args)
			{
				if (!_shouldAnimate)
					return;

				if (args.Position.X * RePos <= -64)
				{
					_parent._backVisual.StartAnimation("Scale", _scaleAnimation);
					_shouldAnimate = false;
				}
				else if (args.Position.X * RePos >= 64)
				{
					_parent._forwardVisual.StartAnimation("Scale", _scaleAnimation);
					_shouldAnimate = false;
				}

			}

			// required to implement IInteractionTrackerOwner
			public void CustomAnimationStateEntered(InteractionTracker sender, InteractionTrackerCustomAnimationStateEnteredArgs args) { }
			public void InertiaStateEntered(InteractionTracker sender, InteractionTrackerInertiaStateEnteredArgs args) { }
			public void RequestIgnored(InteractionTracker sender, InteractionTrackerRequestIgnoredArgs args) { }
		}
	}

	public enum OverscrollNavigationEventArgs
	{
		Back,
		Forward
	}
}
