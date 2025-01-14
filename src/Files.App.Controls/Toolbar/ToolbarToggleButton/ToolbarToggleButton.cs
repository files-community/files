// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class ToolbarToggleButton : ToggleButton, IToolbarItemSet
	{
		// True when a button has its Content property assigned
		private bool _hasContent = false;

		public ToolbarToggleButton()
		{
			DefaultStyleKey = typeof( ToolbarToggleButton );
		}

		protected override void OnApplyTemplate()
		{
			RegisterPropertyChangedCallback( ContentProperty , OnContentChanged );

			base.OnApplyTemplate();

			UpdateContentStates( CheckHasContent() );
		}

		#region Private Getters

		private bool CheckHasContent()
		{
			return _hasContent;
		}

		#endregion

		#region Private Setters

		private void SetHasContent(bool newValue)
		{
			_hasContent = newValue;
		}

		#endregion

		#region Update functions

		/// <summary>
		/// Updates the ToolbarButton's Label string value as it changes.
		/// </summary>
		/// <param name="newLabel"></param>
		private void UpdateLabel(string newLabel)
		{
			///
			/// Updates the internal item's Text or Label
			/// property as it changes.
			///
		}



		private void UpdateContent(object newContent)
		{
			if ( CheckHasContent() == false )
			{
				// We clear the content
			}
			else
			{
				// We make sure the content displays
			}

			UpdateContentStates( CheckHasContent() );
		}



		/// <summary>
		/// Sets the ToolbarButton's ContentState based on whether the
		/// Content property has been assigned.
		/// </summary>
		/// <param name="hasContent"></param>
		private void UpdateContentStates(bool hasContent)
		{
			if ( hasContent )
			{
				VisualStateManager.GoToState( this , HasContentStateName , true );
			}
			else
			{
				VisualStateManager.GoToState( this , HasNoContentStateName , true );
			}
		}



		/// <summary>
		/// Updates the ToolbarButton's ThemedIcon TemplatePart's Style value
		/// </summary>
		/// <param name="newStyle"></param>
		private void UpdateThemedIcon(Style newStyle)
		{
			///
			/// Updates the internal item's ThemedIcon
			/// Style as it changes.
			///
		}



		/// <summary>
		/// Updates the ToolbarButton's ThemedIcon.IconSize double value
		/// </summary>
		/// <param name="newSize"></param>
		private void UpdateIconSize(double newSize)
		{
			///
			/// Updates the internal item's ThemedIcon
			/// IconSize as it changes.
			///
		}

		#endregion

		#region Property Changed Events

		/// <summary>
		/// Invoked when the Label string property has changed
		/// </summary>
		/// <param name="newLabel"></param>
		private void LabelChanged(string newLabel)
		{
			UpdateLabel( newLabel );
		}



		/// <summary>
		/// Invoked when the ThemedIcon Style property has changed.
		/// </summary>
		/// <param name="newStyle"></param>
		private void ThemedIconChanged(Style newStyle)
		{
			UpdateThemedIcon( newStyle );
		}



		/// <summary>
		/// Invoked when the IconSize double property has changed.
		/// </summary>
		/// <param name="newSize"></param>
		private void IconSizeChanged(double newSize)
		{
			UpdateIconSize( newSize );
		}

		#endregion

		#region ButtonBase Events

		/// <summary>
		/// Invoked when the ToolbarButton's ButtonBase OnContentChanged event
		/// is triggered.
		/// </summary>
		/// <param name="newContent"></param>
		private void ContentChanged(object newContent)
		{
			if ( newContent != null )
			{
				SetHasContent( true );
			}
			else
			{
				SetHasContent( false );
			}

			UpdateContent( newContent );
		}

		#endregion
	}
}
