﻿using Microsoft.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class CloseTabsToTheLeftCurrentAction : XamlUICommand
	{
		private readonly IMultitaskingContext context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

		public string Label { get; } = "CloseTabsToTheLeft".GetLocalizedResource();
		public string Description => "TODO: Need to be described.";


		public bool CanExecute => GetIsExecutable();

		public CloseTabsToTheLeftCurrentAction()
		{

			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (context.Control is not null)
			{
				MultitaskingTabsHelpers.CloseTabsToTheLeft(context.CurrentTabItem, context.Control);
			}
			return Task.CompletedTask;
		}

		private bool GetIsExecutable()
		{
			return context.Control is not null && context.CurrentTabIndex > 0;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.SelectedTabIndex):
					NotifyCanExecuteChanged();
					break;
			}
		}
	}
}
