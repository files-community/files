// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Helpers
{
	public sealed class WqlEventQuery
	{
		public string QueryExpression { get; }

		public WqlEventQuery(string queryExpression)
		{
			QueryExpression = queryExpression;
		}
	}
}
