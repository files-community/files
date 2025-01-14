// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	/// <summary>
	/// Represents base class for the UI Actions.
	/// </summary>
	internal abstract class BaseUIAction : ObservableObject
	{
		public virtual bool IsExecutable
			=> UIHelpers.CanShowDialog;

		public BaseUIAction()
		{
			UIHelpers.PropertyChanged += UIHelpers_PropertyChanged;
		}

		private void UIHelpers_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(UIHelpers.CanShowDialog))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
