// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Files.App.Controls
{
	public partial class ThemedIcon
	{
		private bool _isOwnerToggled;
		private bool _isOwnerEnabled;

		private Control? ownerControl = null;
		private ToggleButton? ownerToggleButton = null;

		private void FindOwnerControlStates()
		{
			if (this.FindAscendant<ToggleButton>() is ToggleButton toggleButton)
			{
				ownerToggleButton = toggleButton;

				// IsChecked/IsToggled change aware
				ownerToggleButton.Checked += OwnerControl_IsCheckedChanged;
				ownerToggleButton.Unchecked += OwnerControl_IsCheckedChanged;
				_isOwnerToggled = ownerToggleButton.IsChecked is true;
			}

			if (this.FindAscendant<Control>() is Control control)
			{
				ownerControl = control;

				// IsEnabled change aware
				ownerControl.IsEnabledChanged += OwnerControl_IsEnabledChanged;
				_isOwnerEnabled = ownerControl.IsEnabled;

				UpdateVisualStates();
			}
		}

		private void OwnerControl_IsCheckedChanged(object sender, RoutedEventArgs e)
		{
			if (ownerToggleButton is null)
				return;

			_isOwnerToggled = ownerToggleButton.IsChecked is true;
			UpdateVisualStates();

		}

		private void OwnerControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (ownerControl is null)
				return;

			_isOwnerEnabled = ownerControl.IsEnabled;
			UpdateVisualStates();
		}
	}
}
