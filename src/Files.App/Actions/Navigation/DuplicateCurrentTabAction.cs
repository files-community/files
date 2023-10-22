﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class DuplicateCurrentTabAction : IAction
	{
		private readonly IMultitaskingContext context;

		public string Label
			=> "DuplicateTab".GetLocalizedResource();

		public string Description
			=> "DuplicateCurrentTabDescription".GetLocalizedResource();

		public DuplicateCurrentTabAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();
		}

		public async Task ExecuteAsync()
		{
			var arguments = context.CurrentTabItem.NavigationParameter;
			if (arguments is null)
				await MultitaskingTabsHelpers.AddNewTabByPathAsync(typeof(PaneHolderPage), "Home");
			else
				await MultitaskingTabsHelpers.AddNewTabByParamAsync(arguments.InitialPageType, arguments.NavigationParameter, context.CurrentTabIndex + 1);
		}
	}
}
