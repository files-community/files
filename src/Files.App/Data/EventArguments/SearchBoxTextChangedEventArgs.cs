// Copyright (c) Files Community
// Licensed under the MIT License.

using Files.App.Data.Models;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Files.App.Data.EventArguments
{
	public sealed class SearchBoxTextChangedEventArgs
	{
		public SearchBoxTextChangeReason Reason { get; }

		public SearchBoxTextChangedEventArgs(SearchBoxTextChangeReason reason)
			=> Reason = reason;

		public SearchBoxTextChangedEventArgs(AutoSuggestionBoxTextChangeReason reason)
		{
			Reason = reason switch
			{
				AutoSuggestionBoxTextChangeReason.UserInput => SearchBoxTextChangeReason.UserInput,
				AutoSuggestionBoxTextChangeReason.SuggestionChosen => SearchBoxTextChangeReason.SuggestionChosen,
				_ => SearchBoxTextChangeReason.ProgrammaticChange
			};
		}
	}
}
