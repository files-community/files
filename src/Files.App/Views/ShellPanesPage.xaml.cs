// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System.Runtime.CompilerServices;

namespace Files.App.Views
{
	/// <summary>
	/// Represents <see cref="Page"/> that holds multiple panes.
	/// </summary>
	public sealed partial class ShellPanesPage : Page, IShellPanesPage, ITabBarItemContent, INotifyPropertyChanged
	{
		// Dependency injections

		private IGeneralSettingsService GeneralSettingsService { get; } = Ioc.Default.GetRequiredService<IGeneralSettingsService>();
		private AppModel AppModel { get; } = Ioc.Default.GetRequiredService<AppModel>();

		// Constants

		private const string ShellBorderFocusOnState = "ShellBorderFocusOnState";
		private const string ShellBorderFocusOffState = "ShellBorderFocusOffState";
		private const string ShellBorderDualPaneOffState = "ShellBorderDualPaneOffState";

		// Fields

		private bool _wasRightPaneVisible;

		// Properties

		public bool IsLeftPaneActive
			=> ActivePane == (GetPane(0) as IShellPage);

		public bool IsRightPaneActive
			=> ActivePane == (GetPane(1) as IShellPage);

		public IFilesystemHelpers FilesystemHelpers
			=> ActivePane?.FilesystemHelpers!;

		public bool IsMultiPaneActive
			=> GetPaneCount() > 1;

		public bool IsMultiPaneAvailable
		{
			get
			{
				try
				{
					return !AppModel.IsMainWindowClosed && MainWindow.Instance.Bounds.Width > Constants.UI.MultiplePaneWidthThreshold;
				}
				catch
				{
					return false;
				}
			}
		}

		public IShellPage ActivePaneOrColumn
		{
			get
			{
				// Active shell is column view
				if (ActivePane is not null && ActivePane.IsColumnView && ActivePane.SlimContentPage is ColumnsLayoutPage columnLayoutPage)
					return columnLayoutPage.ActiveColumnShellPage;

				return ActivePane ?? GetPane(0)!;
			}
		}

		private ShellPaneArrangement _ShellPaneArrangement;
		public ShellPaneArrangement ShellPaneArrangement
		{
			get => _ShellPaneArrangement;
			set
			{
				if (_ShellPaneArrangement != value)
				{
					_ShellPaneArrangement = value;
					ArrangePanes();
					NotifyPropertyChanged(nameof(ShellPaneArrangement));
					Pane_ContentChanged(null, null!);
				}
			}
		}

		private bool _WindowIsCompact;
		public bool WindowIsCompact
		{
			get => _WindowIsCompact;
			set
			{
				if (value != _WindowIsCompact)
				{
					_WindowIsCompact = value;

					if (value)
					{
						// Close pane
						_wasRightPaneVisible = GetPaneCount() >= 2;

						if (GetPaneCount() >= 2)
							RemovePane(1);
					}
					else if (_wasRightPaneVisible)
					{
						// Add back pane
						if (GetPaneCount() == 1)
							AddPane();

						_wasRightPaneVisible = false;
					}

					NotifyPropertyChanged(nameof(IsMultiPaneAvailable));
				}
			}
		}

		private TabBarItemParameter? _TabBarItemParameter;
		public TabBarItemParameter? TabBarItemParameter
		{
			get => _TabBarItemParameter;
			set
			{
				if (_TabBarItemParameter != value)
				{
					_TabBarItemParameter = value;
					ContentChanged?.Invoke(this, value!);
				}
			}
		}

		private NavigationParams? _NavParamsLeft;
		public NavigationParams? NavParamsLeft
		{
			get => _NavParamsLeft;
			set
			{
				if (_NavParamsLeft != value)
				{
					_NavParamsLeft = value;
					NotifyPropertyChanged(nameof(NavParamsLeft));

					if (GetPane(0) is ModernShellPage page)
						page.NavParams = value!;
				}
			}
		}

