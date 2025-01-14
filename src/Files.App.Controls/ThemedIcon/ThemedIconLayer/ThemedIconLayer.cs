// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Files.App.Controls
{
	/// <summary>
	/// A Layer for the ThemedIcon control's Layered IconType
	/// </summary>
	public partial class ThemedIconLayer : Control
	{
		private double _layerSize;

		public ThemedIconLayer()
		{
			DefaultStyleKey = typeof(ThemedIconLayer);
		}

		protected override void OnApplyTemplate()
		{
			// Initialize with default values

			base.OnApplyTemplate();

			UpdateIconLayerState();
			LayerPathDataChanged(PathData);
		}

		private void LayerTypeChanged(ThemedIconLayerType layerType)
		{
			UpdateIconLayerState();
		}

		private void LayerPathDataChanged(string pathData)
		{
			if (GetTemplateChild(LayerCanvasPart) is not Canvas layerCanvas)
				return;

			SetPathData(pathData ?? string.Empty, layerCanvas);
		}

		private void LayerSizePropertyChanged(double value)
		{
			// Code to handle the design time Icon Size changing

			_layerSize = value;

			LayerPathDataChanged(PathData);
			UpdateIconLayerState();
		}

		private void IconColorTypeChanged()
		{
			UpdateIconLayerState();
		}

		private void UpdateIconLayerState()
		{
			if (LayerType == ThemedIconLayerType.Accent)
			{
				VisualStateManager.GoToState(
					this,
					IconColorType switch
					{
						ThemedIconColorType.Critical => CriticalStateName,
						ThemedIconColorType.Caution => CautionStateName,	
						ThemedIconColorType.Success => SuccessStateName,	
						ThemedIconColorType.Neutral => NeutralStateName,
						ThemedIconColorType.Custom => CustomColorStateName,
						_ => AccentStateName,
					},
				true);
			}
			else if (LayerType == ThemedIconLayerType.AccentContrast)
			{
				VisualStateManager.GoToState(
					this,
					IconColorType switch
					{
						ThemedIconColorType.Critical => CriticalBGStateName,
						ThemedIconColorType.Caution => CautionBGStateName,
						ThemedIconColorType.Success => SuccessBGStateName,
						ThemedIconColorType.Neutral => NeutralBGStateName,
						ThemedIconColorType.Custom => CustomColorBGStateName,
						_ => AccentContrastStateName,
					},
					true);
			}
			else
			{
				VisualStateManager.GoToState(
					this,
					LayerType switch
					{
						ThemedIconLayerType.Alt => AltStateName,
						_ => BaseStateName,
					},
					true);
			}
		}
		private void SetPathData(string pathData, FrameworkElement element)
		{
			// Code to take the PathData string, and convert it to an actual path

			// If there is no Path Data, we just exit
			// If there is, we create a new Geometry and insert our Path Data string
			// We check the Template Part exists, and is the right type, and assign it's data

			if (string.IsNullOrEmpty(pathData))
				return;

			var geometry = (Geometry)XamlReader.Load(
				$"<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>{pathData}</Geometry>");

			if (GetTemplateChild(LayerPathPart) is Path path)
			{
				path.Data = geometry;
				path.Width = element.Width;
				path.Width = element.Height;
			}
		}
	}
}
