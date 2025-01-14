// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media;

namespace Files.App.Controls
{
	[DependencyProperty<ThemedIconLayerType>("LayerType", nameof(OnLayerTypePropertyChanged), DefaultValue = "ThemedIconLayerType.Base")]
	[DependencyProperty<string>("PathData", nameof(OnLayerPathDataPropertyChanged), DefaultValue = "string.Empty")]
	[DependencyProperty<double>("LayerSize", nameof(OnLayerSizePropertyChanged), DefaultValue = "(double)16")]
	[DependencyProperty<Brush>("LayerColor", nameof(OnColorPropertyChanged))]
	[DependencyProperty<ThemedIconColorType>("IconColorType", nameof(OnIconColorTypePropertyChanged), DefaultValue = "ThemedIconColorType.Normal")]
	public partial class ThemedIconLayer
	{
		protected virtual void OnLayerTypePropertyChanged(ThemedIconLayerType oldValue, ThemedIconLayerType newValue)
		{
			LayerTypeChanged(newValue);
		}

		protected virtual void OnLayerPathDataPropertyChanged(string oldValue, string newValue)
		{
			LayerPathDataChanged(newValue);
		}

		protected virtual void OnLayerSizePropertyChanged(double oldValue, double newValue)
		{
			LayerSizePropertyChanged(newValue);
		}

		protected virtual void OnColorPropertyChanged(Brush oldValue, Brush newValue)
		{
			IconColorTypeChanged();
		}

		protected virtual void OnIconColorTypePropertyChanged(ThemedIconColorType oldValue, ThemedIconColorType newValue)
		{
			IconColorTypeChanged();
		}
	}
}
