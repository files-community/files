// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Files.App.Controls
{
	/// <summary>
	/// An Abstract control to simplify items added to the Toolbar,
	/// and map them to other controls dependent on their overflow
	/// behavior and state
	/// </summary>
	public partial class ToolbarItem : DependencyObject
	{
		# region Update Item Properties

		private void UpdateItemType(ToolbarItemTypes newItemType)
		{
			///
			/// We want to intercept the Item Type that is set
			/// and then ensure we choose the correct control to
			/// map it to internally.
			/// 
			/// ToolbarItemTypes.Content
			/// => ContentPresenter
			/// 
			/// ToolbarItemTypes.Button
			/// => ToolbarButton
			/// => FlyoutMenuItemEx
			/// 
			/// ToolbarItemTypes.Flyout
			/// => ToolbarFlyoutButton
			/// => MenuFlyoutSubItemEx
			/// 
			/// ToolbarItemTypes.Radio
			/// => ToolbarRadioButton
			/// => MenuFlyoutRadioItemEx
			/// 
			/// ToolbarItemTypes.Separator
			/// => ToolbarSeparator
			/// => MenuFlyoutSeparator
			/// 
			/// ToolbarItemTypes.Split
			/// => ToolbarSplitButton
			/// => MenuFlyoutSubItem => MenuFlyoutItem (for the split buttons main command)
			/// 
			/// ToolbarItemTypes.Toggle
			/// => ToolbarToggleButton
			/// => MenuFlyoutToggleItemEx
			/// 
		}



		private void UpdateLabel(string newLabel)
		{ 
			///
			/// Updates the internal item's Text or Label
			/// property as it changes.
			///
		}



		/// <summary>
		/// Updates the ToolbarItem's SubItems property
		/// </summary>
		/// <param name="newItems"></param>
		private void UpdateSubItems(IList<ToolbarItem> newItems)
		{ 		
		}



		private void UpdateContent(object newContent)
		{
			///
			/// Updates the internal item's Content
			/// property
			///
		}



		private void UpdateThemedIcon(Style newStyle)
		{
			///
			/// Updates the internal item's ThemedIcon
			/// Style as it changes.
			///
		}



		private void UpdateKeyboardAcceleratorTextOverride(string newKeyboardAcceleratorText)
		{
			///
			/// Updates the internal Overflow item's
			/// KeyboardAcceleratorTextOverride string as it changes.
			///
		}



		private void UpdateGroupName(string newGroupName)
		{
			///
			/// Updates the internal Radio item's
			/// GroupName string as it changes.
			///
		}



		private void UpdateCommand(XamlUICommand newCommand)
		{
			///
			/// Updates the internal item's
			/// Command as it changes.
			/// 
			/// If the internal item is a Button, this will
			/// set the Click event handler, otherwise we pass
			/// it onto the overflow menu item's Command property.
			///
		}



		private void UpdateCommandParameter(object newCommandParameter)
		{
			///
			/// Updates the internal item's
			/// CommandParameter as it changes.
			/// 
			/// Not sure if this is relevent to the buttons,
			/// but we pass this onto the MenuFlyoutItemEx's 
			/// CommandParameter property.
			///
		}



		private void UpdateOverflowBehavior(OverflowBehaviors newOverflowBehavior)
		{
			///
			/// When we get our ToolbarItem collection, we need to read their
			/// OverflowBehavior value and decide if that item belongs in the
			/// ToolbarItemList list, or in the ToolbarItemOverflowList list.
			/// 
			/// OverflowBehaviours.Auto
			/// The ToolbarItem only moves to Overflow if
			/// there is not enough space in the Toolbar.
			/// 
			/// OverflowBehaviours.Always
			/// The ToolbarItem is placed in the Overflow
			/// menu even if there is enough room in the Toolbar.
			/// 
			/// OverflowBehaviours.Never
			/// The ToolbarItem is never placed in the Overflow
			/// menu, even when there is insufficiant room, and so
			/// does not display.
			///
		}



		/// <summary>
		/// Updates the ToolbarItem's IconSize double value
		/// </summary>
		/// <param name="newSize"></param>
		private void UpdateIconSize(double newSize)
		{
			///
			/// Updates the internal item's ThemedIcon
			/// IconSize as it changes.
			///
		}



		/// <summary>
		/// Updates the ToolbarItem's IsChecked bool value
		/// </summary>
		/// <param name="isChecked"></param>
		private void UpdateIsChecked(bool isChecked)
		{
			///
			/// Updates the internal item's IsChecked
			/// property as it changes.
			/// Primarily used for Toggle ItemTypes.
			///
		}

		#endregion

		#region Property Changed Events

		private void ItemTypeChanged(ToolbarItemTypes newItemType)
		{
			UpdateItemType( newItemType );
		}



		private void OverflowBehaviorChanged(OverflowBehaviors newOverflowBehavior)
		{
			UpdateOverflowBehavior( newOverflowBehavior );
		}



		private void LabelChanged(string newLabel) 
		{
			UpdateLabel( newLabel );
		}




		/// <summary>
		/// Handles changes to the ToolbarItem's SubItems property
		/// </summary>
		/// <param name="newItems"></param>
		private void SubItemsChanged(IList<ToolbarItem> newItems)
		{
			UpdateSubItems( newItems );
		}



		private void ContentChanged(object newContent) 
		{
			UpdateContent( newContent );
		}



		private void ThemedIconChanged(Style newStyle)
		{
			UpdateThemedIcon( newStyle );
		}



		private void KeyboardAcceleratorTextOverrideChanged( string newKeyboardAcceleratorText)
		{
			UpdateKeyboardAcceleratorTextOverride( newKeyboardAcceleratorText );
		}



		private void GroupNameChanged(string newGroupName)
		{
			UpdateGroupName( newGroupName );
		}



		private void CommandChanged(XamlUICommand newCommand)
		{
			UpdateCommand( newCommand );
		}



		private void CommandParameterChanged(object newCommandParameter)
		{
			UpdateCommandParameter( newCommandParameter );
		}


		/// <summary>
		/// Invoked when the IconSize double property has changed.
		/// </summary>
		/// <param name="newSize"></param>
		private void IconSizeChanged(double newSize)
		{
			UpdateIconSize( newSize );
		}


		/// <summary>
		/// Invoked when the IsChecked bool property has changed.
		/// </summary>
		/// <param name="newSize"></param>
		private void IsCheckedChanged(bool isChecked)
		{
			UpdateIsChecked( isChecked );
		}

		#endregion

		#region Internal methods

		///
		/// Properties on this ToolbarItem control will be mapped
		/// onto the other controls we use to handle these items
		/// 
		/// Label
		/// => MenuItemEx.Text
		/// => ToolbarButton.Label
		///		  
		/// ThemedIcon 
		/// => MenuItemEx.ThemedIcon(Style)
		/// => ToolbarButton.ThemedIcon(Style)
		///
		/// GroupName
		/// => RadioMenuFlyoutItemEx.GroupName
		/// => ToolbarRadioButton.GroupName
		/// 
		/// KeyboardAcceleratorTextOverride 
		/// => MenuItemEx.KeyboardAcceleratorTextOverride
		/// => ToolbarButton => Tooltip = Label + KeyboardAcceleratorTextOverride
		/// 
		/// Command
		/// => MenuItemEx.Command
		/// => ToolbarButton.Click event
		/// => ToolbarSplitButton.Click event
		/// => ToolbarToggleButton.OnToggle event
		/// 

		#endregion
	}
}
