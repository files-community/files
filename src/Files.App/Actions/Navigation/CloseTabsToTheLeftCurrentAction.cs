// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class CloseTabsToTheLeftCurrentAction : CloseTabBaseAction
	{
		public override string Label
			=> "CloseTabsToTheLeft".GetLocalizedResource();

		public override string Description
			=> "CloseTabsToTheLeftCurrentDescription".GetLocalizedResource();

		public CloseTabsToTheLeftCurrentAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			MultitaskingTabsHelpers.CloseTabsToTheLeft(context.CurrentTabItem, context.Control!);

			return Task.CompletedTask;
		}

		protected override bool GetIsExecutable()
		{
			return context.Control is not null && context.CurrentTabIndex > 0;
		}

		protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.SelectedTabIndex):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
