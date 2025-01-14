// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Properties
{
	public sealed partial class CompatibilityPage : BasePropertiesPage
	{
		private CompatibilityViewModel? CompatibilityViewModel { get; set; }

		public CompatibilityPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = (PropertiesPageNavigationParameter)e.Parameter;
			if (np.Parameter is ListedItem listedItem)
				CompatibilityViewModel = new CompatibilityViewModel(listedItem);

			base.OnNavigatedTo(e);
		}

		public override Task<bool> SaveChangesAsync()
		{
			if (CompatibilityViewModel is not null)
				return Task.FromResult(CompatibilityViewModel.SetCompatibilityOptions());

			return Task.FromResult(false);
		}

		public override void Dispose()
		{
		}
	}
}
