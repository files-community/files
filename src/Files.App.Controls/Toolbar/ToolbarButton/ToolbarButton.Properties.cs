// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class ToolbarButton : Button, IToolbarItemSet
	{
		#region Label (string)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Label"/> property.
		/// </summary>
		public static readonly DependencyProperty LabelProperty =
			DependencyProperty.Register(
				nameof(Label),
				typeof(string),
				typeof(ToolbarButton),
				new PropertyMetadata(string.Empty, (d, e) => ((ToolbarButton)d).OnLabelPropertyChanged((string)e.OldValue, (string)e.NewValue)));



		/// <summary>
		/// Gets or sets the Label as a String
		/// </summary>
		public string Label
		{
			get => (string)GetValue( LabelProperty );
			set => SetValue( LabelProperty , value );
		}



		protected virtual void OnLabelPropertyChanged(string oldValue , string newValue)
		{
			if ( oldValue != newValue )
			{
				LabelChanged( newValue );
			}
		}

		#endregion

		#region ThemedIcon (Style)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ThemedIcon"/> property.
		/// </summary>
		public static readonly DependencyProperty ThemedIconProperty =
			DependencyProperty.Register(
				nameof(ThemedIcon),
				typeof(Style),
				typeof(ToolbarButton),
				new PropertyMetadata(null, (d, e) => ((ToolbarButton)d).OnThemedIconPropertyChanged((Style)e.OldValue, (Style)e.NewValue)));



		/// <summary>
		/// Gets or sets the Style value for the item's ThemedIcon
		/// </summary>
		public Style ThemedIcon
		{
			get => (Style)GetValue( ThemedIconProperty );
			set => SetValue( ThemedIconProperty , value );
		}



		protected virtual void OnThemedIconPropertyChanged(Style oldValue , Style newValue)
		{
			if ( newValue != oldValue )
			{
				ThemedIconChanged( newValue );
			}
		}

		#endregion

		#region IconSize (double)

		public static readonly DependencyProperty IconSizeProperty =
			DependencyProperty.Register(
				nameof(IconSize),
				typeof(double),
				typeof(ToolbarButton),
				new PropertyMetadata((double)16, (d, e) => ((ToolbarButton)d).OnIconSizePropertyChanged((double)e.OldValue, (double)e.NewValue)));



		/// <summary>
		/// Gets or sets a value indicating the Icon's design size.
		/// </summary>        
		public double IconSize
		{
			get => (double)GetValue( IconSizeProperty );
			set => SetValue( IconSizeProperty , value );
		}



		protected virtual void OnIconSizePropertyChanged(double oldValue , double newValue)
		{
			if ( newValue != oldValue )
			{
				IconSizeChanged( newValue );
			}
		}

		#endregion

		#region ButtonBase Events

		/// <inheritdoc/>
		protected override void OnContentChanged(object oldContent , object newContent)
		{
			if ( newContent != oldContent )
			{
				ContentChanged( newContent );
				base.OnContentChanged( oldContent , newContent );
			}
		}

		#endregion
	}
}
