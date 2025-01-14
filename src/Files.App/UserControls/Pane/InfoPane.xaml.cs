// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Files.App.UserControls
{
	public enum PreviewPanePositions : ushort
	{
		None,
		Right,
		Bottom,
	}

	public sealed partial class InfoPane : UserControl
	{
		public PreviewPanePositions Position { get; private set; } = PreviewPanePositions.None;

		private readonly IInfoPaneSettingsService PaneSettingsService;

		private readonly ICommandManager Commands;

		private IContentPageContext contentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public InfoPaneViewModel ViewModel { get; private set; }

		private ObservableContext Context { get; } = new();

		public InfoPane()
		{
			InitializeComponent();
			PaneSettingsService = Ioc.Default.GetRequiredService<IInfoPaneSettingsService>();
			Commands = Ioc.Default.GetRequiredService<ICommandManager>();
			ViewModel = Ioc.Default.GetRequiredService<InfoPaneViewModel>();
		}

		public void UpdatePosition(double panelWidth, double panelHeight)
		{
			if (panelWidth > 700)
			{
				Position = PreviewPanePositions.Right;
				(MinWidth, MinHeight) = (150, 0);
				VisualStateManager.GoToState(this, "Vertical", true);
			}
			else
			{
				Position = PreviewPanePositions.Bottom;
				(MinWidth, MinHeight) = (0, 140);
				VisualStateManager.GoToState(this, "Horizontal", true);
			}
		}

		private string GetLocalizedResource(string resName) => resName.GetLocalizedResource();

		private void Root_Unloaded(object sender, RoutedEventArgs e)
		{
			PreviewControlPresenter.Content = null;
			Bindings.StopTracking();
		}

		private void Root_SizeChanged(object sender, SizeChangedEventArgs e)
			=> Context.IsHorizontal = Root.ActualWidth >= Root.ActualHeight;

		private void MenuFlyoutItem_Tapped(object sender, TappedRoutedEventArgs e)
			=> ViewModel?.UpdateSelectedItemPreviewAsync(true);

		private void FileTag_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, "PointerOver", true);
		}

		private void FileTag_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			VisualStateManager.GoToState((UserControl)sender, "Normal", true);
		}

		private sealed class ObservableContext : ObservableObject
		{
			private bool isHorizontal = false;
			public bool IsHorizontal
			{
				get => isHorizontal;
				set => SetProperty(ref isHorizontal, value);
			}
		}
		
		private void TagItem_Tapped(object sender, TappedRoutedEventArgs e)
		{
			var tagName = ((sender as StackPanel)?.Children[1] as TextBlock)?.Text;
			if (tagName is null)
				return;

			contentPageContext.ShellPage?.SubmitSearch($"tag:{tagName}");
		}
	}
}