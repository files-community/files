﻿// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

namespace Files.App.Data.Items
{
	public class NavigationBarSuggestionItem : ObservableObject
	{
		private string? _Text;
		public string? Text
		{
			get => _Text;
			set => SetProperty(ref _Text, value);
		}

		private string? _PrimaryDisplay;
		public string? PrimaryDisplay
		{
			get => _PrimaryDisplay;
			set => SetProperty(ref _PrimaryDisplay, value);
		}

		private string? _SecondaryDisplay;
		public string? SecondaryDisplay
		{
			get => _SecondaryDisplay;
			set => SetProperty(ref _SecondaryDisplay, value);
		}

		private string? _SupplementaryDisplay;
		public string? SupplementaryDisplay
		{
			get => _SupplementaryDisplay;
			set => SetProperty(ref _SupplementaryDisplay, value);
		}

		private float _DisplayOpacity = 1.0f;
		public float DisplayOpacity
		{
			get => _DisplayOpacity;
			set => SetProperty(ref _DisplayOpacity, value);
		}
	}
}
