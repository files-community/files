// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Input;
using Files.App.Controls.Primitives;

namespace Files.App.Controls
{
	public partial class ToolbarItem : DependencyObject
	{
		#region ItemType (enum ToolbarItemTypes)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ItemType"/> property.
		/// </summary>
		public static readonly DependencyProperty ItemTypeProperty =
			DependencyProperty.Register(
				nameof(ItemType),
				typeof(ToolbarItemTypes),
				typeof(ToolbarItem),
				new PropertyMetadata(ToolbarItemTypes.Content, (d, e) => ((ToolbarItem)d).OnItemTypePropertyChanged((ToolbarItemTypes)e.OldValue, (ToolbarItemTypes)e.NewValue)));



		/// <summary>
		/// Gets or sets an Enum value to choose from our Toolbar ItemTypes. (Content, Button, FlyoutButton, SplitButton, ToggleButton)
		/// </summary>
		public ToolbarItemTypes ItemType
		{
			get => (ToolbarItemTypes)GetValue( ItemTypeProperty );
			set => SetValue( ItemTypeProperty , value );
		}



		protected virtual void OnItemTypePropertyChanged(ToolbarItemTypes oldValue , ToolbarItemTypes newValue)
		{
			if ( oldValue != newValue ) 
			{
				ItemTypeChanged( newValue );
			}
		}

		#endregion

		#region OverflowBehavior (enum OverflowBehavior)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="OverflowBehavior"/> property.
		/// </summary>
		public static readonly DependencyProperty OverflowBehaviorProperty =
			DependencyProperty.Register(
				nameof(OverflowBehavior),
				typeof(OverflowBehaviors),
				typeof(ToolbarItem),
				new PropertyMetadata(OverflowBehaviors.Auto, (d, e) => ((ToolbarItem)d).OnOverflowBehaviorPropertyChanged((OverflowBehaviors)e.OldValue, (OverflowBehaviors)e.NewValue)));



		/// <summary>
		/// Gets or sets an Enum value to choose from our Toolbar OverflowBehaviors. (Auto, Always, Never)
		/// </summary>
		public OverflowBehaviors OverflowBehavior
		{
			get => (OverflowBehaviors)GetValue( OverflowBehaviorProperty );
			set => SetValue( OverflowBehaviorProperty , value );
		}



		protected virtual void OnOverflowBehaviorPropertyChanged(OverflowBehaviors oldValue , OverflowBehaviors newValue)
		{
			if ( newValue != oldValue )
			{
				OverflowBehaviorChanged( newValue );
			}
		}

		#endregion

		#region Label (string)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Label"/> property.
		/// </summary>
		public static readonly DependencyProperty LabelProperty =
			DependencyProperty.Register(
				nameof(Label),
				typeof(string),
				typeof(ToolbarItem),
				new PropertyMetadata(string.Empty, (d, e) => ((ToolbarItem)d).OnLabelPropertyChanged((string)e.OldValue, (string)e.NewValue)));



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
			if ( newValue != oldValue )
			{ 
				LabelChanged( newValue );
			}
		}

		#endregion

		#region SubItems (IList<ToolbarItem>)

		public static readonly DependencyProperty SubItemsProperty =
			DependencyProperty.Register(
				nameof( SubItems ),
				typeof( IList<ToolbarItem> ),
				typeof(ToolbarItem),
				new PropertyMetadata( new List<ToolbarItem>(), (d, e) => ((ToolbarItem)d).OnSubItemsPropertyChanged(( IList<ToolbarItem> )e.OldValue, ( IList<ToolbarItem> )e.NewValue)));



		public IList<ToolbarItem> SubItems
		{
			get => (IList<ToolbarItem>)GetValue( SubItemsProperty );
			set => SetValue( SubItemsProperty , value );
		}



		protected virtual void OnSubItemsPropertyChanged(IList<ToolbarItem> oldItems , IList<ToolbarItem> newItems)
		{
			if ( newItems != oldItems )
			{
				SubItemsChanged( newItems );
			}
		}

		#endregion

		#region Content (object)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Content"/> property.
		/// </summary>
		public static readonly DependencyProperty ContentProperty =
			DependencyProperty.Register(
				nameof(Content),
				typeof(object),
				typeof(ToolbarItem),
				new PropertyMetadata(null, (d, e) => ((ToolbarItem)d).OnContentPropertyChanged((object)e.OldValue, (object)e.NewValue)));



		/// <summary>
		/// Gets or sets the objects we use as Content for the ToolbarItem.
		/// </summary>
		public object Content
		{
			get => (object)GetValue( ContentProperty );
			set => SetValue( ContentProperty , value );
		}



		protected virtual void OnContentPropertyChanged(object oldValue , object newValue)
		{
			if ( newValue != oldValue )
			{
				ContentChanged( newValue );
			}
		}

		#endregion

