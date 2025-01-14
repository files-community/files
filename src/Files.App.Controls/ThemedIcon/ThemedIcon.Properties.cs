// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Files.App.Controls
{
	[DependencyProperty<string>("FilledIconData", nameof(OnFilledIconPropertyChanged))]
	[DependencyProperty<string>("OutlineIconData", nameof(OnOutlineIconPropertyChanged))]
	[DependencyProperty<Brush>("Color", nameof(OnColorPropertyChanged))]
	[DependencyProperty<ThemedIconTypes>("IconType", nameof(OnIconTypePropertyChanged), DefaultValue = "ThemedIconTypes.Layered")]
	[DependencyProperty<ThemedIconColorType>("IconColorType", nameof(OnIconColorTypePropertyChanged), DefaultValue = "ThemedIconColorType.None")]
	[DependencyProperty<double>("IconSize", nameof(OnIconSizePropertyChanged), DefaultValue = "(double)16")]
	[DependencyProperty<bool>("IsToggled", nameof(OnIsToggledPropertyChanged), DefaultValue = "false")]
	[DependencyProperty<bool>("IsFilled", nameof(OnIsFilledPropertyChanged), DefaultValue = "false")]
	[DependencyProperty<bool>("IsHighContrast", nameof(OnIsHighContrastPropertyChanged), DefaultValue = "false")]
	[DependencyProperty<object>("Layers", nameof(OnLayersPropertyChanged))]
	[DependencyProperty<ToggleBehaviors>("ToggleBehavior", nameof(OnToggleBehaviorPropertyChanged), DefaultValue = "ToggleBehaviors.Auto")]
	public partial class ThemedIcon : Control
	{
		protected virtual void OnFilledIconPropertyChanged(string oldValue, string newValue)
		{
			OnFilledIconChanged();
		}

		protected virtual void OnOutlineIconPropertyChanged(string oldValue, string newValue)
		{
			OnOutlineIconChanged();
		}

		protected virtual void OnColorPropertyChanged(Brush oldValue, Brush newValue)
		{
			OnIconTypeChanged();
			OnIconColorChanged();
		}

		protected virtual void OnIconTypePropertyChanged(ThemedIconTypes oldValue, ThemedIconTypes newValue)
		{
			OnIconTypeChanged();
		}

		protected virtual void OnIconColorTypePropertyChanged(ThemedIconColorType oldValue, ThemedIconColorType newValue)
		{
			OnIconColorTypeChanged();
		}

		protected virtual void OnIconSizePropertyChanged(double oldValue, double newValue)
		{
			UpdateVisualStates();
		}

		protected virtual void OnIsToggledPropertyChanged(bool oldValue, bool newValue)
		{
			UpdateVisualStates();
		}

		protected virtual void OnIsFilledPropertyChanged(bool oldValue, bool newValue)
		{
			UpdateVisualStates();
		}

		protected virtual void OnIsHighContrastPropertyChanged(bool oldValue, bool newValue)
		{
			UpdateVisualStates();
		}

		protected virtual void OnLayersPropertyChanged(object oldValue, object newValue)
		{
			UpdateVisualStates();
		}

		protected virtual void OnToggleBehaviorPropertyChanged(ToggleBehaviors oldValue, ToggleBehaviors newValue)
		{
			UpdateVisualStates();
		}
	}
}
