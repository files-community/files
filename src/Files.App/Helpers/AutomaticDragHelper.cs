using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Win32;

namespace Files.App.Helpers
{
	// https://github.com/microsoft/microsoft-ui-xaml/blob/winui3/release/1.6.3/src/dxaml/xcp/dxaml/lib/AutomaticDragHelper.cpp
	public class AutomaticDragHelper : DependencyObject
	{
		// Added attached dependency property to unregister event handlers
		public static AutomaticDragHelper GetDragHelper(DependencyObject obj)
		{
			return (AutomaticDragHelper)obj.GetValue(DragHelperProperty);
		}

		public static void SetDragHelper(DependencyObject obj, AutomaticDragHelper value)
		{
			obj.SetValue(DragHelperProperty, value);
		}

		public static readonly DependencyProperty DragHelperProperty =
			DependencyProperty.RegisterAttached("DragHelper", typeof(AutomaticDragHelper), typeof(AutomaticDragHelper), new PropertyMetadata(null, OnDragHelperChanged));

		private static void OnDragHelperChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is AutomaticDragHelper old)
				old.StopDetectingDrag();
		}

		// The standard Windows mouse drag box size is defined by SM_CXDRAG and SM_CYDRAG.
		// UIElement uses the standard box size with dimensions multiplied by this constant.
		// This arrangement is in place as accidentally triggering a drag was deemed too easy while
		// selecting several items with the mouse in quick succession.
		private const double UIELEMENT_MOUSE_DRAG_THRESHOLD_MULTIPLIER = 2.0;
		private readonly UIElement m_pOwnerNoRef;
		private readonly bool m_shouldAddInputHandlers;
		private bool m_isCheckingForMouseDrag;
		private Point m_lastMouseRightButtonDownPosition;
		private bool m_dragDropPointerPressedToken, m_dragDropPointerMovedToken, m_dragDropPointerReleasedToken, m_dragDropPointerCaptureLostToken/*, m_dragDropHoldingToken*/;
		//private PointerPoint m_spPointerPoint;
		//private Pointer m_spPointer;
		//private bool m_isHoldingCompleted;
		private bool m_isRightButtonPressed;

		public AutomaticDragHelper(UIElement pUIElement, bool shouldAddInputHandlers)
		{
			m_pOwnerNoRef = pUIElement;
			m_shouldAddInputHandlers = shouldAddInputHandlers;
		}

		// Begin tracking the mouse cursor in order to fire a drag start if the pointer
		// moves a certain distance away from m_lastMouseRightButtonDownPosition.
		public void BeginCheckingForMouseDrag(Pointer pPointer)
		{
			bool captured = m_pOwnerNoRef.CapturePointer(pPointer);

			m_isCheckingForMouseDrag = !!captured;
		}

		// Stop tracking the mouse cursor.
		public void StopCheckingForMouseDrag(Pointer pPointer)
		{
			// Do not call ReleasePointerCapture() more times than we called CapturePointer()
			if (m_isCheckingForMouseDrag)
			{
				m_isCheckingForMouseDrag = false;

				m_pOwnerNoRef.ReleasePointerCapture(pPointer);
			}
		}

		// Return true if we're tracking the mouse and newMousePosition is outside the drag
		// rectangle centered at m_lastMouseRightButtonDownPosition (see IsOutsideDragRectangle).
		bool ShouldStartMouseDrag(Point newMousePosition)
		{
			return m_isCheckingForMouseDrag && IsOutsideDragRectangle(newMousePosition, m_lastMouseRightButtonDownPosition);
		}

		// Returns true if testPoint is outside of the rectangle
		// defined by the SM_CXDRAG and SM_CYDRAG system metrics and
		// dragRectangleCenter.
		bool IsOutsideDragRectangle(Point testPoint, Point dragRectangleCenter)
		{
			double dx = Math.Abs(testPoint.X - dragRectangleCenter.X);
			double dy = Math.Abs(testPoint.Y - dragRectangleCenter.Y);

			double maxDx = PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CXDRAG);
			double maxDy = PInvoke.GetSystemMetrics(Windows.Win32.UI.WindowsAndMessaging.SYSTEM_METRICS_INDEX.SM_CYDRAG);

			maxDx *= UIELEMENT_MOUSE_DRAG_THRESHOLD_MULTIPLIER;
			maxDy *= UIELEMENT_MOUSE_DRAG_THRESHOLD_MULTIPLIER;

			return (dx > maxDx || dy > maxDy);
		}

		public void StartDetectingDrag()
		{
			if (m_shouldAddInputHandlers && !m_dragDropPointerPressedToken)
			{
				m_pOwnerNoRef.PointerPressed += HandlePointerPressedEventArgs;
				m_dragDropPointerPressedToken = true;
			}
		}

		public void StopDetectingDrag()
		{
			if (m_dragDropPointerPressedToken)
			{
				m_pOwnerNoRef.PointerPressed -= HandlePointerPressedEventArgs;
				// zero out the token;
				m_dragDropPointerPressedToken = false;
			}
		}

		public void RegisterDragPointerEvents()
		{
			if (m_shouldAddInputHandlers)
			{
				// Hookup pointer events so we can catch and handle it for drag and drop.
				if (!m_dragDropPointerMovedToken)
				{
					m_pOwnerNoRef.PointerMoved += HandlePointerMovedEventArgs;
					m_dragDropPointerMovedToken = true;
				}

				if (!m_dragDropPointerReleasedToken)
				{
					m_pOwnerNoRef.PointerReleased += HandlePointerReleasedEventArgs;
					m_dragDropPointerReleasedToken = true;
				}

				if (!m_dragDropPointerCaptureLostToken)
				{
					m_pOwnerNoRef.PointerCaptureLost += HandlePointerCaptureLostEventArgs;
					m_dragDropPointerCaptureLostToken = true;
				}
			}
		}

		public void HandlePointerPressedEventArgs(object sender, PointerRoutedEventArgs pArgs)
		{
			Pointer spPointer;
			PointerDeviceType pointerDeviceType = PointerDeviceType.Touch;
			PointerPoint spPointerPoint;

			//m_spPointerPoint = null;
			//m_spPointer = null;
			//m_isHoldingCompleted = false;

			spPointer = pArgs.Pointer;
			pointerDeviceType = spPointer.PointerDeviceType;

			spPointerPoint = pArgs.GetCurrentPoint(m_pOwnerNoRef);

			// Check if this is a mouse button down.
			if (pointerDeviceType == PointerDeviceType.Mouse || pointerDeviceType == PointerDeviceType.Pen)
			{
				// Mouse button down.
				PointerPointProperties spPointerProperties;
				bool isRightButtonPressed = false;

				spPointerProperties = spPointerPoint.Properties;
				isRightButtonPressed = spPointerProperties.IsRightButtonPressed;

				// If the right mouse button was the one pressed...
				if (!m_isRightButtonPressed && isRightButtonPressed)
				{
					m_isRightButtonPressed = true;
					// Start listening for a mouse drag gesture
					m_lastMouseRightButtonDownPosition = spPointerPoint.Position;
					BeginCheckingForMouseDrag(spPointer);

					RegisterDragPointerEvents();
				}
			}
			/*else
			{
				m_spPointerPoint = spPointerPoint;
				m_spPointer = spPointer;

				if (m_shouldAddInputHandlers && !m_dragDropHoldingToken)
				{
					// Touch input occurs, subscribe to holding
					m_pOwnerNoRef.Holding += HandleHoldingEventArgs;
				}

				RegisterDragPointerEvents();
			}*/
		}

		public void HandlePointerMovedEventArgs(object sender, PointerRoutedEventArgs pArgs)
		{
			Pointer spPointer;
			PointerDeviceType pointerDeviceType = PointerDeviceType.Touch;

			spPointer = pArgs.Pointer;
			pointerDeviceType = spPointer.PointerDeviceType;

			// Our behavior is different between mouse and touch.
			// It's up to us to detect mouse drag gestures - if we
			// detect one here, start a drag drop.
			if (pointerDeviceType == PointerDeviceType.Mouse || pointerDeviceType == PointerDeviceType.Pen)
			{
				PointerPoint spPointerPoint;
				Point newMousePosition;

				spPointerPoint = pArgs.GetCurrentPoint(m_pOwnerNoRef);

				newMousePosition = spPointerPoint.Position;
				if (ShouldStartMouseDrag(newMousePosition))
				{
					IAsyncOperation<DataPackageOperation> spAsyncOperation;
					StopCheckingForMouseDrag(spPointer);

					spAsyncOperation = m_pOwnerNoRef.StartDragAsync(spPointerPoint);
				}
			}
		}

		public void HandlePointerReleasedEventArgs(object sender, PointerRoutedEventArgs pArgs)
		{
			Pointer spPointer;
			PointerDeviceType pointerDeviceType = PointerDeviceType.Touch;

			spPointer = pArgs.Pointer;
			pointerDeviceType = spPointer.PointerDeviceType;

			// Check if this is a mouse button up
			if (pointerDeviceType == PointerDeviceType.Mouse || pointerDeviceType == PointerDeviceType.Pen)
			{
				bool isRightButtonPressed = false;
				PointerPoint spPointerPoint;
				PointerPointProperties spPointerProperties;

				spPointerPoint = pArgs.GetCurrentPoint(m_pOwnerNoRef);
				spPointerProperties = spPointerPoint.Properties;
				isRightButtonPressed = spPointerProperties.IsRightButtonPressed;

				// if the mouse right button was the one released...
				if (m_isRightButtonPressed && !isRightButtonPressed)
				{
					m_isRightButtonPressed = false;
					UnregisterEvents();
					// Terminate any mouse drag gesture tracking.
					StopCheckingForMouseDrag(spPointer);
				}
			}
			else
			{
				UnregisterEvents();
			}
		}

		public void HandlePointerCaptureLostEventArgs(object sender, PointerRoutedEventArgs pArgs)
		{
			Pointer spPointer;
			PointerDeviceType pointerDeviceType = PointerDeviceType.Touch;

			spPointer = pArgs.Pointer;

			pointerDeviceType = spPointer.PointerDeviceType;
			if (pointerDeviceType == PointerDeviceType.Mouse || pointerDeviceType == PointerDeviceType.Pen)
			{
				// We're not necessarily going to get a PointerReleased on capture lost, so reset this flag here.
				m_isRightButtonPressed = false;
			}

			UnregisterEvents();
		}

		public void UnregisterEvents()
		{
			// Unregister events handlers
			if (m_dragDropPointerMovedToken)
			{
				m_pOwnerNoRef.PointerMoved -= HandlePointerMovedEventArgs;
				m_dragDropPointerMovedToken = false;
			}

			if (m_dragDropPointerReleasedToken)
			{
				m_pOwnerNoRef.PointerReleased -= HandlePointerReleasedEventArgs;
				m_dragDropPointerReleasedToken = false;
			}

			if (m_dragDropPointerCaptureLostToken)
			{
				m_pOwnerNoRef.PointerCaptureLost -= HandlePointerCaptureLostEventArgs;
				m_dragDropPointerCaptureLostToken = false;
			}

			/*if (m_dragDropHoldingToken)
			{
				m_pOwnerNoRef.Holding -= HandleHoldingEventArgs;
				m_dragDropHoldingToken = false;
			}*/
		}

		/*public void HandleHoldingEventArgs(object sender, HoldingRoutedEventArgs pArgs)
		{
			PointerDeviceType pointerDeviceType = PointerDeviceType.Touch;

			pointerDeviceType = pArgs.PointerDeviceType;

			if (pointerDeviceType == PointerDeviceType.Touch)
			{
				HoldingState holdingState = HoldingState.Started;
				holdingState = pArgs.HoldingState;

				if (holdingState == HoldingState.Started)
				{
					m_isHoldingCompleted = true;
				}
			}
		}*/

		/*public void HandleDirectManipulationDraggingStarted()
		{
			// Release cross-slide viewport now
			m_pOwnerNoRef.DirectManipulationCrossSlideContainerCompleted();
			if (m_isHoldingCompleted)
			{
				m_pOwnerNoRef.OnTouchDragStarted(m_spPointerPoint, m_spPointer);
			}

			m_spPointerPoint = null;
			m_spPointer = null;
		}*/
	}
}
