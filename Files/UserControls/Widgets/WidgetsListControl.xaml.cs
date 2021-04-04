﻿using Windows.UI.Xaml.Controls;
using Files.ViewModels.Widgets;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.Widgets
{
    public sealed partial class WidgetsListControl : UserControl
    {
        public WidgetsListControlViewModel ViewModel
        {
            get => (WidgetsListControlViewModel)DataContext;
            set => DataContext = value;
        }

        public WidgetsListControl()
        {
            this.InitializeComponent();

            this.ViewModel = new WidgetsListControlViewModel();
        }
    }
}
