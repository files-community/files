// Copyright (c) 2024 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Linq;
using System.Collections.Generic;

namespace Files.App.Controls
{
    /// <summary>
    /// A control for a State and Color aware Icon
    /// </summary>
    public partial class ThemedIcon : Control
    {
        // private IAppThemeModeService AppThemeModeService = Ioc.Default.GetRequiredService<IAppThemeModeService>();

        bool _isHighContrast;
        bool _isToggled;
        bool _isEnabled;

        ToggleButton? ownerToggleButton = null;
        AppBarToggleButton? ownerAppBarToggleButton = null;
        Control? ownerControl = null;

        public ThemedIcon()
        {
            DefaultStyleKey = typeof(ThemedIcon);
        }

        protected override void OnApplyTemplate()
        {
            IsEnabledChanged -= OnIsEnabledChanged;

            base.OnApplyTemplate();

            // AppThemeModeService.IsHighContrastChanged += ThemeSettings_OnHighContrastChanged;

            IsEnabledChanged += OnIsEnabledChanged;

            InitialIconStateValues();
            FindOwnerControlStates();
            UpdateIconContent();
            UpdateIconStates();
            UpdateVisualStates();
        }

        private void UpdateIconContent()
        {
            // Updates PathData and Layers

            FilledIconPathUpdate();
            OutlineIconPathUpdate();
            LayeredIconContentUpdate();
        }

        private void FilledIconPathUpdate()
        {
            // Updates Filled Icon from Path Data

            if (GetTemplateChild(FilledPathIconViewBox) is not Viewbox filledViewBox)
                return;

            SetPathData(FilledIconPath, FilledIconData ?? string.Empty, filledViewBox);
        }

        private void OutlineIconPathUpdate()
        {
            // Updates Outline Icon from Path Data

            if (GetTemplateChild(OutlinePathIconViewBox) is not Viewbox outlineViewBox)
                return;

            SetPathData(OutlineIconPath, OutlineIconData ?? string.Empty, outlineViewBox);
        }

        private void LayeredIconContentUpdate()
        {
            // Updates Layered Icon from it's Layers

            if (GetTemplateChild(LayeredPathIconViewBox) is not Viewbox layeredViewBox ||
                GetTemplateChild(LayeredPathCanvas) is not Canvas canvas ||
                Layers is not ICollection<ThemedIconLayer> layers)
                return;

            foreach (var layer in layers)
            {
                canvas.Children
                    .Add(
                    new ThemedIconLayer()
                    {
                        LayerType = layer.LayerType,
                        IconColorType = layer.IconColorType,
                        PathData = layer.PathData,
                        Opacity = layer.Opacity,
                    });
            }
        }

        private void SetPathData(string partName, string pathData, FrameworkElement element)
        {
            // Updates PathData

            if (string.IsNullOrEmpty(pathData))
                return;

            var geometry = (Geometry)XamlReader.Load(
                $"<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{pathData}</Geometry>");

            if (GetTemplateChild(partName) is Path path)
            {
                path.Data = geometry;
                path.Width = element.Width;
                path.Height = element.Height;
            }
        }

        private void FindOwnerControlStates()
        {
            // Finds the owner Control and it's Checked and Enabled state

            //
            // Check if owner Control is a ToggleButton
            // Hooks onto Event handlers when IsChecked and IsUnchecked runs
            // Runs the ToggleChanged event, to set initial value, if the ToggleButton's isChecked is true
            //
            // Check if owner Control is an AppBarToggleButton
            // Hooks onto Event handlers when IsChecked and IsUnchecked runs
            // Runs the ToggleChanged event, to set initial value, if the AppBarToggleButton's isChecked is true
            //
            // Gets the owner Control
            // Hooks onto Event handlers when IsEnabledChanged runs
            // Runs the EnabledChanged event to set initial value
            //

            ownerToggleButton = this.FindAscendant<ToggleButton>();

            if (ownerToggleButton != null)
            {
                ownerToggleButton.Checked += OwnerControl_IsCheckedChanged;
                ownerToggleButton.Unchecked += OwnerControl_IsCheckedChanged;

                ToggleChanged(ownerToggleButton.IsChecked is true);
            }

            ownerAppBarToggleButton = this.FindAscendant<AppBarToggleButton>();

            if (ownerAppBarToggleButton != null)
            {
                ownerAppBarToggleButton.Checked += OwnerControl_IsCheckedChanged;
                ownerAppBarToggleButton.Unchecked += OwnerControl_IsCheckedChanged;

                ToggleChanged(ownerAppBarToggleButton.IsChecked is true);
            }

            ownerControl = this.FindAscendant<Control>();

            if (ownerControl != null)
            {
                ownerControl.IsEnabledChanged += OwnerControl_IsEnabledChanged;

                EnabledChanged(ownerControl.IsEnabled);
            }
        }