		private NavigationParams? _NavParamsRight;
		public NavigationParams? NavParamsRight
		{
			get => _NavParamsRight;
			set
			{
				if (_NavParamsRight != value)
				{
					_NavParamsRight = value;
					NotifyPropertyChanged(nameof(NavParamsRight));

					if (GetPane(1) is ModernShellPage page)
						page.NavParams = value!;
				}
			}
		}

		private IShellPage? _ActivePane;
		public IShellPage? ActivePane
		{
			get => _ActivePane;
			set
			{
				if (_ActivePane != value)
				{
					_ActivePane = value;

					// Reset
					if (GetPane(0) is ModernShellPage firstShellPage)
						firstShellPage.IsCurrentInstance = false;
					if (GetPane(1) is ModernShellPage secondShellPage)
						secondShellPage.IsCurrentInstance = false;

					if (ActivePane is not null)
						ActivePane.IsCurrentInstance = IsCurrentInstance;

					NotifyPropertyChanged(nameof(ActivePane));
					NotifyPropertyChanged(nameof(IsLeftPaneActive));
					NotifyPropertyChanged(nameof(IsRightPaneActive));
					NotifyPropertyChanged(nameof(ActivePaneOrColumn));
					NotifyPropertyChanged(nameof(FilesystemHelpers));

					SetShadow();
				}
			}
		}

		private bool _IsCurrentInstance;
		public bool IsCurrentInstance
		{
			get => _IsCurrentInstance;
			set
			{
				if (_IsCurrentInstance == value)
					return;

				_IsCurrentInstance = value;

				// Reset
				if (GetPane(0) is ModernShellPage firstShellPage)
					firstShellPage.IsCurrentInstance = false;
				if (GetPane(1) is ModernShellPage secondShellPage)
					secondShellPage.IsCurrentInstance = false;

				if (ActivePane is not null)
				{
					ActivePane.IsCurrentInstance = value;

					if (value && ActivePane is BaseShellPage baseShellPage)
						baseShellPage.ContentPage?.ItemManipulationModel.FocusFileList();
				}

				CurrentInstanceChanged?.Invoke(null, this);
			}
		}

		// Events

		public static event EventHandler<ShellPanesPage>? CurrentInstanceChanged;
		public event EventHandler<TabBarItemParameter>? ContentChanged;
		public event PropertyChangedEventHandler? PropertyChanged;

		// Constructor

		public ShellPanesPage()
		{
			InitializeComponent();

			ShellPaneArrangement = GeneralSettingsService.ShellPaneArrangementOption;

			// Initialize the default pane
			AddPane();

			// Set default values
			ActivePane = GetPane(0);

			try
			{
				_WindowIsCompact = MainWindow.Instance.Bounds.Width <= Constants.UI.MultiplePaneWidthThreshold;
				MainWindow.Instance.SizeChanged += MainWindow_SizeChanged;
			}
			catch (Exception)
			{
				// Handle exception in case WinUI Windows is closed
				// (see https://github.com/files-community/Files/issues/15599)

				_WindowIsCompact = false;
			}

			// Open the secondary pane
			if (IsMultiPaneAvailable &&
				GeneralSettingsService.AlwaysOpenDualPaneInNewTab)
				AddPane();
		}

		// Public methods

		/// <inheritdoc/>
		public void OpenSecondaryPane(string path = "", ShellPaneArrangement arrangement = ShellPaneArrangement.None)
		{
			if (GetPaneCount() <= 1)
				AddPane(arrangement is ShellPaneArrangement.None ? GeneralSettingsService.ShellPaneArrangementOption : arrangement);

			NavParamsRight = new() { NavPath = string.IsNullOrEmpty(path) ? "Home" : path };
		}

		/// <inheritdoc/>
		public void ArrangePanes(ShellPaneArrangement arrangement = ShellPaneArrangement.None)
		{
			if (arrangement is not ShellPaneArrangement.None)
				ShellPaneArrangement = arrangement;

			// Clear definitions
			RootGrid.RowDefinitions.Clear();
			RootGrid.ColumnDefinitions.Clear();

			if (ShellPaneArrangement == ShellPaneArrangement.Horizontal)
			{
				foreach (var element in RootGrid.Children)
				{
					if (element is GridSplitter splitter)
					{
						RootGrid.ColumnDefinitions.Add(new() { Width = new(4) });
						splitter.Height = double.NaN;
						splitter.Width = 2;
					}
					else
					{
						RootGrid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star), MinWidth = 100d });
					}

