// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class Toolbar : Control
	{
		// A reference to the current available size for ToolbarItems
		private double _availableSize;

		private ItemsRepeater?				_itemsRepeater;
		private ToolbarItemList?			_toolbarItemsList;
		private ToolbarItemOverflowList?	_toolbarItemsOverflowList;

		private ToolbarItemList				_tempToolbarItemsList;
		private ToolbarItemOverflowList		_tempToolbarItemsOverflowList;


		private double				_smallMinWidth    = 24; // I have set default values, but we pull from resources
		private double				_mediumMinWidth   = 32; // if they are available.
		private double				_largeMinWidth    = 32;
		 
		private double				_smallMinHeight   = 24;
		private double				_mediumMinHeight  = 24;
		private double				_largeMinHeight   = 32;

		private double				_currentMinWidth;
		private double				_currentMinHeight;

		public Toolbar()
		{
			DefaultStyleKey = typeof( Toolbar );
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UpdateMinSizesFromResources();

			if ( Items != null )
			{
				_tempToolbarItemsList = new ToolbarItemList();
				_tempToolbarItemsOverflowList = new ToolbarItemOverflowList();

				UpdateItems( Items );
			}

			SetItemsRepeater( GetTemplateChild( ToolbarItemsRepeaterPartName ) as ItemsRepeater );

			if ( GetItemsRepeater() != null )
			{
				ItemsRepeater itemsRepeater = GetItemsRepeater();
				itemsRepeater.ItemsSource = GetToolbarItemsList();
			}
		}

		#region Private Getters

		private double GetAvailableSize()
		{
			return _availableSize;
		}

		private ItemsRepeater GetItemsRepeater()
		{
			return _itemsRepeater;
		}

		private ToolbarItemList GetToolbarItemsList()
		{
			return _toolbarItemsList;
		}

		private ToolbarItemOverflowList GetToolbarItemsOverflowList()
		{
			return _toolbarItemsOverflowList;
		}

		private double GetSmallMinWidth()
		{
			return _smallMinWidth;
		}

		private double GetMediumMinWidth()
		{
			return _mediumMinWidth;
		}

		private double GetLargeMinWidth()
		{
			return _largeMinWidth;
		}

		private double GetSmallMinHeight()
		{
			return _smallMinHeight;
		}

		private double GetMediumMinHeight()
		{
			return _mediumMinHeight;
		}

		private double GetLargeMinHeight()
		{
			return _largeMinHeight;
		}

		private double GetCurrentMinWidth()
		{
			return _currentMinWidth;
		}

		private double GetCurrentMinHeight()
		{
			return _currentMinHeight;
		}

		#endregion

		#region Private Setters

		private void SetAvailableSize(double newValue)
		{
			_availableSize = newValue;
		}

		private void SetItemsRepeater(ItemsRepeater itemsRepeater)
		{
			_itemsRepeater = itemsRepeater;
		}

		private void SetToolbarItemsList(ToolbarItemList toolbarItemsList)
		{
			_toolbarItemsList = toolbarItemsList;
		}

		private void SetToolbarItemsOverflowList(ToolbarItemOverflowList toolbarItemsOverflowList)
		{
			_toolbarItemsOverflowList = toolbarItemsOverflowList;
		}

		private void SetSmallMinWidth(double newValue)
		{
			_smallMinWidth = newValue;
		}

		private void SetMediumMinWidth(double newValue)
		{
			_mediumMinWidth = newValue;
		}

		private void SetLargeMinWidth(double newValue)
		{
			_largeMinWidth = newValue;
		}

		private void SetSmallMinHeight(double newValue)
		{
			_smallMinHeight = newValue;
		}

		private void SetMediumMinHeight(double newValue)
		{
			_mediumMinHeight = newValue;
		}

		private void SetLargeMinHeight(double newValue)
		{
			_largeMinHeight = newValue;
		}

		private void SetCurrentMinWidth(double newValue)
		{
			_currentMinWidth = newValue;
		}

		private void SetCurrentMinHeight(double newValue)
		{
			_currentMinHeight = newValue;
		}

		#endregion

		#region Update Properties

		/// <summary>
		/// Updates the Toolbar's Items property
		/// </summary>
		/// <param name="newItems"></param>
		private void UpdateItems(IList<ToolbarItem> newItems)
		{
			///
			/// Reads in the ToolbarItem in the Toolbar.Items list
			/// when iterating through them, we ignore any that do not
			/// match the correct object of ToolbarItem
			/// 
			/// Then we read the porperties of each item and assign
			/// each item to the correct lists which we will use to 
			/// manage the Buttons and the Menu items
			///

			foreach ( var item in newItems )
			{
				SortItemsByOverflowBehavior( item );
				Debug.Write( "-> Sorted " + item.Label + " from Items... ..." + Environment.NewLine );
			}

			UpdatePrivateItemList( _tempToolbarItemsList );
			Debug.Write( " | tempItemsList " + _tempToolbarItemsList.Count.ToString() + " *" + Environment.NewLine );

			UpdatePrivateItemOverflowList( _tempToolbarItemsOverflowList );
			Debug.Write( " | tempItemsOverflowList " + _tempToolbarItemsOverflowList.Count.ToString() + " *" + Environment.NewLine );
		}



		/// <summary>
		/// EVENTUALLY REMOVE
		/// </summary>
		private void UpdateItemTemplate(DataTemplate newDataTemplate)
		{
		}



		private void UpdateToolbarSize( ToolbarSizes newToolbarSize )
		{
			UpdateMinSizesFromResources();		
		}



		private void UpdateAvailableSize()
		{
			double newAvailableSize = 0;

			// Do some code to check or respond to size changes for
			// the Toolbar's Items space (ItemsRepeaterLayout?)

			SetAvailableSize( newAvailableSize );

			/// We need to check the Item Widths and Heights
			/// (we know the sizes for buttons, but content will need
			/// to be measured).  We also need to include the layout
			/// spacing values, to determine how many of our items can
			/// fit in the availableSize.
		}



		/// <summary>
		/// Updates the MinWidth and MinHeight for Toolbar Buttons, from
		/// Resources, or uses initial values we set in code.
		/// </summary>
		private void UpdateMinSizesFromResources()
		{
			double smallMinWidth    = (double)Application.Current.Resources[SmallMinWidthResourceKey];
			double smallMinHeight   = (double)Application.Current.Resources[SmallMinHeightResourceKey];

			double mediumMinWidth   = (double)Application.Current.Resources[MediumMinWidthResourceKey];
			double mediumMinHeight  = (double)Application.Current.Resources[MediumMinHeightResourceKey];

			double largeMinWidth    = (double)Application.Current.Resources[LargeMinWidthResourceKey];
			double largeMinHeight   = (double)Application.Current.Resources[LargeMinHeightResourceKey];			

			if ( !double.IsNaN( smallMinWidth )  || !double.IsNaN( smallMinHeight )  ||
				 !double.IsNaN( mediumMinWidth ) || !double.IsNaN( mediumMinHeight ) ||
				 !double.IsNaN( largeMinWidth )  || !double.IsNaN( largeMinHeight )    )
			{
				SetSmallMinWidth( smallMinWidth );
				SetSmallMinHeight( smallMinHeight );

				SetMediumMinWidth( mediumMinWidth );
				SetMediumMinHeight( mediumMinHeight );

				SetLargeMinWidth( largeMinWidth );
				SetLargeMinHeight( largeMinHeight );
			}

			if ( ToolbarSize == ToolbarSizes.Small )
			{
				SetCurrentMinWidth( GetSmallMinWidth() );
				SetCurrentMinHeight( GetSmallMinHeight() );
			}
			else if ( ToolbarSize == ToolbarSizes.Large )
			{
				SetCurrentMinWidth( GetLargeMinWidth() );
				SetCurrentMinHeight( GetLargeMinHeight() );
			}
			else
			{
				SetCurrentMinWidth( GetMediumMinWidth() );
				SetCurrentMinHeight( GetMediumMinHeight() );
			}
		}



		/// <summary>
		/// Updates the Private ToolbarItemList
		/// </summary>
		/// <param name="newList"></param>
		private void UpdatePrivateItemList(ToolbarItemList newList)
		{
			SetToolbarItemsList( newList );
		}



		/// <summary>
		/// Updates the Private ToolbarItemOverflowList
		/// </summary>
		/// <param name="newOverflowList"></param>
		private void UpdatePrivateItemOverflowList(ToolbarItemOverflowList newOverflowList)
		{
			SetToolbarItemsOverflowList( newOverflowList );
		}

		#endregion

		#region Property Changed Events

		/// <summary>
		/// Handles changes to the Items
		/// </summary>
		/// <param name="newItemsSource"></param>
		private void ItemsChanged(IList<ToolbarItem> newItems)
		{
			UpdateItems( newItems );
		}



		/// <summary>
		/// Handles changes to the ItemTemplate
		/// </summary>
		/// <param name="newDataTemplate"></param>
		private void ItemTemplateChanged(DataTemplate newDataTemplate)
		{
			UpdateItemTemplate( newDataTemplate );
		}



		/// <summary>
		/// Handles changes to the ToolbarSize property
		/// </summary>
		/// <param name="newToolbarSize"></param>
		private void ToolbarSizeChanged(ToolbarSizes newToolbarSize)
		{
			UpdateToolbarSize( newToolbarSize );
		}



		/// <summary>
		/// Handles changes to the Private ToolbarItemList
		/// </summary>
		/// <param name="newList"></param>
		private void PrivateItemListChanged(ToolbarItemList newList)
		{
			UpdatePrivateItemList( newList );
		}



		/// <summary>
		/// Handles changes to the Private ToolbarItemOverflowList
		/// </summary>
		/// <param name="newOverflowList"></param>
		private void PrivateItemOverflowListChanged(ToolbarItemOverflowList newOverflowList)
		{
			UpdatePrivateItemOverflowList( newOverflowList );
		}

		#endregion

		#region Sorting

		/// <summary>
		/// Sorts the ToolbarItem based on the it's OverflowBehavior
		/// </summary>
		/// <param name="item"></param>
		private void SortItemsByOverflowBehavior(ToolbarItem item)
		{
			/// We need to check which ToolbarItems go in which list
			/// we have the OverflowBehavior to give us a hint.
			/// Then we pass that item through additional sorting and
			/// then add the relevant control to the lists.
			/// 
			if ( item != null )
			{
				if ( item.OverflowBehavior == OverflowBehaviors.Always )
				{
					AddItemToOverflowList( SortByItemTypeForOverflowItemList( item ) );
				}
				else
				{
					/// Not sure if we check for space at this point, or
					/// When we are adding items to the Private ItemList
					/// 
					if ( item.OverflowBehavior == OverflowBehaviors.Never )
					{
						/// Not sure if we need to behave differently at
						/// this stage for the items, but we can do if
						/// it is needed.
						/// 
						//AddItemToItemList( SortByItemTypeForItemList( item ) );
					}
					else
					{
						//AddItemToItemList( SortByItemTypeForItemList( item ) );
					}

					AddItemToItemList( SortByItemTypeForItemList( item ) );
				}
			}
		}



		/// <summary>
		/// Sorts the ToolbarItem by it's ItemType to add to the
		/// private ItemList
		/// </summary>
		/// <param name="item"></param>
		private IToolbarItemSet SortByItemTypeForItemList(ToolbarItem item)
		{
			switch ( item.ItemType )
			{
				case ToolbarItemTypes.Button:
					// Add ToolbarButton
					return CreateToolbarButton( item.Label , item.ThemedIcon , GetCurrentMinWidth() , GetCurrentMinHeight() , item.IconSize );

				case ToolbarItemTypes.FlyoutButton:
					// Add ToolbarFlyoutButton
					return new ToolbarFlyoutButton();

				case ToolbarItemTypes.RadioButton:
					// Add ToolbarRadioButton
					return new ToolbarRadioButton();

				case ToolbarItemTypes.SplitButton:
					// Add ToolbarSplitButton
					return new ToolbarSplitButton();

				case ToolbarItemTypes.ToggleButton:
					// Add ToolbarToggleButton
					return CreateToolbarToggleButton( item.Label , item.ThemedIcon , GetCurrentMinWidth() , GetCurrentMinHeight() , item.IconSize , item.IsChecked );

				case ToolbarItemTypes.Separator:
					// Add ToolbarToggleButton
					return CreateToolbarSeparator();

				case ToolbarItemTypes.Content:
					// Add Content Presenter
					return null;

				default:
					return null;
			}
		}



		/// <summary>
		/// Sorts the ToolbarItem by it's ItemType to add to the
		/// private OverflowItemList
		/// </summary>
		/// <param name="item"></param>
		private IToolbarOverflowItemSet SortByItemTypeForOverflowItemList(ToolbarItem item)
		{
			switch ( item.ItemType )
			{
				case ToolbarItemTypes.Button:
					// Add MenuFlyoutItemEx
					break;

				case ToolbarItemTypes.FlyoutButton:
					// Add MenuFlyoutSubItemEx
					break;

				case ToolbarItemTypes.RadioButton:
					// Add RadioMenuFlyoutItemEx
					break;

				case ToolbarItemTypes.SplitButton:
					// Add MenuFlyoutSubItemEx + MenuFlyoutItemEx
					break;

				case ToolbarItemTypes.ToggleButton:
					// Add ToggleMenuFlyoutItemEx
					break;

				case ToolbarItemTypes.Separator:
					// Add MenuFlyoutSeparator
					break;
			}

			return null;
		}

		#endregion

		#region Dealing with Overflow and StackPanel Part sizing

		///
		/// We will need methods that handle when the availableSize
		/// is too small to display all the ToolbarItemList
		/// items, so we can remove from the list and add to
		/// the ToolbarItemOverflowList.
		/// 
		/// If the availableSize is increased, then we can move
		/// an item that is not set with OverflowBehavior.Always
		/// out of the ToolbarItemOverflowList, and back into
		/// the ToolbarItemList/
		///

		private void PopulateItemsSourceForItemsRepeater()
		{
			/// We then need to measure each item from the ToolbarItemList
			/// we check the availableSize to see if there is room
			/// If there is room, we place it into the ItemsSource and
			/// remove that value from the availableSize + Spacing value
			/// 

			ItemsRepeater itemsRepeater = GetItemsRepeater();

			foreach ( ToolbarItem item in GetToolbarItemsList() )
			{
				/// We can get the AvailableSize
				/// 
				double availableSize = GetAvailableSize();				

				ObservableCollection<object> itemsSource = new ObservableCollection<object>();

				/// Then we create the ToolbarButton, ToolbarSeparator, ToolbarToggleButton etc
				/// for each item
				/// 
				if ( item.ItemType == ToolbarItemTypes.Button )
				{
					//Add a ToolbarButton to the ItemsSource for the ItemsRepeaterPartName
					itemsSource.Add( new ToolbarButton
					{ 
						Label = item.Label , 
						ThemedIcon = item.ThemedIcon , Command = item.Command , 
						CommandParameter = item.CommandParameter , 
						IconSize = item.IconSize,
					} );
				};

				if ( item.ItemType == ToolbarItemTypes.ToggleButton )
				{
					//Add a ToolbarToggleButton to the ItemsSource for the ItemsRepeaterPartName
					itemsSource.Add( new ToolbarToggleButton
					{
						Label = item.Label ,
						ThemedIcon = item.ThemedIcon ,
						Command = item.Command ,
						CommandParameter = item.CommandParameter ,
						IconSize = item.IconSize ,
					} );
				};

				if ( item.ItemType == ToolbarItemTypes.Separator )
				{
					//Add a ToolbarSeparator to the ItemsSource for the ItemsRepeaterPartName
					//itemsSource.Add( new ToolbarSeparator );
				};
				/// etc

				//SetAvailableSize( availableSize - item.Width );

				/// Once we have gone over the items and there is no more
				/// items to add, then we set the ItemsRepeater's ItemsSource
				///
				itemsRepeater.ItemsSource = itemsSource;
			}

				/// We do this for each item until there is no more space
				/// available, then we check its OverflowBehavior and move
				/// it if neccessary.

			}



		private void PopulateItemsSourceForOverflowMenu()
		{
			/// After the sorting for the ItemsRepeater ItemsSource
			/// whatever was put into the ToolbarItemOverflowList
			/// is what we need to add to the Overflow Menu
			/// 

			foreach ( ToolbarItem item in GetToolbarItemsOverflowList() )
			{
				if ( item != null )
				{
					if ( item.ItemType != ToolbarItemTypes.Separator )
					{ 
						MenuFlyoutSeparator menuFlyoutSeparator = new MenuFlyoutSeparator();
						/// OverflowMenuFlyout.AddMenuItem( menuFlyoutSeparator );
					}

					if ( item.ItemType != ToolbarItemTypes.FlyoutButton )
					{
						MenuFlyoutSubItem menuFlyoutSubItem = new MenuFlyoutSubItem();
						menuFlyoutSubItem.Text = item.Label;
						//menuFlyoutSubItem.ThemedIcon = item.ThemedIcon ;
						//menuFlyoutSubItem.IconSize = item.IconSize ;
						//menuFlyoutSubItem.Items.Add ;

						/// We will need to make child menu items for the 
						/// ToolbarItem's flyout items.
						/// 
					
						/// OverflowMenuFlyout.AddMenuItem( menuFlyoutSubItem );
					}

					if ( item.ItemType != ToolbarItemTypes.Button )
					{
						MenuFlyoutItem menuFlyoutItem= new MenuFlyoutItem();
						//MenuFlyoutItemEx menuFlyoutItemEx = new MenuFlyoutItemEx();
						menuFlyoutItem.Text = item.Label;
						menuFlyoutItem.Command = item.Command;
						menuFlyoutItem.CommandParameter = item.CommandParameter;
						menuFlyoutItem.KeyboardAcceleratorTextOverride = item.KeyboardAcceleratorTextOverride;
						//menuFlyoutItemEx.ThemedIcon = item.ThemedIcon ;
						//menuFlyoutItemEx.IconSize = item.IconSize ;

						/// OverflowMenuFlyout.AddMenuItem( menuFlyoutSeparator );
					}

					if ( item.ItemType != ToolbarItemTypes.ToggleButton )
					{
						ToggleMenuFlyoutItem menuToggleItem = new ToggleMenuFlyoutItem();
						//ToggleMenuFlyoutItemEx menuToggleItemEx = new ToggleMenuFlyoutItemEx();
						menuToggleItem.Text = item.Label;
						menuToggleItem.Command = item.Command;
						menuToggleItem.CommandParameter = item.CommandParameter;
						menuToggleItem.KeyboardAcceleratorTextOverride = item.KeyboardAcceleratorTextOverride;
						//mmenuToggleItemEx.ThemedIcon = item.ThemedIcon ;
						//menuToggleItemEx.IconSize = item.IconSize ;

						/// OverflowMenuFlyout.AddMenuItem( menuFlyoutSeparator );
					}

				}
			}
		}

		#endregion

		#region Create Elements

		private ToolbarButton CreateToolbarButton(string label , Style iconStyle , double minWidth , double minHeight , double iconSize)
		{
			ToolbarButton createdButton = new ToolbarButton
			{
				Label = label ,
				ThemedIcon = iconStyle ,
				MinWidth = minWidth ,
				MinHeight = minHeight ,
				IconSize = iconSize ,
			};

			return createdButton;
		}



		private ToolbarToggleButton CreateToolbarToggleButton(string label , Style iconStyle , double minWidth , double minHeight , double iconSize , bool isChecked)
		{
			ToolbarToggleButton createdToggleButton = new ToolbarToggleButton
			{
				Label = label ,
				ThemedIcon = iconStyle ,
				MinWidth = minWidth ,
				MinHeight = minHeight ,
				IconSize = iconSize ,
				IsChecked = isChecked ,
			};

			return createdToggleButton;
		}



		private ToolbarSeparator CreateToolbarSeparator()
		{
			ToolbarSeparator createdSeparator = new ToolbarSeparator();

			return createdSeparator;
		}

		#endregion

		#region Add ToolbarItems to Lists

		/// <summary>
		/// Adds the given Item into the ToolbarItemOverflowList
		/// </summary>
		/// <param name="item"></param>
		private void AddItemToOverflowList(IToolbarOverflowItemSet item)
		{
			if ( item != null && _tempToolbarItemsOverflowList != null)
			{
				_tempToolbarItemsOverflowList.Add( item );
				Debug.Write( "<- Added " + item.ToString() + " to OverflowList " + Environment.NewLine );
			}
		}



		/// <summary>
		/// Adds the given Item into the ToolbarItemList
		/// </summary>
		/// <param name="item"></param>
		private void AddItemToItemList(IToolbarItemSet item)
		{
			if ( item != null && _tempToolbarItemsList != null )
			{
				_tempToolbarItemsList.Add( item );
				Debug.Write( "Added " + item.ToString() + " to ItemList " + Environment.NewLine );
			}
		}

		#endregion

		#region Remove ToolbarItem from Lists

		/// <summary>
		/// Removes a ToolbarItem from the ToolbarItemOverflowList
		/// </summary>
		/// <param name="item"></param>
		private void RemoveItemFromOverflowList(ToolbarItem item)
		{

		}



		/// <summary>
		/// Removes a ToolbarItem from the ToolbarItemList
		/// </summary>
		/// <param name="item"></param>
		private void RemoveItemFromList(IToolbarItemSet item)
		{

		}

		#endregion
	}
}
