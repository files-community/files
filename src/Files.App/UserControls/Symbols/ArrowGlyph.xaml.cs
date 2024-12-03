using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.AnimatedVisuals;
using Microsoft.UI.Xaml.Media;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Files.App.UserControls.Symbols
{
	[DependencyProperty<double>("FontSize", DefaultValue = "(double)12.0")]
	[DependencyProperty<Brush>("Foreground", nameof(OnColorChange), DefaultValue = "null")]
	[DependencyProperty<ArrowSymbolType>("Arrow", nameof(OnArrowChange), DefaultValue = "Files.App.Data.Enums.ArrowSymbolType.ChevronRight")]
	[DependencyProperty<bool>("UseAnimatedIcon", nameof(OnUseAnimatedIconChanged), DefaultValue = "false")]
	[DependencyProperty<string>("Glyph", DefaultValue = "null")]
	public partial class ArrowGlyph : UserControl, INotifyPropertyChanged, IRealTimeControl
	{
		private static ICommandManager Commands { get; } = Ioc.Default.GetRequiredService<ICommandManager>();

		private string ForwardGlyph { get; } = Commands.NavigateForward.Glyph.BaseGlyph;
		private string BackGlyph { get; } = Commands.NavigateBack.Glyph.BaseGlyph;
		private string ChevronLeft { get; } = "\uE76B";
		private string ChevronRight { get; } = "\uE76C";

		private long _token;

		public event PropertyChangedEventHandler? PropertyChanged;

		public bool IsStaticIconVisible => !UseAnimatedIcon;

		public bool IsAnimatedIconVisible => UseAnimatedIcon;

		public ArrowGlyph()
		{
			InitializeComponent();
			InitializeContentLayout();
			UpdateGlyph();
			RealTimeLayoutService.AddCallback(this, UpdateGlyph);
		}


		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void UpdateGlyph()
		{
			if (UseAnimatedIcon)
			{
				SymbolGrid.Rotation = Arrow switch
				{
					ArrowSymbolType.Forward => FlowDirection == FlowDirection.LeftToRight ? 180 : 0,
					ArrowSymbolType.Back => FlowDirection == FlowDirection.LeftToRight ? 0 : 180,
					_ => 0
				};
				return;
			}

			Glyph = Arrow switch
			{
				ArrowSymbolType.Forward => FlowDirection == FlowDirection.LeftToRight ? ForwardGlyph : BackGlyph,
				ArrowSymbolType.Back => FlowDirection == FlowDirection.LeftToRight ? BackGlyph : ForwardGlyph,
				ArrowSymbolType.ChevronLeft => FlowDirection == FlowDirection.LeftToRight ? ChevronLeft : ChevronRight,
				ArrowSymbolType.ChevronRight => FlowDirection == FlowDirection.LeftToRight ? ChevronRight : ChevronLeft,
				_ => ChevronLeft
			};
		}

		private void OnColorChange(Brush oldValue, Brush newValue)
		{
			if (oldValue != newValue)
			{
				SymbolIcon.Foreground = newValue;
				UpdateGlyph();
			}
		}

		private void OnArrowChange(ArrowSymbolType oldValue, ArrowSymbolType newValue)
		{
			if (oldValue != newValue)
				UpdateGlyph();
		}

		private void OnUseAnimatedIconChanged(bool oldValue, bool newValue)
		{
			if (oldValue != newValue)
			{
				UpdateGlyph();
				OnPropertyChanged(nameof(IsStaticIconVisible));
				OnPropertyChanged(nameof(IsAnimatedIconVisible));
			}
		}

		private void SymbolGrid_Loaded(object sender, RoutedEventArgs e)
		{
			if (UseAnimatedIcon)
			{
				var centerX = SymbolGrid.ActualWidth / 2;
				var centerY = SymbolGrid.ActualHeight / 2;
				SymbolGrid.CenterPoint = new Vector3((float)centerX, (float)centerY, 0);
			}
		}
	}

}
