﻿using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Files.ViewModels.SettingsViewModels
{
    public class MultitaskingViewModel : ObservableObject
    {
        private bool alwaysOpenDualPaneInNewTab = App.AppSettings.AlwaysOpenDualPaneInNewTab;
        private bool isDualPaneEnabled = App.AppSettings.IsDualPaneEnabled;
        private bool isHorizontalTabStripOn = App.AppSettings.IsHorizontalTabStripOn;
        private bool isMultitaskingExperienceAdaptive = App.AppSettings.IsMultitaskingExperienceAdaptive;
        private bool isVerticalTabFlyoutOn = App.AppSettings.IsVerticalTabFlyoutOn;

        public bool AlwaysOpenDualPaneInNewTab
        {
            get
            {
                return alwaysOpenDualPaneInNewTab;
            }
            set
            {
                if (SetProperty(ref alwaysOpenDualPaneInNewTab, value))
                {
                    App.AppSettings.AlwaysOpenDualPaneInNewTab = value;
                }
            }
        }

        public InteractionViewModel InteractionViewModel => App.InteractionViewModel;

        public bool IsDualPaneEnabled
        {
            get
            {
                return isDualPaneEnabled;
            }
            set
            {
                if (SetProperty(ref isDualPaneEnabled, value))
                {
                    App.AppSettings.IsDualPaneEnabled = value;
                }
            }
        }

        public bool IsHorizontalTabStripOn
        {
            get
            {
                return isHorizontalTabStripOn;
            }
            set
            {
                if (SetProperty(ref isHorizontalTabStripOn, value))
                {
                    App.AppSettings.IsHorizontalTabStripOn = value;

                    // Setup the correct multitasking control
                    InteractionViewModel.SetMultitaskingControl();
                }
            }
        }

        public bool IsMultitaskingExperienceAdaptive
        {
            get
            {
                return isMultitaskingExperienceAdaptive;
            }
            set
            {
                if (SetProperty(ref isMultitaskingExperienceAdaptive, value))
                {
                    App.AppSettings.IsMultitaskingExperienceAdaptive = value;

                    // Setup the correct multitasking control
                    InteractionViewModel.SetMultitaskingControl();
                }
            }
        }

        public bool IsVerticalTabFlyoutOn
        {
            get
            {
                return isVerticalTabFlyoutOn;
            }
            set
            {
                if (SetProperty(ref isVerticalTabFlyoutOn, value))
                {
                    App.AppSettings.IsVerticalTabFlyoutOn = value;

                    // Setup the correct multitasking control
                    InteractionViewModel.SetMultitaskingControl();
                }
            }
        }
    }
}