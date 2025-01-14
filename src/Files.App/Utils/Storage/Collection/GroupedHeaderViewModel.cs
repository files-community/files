// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Media;
using System.Runtime.CompilerServices;

namespace Files.App.Utils.Storage
{
	public sealed class GroupedHeaderViewModel : ObservableObject
	{
		public string Key { get; set; }
		public bool Initialized { get; set; }
		public int SortIndexOverride { get; set; }

		private string text;

		public string Text
		{
			get => text ?? ""; // Text is bound to AutomationProperties.Name and can't be null
			set => SetPropertyWithUpdateDelay(ref text, value);
		}

		private string subtext;

		public string Subtext
		{
			get => subtext;
			set => SetPropertyWithUpdateDelay(ref subtext, value);
		}

		private string countText;

		public string CountText
		{
			get => countText;
			set => SetPropertyWithUpdateDelay(ref countText, value);
		}

		private bool showCountTextBelow;

		public bool ShowCountTextBelow
		{
			get => showCountTextBelow;
			set => SetProperty(ref showCountTextBelow, value);
		}

		private ImageSource imageSource;

		public ImageSource ImageSource
		{
			get => imageSource;
			set => SetPropertyWithUpdateDelay(ref imageSource, value);
		}

		private string icon;

		public string Icon
		{
			get => icon;
			set => SetPropertyWithUpdateDelay(ref icon, value);
		}

		private void SetPropertyWithUpdateDelay<T>(ref T field, T newVal, [CallerMemberName] string propName = null)
		{
			if (propName is null)
			{
				return;
			}
			var name = propName.StartsWith("get_", StringComparison.OrdinalIgnoreCase)
				? propName.Substring(4)
				: propName;

			if (!deferPropChangedNotifs)
			{
				SetProperty<T>(ref field, newVal, name);
			}
			else
			{
				field = newVal;
				if (!changedPropQueue.Contains(name))
				{
					changedPropQueue.Add(name);
				}
			}
		}

		public void PausePropertyChangedNotifications()
		{
			deferPropChangedNotifs = true;
		}

		public void ResumePropertyChangedNotifications(bool triggerUpdates = true)
		{
			if (deferPropChangedNotifs == false)
			{
				return;
			}
			deferPropChangedNotifs = false;
			if (triggerUpdates)
			{
				changedPropQueue.ForEach(prop => OnPropertyChanged(prop));
				changedPropQueue.Clear();
			}
		}

		private List<string> changedPropQueue = [];

		// This is true by default to make it easier to initialize groups from a different thread
		private bool deferPropChangedNotifs = true;
	}
}
