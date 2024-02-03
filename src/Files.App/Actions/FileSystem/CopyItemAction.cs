﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Actions
{
	internal class CopyItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Copy".GetLocalizedResource();

		public string Description
			=> "CopyItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconCopy");

		public HotKey HotKey
			=> new(Keys.C, KeyModifiers.Ctrl);

		public bool IsExecutable
			=> ContentPageContext.HasSelection;

		public CopyItemAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (ContentPageContext.ShellPage is not null)
				return UIFilesystemHelpers.CopyItemAsync(ContentPageContext.ShellPage);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
