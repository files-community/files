// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class CloseAllTabsAction : CloseTabBaseAction
	{
		public override string Label
			=> Strings.CloseAllTabs.GetLocalizedResource();

		public override string Description
			=> Strings.CloseAllTabsDescription.GetLocalizedResource();

		public override HotKey HotKey
			=> new(Keys.W, KeyModifiers.CtrlShift);

		public CloseAllTabsAction()
		{
		}

		protected override bool GetIsExecutable()
		{
			return
				context.Control is not null &&
				context.TabCount > 0 &&
				context.CurrentTabItem is not null;
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (context.Control is not null)
				MultitaskingTabsHelpers.CloseAllTabs(context.Control);

			return Task.CompletedTask;
		}
	}
}
