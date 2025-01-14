// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Controls
{
	public partial class Toolbar : Control
	{
		#region ToolbarSize (enum ToolbarSizes)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ToolbarSize"/> property.
		/// </summary>
		public static readonly DependencyProperty ToolbarSizeProperty =
			DependencyProperty.Register(
				nameof(ToolbarSize),
				typeof(ToolbarSizes),
				typeof(Toolbar),
				new PropertyMetadata(ToolbarSizes.Medium, (d, e) => ((Toolbar)d).OnToolbarSizePropertyChanged((ToolbarSizes)e.OldValue, (ToolbarSizes)e.NewValue)));



		/// <summary>
		/// Gets or sets an Enum value to choose from our ToolbarSizes. (Small, Medium, Large)
		/// </summary>
		public ToolbarSizes ToolbarSize
		{
			get => (ToolbarSizes)GetValue( ToolbarSizeProperty );
			set => SetValue( ToolbarSizeProperty , value );
		}


		/// <summary>
		/// Triggers when the ToolbarSize property changes
		/// </summary>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		protected virtual void OnToolbarSizePropertyChanged(ToolbarSizes oldValue , ToolbarSizes newValue)
		{
			if ( newValue != oldValue )
			{
				ToolbarSizeChanged( newValue );
			}
		}

		#endregion

		#region Items (IList<ToolbarItem>)

		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register(
				nameof( Items ), 
				typeof( IList<ToolbarItem> ),
				typeof(Toolbar), 
				new PropertyMetadata( defaultValue: new List<ToolbarItem>(), (d, e) => ((Toolbar)d).OnItemsPropertyChanged(( IList<ToolbarItem> )e.OldValue, ( IList<ToolbarItem> )e.NewValue)));



		public IList<ToolbarItem> Items
		{
			get => ( IList<ToolbarItem> )GetValue( ItemsProperty );
			set => SetValue( ItemsProperty , value );
		}



		private void OnItemsPropertyChanged(IList<ToolbarItem> oldItems , IList<ToolbarItem> newItems)
		{
			if ( newItems != oldItems )
			{
				ItemsChanged( newItems );
			}
		}

		#endregion

		#region ItemTemplate (DataTemplate)

		/// <summary>
		/// The backing <see cref="DependencyProperty"/> for the <see cref="ItemTemplate"/> property.
		/// </summary>
		public static readonly DependencyProperty ItemTemplateProperty =
			DependencyProperty.Register(
				nameof(ItemTemplate),
				typeof(DataTemplate),
				typeof(Toolbar),
				new PropertyMetadata(null, (d, e) => ((Toolbar)d).OnItemTemplatePropertyChanged((DataTemplate)e.OldValue, (DataTemplate)e.NewValue)));



		/// <summary>
		/// Gets or sets an Enum value to choose from our ToolbarSizes. (Small, Medium, Large)
		/// </summary>
		public DataTemplate ItemTemplate
		{
			get => (DataTemplate)GetValue( ItemTemplateProperty );
			set => SetValue( ItemTemplateProperty , value );
		}


		/// <summary>
		/// Triggers when the Toolbar's ItemTemplate property changes
		/// </summary>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		protected virtual void OnItemTemplatePropertyChanged(DataTemplate oldValue , DataTemplate newValue)
		{
			if ( newValue != oldValue )
			{
				ItemTemplateChanged( newValue );
			}
		}

		#endregion

		#region Private ToolbarItemList

		/// <summary>
		/// Identifies the protected ToolbarItemList dependency property.
		/// </summary>
		public static readonly DependencyProperty ItemListProperty =
			DependencyProperty.Register(
				nameof(ItemList),
				typeof(ToolbarItemList),
				typeof(Toolbar),
				new PropertyMetadata(new ToolbarItemList(), (d, e) => ((Toolbar)d).OnItemListPropertyChanged(( ToolbarItemList )e.OldValue, ( ToolbarItemList )e.NewValue)));



		/// <summary>
		/// Gets or sets the protected ToolbarItemList of the control.
		/// </summary>
		private ToolbarItemList ItemList
		{
			get { return (ToolbarItemList)GetValue( ItemListProperty ); }
			set { SetValue( ItemListProperty , value ); }
		}



		private void OnItemListPropertyChanged(ToolbarItemList oldList , ToolbarItemList newList)
		{
			if ( newList != oldList )
			{
				PrivateItemListChanged( newList );
			}
		}
		#endregion

		#region Private ToolbarItemOverflowList

		/// <summary>
		/// Identifies the protected ToolbarItemOverflowList dependency property.
		/// </summary>
		public static readonly DependencyProperty ItemOverflowListProperty =
			DependencyProperty.Register(
				nameof(ItemOverflowList),
				typeof(ToolbarItemOverflowList),
				typeof(Toolbar),
				new PropertyMetadata(new ToolbarItemOverflowList(), (d, e) => ((Toolbar)d).OnItemOverflowListPropertyChanged(( ToolbarItemOverflowList )e.OldValue, ( ToolbarItemOverflowList )e.NewValue)));



		/// <summary>
		/// Gets or sets the protected ToolbarItemOverflowList of the control.
		/// </summary>
		private ToolbarItemOverflowList ItemOverflowList
		{
			get { return (ToolbarItemOverflowList)GetValue( ItemOverflowListProperty ); }
			set { SetValue( ItemOverflowListProperty , value ); }
		}



		private void OnItemOverflowListPropertyChanged(ToolbarItemOverflowList oldList , ToolbarItemOverflowList newList)
		{
			if ( newList != oldList )
			{
				PrivateItemOverflowListChanged( newList );
			}
		}
		#endregion
	}
}
