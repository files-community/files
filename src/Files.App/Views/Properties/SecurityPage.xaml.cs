// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Items;
using Files.App.Data.Parameters;
using Files.App.Utils;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace Files.App.Views.Properties
{
	public sealed partial class SecurityPage : BasePropertiesPage
	{
		private SecurityViewModel SecurityViewModel { get; set; }

		private object _parameter;

		public SecurityPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var parameter = (PropertiesPageNavigationParameter)e.Parameter;
			SecurityViewModel = new(parameter);

			_parameter = parameter;

			base.OnNavigatedTo(e);
		}

		private void OpenSecurityAdvancedPageButton_Click(object sender, RoutedEventArgs e)
		{
			Frame?.Navigate(typeof(SecurityAdvancedPage), _parameter);
		}

		public async override Task<bool> SaveChangesAsync()
			=> await Task.FromResult(true);

		public override void Dispose()
		{
		}
	}
}
