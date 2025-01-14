// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Windows.Input;

namespace Files.App.ViewModels.Settings
{
	public sealed class TagsViewModel : ObservableObject
	{
		private readonly IFileTagsSettingsService fileTagsSettingsService = Ioc.Default.GetRequiredService<IFileTagsSettingsService>();

		private bool isBulkOperation = true;

		private bool isDragStarting = true;

		private bool isCreatingNewTag;
		public bool IsCreatingNewTag
		{
			get => isCreatingNewTag;
			set => SetProperty(ref isCreatingNewTag, value);
		}

		public ObservableCollection<ListedTagViewModel> Tags { get; set; }

		public ICommand AddTagCommand { get; }
		public ICommand SaveNewTagCommand { get; }
		public ICommand CancelNewTagCommand { get; }

		public NewTagViewModel NewTag = new();

		public TagsViewModel()
		{
			// Tags Commands
			AddTagCommand = new RelayCommand(DoAddNewTag);
			SaveNewTagCommand = new RelayCommand(DoSaveNewTag);
			CancelNewTagCommand = new RelayCommand(DoCancelNewTag);

			Tags = [];
			Tags.CollectionChanged += Tags_CollectionChanged;
			fileTagsSettingsService.FileTagList?.ForEach(tag => Tags.Add(new ListedTagViewModel(tag)));

			isBulkOperation = false;
		}

		private void Tags_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (isBulkOperation)
				return;

			// Reaordering ListView has no events, but its collection is updated twice,
			// first to remove the selected item, and second to add the item at the selected position.
			if (isDragStarting)
			{
				isDragStarting = false;
				return;
			}
			isDragStarting = true;

			fileTagsSettingsService.FileTagList = Tags.Select(tagVM => tagVM.Tag).ToList();
		}

		private void DoAddNewTag()
		{
			NewTag.Reset();
			IsCreatingNewTag = true;
		}

		private void DoSaveNewTag()
		{
			IsCreatingNewTag = false;

			fileTagsSettingsService.CreateNewTag(NewTag.Name, NewTag.Color);

			isBulkOperation = true;
			Tags.Clear();
			fileTagsSettingsService.FileTagList?.ForEach(tag => Tags.Add(new ListedTagViewModel(tag)));
			isBulkOperation = false;
		}

		private void DoCancelNewTag()
		{
			IsCreatingNewTag = false;
		}

		public void EditExistingTag(ListedTagViewModel item, string newName, string color)
		{
			fileTagsSettingsService.EditTag(item.Tag.Uid, newName, color);

			isBulkOperation = true;
			Tags.Clear();
			fileTagsSettingsService.FileTagList?.ForEach(tag => Tags.Add(new ListedTagViewModel(tag)));
			isBulkOperation = false;
		}

		public void DeleteExistingTag(ListedTagViewModel item)
		{
			isBulkOperation = true;
			Tags.Remove(item);
			isBulkOperation = false;

			fileTagsSettingsService.DeleteTag(item.Tag.Uid);
		}
	}

	public sealed class NewTagViewModel : ObservableObject
	{
		private string name = string.Empty;
		public string Name
		{
			get => name;
			set
			{
				SetProperty(ref name, value);
				{
					OnPropertyChanged(nameof(CanCommit));
					OnPropertyChanged(nameof(IsNameValid));
				}
			}
		}

		private string color = "#FFFFFFFF";
		public string Color
		{
			get => color;
			set => SetProperty(ref color, value);
		}

		private bool isNameValid = true;
		public bool IsNameValid
		{
			get => isNameValid;
			set
			{
				if (SetProperty(ref isNameValid, value))
					OnPropertyChanged(nameof(CanCommit));
			}
		}

		public bool CanCommit => !string.IsNullOrEmpty(name) && IsNameValid;

		public void Reset()
		{
			Name = string.Empty;
			IsNameValid = true;
			Color = ColorHelpers.RandomColor();
		}
	}
}
