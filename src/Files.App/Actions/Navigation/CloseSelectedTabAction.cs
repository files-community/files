// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class CloseSelectedTabAction : CloseTabBaseAction
	{
		public override string Label
			=> "CloseTab".GetLocalizedResource();

		public override string Description
			=> "CloseSelectedTabDescription".GetLocalizedResource();

		public override HotKey HotKey
			=> new(Keys.W, KeyModifiers.Ctrl);

		public override HotKey SecondHotKey
			=> new(Keys.F4, KeyModifiers.Ctrl);

		public override RichGlyph Glyph
			=> new();

		public CloseSelectedTabAction()
		{
		}

		public override Task ExecuteAsync(object? parameter = null)
		{
			context.Control!.CloseTab(context.CurrentTabItem);

			return Task.CompletedTask;
		}

		protected override bool GetIsExecutable()
		{
			return
				context.Control is not null &&
				context.TabCount > 0 &&
				context.CurrentTabItem is not null;
		}

		protected override void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.CurrentTabItem):
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.TabCount):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
