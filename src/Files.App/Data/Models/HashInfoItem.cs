// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Data.Models
{
	public sealed class HashInfoItem : ObservableObject
	{
		private string _Algorithm;
		public string Algorithm
		{
			get => _Algorithm;
			set => SetProperty(ref _Algorithm, value);
		}

		private string _HashValue;
		public string HashValue
		{
			get => _HashValue;
			set => SetProperty(ref _HashValue, value);
		}

		private bool _IsSelected;
		public bool IsSelected
		{
			get => _IsSelected;
			set => SetProperty(ref _IsSelected, value);
		}

		private bool _IsCalculating;
		public bool IsCalculating
		{
			get => _IsCalculating;
			set => SetProperty(ref _IsCalculating, value);
		}

		private bool _IsCalculated;
		public bool IsCalculated
		{
			get => _IsCalculated;
			set => SetProperty(ref _IsCalculated, value);
		}

		private bool _IsEnabled;
		public bool IsEnabled
		{
			get => _IsEnabled;
			set => SetProperty(ref _IsEnabled, value);
		}
	}
}
