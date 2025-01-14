// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models
{
	public sealed class ListedTagViewModel : ObservableObject
	{
		private TagViewModel _Tag;
		public TagViewModel Tag
		{
			get => _Tag;
			set => SetProperty(ref _Tag, value);
		}

		private bool _IsEditing;
		public bool IsEditing
		{
			get => _IsEditing;
			set => SetProperty(ref _IsEditing, value);
		}

		private bool _IsNameValid = true;
		public bool IsNameValid
		{
			get => _IsNameValid;
			set => SetProperty(ref _IsNameValid, value);
		}

		private bool _CanCommit = false;
		public bool CanCommit
		{
			get => _CanCommit;
			set => SetProperty(ref _CanCommit, value);
		}

		private string _NewName;
		public string NewName
		{
			get => _NewName;
			set => SetProperty(ref _NewName, value);
		}

		private string _NewColor;
		public string NewColor
		{
			get => _NewColor;
			set => SetProperty(ref _NewColor, value);
		}

		public ListedTagViewModel(TagViewModel tag)
		{
			Tag = tag;
			NewColor = tag.Color;
		}
	}
}
