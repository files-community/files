// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class LayoutDetailsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Details;

		public override string Label
			=> "Details".GetLocalizedResource();

		public override string Description
			=> "LayoutDetailsDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconLayout.Details");

		public override HotKey HotKey
			=> new(Keys.Number1, KeyModifiers.CtrlShift);
	}

	internal sealed class LayoutListAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.List;

		public override string Label
			=> "List".GetLocalizedResource();

		public override string Description
			=> "LayoutListDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconLayout.List");

		public override HotKey HotKey
			=> new(Keys.Number2, KeyModifiers.CtrlShift);
	}

	internal sealed class LayoutTilesAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Tiles;

		public override string Label
			=> "Tiles".GetLocalizedResource();

		public override string Description
			=> "LayoutTilesDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconLayout.Tiles");

		public override HotKey HotKey
			=> new(Keys.Number3, KeyModifiers.CtrlShift);
	}

	internal sealed class LayoutGridAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Grid;

		public override string Label
			=> "Grid".GetLocalizedResource();

		public override string Description
			=> "LayoutGridDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconSize.Small");

		public override HotKey HotKey
			=> new(Keys.Number4, KeyModifiers.CtrlShift);
	}

	internal sealed class LayoutColumnsAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Columns;

		public override string Label
			=> "Columns".GetLocalizedResource();

		public override string Description
			=> "LayoutColumnsDescription".GetLocalizedResource();

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconLayout.Columns");

		public override HotKey HotKey
			=> new(Keys.Number5, KeyModifiers.CtrlShift);
	}

	internal sealed class LayoutAdaptiveAction : ToggleLayoutAction
	{
		protected override LayoutTypes LayoutType
			=> LayoutTypes.Adaptive;

		public override string Label
			=> "Adaptive".GetLocalizedResource();

		public override string Description
			=> "LayoutAdaptiveDescription".GetLocalizedResource();

		public override bool IsExecutable
			=> Context.IsLayoutAdaptiveEnabled;

		public override RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.IconLayout.Auto");

		public override HotKey HotKey
			=> new(Keys.Number6, KeyModifiers.CtrlShift);

		protected override void OnContextChanged(string propertyName)
		{
			if (propertyName is nameof(IDisplayPageContext.IsLayoutAdaptiveEnabled))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}

	internal abstract class ToggleLayoutAction : ObservableObject, IToggleAction
	{
		protected readonly IDisplayPageContext Context;

		protected abstract LayoutTypes LayoutType { get; }

		public abstract string Label { get; }

		public abstract string Description { get; }

		public abstract RichGlyph Glyph { get; }

		public abstract HotKey HotKey { get; }

		public bool IsOn
			=> Context.LayoutType == LayoutType;

		public virtual bool IsExecutable
			=> true;

		public ToggleLayoutAction()
		{
			Context = Ioc.Default.GetRequiredService<IDisplayPageContext>();

			Context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			Context.LayoutType = LayoutType;

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IDisplayPageContext.LayoutType))
				OnPropertyChanged(nameof(IsOn));

			if (e.PropertyName is not null)
				OnContextChanged(e.PropertyName);
		}

		protected virtual void OnContextChanged(string propertyName)
		{
		}
	}

	internal sealed class LayoutDecreaseSizeAction : ObservableObject, IAction
	{
		private static readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "DecreaseSize".GetLocalizedResource();

		public string Description
			=> "LayoutDecreaseSizeDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Subtract, KeyModifiers.Ctrl);

		public HotKey MediaHotKey
			=> new(Keys.OemMinus, KeyModifiers.Ctrl, false);

		public bool IsExecutable =>
			ContentPageContext.PageType is not ContentPageTypes.Home &&
			ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes layoutMode &&
			((layoutMode is FolderLayoutModes.DetailsView && UserSettingsService.LayoutSettingsService.DetailsViewSize > DetailsViewSizeKind.Compact) ||
			(layoutMode is FolderLayoutModes.ListView && UserSettingsService.LayoutSettingsService.ListViewSize > ListViewSizeKind.Compact) ||
			(layoutMode is FolderLayoutModes.GridView && UserSettingsService.LayoutSettingsService.GridViewSize > GridViewSizeKind.Small) ||
			(layoutMode is FolderLayoutModes.ColumnView && UserSettingsService.LayoutSettingsService.ColumnsViewSize > ColumnsViewSizeKind.Compact));

		public LayoutDecreaseSizeAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
			UserSettingsService.LayoutSettingsService.PropertyChanged += UserSettingsService_PropertyChanged;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private void UserSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ILayoutSettingsService.DetailsViewSize):
				case nameof(ILayoutSettingsService.ListViewSize):
				case nameof(ILayoutSettingsService.GridViewSize):
				case nameof(ILayoutSettingsService.ColumnsViewSize):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			switch (ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings.LayoutMode)
			{
				case FolderLayoutModes.DetailsView:
					if (UserSettingsService.LayoutSettingsService.DetailsViewSize > DetailsViewSizeKind.Compact)
						UserSettingsService.LayoutSettingsService.DetailsViewSize -= 1;
					break;
				case FolderLayoutModes.ListView:
					if (UserSettingsService.LayoutSettingsService.ListViewSize > ListViewSizeKind.Compact)
						UserSettingsService.LayoutSettingsService.ListViewSize -= 1;
					break;
				case FolderLayoutModes.TilesView:
					break;
				case FolderLayoutModes.GridView:
					if (UserSettingsService.LayoutSettingsService.GridViewSize > GridViewSizeKind.Small)
						UserSettingsService.LayoutSettingsService.GridViewSize -= 1;
					break;
				case FolderLayoutModes.ColumnView:
					if (UserSettingsService.LayoutSettingsService.ColumnsViewSize > ColumnsViewSizeKind.Compact)
						UserSettingsService.LayoutSettingsService.ColumnsViewSize -= 1;
					break;
				default:
					break;
			}

			return Task.CompletedTask;
		}
	}

	internal sealed class LayoutIncreaseSizeAction : ObservableObject, IAction
	{
		private static readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();
		private readonly IContentPageContext ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "IncreaseSize".GetLocalizedResource();

		public string Description
			=> "LayoutIncreaseSizeDescription".GetLocalizedResource();

		public HotKey HotKey
			=> new(Keys.Add, KeyModifiers.Ctrl);

		public HotKey MediaHotKey
			=> new(Keys.OemPlus, KeyModifiers.Ctrl, false);

		public bool IsExecutable =>
			ContentPageContext.PageType is not ContentPageTypes.Home &&
			ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings.LayoutMode is FolderLayoutModes layoutMode && 
			((layoutMode is FolderLayoutModes.DetailsView && UserSettingsService.LayoutSettingsService.DetailsViewSize < DetailsViewSizeKind.ExtraLarge) ||
			(layoutMode is FolderLayoutModes.ListView && UserSettingsService.LayoutSettingsService.ListViewSize < ListViewSizeKind.ExtraLarge) ||
			(layoutMode is FolderLayoutModes.GridView && UserSettingsService.LayoutSettingsService.GridViewSize < GridViewSizeKind.ExtraLarge) ||
			(layoutMode is FolderLayoutModes.ColumnView && UserSettingsService.LayoutSettingsService.ColumnsViewSize < ColumnsViewSizeKind.ExtraLarge));

		public LayoutIncreaseSizeAction()
		{
			ContentPageContext.PropertyChanged += ContentPageContext_PropertyChanged;
			UserSettingsService.LayoutSettingsService.PropertyChanged += UserSettingsService_PropertyChanged;
		}

		private void ContentPageContext_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		private void UserSettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ILayoutSettingsService.DetailsViewSize):
				case nameof(ILayoutSettingsService.ListViewSize):
				case nameof(ILayoutSettingsService.GridViewSize):
				case nameof(ILayoutSettingsService.ColumnsViewSize):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			switch (ContentPageContext.ShellPage?.InstanceViewModel.FolderSettings.LayoutMode)
			{
				case FolderLayoutModes.DetailsView:
					if (UserSettingsService.LayoutSettingsService.DetailsViewSize < DetailsViewSizeKind.ExtraLarge)
						UserSettingsService.LayoutSettingsService.DetailsViewSize += 1;
					break;
				case FolderLayoutModes.ListView:
					if (UserSettingsService.LayoutSettingsService.ListViewSize < ListViewSizeKind.ExtraLarge)
						UserSettingsService.LayoutSettingsService.ListViewSize += 1;
					break;
				case FolderLayoutModes.TilesView:
					break;
				case FolderLayoutModes.GridView:
					if (UserSettingsService.LayoutSettingsService.GridViewSize < GridViewSizeKind.ExtraLarge)
						UserSettingsService.LayoutSettingsService.GridViewSize += 1;
					break;
				case FolderLayoutModes.ColumnView:
					if (UserSettingsService.LayoutSettingsService.ColumnsViewSize < ColumnsViewSizeKind.ExtraLarge)
						UserSettingsService.LayoutSettingsService.ColumnsViewSize += 1;
					break;
				default:
					break;
			}

			return Task.CompletedTask;
		}
	}
}