					element.SetValue(Grid.ColumnProperty, RootGrid.ColumnDefinitions.Count - 1);
				}
			}
			else
			{
				foreach (var element in RootGrid.Children)
				{
					if (element is GridSplitter splitter)
					{
						RootGrid.RowDefinitions.Add(new() { Height = new(4) });
						splitter.Height = 2;
						splitter.Width = double.NaN;
					}
					else
					{
						RootGrid.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star), MinHeight = 100d });
					}

					element.SetValue(Grid.RowProperty, RootGrid.RowDefinitions.Count - 1);
				}
			}

			// Update the default cursor type on hover based on pane arrangement
			foreach (var sizer in GetSizers())
			{
				sizer?.ChangeCursor(
					InputSystemCursor.Create(
						ShellPaneArrangement is ShellPaneArrangement.Horizontal
							? InputSystemCursorShape.SizeWestEast
							: InputSystemCursorShape.SizeNorthSouth));
			}
		}

		/// <inheritdoc/>
		public void CloseActivePane()
		{
			if (ActivePane == (IShellPage)GetPane(0)!)
				RemovePane(0);
			else
				RemovePane(1);

			GetPane(0)?.Focus(FocusState.Programmatic);
			SetShadow();
		}

		/// <inheritdoc/>
		public void FocusOtherPane()
		{
			if (ActivePane == (IShellPage)GetPane(0)!)
				GetPane(1)?.Focus(FocusState.Programmatic);
			else
				GetPane(0)?.Focus(FocusState.Programmatic);
		}

		// Private methods

		private ModernShellPage? GetPane(int index = -1)
		{
			if (index is -1 || RootGrid.Children.Count - 1 < index)
				return null;

			var shellPage = RootGrid.Children[index * 2] as ModernShellPage;
			return shellPage;
		}

		private int GetPaneCount()
		{
			return (RootGrid.Children.Count + 1) / 2;
		}

		private IEnumerable<ModernShellPage> GetPanes()
		{
			return RootGrid.Children.Where(x => RootGrid.Children.IndexOf(x) % 2 == 0).Cast<ModernShellPage>();
		}

		private IEnumerable<GridSplitter> GetSizers()
		{
			return RootGrid.Children.Where(x => RootGrid.Children.IndexOf(x) % 2 == 1).Cast<GridSplitter>();
		}

		private void AddPane(ShellPaneArrangement arrangement = ShellPaneArrangement.None)
		{
			if (arrangement is not ShellPaneArrangement.None)
				ShellPaneArrangement = arrangement;

			var currentPaneAlignmentDirection =
				RootGrid.ColumnDefinitions.Count is 0
					? RootGrid.RowDefinitions.Count is 0
						? ShellPaneArrangement
						: ShellPaneArrangement.Vertical
					: ShellPaneArrangement.Horizontal;

			// Adding new pane is not the first time
			if (RootGrid.Children.Count is not 0)
			{
				// Re-align shell pane
				ArrangePanes(arrangement);

				// Add sizer
				var sizer = new GridSplitter() { IsTabStop = false };
				sizer.DoubleTapped += Sizer_OnDoubleTapped;
				sizer.Loaded += Sizer_Loaded;
				sizer.ManipulationCompleted += Sizer_ManipulationCompleted;
				sizer.ManipulationStarted += Sizer_ManipulationStarted;

				// Add sizer
				RootGrid.Children.Add(sizer);

				// Set to a new column
				if (ShellPaneArrangement is ShellPaneArrangement.Horizontal)
				{
					RootGrid.ColumnDefinitions.Add(new() { Width = new(4) });
					sizer.SetValue(Grid.ColumnProperty, RootGrid.ColumnDefinitions.Count - 1);
					sizer.Height = double.NaN;
					sizer.Width = 2;
				}
				else
				{
					RootGrid.RowDefinitions.Add(new() { Height = new(4) });
					sizer.SetValue(Grid.RowProperty, RootGrid.RowDefinitions.Count - 1);
					sizer.Height = 2;
					sizer.Width = double.NaN;
				}
			}

			// Add new pane
			var page = new ModernShellPage() { PaneHolder = this };
			RootGrid.Children.Add(page);

			if (ShellPaneArrangement is ShellPaneArrangement.Horizontal)
			{
				// Add a new definition
				RootGrid.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star), MinWidth = 100d });
				page.SetValue(Grid.ColumnProperty, RootGrid.ColumnDefinitions.Count - 1);

				// Reset width of every definition
				foreach (var definition in RootGrid.ColumnDefinitions.Where(x => RootGrid.ColumnDefinitions.IndexOf(x) % 2 == 0))
					definition.Width = new(1, GridUnitType.Star);
			}
			else
			{
				// Add a new definition
				RootGrid.RowDefinitions.Add(new() { Height = new(1, GridUnitType.Star), MinHeight = 100d });
				page.SetValue(Grid.RowProperty, RootGrid.RowDefinitions.Count - 1);

				// Reset width of every definition
				foreach (var definition in RootGrid.RowDefinitions.Where(x => RootGrid.RowDefinitions.IndexOf(x) % 2 == 0))
					definition.Height = new(1, GridUnitType.Star);
			}

			// Hook event
			page.ContentChanged += Pane_ContentChanged;
			page.Loaded += Pane_Loaded;

			// Focus
			ActivePane = GetPane(GetPaneCount() - 1);

			NotifyPropertyChanged(nameof(IsMultiPaneActive));
		}

		private void RemovePane(int index = -1)
		{
			if (index is -1)
				return;

			// Get proper position of sizer that resides with the pane that is wanted to be removed
			var childIndex = index * 2 - 1;
			childIndex = childIndex >= 0 ? childIndex : 0;
			if (childIndex >= RootGrid.Children.Count)
				return;

			// Left pane is being removed
			if (childIndex == 0)
			{
				var wasMultiPaneActive = IsMultiPaneActive;

				// Remove sizer and pane
				RootGrid.Children.RemoveAt(0);

				if (ShellPaneArrangement is ShellPaneArrangement.Horizontal)
					RootGrid.ColumnDefinitions.RemoveAt(0);
				else
					RootGrid.RowDefinitions.RemoveAt(0);

				if (wasMultiPaneActive)
				{
					RootGrid.Children.RemoveAt(0);

					if (ShellPaneArrangement is ShellPaneArrangement.Horizontal)
						RootGrid.ColumnDefinitions.RemoveAt(0);
					else
						RootGrid.RowDefinitions.RemoveAt(0);

					RootGrid.Children[0].SetValue(Grid.ColumnProperty, 0);
					_NavParamsLeft = new() { NavPath = GetPane(0)?.TabBarItemParameter?.NavigationParameter as string ?? string.Empty };
					_NavParamsRight = null;
					ActivePane = GetPane(0);
				}
			}
			// Right pane is being removed
			else
			{
				// Remove sizer and pane
				RootGrid.Children.RemoveAt(childIndex);
				RootGrid.Children.RemoveAt(childIndex);

				if (ShellPaneArrangement is ShellPaneArrangement.Horizontal)
				{
					RootGrid.ColumnDefinitions.RemoveAt(childIndex);
					RootGrid.ColumnDefinitions.RemoveAt(childIndex);
				}
				else
				{
					RootGrid.RowDefinitions.RemoveAt(childIndex);
					RootGrid.RowDefinitions.RemoveAt(childIndex);
				}

				_NavParamsRight = null;
			}

			Pane_ContentChanged(null, null!);
			NotifyPropertyChanged(nameof(IsMultiPaneActive));
		}

		private void SetShadow()
		{
			if (IsMultiPaneActive)
			{
				// Add theme shadow to the active pane
				if (GetPane(1) is ModernShellPage rightShellPage)
				{
					rightShellPage.RootGrid.Translation = new System.Numerics.Vector3(0, 0, IsLeftPaneActive ? 0 : 32);
					VisualStateManager.GoToState(GetPane(0), IsLeftPaneActive ? ShellBorderFocusOnState : ShellBorderFocusOffState, true);
				}

				if (GetPane(0) is ModernShellPage leftShellPage)
				{
					leftShellPage.RootGrid.Translation = new System.Numerics.Vector3(0, 0, IsLeftPaneActive ? 32 : 0);
					VisualStateManager.GoToState(GetPane(1), IsLeftPaneActive ? ShellBorderFocusOffState : ShellBorderFocusOnState, true);
				}
			}
			else
			{
				if (GetPane(0) is ModernShellPage leftShellPage)
					leftShellPage.RootGrid.Translation = new System.Numerics.Vector3(0, 0, 8);

				VisualStateManager.GoToState(GetPane(0), ShellBorderDualPaneOffState, true);
			}
		}

		// Override methods

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			if (eventArgs.Parameter is string navPath)
			{
				NavParamsLeft = new() { NavPath = navPath };
				NavParamsRight = new() { NavPath = "Home" };
			}
			else if (eventArgs.Parameter is PaneNavigationArguments paneArgs)
			{
				NavParamsLeft = new()
				{
					NavPath = paneArgs.LeftPaneNavPathParam,
					SelectItem = paneArgs.LeftPaneSelectItemParam
				};

				// Creates new pane
				if (GetPaneCount() is 1 &&
					IsMultiPaneAvailable &&
					paneArgs.RightPaneNavPathParam is not null)
					AddPane();

				NavParamsRight = new()
				{
					NavPath = paneArgs.RightPaneNavPathParam,
					SelectItem = paneArgs.RightPaneSelectItemParam
				};

				ShellPaneArrangement =
					paneArgs.ShellPaneArrangement is ShellPaneArrangement.None
						? ShellPaneArrangement.Horizontal
						: paneArgs.ShellPaneArrangement;
			}

			TabBarItemParameter = new()
			{
				InitialPageType = typeof(ShellPanesPage),
				NavigationParameter = new PaneNavigationArguments()
				{
					LeftPaneNavPathParam = NavParamsLeft?.NavPath,
					LeftPaneSelectItemParam = NavParamsLeft?.SelectItem,
					RightPaneNavPathParam = GetPaneCount() >= 2 ? NavParamsRight?.NavPath : null,
					RightPaneSelectItemParam = GetPaneCount() >= 2 ? NavParamsRight?.SelectItem : null,
					ShellPaneArrangement = ShellPaneArrangement,
				}
			};
		}

		// Event methods

		public Task TabItemDragOver(object sender, DragEventArgs e)
		{
			return ActivePane?.TabItemDragOver(sender, e) ?? Task.CompletedTask;
		}

		public Task TabItemDrop(object sender, DragEventArgs e)
		{
			return ActivePane?.TabItemDrop(sender, e) ?? Task.CompletedTask;
		}

		private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
		{
			WindowIsCompact = MainWindow.Instance.Bounds.Width <= Constants.UI.MultiplePaneWidthThreshold;
		}

		private void Pane_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is UIElement element)
			{
				element.GotFocus += Pane_GotFocus;
				element.RightTapped += Pane_RightTapped;
				element.PointerPressed += Pane_PointerPressed;
			}
		}

		private void Pane_ContentChanged(object? sender, TabBarItemParameter e)
		{
			TabBarItemParameter = new()
			{
				InitialPageType = typeof(ShellPanesPage),
				NavigationParameter = new PaneNavigationArguments()
				{
					LeftPaneNavPathParam = GetPane(0)?.TabBarItemParameter?.NavigationParameter as string ?? e?.NavigationParameter as string,
					RightPaneNavPathParam = GetPaneCount() >= 2 ? GetPane(1)?.TabBarItemParameter?.NavigationParameter as string : null,
					ShellPaneArrangement = ShellPaneArrangement,
				}
			};
		}

		private void Pane_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (sender != ActivePane && sender is IShellPage shellPage && shellPage.SlimContentPage is not ColumnsLayoutPage)
				(sender as UIElement)?.Focus(FocusState.Pointer);
		}

		private void Pane_GotFocus(object sender, RoutedEventArgs e)
		{
			var isLeftPane = sender == (GetPane(0) as IShellPage);

			// Clear selection in left pane
			if (isLeftPane && GetPane(1) is ModernShellPage secondShellPage && (secondShellPage.SlimContentPage?.IsItemSelected ?? false))
			{
				secondShellPage.SlimContentPage.LockPreviewPaneContent = true;
				secondShellPage.SlimContentPage.ItemManipulationModel.ClearSelection();
				secondShellPage.SlimContentPage.LockPreviewPaneContent = false;
			}
			// Clear selection in right pane
			else if (!isLeftPane && GetPane(0) is ModernShellPage firstShellPage && (firstShellPage.SlimContentPage?.IsItemSelected ?? false))
			{
				firstShellPage.SlimContentPage.LockPreviewPaneContent = true;
				firstShellPage.SlimContentPage.ItemManipulationModel.ClearSelection();
				firstShellPage.SlimContentPage.LockPreviewPaneContent = false;
			}

			var newActivePane = isLeftPane ? GetPane(0) : GetPane(1);
			if (ActivePane != (newActivePane as IShellPage))
				ActivePane = newActivePane;
		}

		private void Pane_RightTapped(object sender, RoutedEventArgs e)
		{
			if (sender != ActivePane && sender is IShellPage shellPage && shellPage.SlimContentPage is not ColumnsLayoutPage)
				((UIElement)sender).Focus(FocusState.Programmatic);
		}

		private void Sizer_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is GridSplitter sizer)
			{
				sizer.ChangeCursor(
					InputSystemCursor.Create(
						ShellPaneArrangement is ShellPaneArrangement.Horizontal
							? InputSystemCursorShape.SizeWestEast
							: InputSystemCursorShape.SizeNorthSouth));
			}
		}

		private void Sizer_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if (ShellPaneArrangement is ShellPaneArrangement.Horizontal)
			{
				var definitions = RootGrid.ColumnDefinitions.Where(x => RootGrid.ColumnDefinitions.IndexOf(x) % 2 == 0);
				definitions?.ForEach(x => x.Width = new GridLength(1, GridUnitType.Star));
			}
			else
			{
				var definitions = RootGrid.RowDefinitions.Where(x => RootGrid.RowDefinitions.IndexOf(x) % 2 == 0);
				definitions?.ForEach(x => x.Height = new GridLength(1, GridUnitType.Star));
			}
		}

		private void Sizer_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
		{
			this.ChangeCursor(
				InputSystemCursor.Create(
					ShellPaneArrangement is ShellPaneArrangement.Horizontal
						? InputSystemCursorShape.SizeWestEast
						: InputSystemCursorShape.SizeNorthSouth));
		}

		private void Sizer_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			if (GetPane(1) is ModernShellPage secondShellPage &&
				secondShellPage.ActualWidth <= 100)
				RemovePane(1);

			this.ChangeCursor(InputSystemCursor.Create(InputSystemCursorShape.Arrow));
		}

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// Disposer

		public void Dispose()
		{
			MainWindow.Instance.SizeChanged -= MainWindow_SizeChanged;

			// Dispose panes
			foreach (var pane in GetPanes())
			{
				pane.Loaded -= Pane_Loaded;
				pane.ContentChanged -= Pane_ContentChanged;
				pane.GotFocus -= Pane_GotFocus;
				pane.RightTapped -= Pane_RightTapped;
				pane.PointerPressed -= Pane_PointerPressed;
				pane.Dispose();
			}

			// Dispose sizers
			foreach (var sizer in GetSizers())
			{
				sizer.DoubleTapped -= Sizer_OnDoubleTapped;
				sizer.Loaded -= Sizer_Loaded;
				sizer.ManipulationCompleted -= Sizer_ManipulationCompleted;
				sizer.ManipulationStarted -= Sizer_ManipulationStarted;
			}
		}
	}
}
