using Files.Helpers.XamlHelpers;
using Files.ViewModels;
using System.Windows.Input;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;

namespace Files.UserControls
{
    public sealed partial class NavigationToolbar : UserControl
    {
        public NavToolbarViewModel ViewModel
        {
            get => (NavToolbarViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(NavToolbarViewModel), typeof(NavigationToolbar), new PropertyMetadata(null));

        public ISearchBox SearchBox => ViewModel.SearchBox;

        public MainViewModel MainViewModel => App.MainViewModel;

        public static readonly DependencyProperty CanPasteInPageProperty = DependencyProperty.Register(
          "CanPasteInPage",
          typeof(bool),
          typeof(NavigationToolbar),
          new PropertyMetadata(null)
        );

        public bool CanPasteInPage
        {
            get
            {
                return (bool)GetValue(CanPasteInPageProperty);
            }
            set
            {
                SetValue(CanPasteInPageProperty, value);
            }
        }

        public SettingsViewModel AppSettings => App.AppSettings;

        public NavigationToolbar()
        {
            InitializeComponent();
            Loading += NavigationToolbar_Loading;
        }

        private void NavigationToolbar_Loading(FrameworkElement sender, object args)
        {
            OngoingTasksViewModel.ProgressBannerPosted += OngoingTasksActions_ProgressBannerPosted;
        }

        private void VisiblePath_Loaded(object sender, RoutedEventArgs e)
        {
            // AutoSuggestBox won't receive focus unless it's fully loaded
            VisiblePath.Focus(FocusState.Programmatic);
            DependencyObjectHelpers.FindChild<TextBox>(VisiblePath)?.SelectAll();
        }

        private void ManualPathEntryItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsEditModeEnabled = true;
        }

        private void VisiblePath_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                ViewModel.IsEditModeEnabled = false;
            }
        }

        private void VisiblePath_LostFocus(object sender, RoutedEventArgs e)
        {
            if (FocusManager.GetFocusedElement() is FlyoutBase ||
                FocusManager.GetFocusedElement() is AppBarButton ||
                FocusManager.GetFocusedElement() is Popup)
            {
                return;
            }

            var element = FocusManager.GetFocusedElement();
            var elementAsControl = element as Control;
            if (elementAsControl == null)
            {
                return;
            }
            else if (elementAsControl.FocusState != FocusState.Programmatic && elementAsControl.FocusState != FocusState.Keyboard)
            {
                ViewModel.IsEditModeEnabled = false;
            }
            else
            {
                if (ViewModel.IsEditModeEnabled)
                {
                    this.VisiblePath.Focus(FocusState.Programmatic);
                }
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e) => ViewModel.SwitchSearchBoxVisibility();

        private void SearchBox_Escaped(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args) => ViewModel.CloseSearchBox();

        private void SearchRegion_LostFocus(object sender, RoutedEventArgs e) => ViewModel.SearchRegion_LostFocus(sender, e);

        private void VisiblePath_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => ViewModel.VisiblePath_QuerySubmitted(sender, args);

        public void SetShellCommandBarContextItems()
        {
        }

        public bool ShowSearchBox
        {
            get { return (bool)GetValue(ShowSearchBoxProperty); }
            set { SetValue(ShowSearchBoxProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CollapseSearchBox.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowSearchBoxProperty =
            DependencyProperty.Register(nameof(ShowSearchBox), typeof(bool), typeof(NavigationToolbar), new PropertyMetadata(null));

        public static readonly DependencyProperty SettingsButtonCommandProperty = DependencyProperty.Register(nameof(SettingsButtonCommand), typeof(ICommand), typeof(NavigationToolbar), new PropertyMetadata(null));

        public ICommand SettingsButtonCommand
        {
            get => (ICommand)GetValue(SettingsButtonCommandProperty);
            set => SetValue(SettingsButtonCommandProperty, value);
        }

        public OngoingTasksViewModel OngoingTasksViewModel { get; set; }

        private void OngoingTasksActions_ProgressBannerPosted(object sender, PostedStatusBanner e)
        {
            if (AppSettings.ShowOngoingTasksTeachingTip)
            {
                OngoingTasksTeachingTip.IsOpen = true;
                OngoingTasksTeachingTip.Visibility = Windows.UI.Xaml.Visibility.Visible;
                AppSettings.ShowOngoingTasksTeachingTip = false;
            }
            else
            {
                OngoingTasksTeachingTip.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                OngoingTasksTeachingTip.IsOpen = false;
            }
        }

        public bool ShowOngoingTasks
        {
            get => (bool)GetValue(ShowOngoingTasksProperty);
            set => SetValue(ShowOngoingTasksProperty, value);
        }

        // Using a DependencyProperty as the backing store for ShowOngoingTasks.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowOngoingTasksProperty =
            DependencyProperty.Register(nameof(ShowOngoingTasks), typeof(bool), typeof(NavigationToolbar), new PropertyMetadata(null));

        public bool ShowSettingsButton
        {
            get => (bool)GetValue(dp: ShowSettingsButtonProperty);
            set => SetValue(ShowSettingsButtonProperty, value);
        }

        // Using a DependencyProperty as the backing store for ShowSettingsButton.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowSettingsButtonProperty =
            DependencyProperty.Register(nameof(ShowSettingsButton), typeof(bool), typeof(NavigationToolbar), new PropertyMetadata(null));
    }
}