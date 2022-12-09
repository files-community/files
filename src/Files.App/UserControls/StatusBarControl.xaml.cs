using Files.App.DataModels;
using Files.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Files.App.UserControls
{
	public sealed partial class StatusBarControl : UserControl, INotifyPropertyChanged
	{
		public AppModel AppModel
			=> App.AppModel;

		public DirectoryPropertiesViewModel DirectoryPropertiesViewModel
		{
			get => (DirectoryPropertiesViewModel)GetValue(DirectoryPropertiesViewModelProperty);
			set => SetValue(DirectoryPropertiesViewModelProperty, value);
		}

		public static readonly DependencyProperty DirectoryPropertiesViewModelProperty =
			DependencyProperty.Register(
				nameof(DirectoryPropertiesViewModel),
				typeof(DirectoryPropertiesViewModel),
				typeof(StatusBarControl),
				new PropertyMetadata(null));

		public SelectedItemsPropertiesViewModel SelectedItemsPropertiesViewModel
		{
			get => (SelectedItemsPropertiesViewModel)GetValue(SelectedItemsPropertiesViewModelProperty);
			set => SetValue(SelectedItemsPropertiesViewModelProperty, value);
		}

		public static readonly DependencyProperty SelectedItemsPropertiesViewModelProperty =
			DependencyProperty.Register(
				nameof(SelectedItemsPropertiesViewModel),
				typeof(SelectedItemsPropertiesViewModel),
				typeof(StatusBarControl),
				new PropertyMetadata(null));

		public bool ShowInfoText
		{
			get => (bool)GetValue(ShowInfoTextProperty);
			set => SetValue(ShowInfoTextProperty, value);
		}

		public static readonly DependencyProperty ShowInfoTextProperty =
			DependencyProperty.Register(
				nameof(ShowInfoText),
				typeof(bool),
				typeof(StatusBarControl),
				new PropertyMetadata(null));

		public StatusBarControl()
		{
			this.InitializeComponent();
		}

		private void FullTrustStatus_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			FullTrustStatusTeachingTip.IsOpen = true;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
