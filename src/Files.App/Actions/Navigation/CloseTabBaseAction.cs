// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal abstract class CloseTabBaseAction : ObservableObject, IAction
	{
		protected readonly IMultitaskingContext context;

		public abstract string Label { get; }

		public abstract string Description { get; }
			
		public bool IsExecutable
			=> GetIsExecutable();

		public virtual HotKey HotKey
			=> HotKey.None;
		
		public virtual HotKey SecondHotKey
			=> HotKey.None;

		public virtual RichGlyph Glyph
			=> RichGlyph.None;

		public CloseTabBaseAction()
		{
			context = Ioc.Default.GetRequiredService<IMultitaskingContext>();

			context.PropertyChanged += Context_PropertyChanged;
		}

		public abstract Task ExecuteAsync(object? parameter = null);

		protected virtual bool GetIsExecutable()
		{
			return context.Control is not null && context.TabCount > 1;
		}

		protected virtual void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IMultitaskingContext.Control):
				case nameof(IMultitaskingContext.TabCount):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