        private void OwnerControl_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            // Responds to owner checked changes

            if (ownerToggleButton is null && ownerAppBarToggleButton is null)
                return;

            if (ownerToggleButton is not null)
                ToggleChanged(ownerToggleButton.IsChecked is true);
            else if (ownerAppBarToggleButton is not null)
                ToggleChanged(ownerAppBarToggleButton.IsChecked is true);
        }

        private void OwnerControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Responds to owner control enabled changes

            if (ownerControl is null)
                return;

            EnabledChanged(ownerControl.IsEnabled);
        }

        private void ToggleChanged(bool value)
        {
            // Handles the IsToggled property change

            _isToggled = value;

            UpdateVisualStates();
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Handles for the derived control's IsEnabled property change

            EnabledChanged((bool)e.NewValue);
        }

        private void EnabledChanged(bool value)
        {
            // Handles the IsEnabled property change

            _isEnabled = value;

            UpdateVisualStates();
        }

        private void ThemeSettings_OnHighContrastChanged(object sender, bool e)
        {
            HighContrastChanged(e);
        }

        private void HighContrastChanged(bool value)
        {
            // handles HighContrast property change

            _isHighContrast = value;

            UpdateVisualStates();
        }

        private void InitialIconStateValues()
        {
            _isEnabled = IsEnabled;
            _isToggled = IsToggled;
        }

        private void UpdateIconStates()
        {
            ToggleChanged(_isToggled);
            EnabledChanged(_isEnabled);
            HighContrastChanged(_isHighContrast);
        }

        private void UpdateVisualStates()
        {
            // Updates all Icon Visual States.

            UpdateIconTypeStates();
            UpdateIconColorTypeStates();
        }

        private void UpdateIconTypeStates()
        {
            // Handles changes to the IconType and setting the correct Visual States.

            // Handles the three IconType states, based on the ThemedIcon.IconType value
            // as well as states derived from owner controls, and other properties

            // We first check for isToggled and Filled icon types
            // Then we check for Contrast and Disabled states, to replace Layered with Outline and set EnabledStates
            // Finally we assigned Filled and Layered states, and default otherwise to Outline

            if (_isToggled is true || IsToggled is true || IconType == ThemedIconTypes.Filled)
            {
                VisualStateManager.GoToState(this, FilledTypeStateName, true);
                return;
            }
            else if (_isHighContrast is true || _isEnabled is false || IsEnabled is false)
            {
                VisualStateManager.GoToState(
                    this,
                    IconType switch
                    {
                        ThemedIconTypes.Filled => FilledTypeStateName,
                        ThemedIconTypes.Layered => OutlineTypeStateName,
                        _ => OutlineTypeStateName,
                    },
                    true);

                VisualStateManager.GoToState(this, NotEnabledStateName, true);
            }
            else
            {
                VisualStateManager.GoToState(
                    this,
                    IconType switch
                    {
                        ThemedIconTypes.Filled => FilledTypeStateName,
                        ThemedIconTypes.Layered => LayeredTypeStateName,
                        _ => OutlineTypeStateName,
                    },
                    true);

                VisualStateManager.GoToState(this, EnabledStateName, true);
            }
        }

        private void UpdateIconColorTypeStates()
        {
            // Handles changes to the IconColorType and setting the correct Visual States.

            // We first check if the Icon is Disabled
            // Then we check if the Disabled Icon is Toggled

            // We then assume the Icon is Enabled
            // We then check the Toggled state for the Contrast Icons
            // We have two states depending on toggle.

            // Finally we act on all other Enabled states
            // We check for Toggled state
            // And update the IconColorType in the Layered Icon's Layers
            if (_isEnabled is false || IsEnabled is false)
            {
                if (_isToggled is true || IsToggled is true)
                {
                    VisualStateManager.GoToState(this, DisabledToggleStateName, true);
                }
                else
                {
                    VisualStateManager.GoToState(this, DisabledStateName, true);
                }
            }
            else
            {
                if (_isToggled is true || IsToggled is true)
                {
                    VisualStateManager.GoToState(this, ToggleStateName, true);
                }
                else
                {
                    VisualStateManager.GoToState(
                        this,
                        IconColorType switch
                        {
                            ThemedIconColorType.Critical  => CriticalStateName,
                            ThemedIconColorType.Caution => CautionStateName,
                            ThemedIconColorType.Success => SuccessStateName,
                            ThemedIconColorType.Neutral => NeutralStateName,
                            ThemedIconColorType.Accent => AccentStateName,
                            _ => NormalStateName,
                        },
                        true);
                }

                if (GetTemplateChild(LayeredPathCanvas) is Canvas canvas)
                {
                    foreach (var layer in canvas.Children.Cast<ThemedIconLayer>())
                        layer.IconColorType = IconColorType;
                    
                }
            }
        }
    }
}