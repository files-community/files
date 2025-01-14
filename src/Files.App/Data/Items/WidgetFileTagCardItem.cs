// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.Core.Storage.Extensions;
using Files.Shared.Utils;
using System.Windows.Input;

namespace Files.App.Data.Items
{
	public sealed partial class WidgetFileTagCardItem : WidgetCardItem
	{
		// Dependency injections

		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		// Fields

		private readonly IStorable _associatedStorable;

		// Properties

		public bool IsFolder
			=> _associatedStorable is IFolder;

		private IImage? _Icon;
		public IImage? Icon
		{
			get => _Icon;
			set => SetProperty(ref _Icon, value);
		}

		private string? _Name;
		public string? Name
		{
			get => _Name;
			set => SetProperty(ref _Name, value);
		}

		private string _Path;
		public override string Path
		{
			get => _Path;
			set => SetProperty(ref _Path, value);
		}

		// Commands

		public ICommand ClickCommand { get; }

		public WidgetFileTagCardItem(IStorable associatedStorable, IImage? icon)
		{
			_associatedStorable = associatedStorable;
			_Icon = icon;
			_Name = associatedStorable.Name;
			_Path = associatedStorable.TryGetPath();
			Item = this;

			ClickCommand = new AsyncRelayCommand(ClickAsync);
		}

		private Task ClickAsync()
		{
			return NavigationHelpers.OpenPath(_associatedStorable.Id, ContentPageContext.ShellPage!);
		}
	}
}