		#region ThemedIcon (style)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ThemedIcon"/> property.
		/// </summary>
		public static readonly DependencyProperty ThemedIconProperty =
			DependencyProperty.Register(
				nameof(ThemedIcon),
				typeof(Style),
				typeof(ToolbarItem),
				new PropertyMetadata(null, (d, e) => ((ToolbarItem)d).OnThemedIconPropertyChanged((Style)e.OldValue, (Style)e.NewValue)));



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
				typeof(ToolbarItem),
				new PropertyMetadata((double)16, (d, e) => ((ToolbarItem)d).OnIconSizePropertyChanged((double)e.OldValue, (double)e.NewValue)));



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

		#region IsChecked (bool)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="IsChecked"/> property.
		/// </summary>
		public static readonly DependencyProperty IsCheckedProperty =
			DependencyProperty.Register(
				nameof(IsChecked),
				typeof(bool),
				typeof(ToolbarItem),
				new PropertyMetadata(false, (d, e) => ((ToolbarItem)d).OnIsCheckedPropertyChanged((bool)e.OldValue, (bool)e.NewValue)));



		/// <summary>
		/// Gets or sets the IsChecked as a bool
		/// </summary>
		public bool IsChecked
		{
			get => (bool)GetValue( IsCheckedProperty );
			set => SetValue( IsCheckedProperty , value );
		}



		protected virtual void OnIsCheckedPropertyChanged(bool oldValue , bool newValue)
		{
			if ( newValue != oldValue )
			{
				IsCheckedChanged( newValue );
			}
		}

		#endregion

		#region KeyboardAcceleratorTextOverride (string)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="KeyboardAcceleratorTextOverride"/> property.
		/// </summary>
		public static readonly DependencyProperty KeyboardAcceleratorTextOverrideProperty =
			DependencyProperty.Register(
				nameof(KeyboardAcceleratorTextOverride),
				typeof(string),
				typeof(ToolbarItem),
				new PropertyMetadata(string.Empty, (d, e) => ((ToolbarItem)d).OnKeyboardAcceleratorTextOverridePropertyChanged((string)e.OldValue, (string)e.NewValue)));



		/// <summary>
		/// Gets or sets the KeyboardAcceleratorTextOverride as a String
		/// </summary>
		public string KeyboardAcceleratorTextOverride
		{
			get => (string)GetValue( KeyboardAcceleratorTextOverrideProperty );
			set => SetValue( KeyboardAcceleratorTextOverrideProperty , value );
		}



		protected virtual void OnKeyboardAcceleratorTextOverridePropertyChanged(string oldValue , string newValue)
		{
			if ( newValue != oldValue )
			{
				KeyboardAcceleratorTextOverrideChanged( newValue );
			}
		}

		#endregion

		#region GroupName (string)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="GroupName"/> property.
		/// </summary>
		public static readonly DependencyProperty GroupNameProperty =
			DependencyProperty.Register(
				nameof(GroupName),
				typeof(string),
				typeof(ToolbarItem),
				new PropertyMetadata(string.Empty, (d, e) => ((ToolbarItem)d).OnGroupNamePropertyChanged((string)e.OldValue, (string)e.NewValue)));



		/// <summary>
		/// Gets or sets the GroupName as a String
		/// </summary>
		public string GroupName
		{
			get => (string)GetValue( GroupNameProperty );
			set => SetValue( GroupNameProperty , value );
		}



		protected virtual void OnGroupNamePropertyChanged(string oldValue , string newValue)
		{
			if ( newValue != oldValue )
			{
				GroupNameChanged( newValue );
			}
		}

		#endregion

		#region Command (Command)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="Command"/> property.
		/// </summary>
		public static readonly DependencyProperty CommandProperty =
			DependencyProperty.Register(
				nameof(Command),
				typeof(XamlUICommand),
				typeof(ToolbarItem),
				new PropertyMetadata(null, (d, e) => ((ToolbarItem)d).OnCommandPropertyChanged((XamlUICommand)e.OldValue, (XamlUICommand)e.NewValue)));



		/// <summary>
		/// Gets or sets the Command associated with the ToolbarItem as a XamlUICommand
		/// </summary>
		public XamlUICommand Command
		{
			get => (XamlUICommand)GetValue( CommandProperty );
			set => SetValue( CommandProperty , value );
		}



		protected virtual void OnCommandPropertyChanged(XamlUICommand oldValue , XamlUICommand newValue)
		{
			if ( newValue != oldValue )
			{
				CommandChanged( newValue );
			}
		}

		#endregion

		#region CommandParameter (CommandParameter)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="CommandParameter"/> property.
		/// </summary>
		public static readonly DependencyProperty CommandParameterProperty =
			DependencyProperty.Register(
				nameof(CommandParameter),
				typeof(object),
				typeof(ToolbarItem),
				new PropertyMetadata(null, (d, e) => ((ToolbarItem)d).OnCommandParameterPropertyChanged((object)e.OldValue, (object)e.NewValue)));



		/// <summary>
		/// Gets or sets the CommandParameter associated with the ToolbarItem as an Object
		/// </summary>
		public object CommandParameter
		{
			get => (object)GetValue( CommandParameterProperty );
			set => SetValue( CommandParameterProperty , value );
		}



		protected virtual void OnCommandParameterPropertyChanged(object oldValue , object newValue)
		{
			if ( newValue != oldValue )
			{
				CommandParameterChanged( newValue );
			}
		}

		#endregion
	}
}
