// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.ContentLayouts
{
	/// <summary>
	/// Represents the browser page of Details View
	/// </summary>
	public sealed partial class DetailsLayoutPage : BaseLayout
	{
		private readonly DetailsLayoutViewModel ViewModel;

		private readonly ICommandManager Commands;

		public DetailsLayoutPage() : base()
		{
			// Dependency injection
			ViewModel = Ioc.Default.GetRequiredService<DetailsLayoutViewModel>();
			Commands = Ioc.Default.GetRequiredService<ICommandManager>();
			BaseViewModel = ViewModel;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			ViewModel.OnNavigatedTo(e);
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			ViewModel.OnNavigatingFrom(e);
		}
	}
}
