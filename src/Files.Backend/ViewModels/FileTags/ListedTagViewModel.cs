﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace Files.Backend.ViewModels.FileTags
{
	public class ListedTagViewModel : ObservableObject
	{
		private TagViewModel tag;
		public TagViewModel Tag 
		{
			get => tag;
			set => SetProperty(ref tag, value);
		}

		private bool isEditing;
		public bool IsEditing
		{
			get => isEditing;
			set => SetProperty(ref isEditing, value);
		}

		public ListedTagViewModel(TagViewModel tag)
		{
			Tag = tag;
		}
	}
}
